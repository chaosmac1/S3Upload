namespace Up3.Kernel.Adapter.Interface;

public interface IPasswordAlg {
    public string CreateHash(string password);
    
    public bool VerifyHash(string hash, string password);
}