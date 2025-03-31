using Minio;
using Up3.Kernel.Adapter.Class;
using Up3.Kernel.Adapter.Interface;
using Up3.Kernel.Adapter.Struct;
using Up3.Service.Upload.Adapter;
using Up3.Service.Upload.Domain.Interface;
using IMinioClientFactory = Up3.Service.Upload.Domain.Interface.IMinioClientFactory;

namespace Up3.Service.Upload.Domain.Class;


public class UploadFileContextFactory: IUploadFileContextFactory {
    private readonly IEnv env;
    private readonly IUploadService _uploadService;
    private readonly IMinioClientFactory _minioClientFactory;

    public UploadFileContextFactory(IEnv env, IUploadService uploadService, IMinioClientFactory minioClientFactory) {
        this.env = env;
        _uploadService = uploadService;
        _minioClientFactory = minioClientFactory;
    }

    public UploadFileState CreateWithPasswordAndPutInQueue(string fullPath, Password password) {
        var cancellationTokenSource = new CancellationTokenSource();
        var uploadFileState = UploadFileState.Create(
            env, 
            ProcessId.NewProcessId(),
            true, 
            password, 
            fullPath, 
            _minioClientFactory, 
            cancellationTokenSource.Token
        );
        
        this._uploadService.AddUploadFileState(uploadFileState, cancellationTokenSource);
        
        return uploadFileState;
    } 
    
    public UploadFileState CreateAndPutInQueue(string fullPath) {
        var cancellationTokenSource = new CancellationTokenSource();
        var uploadFileState =  UploadFileState.Create(
            env, 
            ProcessId.NewProcessId(),
            false, 
            default, 
            fullPath, 
            _minioClientFactory, 
            cancellationTokenSource.Token
        );

        this._uploadService.AddUploadFileState(uploadFileState, cancellationTokenSource);
        
        return uploadFileState;
    }
}