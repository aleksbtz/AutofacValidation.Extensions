using System.Text;
using Autofac.Core;

namespace AutofacValidationExtensions.Models.Errors;

public class CaptiveDependencyError : ValidationErrorBase, IEquatable<CaptiveDependencyError>
{
    public readonly List<(Type type, ServiceLifetimes lifetime)> Dependencies;
    public readonly ServiceLifetimes ActualTypeLifetime;

    public CaptiveDependencyError(
        Type resolvedType,
        Type actualType,
        ServiceLifetimes actualTypeLifetime,
        List<(Type, ServiceLifetimes)> dependencies,
        IComponentRegistration? actualRegistration = null,
        int? registrationNumber = null) :
        base(resolvedType, actualType, ValidationErrorTypes.CaptiveDependency, actualRegistration, registrationNumber)
    {
        Dependencies = dependencies;
        ActualTypeLifetime = actualTypeLifetime;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"{ActualType.Name} with lifetime {ActualTypeLifetime} captures the following types:\n");
        Dependencies.ForEach(deps => stringBuilder.Append($"\t{deps.type.Name} with lifetime {deps.lifetime}\n"));
        return stringBuilder.ToString();
    }

    public bool Equals(CaptiveDependencyError? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return base.Equals(other) &&
               ActualTypeLifetime == other.ActualTypeLifetime &&
               Dependencies.SequenceEqual(other.Dependencies);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CaptiveDependencyError)obj);
    }
}