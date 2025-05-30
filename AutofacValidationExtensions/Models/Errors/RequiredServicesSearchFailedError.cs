using Autofac.Core;

namespace AutofacValidationExtensions.Models.Errors;

public class RequiredServicesSearchFailedError : ValidationErrorBase, IEquatable<RequiredServicesSearchFailedError>
{
    public readonly RequiredServicesSearchStatus Status;
    public readonly HashSet<Type> SuggestedTypesToAdd;

    public RequiredServicesSearchFailedError(
        Type resolvedType,
        Type actualType,
        RequiredServicesSearchStatus status,
        IComponentRegistration? actualRegistration = null,
        int? registrationNumber = null,
        HashSet<Type>? suggestedTypesToAdd = null) :
        base(resolvedType, actualType, ValidationErrorTypes.RequiredServicesSearchFailed, actualRegistration, registrationNumber)
    {
        Status = status;
        SuggestedTypesToAdd = suggestedTypesToAdd ?? new HashSet<Type>();
    }

    public override string ToString() =>
        $"{ActualType.Name} registration has error: {Status}." +
        (SuggestedTypesToAdd.Count == 0
            ? ""
            : (" Try add: " + string.Join(", ", SuggestedTypesToAdd.Select(tp => tp.Name)))) +
        "\n";

    public bool Equals(RequiredServicesSearchFailedError? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) &&
               Status == other.Status &&
               SuggestedTypesToAdd.SequenceEqual(other.SuggestedTypesToAdd);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RequiredServicesSearchFailedError)obj);
    }
}