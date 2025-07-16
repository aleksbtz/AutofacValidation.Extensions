namespace AutofacValidation.Extensions.Models.Errors;

public class DiValidationException : Exception
{
    public DiValidationException(string? message) : base(message)
    {
    }
}