using Verifier.Api.OpenApi;
using Verifier.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
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
