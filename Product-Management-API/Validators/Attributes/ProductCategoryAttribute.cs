using System.ComponentModel.DataAnnotations;
using Product_Management_API.Features.Products;

namespace Product_Management_API.Validators.Attributes;

/// <summary>
/// Validates that the provided category is a valid ProductCategory enum value.
/// Also ensures the category is not deprecated or restricted.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ProductCategoryAttribute : ValidationAttribute
{
    private static readonly ProductCategory[] ValidCategories = 
    {
        ProductCategory.Electronics,
        ProductCategory.Clothing,
        ProductCategory.Books,
        ProductCategory.Home
    };

    public ProductCategoryAttribute()
    {
        ErrorMessage = "Invalid product category. Must be Electronics (0), Clothing (1), Books (2), or Home (3).";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return new ValidationResult("Product category is required.");
        }

        // Check if it's a valid enum value
        if (!Enum.IsDefined(typeof(ProductCategory), value))
        {
            return new ValidationResult(ErrorMessage);
        }

        var category = (ProductCategory)value;

        // Check if it's in the allowed categories list
        if (!ValidCategories.Contains(category))
        {
            return new ValidationResult($"Category '{category}' is not allowed or has been deprecated.");
        }

        return ValidationResult.Success;
    }
}