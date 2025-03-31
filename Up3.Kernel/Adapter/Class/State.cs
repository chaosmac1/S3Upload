using Up3.Kernel.Adapter.Interface;

namespace Up3.Kernel.Adapter.Class;

public abstract class State<T> : IState<T> where T: struct {
    private T _value;
    
    public T Value => _value;

    public event EventHandler? ChangedEventHandler;
    
    public T SetState { set => _value = value; }
    
    public T SetStateAndNotify {
        set {
            if (Equals(this._value, value)) {
                return;
            }
            SetState = value;
            ChangedEventHandler?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        ChangedEventHandler = null;
    }
}