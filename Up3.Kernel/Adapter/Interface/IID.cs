namespace Up3.Kernel.Adapter.Interface;

public interface IID: IEquatable<IID> {
    public Guid ID { get; }
}