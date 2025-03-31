using Tinyhand;

namespace Up3.Service.Upload.Adapter;

public enum EUploadStatus {
    Initial = 0,
    Compress = 1,
    Upload = 2,
    Finished = 3,
    Error = 4,
    Cancelled = 5,
    Deleted = 6,
}