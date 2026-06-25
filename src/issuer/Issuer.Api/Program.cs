using Issuer.Api.OpenApi;
using Issuer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddIssuerOpenApiDocumentation();
builder.Services.AddIssuerInfrastructure(builder.Configuration);

var app = builder.Build();

app.ValidateIssuerConfiguration();

if (app.Environment.IsDevelopment())
{
    app.MapIssuerOpenApiDocumentation();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
