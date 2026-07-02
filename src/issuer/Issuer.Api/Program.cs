using Issuer.Api;
using Issuer.Api.OpenApi;
using Issuer.Application;
using Issuer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Issuer.Api.IssuerFailureExceptionFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IIssuerRequestContext, HttpIssuerRequestContext>();
builder.Services.AddIssuerJwtAuthentication(builder.Configuration);
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
