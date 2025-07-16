using Autofac.Core;

namespace AutofacValidation.Extensions.Models.Errors;

public abstract class ValidationErrorBase : IEquatable<ValidationErrorBase>
{
    public readonly Type ResolvedType;
    public readonly Type ActualType;
    public readonly ValidationErrorTypes ErrorType;
    

    // used during debugging
    // ReSharper disable once NotAccessedField.Global
    public readonly IComponentRegistration? ActualRegistration;
    
    // used during debugging
    // ReSharper disable once NotAccessedField.Global
    public readonly int? RegistrationNumber;

    protected ValidationErrorBase(
        Type resolvedType, 
        Type actualType, 
        ValidationErrorTypes errorType, 
        IComponentRegistration? actualRegistration = null,
        int? registrationNumber = null)
    {
        ResolvedType = resolvedType;
        ActualType = actualType;
        ErrorType = errorType;
        ActualRegistration = actualRegistration;
        RegistrationNumber = registrationNumber;
    }

    public bool Equals(ValidationErrorBase? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ResolvedType == other.ResolvedType &&
               ActualType == other.ActualType &&
               ErrorType == other.ErrorType;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ValidationErrorBase)obj);
    }
}