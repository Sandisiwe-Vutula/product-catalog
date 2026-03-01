using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProductCatalog.API.Data;
using ProductCatalog.API.Extensions;
using ProductCatalog.API.Interfaces;
using ProductCatalog.API.Models;
using ProductCatalog.API.Repositories;
using ProductCatalog.API.Services;
using ProductCatalog.API.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Catalog API",
        Version = "v1",
        Description = "REST API for managing a product catalog with categories and inventory."
    });
});

// EF Core - in-memory database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ProductCatalogDb"));

// Repository registrations — demonstrates different storage strategies:
// ProductRepository uses EF Core (IQueryable + LINQ extensions)
// InMemoryCategoryRepository uses pure Dictionary<int,Category> — no EF
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<ICategoryRepository, InMemoryCategoryRepository>();

// Service registrations
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Search cache — singleton so it persists across requests
builder.Services.AddSingleton<ISearchCacheService>(sp =>
    new SearchCacheService(
        sp.GetRequiredService<ILogger<SearchCacheService>>(),
        ttl: TimeSpan.FromSeconds(30)));

// ProductSearchEngine — registered as singleton with fluent field configuration
builder.Services.AddSingleton<IProductSearchEngine<Product>>(sp =>
{
    var engine = new ProductSearchEngine<Product>();

    engine
        .AddField("Name",        p => p.Name ?? string.Empty,        2.0)
        .AddField("Description", p => p.Description ?? string.Empty, 1.0)
        .AddField("SKU",         p => p.SKU ?? string.Empty,         1.5);

    return engine;
});

// CORS — allow Angular dev server
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Custom middleware
app.UseRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog API v1");
    c.RoutePrefix = string.Empty;
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Seed EF in-memory database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
