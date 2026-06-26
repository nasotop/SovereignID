using Verifier.Api;
using Verifier.Api.OpenApi;
using Verifier.Application;
using Verifier.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<VerifierFailureExceptionFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IVerifierRequestContext, HttpVerifierRequestContext>();
builder.Services.AddVerifierOpenApiDocumentation();
builder.Services.AddVerifierInfrastructure(builder.Configuration);

var app = builder.Build();

app.ValidateVerifierConfiguration();

if (app.Environment.IsDevelopment())
{
    app.MapVerifierOpenApiDocumentation();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
