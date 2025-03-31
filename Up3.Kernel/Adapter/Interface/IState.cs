namespace Up3.Kernel.Adapter.Interface;

public interface IState: IDisposable {
    event EventHandler? ChangedEventHandler;
}

public interface IState<T> : IState where T : struct {
    T Value { get; }
    T SetState { set; }
    T SetStateAndNotify { set; }
}