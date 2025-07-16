namespace AutofacValidation.Extensions.Models;

public enum RequiredServicesSearchStatus
{
    Success,
    NoAvailableConstructors,
    NotEnoughRegistrationsToUseAnyConstructors,
    SelectConstructorError
}