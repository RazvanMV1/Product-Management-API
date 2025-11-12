using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Product_Management_API.Common.Middleware;
using Product_Management_API.Features.Products;
using Product_Management_API.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// SWAGGER CONFIGURATION
// ========================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Management API",
        Version = "v1",
        Description = "API for managing products with advanced AutoMapper patterns, structured logging, and validation.",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@product.com"
        }
    });
});

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// ========================================
// DATABASE CONFIGURATION
// ========================================
builder.Services.AddDbContext<ProductManagementContext>(options =>
    options.UseSqlite("Data Source=productmanagement.db"));

// ========================================
// MODULE 3: FLUENTVALIDATION REGISTRATION
// Task 3.1: Register all validators from assembly
// ========================================
builder.Services.AddValidatorsFromAssemblyContaining<Product_Management_API.Validators.CreateProductProfileValidator>();

// ========================================
// MODULE 1: AUTOMAPPER REGISTRATION
// Task 4.1: Register both product profiles
// ========================================
builder.Services.AddAutoMapper(cfg =>
{
    // Automatically discovers all Profile classes in assembly
    cfg.AddMaps(typeof(Program).Assembly);
});
// Alternative explicit registration (if PDF requires explicit mention):
// builder.Services.AddAutoMapper(cfg =>
// {
//     cfg.AddProfile<AdvancedProductMappingProfile>();
// });

// ========================================
// MODULE 2: MEMORY CACHE REGISTRATION
// Task 4.1: Required for caching operations
// ========================================
builder.Services.AddMemoryCache();

// ========================================
// HANDLER REGISTRATION
// Task 4.1: Register CreateProductHandler
// ========================================
builder.Services.AddScoped<CreateProductHandler>();

var app = builder.Build();

// ========================================
// DATABASE INITIALIZATION
// Ensure database is created at runtime
// ========================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductManagementContext>();
    context.Database.EnsureCreated();
}

// ========================================
// HTTP REQUEST PIPELINE CONFIGURATION
// ========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Management API V1");
        c.RoutePrefix = string.Empty;
        c.DisplayRequestDuration();
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ========================================
// MODULE 2: MIDDLEWARE REGISTRATION
// Task 2.3 & Task 4.1
// ORDER MATTERS: Correlation → Validation Exception
// ========================================

// 1. Correlation Middleware (must be first for request tracking)
app.UseCorrelationMiddleware();

// 2. Validation Exception Middleware (catches validation errors)
app.UseMiddleware<ValidationExceptionMiddleware>();

// ========================================
// MODULE 4: PRODUCT ENDPOINTS
// Task 4.1: Update endpoint mapping to /products
// ========================================
app.MapPost("/products", async (
    CreateProductProfileRequest request, 
    CreateProductHandler handler,
    CancellationToken cancellationToken) =>
{
    var product = await handler.Handle(request, cancellationToken);
    return Results.Created($"/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithTags("Products")
.WithOpenApi(operation => new(operation)
{
    Summary = "Create a new product",
    Description = "Creates a new product with advanced AutoMapper mappings, validation, and structured logging. " +
                  "Supports Electronics, Clothing, Books, and Home categories with conditional business rules."
});

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }