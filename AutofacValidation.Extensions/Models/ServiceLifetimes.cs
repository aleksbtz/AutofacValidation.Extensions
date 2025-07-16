namespace AutofacValidation.Extensions.Models;

public enum ServiceLifetimes
{
    Singleton = 0,
    PerScoped = 1,
    PerDependency = 2,
}