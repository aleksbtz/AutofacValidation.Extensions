using Autofac;
using AutofacValidation.Extensions.Models;
using AutofacValidation.Extensions.ActivatorsExtensions;
using FluentAssertions;
using NUnit.Framework;
using TestTools;

#region ReSharperSettings
// ReSharper disable UnusedType.Local
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedVariable
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable EmptyConstructor
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable ConvertToLocalFunction
// ReSharper disable ConvertToConstant.Local
#pragma warning disable CS8321
#endregion

namespace AutofacValidation.Extensions.Tests;

[TestFixture]
public class GetRequiredTypesForActivatorTests
{

#region TestClasses
    private interface IDeps1 { }
    private class Deps1 : IDeps1 {}

    private interface IDeps2 { }
    private class Deps2 : IDeps2 {  }
    
    private interface IDeps3 { }
    
    private interface IDeps4 { }

    private class WithoutDeps
    {
        public WithoutDeps() { }
    }

    private class SingleDeps
    {
        public SingleDeps(IDeps1 deps1) { }
    }
    
    private class InternalConstructor
    {
        internal InternalConstructor(IDeps1 deps1) { }
    }
    
    private class DoubleDeps
    {
        public DoubleDeps(IDeps1 deps1, IDeps2 deps2) { }
    }
    
    private class FourDeps
    {
        public FourDeps(IDeps1 deps1, IDeps2 deps2, IDeps3 deps3, IDeps4 deps4) { }
    }
    
    private class DepsFromFourDeps
    {
        public DepsFromFourDeps(FourDeps fourDeps) { }
    }

    private class MultipleConstructors
    {
        public MultipleConstructors() { }
        public MultipleConstructors(IDeps1 deps1) { }
        public MultipleConstructors(IDeps1 deps1, IDeps2 deps2) { }
    }
    
    private class DefaultValueNullConstructor
    {
        public DefaultValueNullConstructor(IDeps1 deps1, IDeps2? deps2 = null) { }
    }
    
    private class DefaultValueConstructor
    {
        public DefaultValueConstructor(IDeps1 deps1, string? someString = "someString" ) { }
    }
    
    private class DefaultValueNullableConstructor
    {
        public DefaultValueNullableConstructor(IDeps1 deps1, IDeps2? deps2) { }
    }
    

    private class SameSignatureConstructors
    {
        public SameSignatureConstructors(IEnumerable<IDeps1> depsArr) { }
        public SameSignatureConstructors(IDeps1[] depsArr) { }
    }
    
    private class IEnumerableConstructor
    {
        public IEnumerableConstructor(IEnumerable<IDeps1> depsArr) { }
    }
    
#endregion

    private static IEnumerable<TestCaseData> ReflectionActivatorTestCases
    {
        get
        {
            //Ignore - Autofac 6.0.0 throw an error automatically  
            // yield return CreateTestCase(
            //     typeof(WithoutDeps),
            //     (RequiredServicesSearchStatus.Success, new HashSet<Type>()));
        
            yield return CreateTestCase(
                typeof(SingleDeps),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>());
            yield return CreateTestCase(
                typeof(SingleDeps),
                (RequiredServicesSearchStatus.NotEnoughRegistrationsToUseAnyConstructors, new HashSet<Type>(){ typeof(IDeps1) }));

            yield return CreateTestCase(
                typeof(MultipleConstructors),
                (RequiredServicesSearchStatus.Success, new HashSet<Type>()));
            yield return CreateTestCase(
                typeof(MultipleConstructors),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>());
            yield return CreateTestCase(
                typeof(MultipleConstructors),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1), typeof(IDeps2) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>().AddSingleInstance<IDeps2, Deps2>());

            yield return CreateTestCase(
                typeof(SameSignatureConstructors),
                (RequiredServicesSearchStatus.SelectConstructorError, new HashSet<Type>()),
                builder => builder.AddSingleInstance<IDeps1, Deps1>());
            
            //Ignore - Autofac 6.0.0 throw an error automatically      
            // yield return CreateTestCase(
            //     typeof(InternalConstructor),
            //     (RequiredServicesSearchStatus.NoAvailableConstructors, new HashSet<Type>()),
            //     builder => builder.AddSingleInstance<InternalConstructor>());

            yield return CreateTestCase(
                typeof(IEnumerableConstructor),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IEnumerable<IDeps1>) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>());
            yield return CreateTestCase(
                typeof(IEnumerableConstructor),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IEnumerable<IDeps1>) }));

            yield return CreateTestCase(
                typeof(DefaultValueNullableConstructor),
                (RequiredServicesSearchStatus.NotEnoughRegistrationsToUseAnyConstructors, new HashSet<Type>(){ typeof(IDeps1), typeof(IDeps2) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>());
            yield return CreateTestCase(
                typeof(DefaultValueNullableConstructor),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1), typeof(IDeps2) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>().AddSingleInstance<IDeps2, Deps2>());

            yield return CreateTestCase(
                typeof(DefaultValueNullConstructor),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>());

            yield return CreateTestCase(
                typeof(DefaultValueConstructor),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>());
            yield return CreateTestCase(
                typeof(DefaultValueConstructor),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1) }),
                builder => builder.AddSingleInstance<IDeps1, Deps1>().AddSingleInstance<string>("string1"));

            TestCaseData CreateTestCase(
                Type targetType,
                (RequiredServicesSearchStatus, HashSet<Type>) expectedRequiredTypesResult,
                Action<ContainerBuilder>? additionalRegistrations = null)
            {
                var containerBuilder = new ContainerBuilder();
                additionalRegistrations ??= (_) => { };

                containerBuilder.RegisterType(targetType).SingleInstance();
                additionalRegistrations(containerBuilder);
                var scope = containerBuilder.Build();
                var activator = scope.ComponentRegistry.Registrations
                    .First(reg => reg.Activator.LimitType == targetType)
                    .Activator;

                (RequiredServicesSearchStatus Status, HashSet<Type> Types) GetRequiredTypesForActivator() =>
                    activator.GetRequiredTypes(scope);

                var testCaseData = new TestCaseData(
                    GetRequiredTypesForActivator,
                    expectedRequiredTypesResult).SetName("ReflectionActivatorTest");
                return testCaseData;
            }
        }
    }
    
    private static IEnumerable<TestCaseData> DelegateActivatorTestCases
    {
        get
        {
            yield return CreateTestCase(
                typeof(SingleDeps),
                builder => builder.AddSingleInstance(_ => new SingleDeps(new Deps1())),
                (RequiredServicesSearchStatus.Success, new HashSet<Type>()));

            yield return CreateTestCase(
                typeof(SingleDeps),
                builder => builder.AddSingleInstance(ctx =>
                {
                    var deps1 = ctx.Resolve<IDeps1>();
                    var deps2 = ctx.Resolve<IDeps2>();
                    var deps3 = ctx.Resolve<IDeps3>();
                    return new SingleDeps(deps1);
                }),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1), typeof(IDeps2), typeof(IDeps3) }));

            yield return CreateTestCase(
                typeof(MultipleConstructors),
                builder => builder.AddSingleInstance(ctx =>
                {
                    var deps1 = ctx.Resolve<IDeps1>();
                    var deps2 = ctx.Resolve<Deps2>();
                    return new MultipleConstructors(deps1, deps2);
                }),
                (RequiredServicesSearchStatus.Success, new HashSet<Type> { typeof(IDeps1), typeof(Deps2) }));

            IDeps3 LocalCall(IComponentContext ctx) => ctx.Resolve<IDeps3>();
            var lambdaCall = (IComponentContext ctx) => ctx.Resolve<IDeps4>();
            yield return CreateTestCase(
                typeof(DepsFromFourDeps),
                builder => builder.AddSingleInstance(ctx =>
                {
                    var flag = false;
                    IDeps1 deps1 = null!;
                    if (flag) deps1 = ctx.Resolve<Deps1>();
                    IDeps2 deps2 = null!;
                    for (var i = 0; i < 10; i++) deps2 = ctx.Resolve<IDeps2>();
                    var deps3 = LocalCall(ctx);
                    var deps4 = lambdaCall(ctx);

                    return new DepsFromFourDeps(new FourDeps(deps1, deps2, deps3, deps4));
                }),
                (RequiredServicesSearchStatus.Success,
                    new HashSet<Type> { typeof(Deps1), typeof(IDeps2), typeof(IDeps3), typeof(IDeps4) }));
            
            yield return CreateTestCase(
                typeof(WithoutDeps),
                builder => builder.AddSingleInstance(ctx =>
                {
                    var deps1 = ctx.Resolve<Deps1>();
                    IDeps2 LocalFuncWithResolve(IComponentContext ctx1) => ctx1.Resolve<IDeps2>();
                    var deps3 = ctx.Resolve<IDeps3>();
                    var lambdaWithResolve = (IComponentContext ctx2) => ctx2.Resolve<IDeps4>();
                    return new WithoutDeps();
                }),
                (RequiredServicesSearchStatus.Success,
                    new HashSet<Type> { typeof(Deps1), typeof(IDeps3) }));

            yield return CreateTestCase(
                typeof(SingleDeps),
                builder => builder.AddSingleInstance(ctx =>
                {
                    //Runtime type resolve not support
                    var deps1 = (IDeps1)ctx.Resolve(typeof(IDeps1));
                    var deps2 = (IDeps2)ctx.Resolve(typeof(IDeps2));
                    var deps3 = (IDeps3)ctx.Resolve(typeof(IDeps3));
                    return new SingleDeps(deps1);
                }),
                (RequiredServicesSearchStatus.Success, new HashSet<Type>()));


            TestCaseData CreateTestCase(
                Type targetType,
                Action<ContainerBuilder> registerTestTypes,
                (RequiredServicesSearchStatus, HashSet<Type>) expectedRequiredTypesResult)
            {
                var containerBuilder = new ContainerBuilder();
                registerTestTypes(containerBuilder);
                var scope = containerBuilder.Build();
                var activator = scope.ComponentRegistry.Registrations
                    .First(reg => reg.Activator.LimitType == targetType)
                    .Activator;

                (RequiredServicesSearchStatus Status, HashSet<Type> Types) GetRequiredTypesForActivator() =>
                    activator.GetRequiredTypes(scope);

                var testCaseData = new TestCaseData(
                    GetRequiredTypesForActivator,
                    expectedRequiredTypesResult).SetName("DelegateActivatorTest");
                return testCaseData;
            }
        }
    }


    [Test]
    [TestCaseSource(nameof(ReflectionActivatorTestCases))]
    [TestCaseSource(nameof(DelegateActivatorTestCases))]
    public void Tests(
        Func<(RequiredServicesSearchStatus, HashSet<Type>)> getRequiredTypes, 
        (RequiredServicesSearchStatus, HashSet<Type>) expectedRequiredTypesResult)
    {
        getRequiredTypes().Should().BeEquivalentTo(expectedRequiredTypesResult);
    }
}