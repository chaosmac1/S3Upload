namespace Up3.WebServer.State;

public interface IState {
    public event Action OnChange;
}

public interface IState<T>: IState where T: struct {
    public T Value { get; set; }
} 

public abstract class State<T>: IState, IState<T> where T: struct {
    private T _value;
    public T Value {
        get => _value;
        set {
            _value = value;
            OnChange?.Invoke();
        }
    }

    public event Action? OnChange;

    public State() { }

    public void SetValueWithNoNotify(T value) => _value = value;
}