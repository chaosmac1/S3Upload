using Microsoft.Extensions.DependencyInjection;
using Up3.DefaultNuget;
using Up3.Kernel.Adapter.Interface;
using Up3.Service.Upload.Adapter;
using Up3.Service.Upload.Domain;
using Up3.Service.Upload.Domain.Class;
using Up3.Service.Upload.Domain.Interface;

namespace Up3.Service.Upload;

public class ServiceUploadBinder: IServiceBinder {
    public void Bind(IServiceCollection serviceCollection) {
        serviceCollection.AddScoped<IMinioClientFactory>(x => new MinioClientFactory(x.GetService<IEnv>()!));
        serviceCollection.AddScoped<IUploadFileContextFactory>(x => new UploadFileContextFactory(x.GetService<IEnv>()!,  x.GetService<IUploadService>()!, x.GetService<IMinioClientFactory>()!));
        serviceCollection.AddHostedService<IUploadService>(_ => new UploadService());
    }
}