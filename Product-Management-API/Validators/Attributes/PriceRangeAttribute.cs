using System.ComponentModel.DataAnnotations;

namespace Product_Management_API.Validators.Attributes;

/// <summary>
/// Validates price range with currency formatting
/// Constructor accepts double parameters (converted to decimal)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class PriceRangeAttribute : ValidationAttribute
{
    public decimal MinPrice { get; }
    public decimal MaxPrice { get; }

    /// <summary>
    /// PDF Requirement: Constructor accepts double, converts to decimal
    /// </summary>
    public PriceRangeAttribute(double min, double max)
    {
        MinPrice = (decimal)min;
        MaxPrice = (decimal)max;
        ErrorMessage = $"Price must be between {MinPrice:C} and {MaxPrice:C}.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult("Price is required.");
        }

        if (!decimal.TryParse(value.ToString(), out var price))
        {
            return new ValidationResult("Price must be a valid decimal number.");
        }

        // Range validation
        if (price < MinPrice || price > MaxPrice)
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }

    public override string FormatErrorMessage(string name)
    {
        // Currency formatting (PDF requirement)
        return $"{name} must be between {MinPrice:C} and {MaxPrice:C}.";
    }
}