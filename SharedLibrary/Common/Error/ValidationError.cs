namespace Common.Error;

public sealed class ValidationError
{
    public enum ValidationErrorType
    {
        NotFound,
        InvalidValue
    }
    
    public string Context { get; }
    public string Field { get; }
    public ValidationErrorType Type { get; }
    public string Message { get; }

    public ValidationError(
        string context,
        string field,
        ValidationErrorType type,
        string message)
    {
        Context = context;
        Field = field;
        Type = type;
        Message = message;
    }

    public override string ToString()
    {
        return $"{Context} | {Field} | {Type} | {Message}";
    }
}