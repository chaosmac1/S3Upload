using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Up3.Service.Upload.Adapter;
using Up3.Service.Upload.Domain.Class;

namespace Up3.Terminal;

public static class Program {
    private static string _startPath = Environment.CurrentDirectory;
    
    public static async Task Main(string[] args) {
        await using var globalProvider = Up3.Init.InitializeRepository.Initialize().BuildServiceProvider();
        await using (var serviceScope = globalProvider.CreateAsyncScope()) {
            var provider = serviceScope.ServiceProvider;
            
            var uploadService = provider.GetService<Up3.Service.Upload.Adapter.IUploadService>() ?? throw new NullReferenceException(nameof(Up3.Service.Upload.Adapter.IUploadService));
            var uploadFileContextFactory = provider.GetService<Up3.Service.Upload.Adapter.IUploadFileContextFactory>()
                                           ?? throw new NullReferenceException(nameof(Up3.Service.Upload.Adapter.IUploadFileContextFactory));

            UploadFileState state = uploadFileContextFactory.CreateAndPutInQueue(GetUploadPathOrExit(args));

            state.ChangedEventHandler += (sender, eventArgs) => {
                var v = state.Value;
                Console.WriteLine($"ProgressState: {v.ProgressState}, Progress: {v.Progress}%");
            };

            while (state.Value.ProgressState != EProgressState.Finished) {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            Console.WriteLine($"ETag: {state.Value.ETag}");
        }

        Console.WriteLine("Stop Services");
        await globalProvider.GetService<IUploadService>()!.StopAsync(CancellationToken.None);
    }

    
    public static string GetUploadPathOrExit(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("No Path is given");
            System.Environment.Exit(1);
            throw new UnreachableException();
        }


        if (Path.GetInvalidPathChars().Select(c => args[0].Contains(c)).Any()) {
            Console.WriteLine("Path Has Invalid Chars");
            System.Environment.Exit(1);
            throw new UnreachableException();
        }
        
        var path = args[0][0] is '/' or '\\'? args[0]: Path.Combine(_startPath, args[0]);
        
        if (!System.IO.Path.Exists(path)) {
            Console.WriteLine("Path not found");
            System.Environment.Exit(1);
            throw new UnreachableException();
        }

        return path;
    }
}