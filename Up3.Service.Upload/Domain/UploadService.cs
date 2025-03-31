using System.Collections.Concurrent;
using Up3.Kernel.Adapter.Class;
using Up3.Service.Upload.Adapter;
using Up3.Service.Upload.Domain.Class;

namespace Microsoft.Extensions.DependencyInjection.Domain;

public class UploadService: IUploadService {
    private SemaphoreSlim _semaphoreSlimSelf = new SemaphoreSlim(1, 1);
    private SemaphoreSlim _SemaphoreSlimMaxUpload = new SemaphoreSlim(1, 5);
    
    private readonly ConcurrentDictionary<ProcessId, (UploadFileState state, CancellationTokenSource CancellationTokenSource)> 
        _uploadFileStateDictionary = new();
    
    
    public async Task StartAsync(CancellationToken cancellationToken) {
        while (true) {
            await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) {
                await Task.FromCanceled(cancellationToken);
                return;
            }
            
            try {
                await _semaphoreSlimSelf.WaitAsync(cancellationToken);
                var utcNow = DateTime.UtcNow;
                foreach (var (state, _) in _uploadFileStateDictionary.Values
                             .Where(static x => x.state.Value.ProgressState == EProgressState.Finished)
                             .Where(x => x.state.Value.CleanUpDateTime > utcNow)) {
                    _ = Task.Factory.StartNew(async () => {
                        await state.StartDeletedAsync();
                    }, TaskCreationOptions.DenyChildAttach);
                    
                    _uploadFileStateDictionary.TryRemove(state.Value.Id, out _);
                }
            }
            finally {
                _semaphoreSlimSelf.Release();
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        try {
            await _semaphoreSlimSelf.WaitAsync(cancellationToken);
            await Task.WhenAll(_uploadFileStateDictionary.Values.ToArray().Select(x => {
                var state = x.state;
                x.CancellationTokenSource.Cancel();
                state.Dispose();
                Func<Task> func = async Task () => {
                    await Task.Yield();
                    
                    while (true) {
                        if (state.Value.ProgressState != EProgressState.Cancelled) {
                            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                            continue;
                        }
                        
                        return;
                    }
                };
                return Task.Factory.StartNew(func, TaskCreationOptions.DenyChildAttach);
            }));
        }
        finally {
            _uploadFileStateDictionary.Clear();
            _semaphoreSlimSelf.Release();
        }
    }

    public async Task AddUploadFileStateAsync(UploadFileState uploadFileState, CancellationTokenSource source) {
        try {
            _uploadFileStateDictionary[uploadFileState.Value.Id] = (uploadFileState, source);
            await _SemaphoreSlimMaxUpload.WaitAsync();
            await uploadFileState.StartAsync();
        }
        finally {
            _SemaphoreSlimMaxUpload.Release();
        }
    }

    public void AddUploadFileState(UploadFileState uploadFileState, CancellationTokenSource source) {
        _uploadFileStateDictionary[uploadFileState.Value.Id] = (uploadFileState, source);
        
        Task.Factory.StartNew(async () => { await AddUploadFileStateAsync(uploadFileState, source); },
            TaskCreationOptions.LongRunning
        );
    }

    public Task StopUploadAsync(ProcessId processId) {
        if (_uploadFileStateDictionary.TryRemove(processId, out var tuple)) {
            var (_, source) = tuple;
            source.Cancel();
        }

        return Task.CompletedTask;
    }
}