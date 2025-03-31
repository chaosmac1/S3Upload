using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Domain;
using Up3.Kernel.Adapter.Interface;
using Up3.Service.Upload.Adapter;
using Up3.Service.Upload.Domain.Class;
using Up3.Service.Upload.Domain.Interface;

namespace Microsoft.Extensions.DependencyInjection;

public static class Register {
    public static IServiceCollection UseServiceUpload(this IServiceCollection serviceCollection) {
        serviceCollection.AddScoped<IMinioClientFactory>(x => new MinioClientFactory(x.GetService<IEnv>()!));
        serviceCollection.AddScoped<IUploadFileContextFactory>(x => new UploadFileContextFactory(x.GetService<IEnv>()!,  x.GetService<IUploadService>()!, x.GetService<IMinioClientFactory>()!));
        serviceCollection.AddHostedService<IUploadService>(_ => new UploadService());
        return serviceCollection;
    }
}