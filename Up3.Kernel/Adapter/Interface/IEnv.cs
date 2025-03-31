namespace Up3.Kernel.Adapter.Interface;

public interface IEnv {
    public string TmpPath { get; }
    public string DatabasePath { get; }
    public string DatabaseName { get; }
    public string BucketName { get; }
    public TimeSpan BlockLifeTime { get; }

    public string Endpoint { get; }
    public string AccessKey { get; }
    public string SecretKey { get; }
}