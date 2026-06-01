using Auth.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Auth.Api.AuthFailureExceptionFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddAuthInfrastructure(builder.Configuration);

var app = builder.Build();

app.ValidateAuthConfiguration();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
