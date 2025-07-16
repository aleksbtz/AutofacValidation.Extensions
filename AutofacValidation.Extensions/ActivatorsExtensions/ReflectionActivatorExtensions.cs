using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using AutofacValidation.Extensions.Models;

namespace AutofacValidation.Extensions.ActivatorsExtensions;

public static class ReflectionActivatorExtensions
{
    public static (RequiredServicesSearchStatus, HashSet<Type>) GetRequiredTypes(this ReflectionActivator activator,
        IComponentContext context)
    {
        var availableConstructors = new List<ConstructorInfo>();
        try
        {
            availableConstructors = activator.ConstructorFinder.FindConstructors(activator.LimitType).ToList();
        }
        catch (Exception)
        {
            var a = 0;
            // ignored
        }

        if (availableConstructors.Count == 0)
            return (RequiredServicesSearchStatus.NoAvailableConstructors, new HashSet<Type>());
        var defaultParameters = new Parameter[] { new AutowiringParameter(), new DefaultValueParameter() };
        var binders = availableConstructors.Select(ctorInfo => new ConstructorBinder(ctorInfo));
        var validBindings = binders
            .Select(binder => binder.Bind(defaultParameters, context))
            .Where(constructorBinding => constructorBinding.CanInstantiate)
            .ToArray();

        if (validBindings.Length == 0)
        {
            var parametersTypes = GetParametersTypesFromConstructorInfo(availableConstructors.First());
            return (RequiredServicesSearchStatus.NotEnoughRegistrationsToUseAnyConstructors, parametersTypes);
        }

        BoundConstructor resolveConstructorBinding;
        try
        {
            resolveConstructorBinding =
                activator.ConstructorSelector.SelectConstructorBinding(validBindings, defaultParameters);
        }
        catch (DependencyResolutionException)
        {
            return (RequiredServicesSearchStatus.SelectConstructorError, new HashSet<Type>());
        }

        var result = GetParametersTypesFromConstructorInfo(resolveConstructorBinding.TargetConstructor);
        return (RequiredServicesSearchStatus.Success, result);
    }

    private static HashSet<Type> GetParametersTypesFromConstructorInfo(ConstructorInfo constructorInfo) =>
        constructorInfo.GetParameters()
            .Where(parameterInfo => !parameterInfo.HasDefaultValue)
            .Select(parameterInfo => parameterInfo.ParameterType)
            .ToHashSet();
}