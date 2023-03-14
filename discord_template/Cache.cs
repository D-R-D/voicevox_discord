
public class Cache<T>
{
    private T? _Value;
    private readonly Func<T> _Getter;

    public T Value {
        get {
            if (_Value == null) {
                _Value = _Getter.Invoke();
            }
            return _Value;
        }
    }

    public Cache(Func<T> getter)
    {
        _Getter = getter;
    }
}