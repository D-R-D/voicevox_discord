public class Cache<T>
{
    private T? _Value;
    private readonly Func<T> _Getter;

    // Valueを取得しようとしたときに初めての取得であれば_Getterの保持する処理を実行する
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

public class Cache<T1, T2>
{
    private T1? _Value;
    private readonly Func<T2, T1> _Getter;

    public Cache(Func<T2, T1> getter)
    {
        _Getter = getter;
    }

    public T1 Get(T2 origin)
    {
        if (_Value == null)
        {
            _Value = _Getter.Invoke(origin);
        }
        return _Value;
    }
}