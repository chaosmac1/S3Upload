using Up3.Kernel.Adapter.Interface;

namespace Up3.Kernel.Adapter.Struct;


public readonly struct PasswordHash {
    private readonly string _hash;
    private readonly IPasswordAlg  _algorithm;
    
    public PasswordHash(string hash, IPasswordAlg algorithm) {
        _hash = hash;
        _algorithm = algorithm;
    }
    
    public bool VerifyHash(string password) => _algorithm.VerifyHash(_hash, password);
}