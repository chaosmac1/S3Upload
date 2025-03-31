using Up3.Kernel.Adapter.Interface;

namespace Up3.Kernel.Adapter.Class;

public readonly struct ProcessId: IID {
    public bool Equals(IID? other) => other is not null && ID == other.ID;

    public static ProcessId NewProcessId() => new ProcessId { ID = Guid.CreateVersion7() };
    
    public Guid ID { get; private init; }

    public override string ToString() {
        return this.ID.ToString();
    }
}