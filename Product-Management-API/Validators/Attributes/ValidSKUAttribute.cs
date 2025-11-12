using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Product_Management_API.Validators.Attributes;

/// <summary>
/// Validates SKU format: 5-20 characters, uppercase letters/numbers/hyphens only
/// Implements client-side validation support
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
{
    private const string SkuPattern = @"^[A-Z0-9\-]{5,20}$";

    public ValidSKUAttribute()
    {
        ErrorMessage = "SKU must be 5-20 characters long and contain only uppercase letters, numbers, and hyphens.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult("SKU is required.");
        }

        // Remove spaces before validation (PDF requirement)
        var sku = value.ToString()!.Replace(" ", "");

        // Length validation
        if (sku.Length < 5 || sku.Length > 20)
        {
            return new ValidationResult("SKU must be between 5 and 20 characters.");
        }

        // Format validation
        if (!System.Text.RegularExpressions.Regex.IsMatch(sku, SkuPattern))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
    
    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-validsku"] = ErrorMessage ?? "Invalid SKU format.";
        context.Attributes["data-val-validsku-pattern"] = SkuPattern;
    }
}