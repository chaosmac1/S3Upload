using Microsoft.Extensions.DependencyInjection;
using Up3.Service.Upload;

namespace Up3.Init;

public static class InitializeRepository {
    public static IServiceCollection Initialize(IServiceCollection? serviceCollection = null) {
        serviceCollection = new ServiceCollection();
        
        new ServiceUploadBinder().Bind(serviceCollection);


        return serviceCollection;
    }
}