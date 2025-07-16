using System.Text;
using Autofac.Core;

namespace AutofacValidation.Extensions.Models.Errors;

public class MissingRegistrationError : ValidationErrorBase, IEquatable<MissingRegistrationError>
{
    public readonly HashSet<Type> Dependencies;

    public MissingRegistrationError(
        Type resolvedType,
        Type actualType,
        HashSet<Type> dependencies,
        IComponentRegistration? actualRegistration = null,
        int? registrationNumber = null) :
        base(resolvedType, actualType, ValidationErrorTypes.MissingRegistration, actualRegistration, registrationNumber)
    {
        Dependencies = dependencies;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"{ActualType.Name} requires the following types which are not registered:\n");
        Dependencies.ToList().ForEach(depsType => stringBuilder.Append($"\t{depsType.Name}\n"));
        return stringBuilder.ToString();
    }

    public bool Equals(MissingRegistrationError? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) && Dependencies.SequenceEqual(other.Dependencies);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MissingRegistrationError)obj);
    }
}