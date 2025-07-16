using System.Collections;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using AutofacValidation.Extensions.ActivatorsExtensions;
using AutofacValidation.Extensions.Models;
using AutofacValidation.Extensions.Models.Errors;

namespace AutofacValidation.Extensions;

public static class ContainerBuilderExtensions
{
    private static readonly List<Type> ignoredTypes =
        new() { typeof(IComponentContext), typeof(ILifetimeScope), typeof(LifetimeScope), typeof(IServiceProvider) };

    private static bool SkipRegistration(Type registrationActualType) =>
        ignoredTypes.Contains(registrationActualType) ||
        registrationActualType.FullName!.StartsWith("Microsoft");

    public static ContainerBuilder ValidateOnBuild(
        this ContainerBuilder containerBuilder)
    {
        containerBuilder.RegisterBuildCallback(ctx => 
            ValidateScope(ctx).EnsureSuccess());
        return containerBuilder;
    }

    public static ContainerBuilder ValidateOnBuild(
        this ContainerBuilder containerBuilder,
        Func<ValidationErrorBase, bool> shouldSkipErrorFilter)
    {
        containerBuilder.RegisterBuildCallback(ctx => 
            ValidateScope(ctx).FilterErrors(shouldSkipErrorFilter).EnsureSuccess());
        return containerBuilder;
    }

    public static ContainerBuilder ValidateOnBuild(
        this ContainerBuilder containerBuilder,
        Action<ContainerValidationResult> errorsHandler)
    {
        containerBuilder.RegisterBuildCallback(scope =>
        {
            var validationResult = ValidateScope(scope);
            errorsHandler(validationResult);
        });
        return containerBuilder;
    }

    private static ContainerValidationResult ValidateScope(IComponentContext scope)
    {
        var result = new List<ValidationErrorBase>();
        var checkedRegistrationsCounter = 0;
        foreach (var componentRegistration in scope.ComponentRegistry.Registrations)
        {
            var actualType = componentRegistration.Activator.LimitType;
            if (SkipRegistration(actualType))
            {
                checkedRegistrationsCounter++;
                continue;
            }

            var (requiredTypesResultStatus, requiredTypes) = 
                componentRegistration.Activator.GetRequiredTypes(scope);
                
            requiredTypes = requiredTypes.Where(tp => !SkipRegistration(tp)).ToHashSet();
            var resolvedType =
                componentRegistration.Services.Any() &&
                componentRegistration.Services.First() is IServiceWithType serviceWithType
                    ? serviceWithType.ServiceType
                    : actualType;
                    
            //RequiredServicesSearchFailedError
            if (requiredTypesResultStatus != RequiredServicesSearchStatus.Success)
            {
                var suggestedTypesToAdd = requiredTypes
                    .Where(requiredType => !scope.IsRegistered(requiredType)).ToHashSet();
                result.Add(new RequiredServicesSearchFailedError(
                    resolvedType,
                    actualType,
                    requiredTypesResultStatus,
                    componentRegistration,
                    checkedRegistrationsCounter,
                    suggestedTypesToAdd));
                checkedRegistrationsCounter++;
                continue;
            }

            //MissingRegistrationError
            var missingRegistration = new HashSet<Type>();
            foreach (var requiredType in requiredTypes.Where(requiredType => !scope.IsRegistered(requiredType)))
            {
                missingRegistration.Add(requiredType);
            }

            if (missingRegistration.Any())
            {
                result.Add(new MissingRegistrationError(resolvedType, actualType, missingRegistration,
                    componentRegistration, checkedRegistrationsCounter));
            }

            //CaptiveDependencyError
            var captiveDependencies = new List<(Type, ServiceLifetimes)>();
            var lifeTime = GetLifetimeFromAutofacReg(componentRegistration);
            var requiredTypesForCaptiveDependenciesCheck = requiredTypes
                .Select(tp => typeof(IEnumerable).IsAssignableFrom(tp) ? GetInnerTypeForIEnumerable(tp) : tp)
                .ToHashSet();
            foreach (var requiredType in requiredTypesForCaptiveDependenciesCheck)
            {
                var assiciatedRegistrations = scope.ComponentRegistry
                    .RegistrationsFor(new TypedService(requiredType));
                foreach (var registration in assiciatedRegistrations)
                {
                    var depsLifetime = GetLifetimeFromAutofacReg(registration);

                    if (lifeTime < depsLifetime)
                    {
                        captiveDependencies.Add((requiredType, depsLifetime));
                    }
                }
            }

            if (captiveDependencies.Any())
            {
                result.Add(new CaptiveDependencyError(resolvedType, actualType, lifeTime, captiveDependencies,
                    componentRegistration, checkedRegistrationsCounter));
            }

            checkedRegistrationsCounter++;
        }

        return new ContainerValidationResult(result);
    }

    private static ServiceLifetimes GetLifetimeFromAutofacReg(IComponentRegistration componentRegistry)
    {
        if (componentRegistry.Lifetime.GetType() == typeof(CurrentScopeLifetime))
        {
            return componentRegistry.Sharing == InstanceSharing.None
                ? ServiceLifetimes.PerDependency
                : ServiceLifetimes.PerScoped;
        }

        return ServiceLifetimes.Singleton;
    }

    private static Type GetInnerTypeForIEnumerable(Type enumerableType)
    {
        if (enumerableType.IsGenericType)
        {
            var genericArguments = enumerableType.GetGenericArguments();
            return genericArguments.Length <= 0 ? enumerableType : genericArguments.First();
        }

        if (enumerableType.IsArray)
        {
            var elementType = enumerableType.GetElementType();
            if (elementType != null) return elementType;
        }

        return enumerableType;
    }
}