using System.Diagnostics;

namespace Up3.Terminal;

public static class Program {
    private static string _startPath = Environment.CurrentDirectory;
    
    public static async Task Main(string[] args) {
        var path = GetUploadPathOrExist(args);
        
        
        
        
        
        
        
        
        
        System.
        Console.WriteLine("Hello, World!");       
    }

    public static string GetUploadPathOrExist(string[] args) {
        if (args.Length == 0) {
            Console.WriteLine("No Path is given");
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