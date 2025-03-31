using Up3.Kernel.Adapter.Class;
using Up3.Kernel.Adapter.Interface;
using Up3.Kernel.Adapter.Struct;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Up3.Service.Upload.Domain.Interface;

namespace Up3.Service.Upload.Domain.Class;

public enum EProgressState : uint {
    None,
    Initialization,
    Compress,
    Upload,
    Finished,
    Error,
    Cancelled,
    Deleted,
}

public record struct UploadFileStateValue(
    ProcessId Id,
    float Progress,
    long FileSize,
    long UploadedSize,
    string FileName,
    string FullPath,
    DateTime StarDateTime,
    DateTime? EndDateTime,
    DateTime? CleanUpDateTime,
    string? ETag,
    string TmpPath,
    bool UseCompression,
    Password Password,
    string BucketName,
    EProgressState ProgressState,
    string? ErrorMessage
);

public sealed class UploadFileState: State<UploadFileStateValue> {
    private readonly CancellationToken _cancellationToken;
    private readonly System.Threading.SemaphoreSlim _semaphoreSlim =  new System.Threading.SemaphoreSlim(1, 1);
    private readonly IMinioClientFactory _minioClientFactory;
    private readonly IEnv _env;
    private string? _pathToCommpresFile = null;
    
    
    private UploadFileState(UploadFileStateValue uploadFileStateValue, IMinioClientFactory minioClientFactory, IEnv env, CancellationToken cancellationToken) {
        _cancellationToken = cancellationToken;
        this.SetState = uploadFileStateValue;
        _minioClientFactory = minioClientFactory;
        this._env = env;
    }

    public static UploadFileState Create(
        IEnv env, 
        ProcessId id, 
        bool useCompression, 
        Password password, 
        string fullPath, 
        IMinioClientFactory minioClientFactory, 
        CancellationToken cancellationToken = default) {
        var fileName = Path.GetFileName(fullPath);
        var uploadFileState =  new UploadFileState(new  UploadFileStateValue() with {
            Id = id,
            Progress = -1,
            FileSize = -1,
            UploadedSize = -1,
            FileName = fileName,
            FullPath = fullPath,
            StarDateTime = DateTime.UtcNow,
            EndDateTime = null,
            CleanUpDateTime = null,
            ETag = null,
            TmpPath = env.TmpPath,
            UseCompression = useCompression,
            Password = password,
            BucketName = env.BucketName,
            ProgressState = EProgressState.None,
            ErrorMessage = null
        }, minioClientFactory, env, cancellationToken);

        cancellationToken.Register(() => {
            Task.Factory.StartNew(async () => {
                try {
                    await uploadFileState._semaphoreSlim.WaitAsync(CancellationToken.None);
                    uploadFileState.SetStateToCancelInLock();
                }
                finally {
                    uploadFileState._semaphoreSlim.Release();
                }
            }, TaskCreationOptions.PreferFairness);
        });
        
        return uploadFileState;
    }

    public async Task StartAsync() {
        
        if (!ProgressStateMustBe(Value.ProgressState, EProgressState.None)) return;
        try {
            await _semaphoreSlim.WaitAsync(_cancellationToken);
            var state = Value;
            
            if (CheckCancellationAndSetStateAndNotifyInLock()) return;
            if (!ProgressStateMustBe(Value.ProgressState, EProgressState.None)) return;
        }
        finally {
            _semaphoreSlim.Release();
        }

        await InitializationAsync();
    }

    public async Task StartDeletedAsync() {
        if (!ProgressStateMustBeNot(Value.ProgressState, EProgressState.Deleted, EProgressState.Error, EProgressState.None)) return;
        try {
            await _semaphoreSlim.WaitAsync(_cancellationToken);
            if (!ProgressStateMustBeNot(Value.ProgressState, EProgressState.Deleted, EProgressState.Error, EProgressState.None)) return;
            if (CheckCancellationAndSetStateAndNotifyInLock()) return;

            using var minioClient = _minioClientFactory.CreateMinioClient();
            
            await minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                .WithBucket(Value.ETag), CancellationToken.None);
                
            this.SetStateAndNotify = Value with { ProgressState = EProgressState.Deleted };
        }
        catch (Exception ex) {
            this.SetStateAndNotify = Value with { ProgressState = EProgressState.Error,  ErrorMessage = ex.Message };
        }
        finally {
            _semaphoreSlim.Release();
        }
    }
    
    private async Task InitializationAsync() {
        if (!ProgressStateMustBe(Value.ProgressState, EProgressState.None)) {
            return;
        }
        
        try {
            await _semaphoreSlim.WaitAsync(_cancellationToken);
            var state = Value;
            
            if (!ProgressStateMustBe(Value.ProgressState, EProgressState.None)) {
                return;
            }
            
            SetStateAndNotify = state with {
                ProgressState = EProgressState.Initialization
            };
            
            if (CheckCancellationAndSetStateAndNotifyInLock()) return;
        }
        finally {
            _semaphoreSlim.Release();
        }

        await (Value.UseCompression switch {
            true => this.CompressAsync(),
            _ => this.UploadAsync()
        });
    }

    private async Task CompressAsync() {
        _pathToCommpresFile = Value.TmpPath + "/" + Value.Id + ".zip";
        bool compressFinished = false;
        
        if (!ProgressStateMustBe(Value.ProgressState, EProgressState.Initialization)) return;
        
        try {
            await _semaphoreSlim.WaitAsync(_cancellationToken);
            if (!ProgressStateMustBe(Value.ProgressState, EProgressState.Initialization)) return;
            SetStateAndNotify = Value with {
                ProgressState = EProgressState.Compress
            };
            
            if (CheckCancellationAndSetStateAndNotifyInLock()) return;

            
            await using var fileReadStream = File.OpenRead(Value.FullPath);
            await using var fileWriteStream = File.OpenWrite(_pathToCommpresFile);
            
            var updaterInfoTask = Task.Factory.StartNew(async () => {
                SetStateAndNotify = Value with { FileSize = 0 };
                
                while (!compressFinished) {
                    if (_cancellationToken.IsCancellationRequested) return;
                    try {
                        if (fileWriteStream is null) continue;
                        
                        SetStateAndNotify = Value with { FileSize = fileWriteStream!.Length };
                        await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);
                    }
                    catch (Exception) {
                        // ignored
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            
            await using var zipStream = new ZipOutputStream(fileWriteStream);
            zipStream.SetLevel(8);
            zipStream.Password = Value.Password.Value;
        
            ZipEntry entry = new ZipEntry(Value.FileName) {
                DateTime = DateTime.UtcNow,
                Size = fileReadStream.Length
            };
            
            await zipStream.PutNextEntryAsync(entry, _cancellationToken);
            
            // Datei in ZIP schreiben
            await fileReadStream.CopyToAsync(zipStream, _cancellationToken);
            
            await zipStream.CloseEntryAsync(_cancellationToken);
            zipStream.IsStreamOwner = true;
            
            compressFinished = true;
            await updaterInfoTask;
        }
        finally {
            compressFinished = true;
            _semaphoreSlim.Release();
        }

        await UploadAsync();
    }

    private async Task UploadAsync() {
        bool uploadEnd = false;
        
        if (!ProgressStateMustBe(Value.ProgressState, EProgressState.Compress, EProgressState.Initialization)) return;
        try {
            await _semaphoreSlim.WaitAsync(_cancellationToken);
            if (!ProgressStateMustBe(Value.ProgressState, EProgressState.Compress, EProgressState.Initialization)) return;
            SetStateAndNotify = this.Value with {
                ProgressState = EProgressState.Upload
            };
            
            if (CheckCancellationAndSetStateAndNotifyInLock()) return;
        }
        finally {
            _semaphoreSlim.Release();
        }

        PutObjectResponse? putObjectResponse = null;
        try {
            await using var streamReader = File.OpenRead(this.Value.UseCompression
                ? this._pathToCommpresFile!
                : this.Value.FullPath
            );

            SetStateAndNotify = Value with { FileSize = streamReader.Length };

            var bucketName = Value.BucketName;
            var contentType = Value.UseCompression ? "application/zip" : "application/octet-stream";
            var objectName = Value.UseCompression ? Value.Id.ID + ".zip" : Value.FileName;

            var progress = new Progress<ProgressReport>(async void (report) => {
                try {
                    await _semaphoreSlim.WaitAsync(_cancellationToken);
                    if (CheckCancellationAndSetStateAndNotifyInLock()) return;

                    SetStateAndNotify = (this.Value.ProgressState == EProgressState.Finished || uploadEnd) switch {
                        true => this.Value with {
                            Progress = 100.0f, UploadedSize = this.Value.FileSize
                        },
                        _ => this.Value with {
                            Progress = report.Percentage / 100.0f, UploadedSize = report.TotalBytesTransferred
                        }
                    };
                }
                catch (Exception e) {
                    SetStateAndNotify = Value with { ErrorMessage = e.Message };
                    uploadEnd = true;
                    _cancellationToken.ThrowIfCancellationRequested();
                }
                finally {
                    _semaphoreSlim.Release();
                }
            });

            PutObjectArgs putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithObjectSize(streamReader.Length)
                .WithStreamData(streamReader)
                .WithContentType(contentType)
                .WithProgress(progress);
            
            using var minioClient = _minioClientFactory.CreateMinioClient();
            putObjectResponse = await minioClient.PutObjectAsync(putObjectArgs, _cancellationToken);
        }
        catch (Exception e) {
            uploadEnd = true;
            
            if (Value.ErrorMessage is null) {
                SetStateAndNotify = Value with { ErrorMessage = e.Message };
            }


            _ = Task.Factory.StartNew(ErrorAsync, TaskCreationOptions.DenyChildAttach);
        }
        finally {
            uploadEnd = true;
        }
        
        
        if (putObjectResponse is { ResponseStatusCode: HttpStatusCode.OK }) {
            try {
                await _semaphoreSlim.WaitAsync(CancellationToken.None);
                SetStateAndNotify = Value with { ETag = putObjectResponse.Etag};
            }
            finally {
                _semaphoreSlim.Release();
            }

            await FinishedAsync();
            return;
        }
    }

    private async Task FinishedAsync() {
        if (!ProgressStateMustBe(Value.ProgressState, EProgressState.Upload)) return;
        try {
            await _semaphoreSlim.WaitAsync(_cancellationToken);
            if (!ProgressStateMustBe(Value.ProgressState, EProgressState.Upload)) return;
            var utcNow = DateTime.UtcNow; 
            SetStateAndNotify = this.Value with {
                ProgressState = EProgressState.Finished,
                EndDateTime = utcNow,
                CleanUpDateTime = utcNow.AddTicks(this._env.BlockLifeTime.Ticks)
            };
        }
        finally {
            _semaphoreSlim.Release();
        }
    }

    private async Task ErrorAsync() {
        try {
            await _semaphoreSlim.WaitAsync(_cancellationToken);
            SetStateAndNotify = this.Value with { ProgressState = EProgressState.Error };
        }
        finally {
            _semaphoreSlim.Release();
        }
    }
    
    private bool CheckCancellationAndSetStateAndNotifyInLock() {
        if (!_cancellationToken.IsCancellationRequested) {
            return false;
        }
        
        SetStateToCancelInLock();
        return true;
    }
    
    private void SetStateToCancelInLock() {
        if (ProgressStateMustBeNot(Value.ProgressState, EProgressState.Cancelled)) return;
        
        this.SetStateAndNotify = Value with {
            ProgressState = EProgressState.Cancelled
        };
    }

    private static bool ProgressStateMustBe(EProgressState check, params Span<EProgressState> state) {
        foreach (var must in state) {
            if (check != must) {
                continue;
            }

            return true;
        }
        
        return false;
    }
    
    private static bool ProgressStateMustBeNot(EProgressState check, params Span<EProgressState> state) {
        foreach (var must in state) {
            if (check != must) {
                continue;
            }

            return true;
        }
        
        return false;
    }
}