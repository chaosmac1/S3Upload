using Minio;
using Up3.Kernel.Adapter.Interface;
using IMinioClientFactory = Up3.Service.Upload.Domain.Interface.IMinioClientFactory;

namespace Up3.Service.Upload.Domain.Class;

public sealed class MinioClientFactory : IMinioClientFactory {
    private readonly IEnv _env;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MinioClientFactory(IEnv env) => _env = env;

    public IMinioClient CreateMinioClient() {
        var endpoint = _env.Endpoint;
        var accessKey = _env.AccessKey;
        var secretKey = _env.SecretKey;

        return new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build()
            ;
    }
}