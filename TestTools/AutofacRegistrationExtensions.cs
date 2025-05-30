using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Features.Scanning;

namespace TestTools;

public static class AutofacRegistrationExtensions
{
    public static ContainerBuilder AddModule<TModule>(this ContainerBuilder services)
        where TModule : IModule, new()
    {
        services.RegisterModule<TModule>();
        return services;
    }

    public static ContainerBuilder AddSingleInstance<TService>(
        this ContainerBuilder services,
        Func<IComponentContext, TService> implementationFactory)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));
        services.Register(
                implementationFactory)
            .As<TService>()
            .SingleInstance();
        return services;
    }

    public static ContainerBuilder AddSingleInstance<TService>(
        this ContainerBuilder services,
        TService implementation)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (implementation == null) throw new ArgumentNullException(nameof(implementation));
        services.RegisterInstance(implementation).SingleInstance();
        return services;
    }

    public static ContainerBuilder AddSingleInstance<TService>(
        this ContainerBuilder services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        services.RegisterType<TService>().SingleInstance();
        return services;
    }

    public static ContainerBuilder AddSingleInstance<TService, TImplementation>(this ContainerBuilder services)
        where TService : class
        where TImplementation : class, TService
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        services.RegisterType<TImplementation>().As<TService>().SingleInstance();
        return services;
    }

    public static ContainerBuilder AddInstancePerLifetimeScope<TService>(
        this ContainerBuilder services,
        Func<IComponentContext, TService> implementationFactory)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));
        services.Register(
                implementationFactory)
            .As<TService>()
            .InstancePerLifetimeScope();
        return services;
    }

    public static ContainerBuilder AddInstancePerLifetimeScope<TService>(
        this ContainerBuilder services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        services.RegisterType<TService>().InstancePerLifetimeScope();
        return services;
    }
    
    public static ContainerBuilder AddInstancePerDepencency<TService>(this ContainerBuilder services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        services.RegisterType<TService>().As<TService>().InstancePerDependency();
        return services;
    }

    public static ContainerBuilder AddInstancePerLifetimeScope<TService, TImplementation>(this ContainerBuilder services)
        where TService : class
        where TImplementation : class, TService
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        services.RegisterType<TImplementation>().As<TService>().InstancePerLifetimeScope();
        return services;
    }
    
    public static ContainerBuilder AddInstancePerDepencency<TService, TImplementation>(this ContainerBuilder services)
        where TService : class
        where TImplementation : class, TService
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        services.RegisterType<TImplementation>().As<TService>().InstancePerDependency();
        return services;
    }

    public static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> Except(
        this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> builder,
        params Type[] typesToExclude)
    {
        var excludedTypes = new HashSet<Type>(typesToExclude);
        return builder.Where(t => !excludedTypes.Contains(t));
    }

    public static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> ExceptAssignableTo<TService>(
        this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> builder)
        where TService : class
    {
        return builder.Where(t => !typeof(TService).IsAssignableFrom(t));
    }

    public static ContainerBuilder RegisterAllImplementations<TService>(this ContainerBuilder builder, Assembly assembly)
        where TService : class
    {
        builder.RegisterAssemblyTypes(assembly)
            .Where(t => typeof(TService).IsAssignableFrom(t))
            .As<TService>()
            .AsSelf()
            .SingleInstance();
        return builder;
    }
}