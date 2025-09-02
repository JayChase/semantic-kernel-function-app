using System.ComponentModel.DataAnnotations;

public static class ModelValidator
{
    public static (bool IsValid, List<string> Errors) Validate<T>(T? model)
    {
        var errors = new List<string>();

        if (model is null)
        {
            errors.Add("Model is null.");
            return (false, errors);
        }

        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        bool isValid = Validator.TryValidateObject(model, context, results, true);

        errors.AddRange(results.Select(r => r.ErrorMessage ?? "Unknown validation error"));
        return (isValid, errors);
    }
}
