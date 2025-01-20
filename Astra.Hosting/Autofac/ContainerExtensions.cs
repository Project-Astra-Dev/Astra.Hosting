using Autofac;
using Autofac.Builder;
using Autofac.Core;
using System.Runtime.InteropServices;

namespace Astra.Hosting.Autofac;

public static class ContainerExtensions
{
    public static ContainerBuilder AddSingletonFactory<TInterface, TImplementation>(
        this ContainerBuilder builder, 
        Func<IComponentContext, IEnumerable<Parameter>, TImplementation> configure
    )
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.Register<TImplementation>(configure)
            .As<TInterface>()
            .SingleInstance();
        return builder;
    }
    
    public static ContainerBuilder AddSingleton<TInterface, TImplementation>(
            this ContainerBuilder builder, 
            [Optional] Action<TImplementation>? configure
        )
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.RegisterType<TImplementation>()
            .As<TInterface>()
            .OnActivated(options => configure?.Invoke(options.Instance))
            .SingleInstance();
        return builder;
    }

    public static ContainerBuilder AddSingleton<TImplementation>(
            this ContainerBuilder builder,
            [Optional] Action<TImplementation>? configure
        )
        where TImplementation : class
    {
        builder.RegisterType<TImplementation>()
            .AsSelf()
            .OnActivated(options => configure?.Invoke(options.Instance))
            .SingleInstance();
        return builder;
    }
    
    public static ContainerBuilder AddScopedFactory<TInterface, TImplementation>(
            this ContainerBuilder builder, 
            Func<IComponentContext, IEnumerable<Parameter>, TImplementation> configure
        )
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.Register<TImplementation>(configure)
            .As<TInterface>()
            .InstancePerLifetimeScope();
        return builder;
    }
    
    public static ContainerBuilder AddScoped<TInterface, TImplementation>(
        this ContainerBuilder builder,
        [Optional] Action<TImplementation>? configure)
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.RegisterType<TImplementation>()
            .As<TInterface>()
            .OnActivated(options => configure?.Invoke(options.Instance))
            .InstancePerLifetimeScope();
        return builder;
    }

    public static ContainerBuilder AddScoped<TImplementation>(
        this ContainerBuilder builder,
        [Optional] Action<TImplementation>? configure)
        where TImplementation : class
    {
        builder.RegisterType<TImplementation>()
            .AsSelf()
            .OnActivated(options => configure?.Invoke(options.Instance))
            .InstancePerLifetimeScope();
        return builder;
    }
    
    public static ContainerBuilder AddTransientFactory<TInterface, TImplementation>(
            this ContainerBuilder builder, 
            Func<IComponentContext, IEnumerable<Parameter>, TImplementation> configure
        )
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.Register<TImplementation>(configure)
            .As<TInterface>()
            .InstancePerDependency();
        return builder;
    }
    
    public static ContainerBuilder AddTransient<TInterface, TImplementation>(
        this ContainerBuilder builder,
        [Optional] Action<TImplementation>? configure,
        [Optional] Action<TImplementation>? onRelease)
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.RegisterType<TImplementation>()
            .As<TInterface>()
            .OnActivated(options => configure?.Invoke(options.Instance))
            .OnRelease(options => onRelease?.Invoke(options))
            .InstancePerDependency();
        return builder;
    }

    public static ContainerBuilder AddTransient<TImplementation>(
        this ContainerBuilder builder,
        [Optional] Action<TImplementation>? configure,
        [Optional] Action<TImplementation>? onRelease)
        where TImplementation : class
    {
        builder.RegisterType<TImplementation>()
            .AsSelf()
            .OnActivated(options => configure?.Invoke(options.Instance))
            .OnRelease(options => onRelease?.Invoke(options))
            .InstancePerDependency();
        return builder;
    }
    
    public static ContainerBuilder AddKeyedSingleton<TInterface, TImplementation>(
        this ContainerBuilder builder,
        object serviceKey,
        [Optional] Action<TImplementation>? configure)
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.RegisterType<TImplementation>()
            .Keyed<TInterface>(serviceKey)
            .OnActivated(options => configure?.Invoke(options.Instance))
            .SingleInstance();
        return builder;
    }

    public static ContainerBuilder AddLazy<TInterface, TImplementation>(
        this ContainerBuilder builder)
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.RegisterType<TImplementation>()
            .As<TInterface>()
            .As<Lazy<TInterface>>()
            .InstancePerDependency();
        return builder;
    }

    public static ContainerBuilder AddGeneric(
        this ContainerBuilder builder,
        Type genericType,
        Action<IRegistrationBuilder<object, ReflectionActivatorData, DynamicRegistrationStyle>>? configureRegistration = null)
    {
        var registration = builder.RegisterGeneric(genericType);
        configureRegistration?.Invoke(registration);
        return builder;
    }

    public static ContainerBuilder AddWithParameters<TInterface, TImplementation>(
        this ContainerBuilder builder,
        params Parameter[] parameters)
        where TImplementation : class, TInterface
        where TInterface : notnull
    {
        builder.RegisterType<TImplementation>()
            .As<TInterface>()
            .WithParameters(parameters)
            .InstancePerDependency();
        return builder;
    }
}