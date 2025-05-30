namespace AutofacValidationExtensions.Models;

public enum RequiredServicesSearchStatus
{
    Success,
    NoAvailableConstructors,
    NotEnoughRegistrationsToUseAnyConstructors,
    SelectConstructorError
}