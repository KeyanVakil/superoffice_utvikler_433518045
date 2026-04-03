using Microsoft.EntityFrameworkCore;
using MarketFlow.Api.Data;
using MarketFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<SegmentService>();
builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<JourneyService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddScoped<AiSuggestionService>();

// Controllers + JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MarketFlow API",
        Version = "v1",
        Description = "CRM Marketing Automation Platform API"
    });
});

// CORS — allow all origins for demo
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedData.SeedAsync(db);
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketFlow API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseCors();
app.MapControllers();

app.Run();

public partial class Program { }
