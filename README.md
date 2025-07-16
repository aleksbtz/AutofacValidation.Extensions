This library allows to validate [Autofac](https://github.com/autofac/Autofac) container for registration errors.

## Main Features:
* **Easy to use** - you don't need to modify or prepare the container in any way before using this library
* **Support both Reflection and Delegate registrations**, in particular it can find non-obvious dependencies between entities by processing delegate instructions
* **On-build validation** - if the container has any registration issues, you will know about it during container build, i.e. before it is used by the application
* **Read-only** - the library validate the container without creating any entities, in particular it does not call the 'Resolve' method

## How to use:
Just add one line:
```
var container = new ContainerBuilder();
container.RegisterType<ClassA>().AsSelf().SingleInstance();
...
//add this line before calling the 'Build' method
container.ValidateOnBuild();
container.Build();
```
If the container has any registration issues, an error with a detailed description will be thrown.

You can also use method-overload and add a custom filter for the errors found. For example, you can skip all errors related to classes in a specific namespace.
```
container.ValidateOnBuild(err => err.ActualType.FullName.StartsWith("CustomNamespace"));
```

## Example:
Let's say you have the following code:
```
using Autofac;
using AutofacValidationExtensions;

namespace Example;

public static class Program
{
    public class A { public string someString; }
    public class B(A aDep) { }
    public class C(string strDep, B bDep) { }
    public class D(B bDep) { }
    
    public static void Main(string[] args)
    {
        var container = new ContainerBuilder();
        
        container.RegisterType<B>().AsSelf().InstancePerDependency();
        container.Register(ctx =>
        {
            var aHiddenDep = ctx.Resolve<A>();
            var bDep = ctx.Resolve<B>();
            return new C(aHiddenDep.someString, bDep);
        }).SingleInstance();
        container.RegisterType<D>().AsSelf().SingleInstance();
        container.ValidateOnBuild();
        
        container.Build();
    }  
}
```

On call the 'Build' method, the following error will be thrown:
```
Unhandled exception. AutofacValidation.Extensions.Models.Errors.DiValidationException: DI container validation errors:
RequiredServicesSearchFailed:
    B registration has error: NotEnoughRegistrationsToUseAnyConstructors. Try add: A

MissingRegistration:
    C requires the following types which are not registered: A

CaptiveDependency:
    C with lifetime Singleton captures the following types:
        B with lifetime PerDependency
    D with lifetime Singleton captures the following types:
        B with lifetime PerDependency

```
Other examples of validations with different registration variations can be found in the project with tests.

## Error types meaning:
* **RequiredServicesSearchFailed** - means that it was not possible to automatically select a constructor with an existing set of registrations in the container. Used for Reflection-registration.
* **MissingRegistration** - means that the delegate method is trying to resolve an entity from the container that is not registered. Used for Delegate-registration. 
* **CaptiveDependency** - means that an entity with a long lifetime depends on an entity with a short lifetime. For example Scoped depends from Singleton. Used for both Reflection and Delegate registrations. 

## Limitations:
* For delegate-registrations the library will work correctly only when the type of the required service is passed through a generic-type(`ctx.Resolve<T>()`). For example, in the following case the required type is passed through a `Resolve`-method parameter, and the dependency `TargetService`->`A` will not be detected:
```
builder
  .Register(ctx =>
  {
    var wrappper = new WrapperForA(ctx.Resolve(typeof(A)))
    return new TargetService(wrappper);  
  });
```
* If the container uses advanced Autofact features, as well as tools from additional Nuget-packages, then validation may not work correctly or skip some errors. Examples of test containers, on which validation works well, can be found in the project with tests.