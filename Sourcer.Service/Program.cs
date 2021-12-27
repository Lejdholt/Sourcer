using Sourcer.Service;


var builder = WebApplication.CreateBuilder(args);
var services = builder
    .Services;

services.AddHostedService<Worker>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();