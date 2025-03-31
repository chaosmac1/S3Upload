namespace Up3.Kernel.Adapter.Struct;

public record struct Password(string? Value) {
    public bool HasPassword => Value is not null;
    public bool NoPassword => Value is null;
}