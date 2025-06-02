using Autofac;
using AutofacValidationExtensions.Models;
using AutofacValidationExtensions.Models.Errors;
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
#endregion

namespace AutofacValidationExtensions.Tests;

[TestFixture]
public class ContainerBuilderValidationTests
{
#region TestClasses

    private class WithoutDeps
    {
        public WithoutDeps() { }
    }
    
    private class SingleDeps : ISingleDeps
    {
        public SingleDeps(IDeps1 deps1) { }
    }
    public interface ISingleDeps { }
    
    private class DoubleConstructorsWithSingleDeps
    {
        public DoubleConstructorsWithSingleDeps(IDeps1 deps1) { }
        
        public DoubleConstructorsWithSingleDeps(IDeps2 deps1) { }
    }
    
    private class DoubleDeps
    {
        public DoubleDeps(IDeps1 deps1, IDeps2 deps2) { }
    }
    
    private class DoubleAndSingleDeps
    {
        public DoubleAndSingleDeps(IDeps1 deps1) { }
        public DoubleAndSingleDeps(IDeps1 deps1, IDeps2 deps2) { }
    }
    private class InternalConstructor
    {
        internal InternalConstructor(IDeps1 deps1) { }
    }
    
    
    private class ListDeps
    {
        public ListDeps(IEnumerable<IDeps1> deps1List) { }
    }
    
    private class DoubleListDeps
    {
        public DoubleListDeps(IEnumerable<IDeps1> deps1List) { }
        
        public DoubleListDeps(IDeps1[] deps1Arr) { }
    }
    
    private class SingleAndDoubleDeps
    {
        public SingleAndDoubleDeps(IDeps1 deps1) { }
        public SingleAndDoubleDeps(IDeps1 deps1, IDeps2 deps2) { }
    }

    private class Deps1 : IDeps1
    {
        public Deps1(){}
    }
    private interface IDeps1 { }
    
    private class Deps2 : IDeps2
    {
        public Deps2(){}
    }
    private interface IDeps2 { }
    
    private class Deps3 : IDeps3
    {
        public Deps3(){}
    }
    private interface IDeps3 { }

#endregion

private static IEnumerable<TestCaseData> RequiredServicesSearchErrorsTestCases
    {
        get
        {
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<WithoutDeps>(),
                new List<RequiredServicesSearchFailedError>());

            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<ISingleDeps, SingleDeps>()
                    .AddSingleInstance<DoubleDeps>(),
                new List<RequiredServicesSearchFailedError>()
                {
                    new RequiredServicesSearchFailedError(
                        resolvedType: typeof(ISingleDeps), 
                        actualType: typeof(SingleDeps),
                        RequiredServicesSearchStatus.NotEnoughRegistrationsToUseAnyConstructors,
                        suggestedTypesToAdd: new HashSet<Type>() { typeof(IDeps1) }),
                    new(
                        typeof(DoubleDeps), 
                        typeof(DoubleDeps),
                        RequiredServicesSearchStatus.NotEnoughRegistrationsToUseAnyConstructors,
                        suggestedTypesToAdd: new HashSet<Type>() { typeof(IDeps1), typeof(IDeps2) }),   
                });
                
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<ISingleDeps, SingleDeps>()
                    .AddSingleInstance<DoubleDeps>(),
                new List<RequiredServicesSearchFailedError>()
                {
                    new(
                        typeof(DoubleDeps), 
                        typeof(DoubleDeps),
                        RequiredServicesSearchStatus.NotEnoughRegistrationsToUseAnyConstructors,
                        suggestedTypesToAdd: new HashSet<Type>() { typeof(IDeps2) }),   
                });
                
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<IDeps2, Deps2>()
                    .AddSingleInstance<ISingleDeps, SingleDeps>()
                    .AddSingleInstance<DoubleDeps>(),
                new List<RequiredServicesSearchFailedError>());

            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<IDeps2, Deps2>()
                    .AddSingleInstance<DoubleConstructorsWithSingleDeps>(),
                new List<RequiredServicesSearchFailedError>()
                {
                    new(
                        typeof(DoubleConstructorsWithSingleDeps), 
                        typeof(DoubleConstructorsWithSingleDeps),
                        RequiredServicesSearchStatus.SelectConstructorError), 
                });
                
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<DoubleAndSingleDeps>(),
                new List<RequiredServicesSearchFailedError>());
                
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<IDeps2, Deps2>()
                    .AddSingleInstance<DoubleAndSingleDeps>(),
                new List<RequiredServicesSearchFailedError>());

            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<DoubleListDeps>(),
                new List<RequiredServicesSearchFailedError>()
                {
                    new(
                        typeof(DoubleListDeps), 
                        typeof(DoubleListDeps),
                        RequiredServicesSearchStatus.SelectConstructorError), 
                });
             
            //it will not be a problem to call scope.Resolve<InternalConstructor>();
            //but it throw an error if call scope.Resolve<InternalConstructor[]>();
            //Ignore - Autofac 6.0.0 throw an error automatically     
            // yield return CreateTestCase(
            //     builder => builder
            //         .AddSingleInstance<InternalConstructor>()
            //         .AddSingleInstance(_ => new InternalConstructor(new Deps1())),
            //     new List<RequiredServicesSearchFailedError>()
            //     {
            //         new(
            //             typeof(InternalConstructor), 
            //             typeof(InternalConstructor),
            //             RequiredServicesSearchStatus.NoAvailableConstructors), 
            //     });

            TestCaseData CreateTestCase(
                Action<ContainerBuilder> registerTestServices,
                IEnumerable<RequiredServicesSearchFailedError> expectedErrors)
            {
                var testCaseData = new TestCaseData(
                    registerTestServices,
                    expectedErrors.Select(err => (ValidationErrorBase)err).ToList(),
                    ValidationErrorTypes.RequiredServicesSearchFailed);
                testCaseData.SetName("RequiredServicesSearchErrors");
                return testCaseData;
            }
        }
    }

    private static IEnumerable<TestCaseData> MissingRegistrationTestCases
    {
        get
        {
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance(ctx =>
                    {
                        var deps1 = ctx.Resolve<Deps1>();
                        var iDeps2 = ctx.Resolve<IDeps2>();
                        var doubleDeps = new DoubleDeps(deps1, iDeps2);
                        return new WithoutDeps();
                    }),
                new List<MissingRegistrationError>()
                {
                    new(typeof(WithoutDeps),typeof(WithoutDeps), new HashSet<Type>{typeof(Deps1), typeof(IDeps2)})  
                });
    
            Deps2 Deps2Fabric(IComponentContext ctx) => ctx.Resolve<Deps2>();
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<ISingleDeps>(ctx =>
                    {
                        var deps1 = new Deps1();
                        var iDeps2 = Deps2Fabric(ctx);
                        var doubleDeps = new DoubleDeps(deps1, iDeps2);
                        return new SingleDeps(deps1);
                    }),
                new List<MissingRegistrationError>()
                {
                    new(typeof(ISingleDeps),typeof(ISingleDeps), new HashSet<Type>{typeof(Deps2)})  
                });

            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<IEnumerable<IDeps1>>(_ => new List<IDeps1>())
                    .AddSingleInstance<ListDeps>(),
                new List<MissingRegistrationError>());
                
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<SingleDeps>(),
                new List<MissingRegistrationError>());

            TestCaseData CreateTestCase(
                Action<ContainerBuilder> registerTestServices,
                IEnumerable<MissingRegistrationError> expectedErrors)
            {
                var testCaseData = new TestCaseData(
                    registerTestServices,
                    expectedErrors.Select(err => (ValidationErrorBase)err).ToList(),
                    ValidationErrorTypes.MissingRegistration);
                testCaseData.SetName("MissingRegistrationErrors");
                return testCaseData;
            }
        }
    }
    
    private static IEnumerable<TestCaseData> CaptiveDependencyTestCases
    {
        get
        {
            yield return CreateTestCase(
                builder => builder
                    .AddInstancePerLifetimeScope<IDeps1, Deps1>()
                    .AddSingleInstance<SingleDeps>(),
                new List<CaptiveDependencyError>()
                {
                    new(
                        typeof(SingleDeps),
                        typeof(SingleDeps),
                        ServiceLifetimes.Singleton,
                        new List<(Type, ServiceLifetimes)>
                            { (typeof(IDeps1), ServiceLifetimes.PerScoped) }),
                });
    
            yield return CreateTestCase(
                builder => builder
                    .AddInstancePerLifetimeScope<IDeps1, Deps1>()
                    .AddInstancePerLifetimeScope<IDeps2, Deps2>()
                    .AddSingleInstance<DoubleDeps>(),
                new List<CaptiveDependencyError>()
                {
                    new(
                        typeof(DoubleDeps),
                        typeof(DoubleDeps),
                        ServiceLifetimes.Singleton,
                        new List<(Type, ServiceLifetimes)>
                        {
                            (typeof(IDeps1), ServiceLifetimes.PerScoped),
                            (typeof(IDeps2), ServiceLifetimes.PerScoped),
                        })
                });
    
            yield return CreateTestCase(
                builder => builder
                    .AddInstancePerDepencency<IDeps1, Deps1>()
                    .AddInstancePerLifetimeScope<ISingleDeps, SingleDeps>()
                    .AddInstancePerLifetimeScope<IDeps2, Deps2>()
                    .AddSingleInstance<DoubleDeps>(),
                new List<CaptiveDependencyError>()
                {
                    new(
                        typeof(ISingleDeps),
                        typeof(SingleDeps),
                        ServiceLifetimes.PerScoped,
                        new List<(Type, ServiceLifetimes)>
                            { (typeof(IDeps1), ServiceLifetimes.PerDependency) }),
                    new(
                        typeof(DoubleDeps),
                        typeof(DoubleDeps),
                        ServiceLifetimes.Singleton,
                        new List<(Type, ServiceLifetimes)>
                        {
                            (typeof(IDeps1), ServiceLifetimes.PerDependency),
                            (typeof(IDeps2), ServiceLifetimes.PerScoped),
                        }),
                });
                
            //collection source automatically create resolve delegate for IEnumerable
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<ListDeps>(),
                new List<CaptiveDependencyError>());
            
            //should react on inner IEnumerable type
            yield return CreateTestCase(
                builder => builder
                    .AddInstancePerLifetimeScope<IDeps1, Deps1>()
                    .AddSingleInstance<ListDeps>(),
                new List<CaptiveDependencyError>()
                {
                    new(
                        typeof(ListDeps),
                        typeof(ListDeps),
                        ServiceLifetimes.Singleton,
                        new List<(Type, ServiceLifetimes)>
                            { (typeof(IDeps1), ServiceLifetimes.PerScoped) })
                });
               
            //should check each T registration for IEnumerable<T> dependency
            yield return CreateTestCase(
                builder => builder
                    .AddInstancePerLifetimeScope<IDeps1, Deps1>()
                    .AddInstancePerLifetimeScope<IDeps1, Deps1>()
                    .AddSingleInstance<IDeps1, Deps1>()
                    .AddSingleInstance<ListDeps>(),
                new List<CaptiveDependencyError>()
                {
                    new(
                        typeof(ListDeps),
                        typeof(ListDeps),
                        ServiceLifetimes.Singleton,
                        new List<(Type, ServiceLifetimes)>
                            { (typeof(IDeps1), ServiceLifetimes.PerScoped), (typeof(IDeps1), ServiceLifetimes.PerScoped) })
                });
            
            yield return CreateTestCase(
                builder => builder
                    .AddSingleInstance<IEnumerable<IDeps1>>(_ => new List<IDeps1>())
                    .AddSingleInstance<ListDeps>(),
                new List<CaptiveDependencyError>());
    
    
            TestCaseData CreateTestCase(
                Action<ContainerBuilder> registerTestServices,
                IEnumerable<CaptiveDependencyError> expectedErrors)
            {
                var testCaseData = new TestCaseData(
                    registerTestServices,
                    expectedErrors.Select(err => (ValidationErrorBase)err).ToList(),
                    ValidationErrorTypes.CaptiveDependency);
                testCaseData.SetName("CaptiveDependencyErrors");
                return testCaseData;
            }
        }
    }
    
    [Test]
    [TestCaseSource(nameof(RequiredServicesSearchErrorsTestCases))]
    [TestCaseSource(nameof(MissingRegistrationTestCases))]
    [TestCaseSource(nameof(CaptiveDependencyTestCases))]
    public void Tests(
        Action<ContainerBuilder> registerTestServices, 
        IEnumerable<ValidationErrorBase> expectedErrors,
        ValidationErrorTypes errorType)
    {
        var builder = new ContainerBuilder();
        registerTestServices(builder);
        builder.ValidateOnBuild(CheckErrors);
        builder.Build();

        void CheckErrors(ContainerValidationResult validationResult)
        {
            var errorsForCheck = 
                validationResult.ValidationErrors.Where(err => err.ErrorType == errorType);
            errorsForCheck.Should().BeEquivalentTo(expectedErrors);
            validationResult.ValidationErrors.Count(err => err.ErrorType != errorType).Should().Be(0);  
        }
    }
}