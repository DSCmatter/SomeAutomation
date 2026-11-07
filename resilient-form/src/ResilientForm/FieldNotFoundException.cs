namespace ResilientForm;

public sealed class FieldNotFoundException : Exception
{
    public FieldNotFoundException(string message)
        : base(message)
    {
    }
}
