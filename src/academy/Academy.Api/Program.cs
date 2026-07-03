using Academy.Api.OpenApi;
using Academy.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Academy.Api.AcademyFailureExceptionFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddAcademyOpenApiDocumentation();
builder.Services.AddAcademyInfrastructure(builder.Configuration);

var app = builder.Build();

app.ValidateAcademyConfiguration();

if (app.Environment.IsDevelopment())
{
    app.MapAcademyOpenApiDocumentation();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;

