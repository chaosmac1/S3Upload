using Up3.Service.Upload.Domain;
using Up3.Kernel.Adapter.Struct;
using Up3.Service.Upload.Domain.Class;

namespace Up3.Service.Upload.Adapter;

public interface IUploadFileContextFactory {
    public UploadFileState CreateWithPasswordAndPutInQueue(string fullPath, Password password);

    public UploadFileState CreateAndPutInQueue(string fullPath);
}