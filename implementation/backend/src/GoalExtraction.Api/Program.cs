using System.Text.Json;
using System.Text.Json.Serialization;
using GoalExtraction.Application;
using GoalExtraction.Infrastructure;
using GoalExtraction.Infrastructure.Persistence;
using GoalExtraction.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Health Checks
builder.Services.AddHealthChecks();

// CORS Configuration
const string frontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Goal Extraction API",
        Version = "v1",
        Description = "Clean Architecture API for Agentic Goal Extraction & Persistence Canvas"
    });
});

// Application Services (MediatR, FluentValidation)
builder.Services.AddApplicationServices();

// Infrastructure Services (DbContext, Repositories, DbConnectionFactory, Initializer)
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Initialize database and seed default data
await DatabaseInitializer.InitializeAsync(app.Services);

// Middlewares: Correlation Tracking & Global Exception Handling
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true) // Enable Swagger for verification
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Goal Extraction API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors(frontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

// Required for WebApplicationFactory<Program> in IntegrationTests
public partial class Program { }
