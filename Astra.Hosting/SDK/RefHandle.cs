namespace Astra.Hosting.SDK;

public sealed class RefHandle<T>
{
    private T _value;
    public RefHandle(T value) => _value = value;

    public T Value
    {
        get => _value;
        set => _value = value;
    }
}