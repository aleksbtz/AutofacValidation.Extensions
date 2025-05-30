using System.Text;
using AutofacValidationExtensions.Models.Errors;

namespace AutofacValidationExtensions.Models;

public class ContainerValidationResult
{
    public readonly List<ValidationErrorBase> ValidationErrors;
    public bool IsSuccessful => !ValidationErrors.Any();

    public void EnsureSuccess()
    {
        if (!IsSuccessful) throw new DiValidationException(ToString());
    }

    public ContainerValidationResult(List<ValidationErrorBase> errors)
    {
        ValidationErrors = errors;
    }

    public override string ToString()
    {
        if (ValidationErrors.Count == 0) return "DI container validation completed successfully!";
        var messageText = new StringBuilder();
        ValidationErrors
            .GroupBy(err => err.ErrorType)
            .ToList()
            .ForEach(group =>
            {
                messageText.Append($"{group.Key}:\n");
                group.ToList().ForEach(err => messageText
                    .Append(($"\t{err.ToString()?.Replace("\t", "\t\t")}")));
                messageText.Append('\n');
            });
        return $"DI container validation errors:\n{messageText}";
    }

    public ContainerValidationResult FilterErrors(Func<ValidationErrorBase, bool> shouldSkip) =>
        new(ValidationErrors.Where(err => !shouldSkip(err)).ToList());
}