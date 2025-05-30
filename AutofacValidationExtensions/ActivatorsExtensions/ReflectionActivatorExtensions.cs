using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using AutofacValidationExtensions.Models;

namespace AutofacValidationExtensions.ActivatorsExtensions;

public static class ReflectionActivatorExtensions
{
    public static (RequiredServicesSearchStatus, HashSet<Type>) GetRequiredTypes(this ReflectionActivator activator,
        IComponentContext context)
    {
        var availableConstructors = Array.Empty<ConstructorInfo>();
        try
        {
            availableConstructors = activator.ConstructorFinder.FindConstructors(activator.LimitType);
        }
        catch (Exception)
        {
            // ignored
        }

        if (availableConstructors.Length == 0)
            return (RequiredServicesSearchStatus.NoAvailableConstructors, new HashSet<Type>());
        var defaultParameters = new Parameter[] { new AutowiringParameter(), new DefaultValueParameter() };
        var validBindings = availableConstructors
            .Select(constructorInfo => new ConstructorParameterBinding(constructorInfo, defaultParameters, context))
            .Where(constructorBinding => constructorBinding.CanInstantiate)
            .ToArray();

        if (validBindings.Length == 0)
        {
            var parametersTypes = GetParametersTypesFromConstructorInfo(availableConstructors.First());
            return (RequiredServicesSearchStatus.NotEnoughRegistrationsToUseAnyConstructors, parametersTypes);
        }

        ConstructorParameterBinding resolveConstructorBinding;
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