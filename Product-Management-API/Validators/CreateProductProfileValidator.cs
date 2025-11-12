using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Product_Management_API.Persistence;
using Product_Management_API.Features.Products;

namespace Product_Management_API.Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private readonly ProductManagementContext _context;
    private readonly ILogger<CreateProductProfileValidator> _logger;

    // Inappropriate words list for Name validation
    private static readonly string[] InappropriateWords = 
    {
        "damn", "hell", "crap", "stupid", "idiot", "fake", "scam", "trash"
    };

    // Restricted words for Home category
    private static readonly string[] HomeRestrictedWords = 
    {
        "weapon", "gun", "knife", "explosive", "dangerous", "toxic"
    };

    // Technology keywords for Electronics validation
    private static readonly string[] TechnologyKeywords = 
    {
        "smart", "digital", "tech", "electronic", "wireless", "bluetooth", 
        "wifi", "computer", "laptop", "phone", "tablet", "camera", "gaming"
    };

    public CreateProductProfileValidator(
        ProductManagementContext context,
        ILogger<CreateProductProfileValidator> logger)
    {
        _context = context;
        _logger = logger;

        // ========================================
        // TASK 3.1: BASIC VALIDATION RULES
        // ========================================

        // Rule 1: Name validation (4 rules)
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .Length(1, 200)
            .WithMessage("Product name must be between 1 and 200 characters.")
            .Must(BeValidName)
            .WithMessage("Product name contains inappropriate content.")
            .MustAsync(BeUniqueName)
            .WithMessage("A product with this name already exists for the same brand.");

        // Rule 2: Brand validation (3 rules)
        RuleFor(x => x.Brand)
            .NotEmpty()
            .WithMessage("Brand is required.")
            .Length(2, 100)
            .WithMessage("Brand must be between 2 and 100 characters.")
            .Must(BeValidBrandName)
            .WithMessage("Brand can only contain letters, spaces, hyphens, apostrophes, dots, and numbers.");

        // Rule 3: SKU validation (3 rules)
        RuleFor(x => x.SKU)
            .NotEmpty()
            .WithMessage("SKU is required.")
            .Must(BeValidSKU)
            .WithMessage("SKU must be 5-20 characters long and contain only uppercase letters, numbers, and hyphens.")
            .MustAsync(BeUniqueSKU)
            .WithMessage("SKU '{PropertyValue}' already exists in the system.");

        // Rule 4: Category validation (1 rule)
        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Invalid product category. Must be Electronics (0), Clothing (1), Books (2), or Home (3).");

        // Rule 5: Price validation (2 rules)
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than $0.")
            .LessThan(10000)
            .WithMessage("Price must be less than $10,000.");

        // Rule 6: ReleaseDate validation (2 rules)
        RuleFor(x => x.ReleaseDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Release date cannot be in the future.")
            .GreaterThan(new DateTime(1900, 1, 1))
            .WithMessage("Release date cannot be before year 1900.");

        // Rule 7: StockQuantity validation (2 rules)
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative.")
            .LessThanOrEqualTo(100000)
            .WithMessage("Stock quantity cannot exceed 100,000 units.");

        // Rule 8: ImageUrl validation (3 rules)
        RuleFor(x => x.ImageUrl)
            .Must(BeValidImageUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("ImageUrl must be a valid HTTP/HTTPS URL ending with .jpg, .jpeg, .png, .gif, or .webp.");

        // Rule 9: Business Rules validation
        RuleFor(x => x)
            .MustAsync(PassBusinessRules)
            .WithMessage("Product does not pass business rule validation.");

        // ========================================
        // TASK 3.3: CONDITIONAL VALIDATION
        // ========================================

        // Electronics-specific rules (3 rules)
        When(x => x.Category == ProductCategory.Electronics, () =>
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(50.00m)
                .WithMessage("Electronics products must have a minimum price of $50.00.");

            RuleFor(x => x.Name)
                .Must(ContainTechnologyKeywords)
                .WithMessage("Electronics products must contain technology-related keywords in the name.");

            RuleFor(x => x.ReleaseDate)
                .GreaterThan(DateTime.UtcNow.AddYears(-5))
                .WithMessage("Electronics products must be released within the last 5 years.");
        });

        // Home-specific rules (2 rules)
        When(x => x.Category == ProductCategory.Home, () =>
        {
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(200.00m)
                .WithMessage("Home products must have a maximum price of $200.00.");

            RuleFor(x => x.Name)
                .Must(BeAppropriateForHome)
                .WithMessage("Home product name contains restricted content.");
        });

        // Clothing-specific rules (1 rule)
        When(x => x.Category == ProductCategory.Clothing, () =>
        {
            RuleFor(x => x.Brand)
                .MinimumLength(3)
                .WithMessage("Clothing products must have a brand name with at least 3 characters.");
        });

        // Cross-field validation: Expensive products (1 rule)
        RuleFor(x => x.StockQuantity)
            .LessThanOrEqualTo(20)
            .When(x => x.Price > 100)
            .WithMessage("Products priced over $100 must have limited stock (maximum 20 units).");
    }

    // ========================================
    // VALIDATION METHODS (7 Required)
    // ========================================

    /// <summary>
    /// Task 3.1: Check Name against inappropriate words list
    /// </summary>
    private bool BeValidName(string name)
    {
        var lowerName = name.ToLower();
        var containsInappropriate = InappropriateWords.Any(word => lowerName.Contains(word));

        if (containsInappropriate)
        {
            _logger.LogWarning("Product name validation failed: Name contains inappropriate content.");
        }

        return !containsInappropriate;
    }

    /// <summary>
    /// Task 3.1: Async database check for Name+Brand combination uniqueness
    /// </summary>
    private async Task<bool> BeUniqueName(CreateProductProfileRequest request, string name, CancellationToken cancellationToken)
    {
        var exists = await _context.Products
            .AnyAsync(p => p.Name == name && p.Brand == request.Brand, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("Product name validation failed: Name '{Name}' already exists for brand '{Brand}'.", 
                name, request.Brand);
        }

        return !exists;
    }

    /// <summary>
    /// Task 3.1: Regex validation for Brand (letters, spaces, hyphens, apostrophes, dots, numbers)
    /// </summary>
    private bool BeValidBrandName(string brand)
    {
        var isValid = System.Text.RegularExpressions.Regex.IsMatch(brand, @"^[a-zA-Z0-9\s\-'.]+$");

        if (!isValid)
        {
            _logger.LogWarning("Brand validation failed: Brand '{Brand}' contains invalid characters.", brand);
        }

        return isValid;
    }

    /// <summary>
    /// Task 3.1: SKU format validation (alphanumeric with hyphens, 5-20 characters)
    /// </summary>
    private bool BeValidSKU(string sku)
    {
        var isValid = System.Text.RegularExpressions.Regex.IsMatch(sku, @"^[A-Z0-9\-]{5,20}$");

        if (!isValid)
        {
            _logger.LogWarning("SKU validation failed: SKU '{SKU}' has invalid format.", sku);
        }

        return isValid;
    }

    /// <summary>
    /// Task 3.1: Async database check for SKU uniqueness
    /// </summary>
    private async Task<bool> BeUniqueSKU(string sku, CancellationToken cancellationToken)
    {
        var exists = await _context.Products.AnyAsync(p => p.SKU == sku, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("SKU validation failed: SKU '{SKU}' already exists in the system.", sku);
        }

        return !exists;
    }

    /// <summary>
    /// Task 3.1: URL and image extension validation
    /// </summary>
    private bool BeValidImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true; // Optional field

        // Check valid HTTP/HTTPS URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning("ImageUrl validation failed: Invalid URL format.");
            return false;
        }

        // Check image extension
        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var hasValidExtension = validExtensions.Any(ext => url.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        if (!hasValidExtension)
        {
            _logger.LogWarning("ImageUrl validation failed: URL does not end with valid image extension.");
        }

        return hasValidExtension;
    }

    /// <summary>
    /// Task 3.1: Complex async business rule validation (4 rules)
    /// </summary>
    private async Task<bool> PassBusinessRules(CreateProductProfileRequest request, CancellationToken cancellationToken)
    {
        // Rule 1: Daily product addition limit (max 500 per day)
        var today = DateTime.UtcNow.Date;
        var todayProductCount = await _context.Products
            .CountAsync(p => p.CreatedAt.Date == today, cancellationToken);

        if (todayProductCount >= 500)
        {
            _logger.LogWarning("Business rule failed: Daily product limit reached ({Count}/500).", todayProductCount);
            return false;
        }

        // Rule 2: Electronics minimum price check ($50.00)
        if (request.Category == ProductCategory.Electronics && request.Price < 50.00m)
        {
            _logger.LogWarning("Business rule failed: Electronics product price ${Price} is below minimum $50.00.", request.Price);
            return false;
        }

        // Rule 3: Home product content restrictions
        if (request.Category == ProductCategory.Home)
        {
            var lowerName = request.Name.ToLower();
            var containsRestricted = HomeRestrictedWords.Any(word => lowerName.Contains(word));

            if (containsRestricted)
            {
                _logger.LogWarning("Business rule failed: Home product name contains restricted content.");
                return false;
            }
        }

        // Rule 4: High-value product stock limit (>$500 = max 10 stock)
        if (request.Price > 500 && request.StockQuantity > 10)
        {
            _logger.LogWarning("Business rule failed: High-value product (${Price}) cannot have stock > 10 (current: {Stock}).", 
                request.Price, request.StockQuantity);
            return false;
        }

        _logger.LogInformation("All business rules passed for product '{Name}' (SKU: {SKU}).", request.Name, request.SKU);
        return true;
    }

    // ========================================
    // TASK 3.3: HELPER METHODS (2 Required)
    // ========================================

    /// <summary>
    /// Task 3.3: Check if Name contains technology keywords
    /// </summary>
    private bool ContainTechnologyKeywords(string name)
    {
        var lowerName = name.ToLower();
        return TechnologyKeywords.Any(keyword => lowerName.Contains(keyword));
    }

    /// <summary>
    /// Task 3.3: Check if Name is appropriate for Home products
    /// </summary>
    private bool BeAppropriateForHome(string name)
    {
        var lowerName = name.ToLower();
        return !HomeRestrictedWords.Any(word => lowerName.Contains(word));
    }
}