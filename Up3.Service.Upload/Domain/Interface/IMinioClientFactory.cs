using Minio;

namespace Up3.Service.Upload.Domain.Interface;

public interface IMinioClientFactory {
    IMinioClient CreateMinioClient();
}