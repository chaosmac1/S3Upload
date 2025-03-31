using Microsoft.Extensions.Hosting;
using Up3.Kernel.Adapter.Class;
using Up3.Service.Upload.Domain.Class;

namespace Up3.Service.Upload.Adapter;

public interface IUploadService: IHostedService {
    public Task AddUploadFileStateAsync(UploadFileState uploadFileState, CancellationTokenSource source);
    
    public void AddUploadFileState(UploadFileState uploadFileState, CancellationTokenSource source);
    
    public Task StopUploadAsync(ProcessId processId);
}