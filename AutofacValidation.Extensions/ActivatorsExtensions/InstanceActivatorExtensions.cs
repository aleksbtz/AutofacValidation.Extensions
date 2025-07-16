using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Activators.Reflection;
using Autofac.Core.Activators.ProvidedInstance;
using AutofacValidation.Extensions.Models;

namespace AutofacValidation.Extensions.ActivatorsExtensions;

public static class InstanceActivatorExtensions
{
    public static (RequiredServicesSearchStatus Status, HashSet<Type> Types) GetRequiredTypes(
        this IInstanceActivator activator, IComponentContext context) =>
        activator switch
        {
            ReflectionActivator reflectionActivator => reflectionActivator.GetRequiredTypes(context),
            DelegateActivator delegateActivator => delegateActivator.GetRequiredTypes(),
            ProvidedInstanceActivator => (RequiredServicesSearchStatus.Success, new HashSet<Type>()),
            _ => (RequiredServicesSearchStatus.Success, new HashSet<Type>()),
        };
}