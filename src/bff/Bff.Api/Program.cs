using Bff.Api.OpenApi;
using SovereignID.Bff.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddBffOpenApiDocumentation();
builder.Services.AddBffDownstreamClients(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapBffOpenApiDocumentation();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
