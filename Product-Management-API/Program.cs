using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Product_Management_API.Features.Products;
using Product_Management_API.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "Product Management API",
                Version = "v1",
                Description = "API for managing products.",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@product.com" 
                }
            });
    });

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<ProductManagementContext>(options =>
    options.UseSqlite("Data Source=productmanagement.db"));
builder.Services.AddScoped<CreateProductHandler>();

var app = builder.Build();

// Ensure the database is created at runtime
using (var scope = app.Services.CreateScope())
{   
    var context = scope.ServiceProvider.GetRequiredService<ProductManagementContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
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

app.MapPost("/products", async (CreateProductProfileRequest request, CreateProductHandler handler) =>)
{
    var product = await handler.Handle(request);
    return Results.Created($"/products/{product.Id}", product);
}).WithName("CreateProduct")
  .WithTags("Products");


app.Run();