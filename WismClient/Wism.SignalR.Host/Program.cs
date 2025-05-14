using Wism.SignalR.Host.Hubs;
using Wism.SignalR.Host.Services;
using Microsoft.AspNetCore.SignalR; 
using Microsoft.AspNetCore.SignalR.Protocol; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<NamedPipeListenerService>();

builder.Services
    .AddSignalR()
    .AddNewtonsoftJsonProtocol(options =>
    {
        options.PayloadSerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
    });

var app = builder.Build();

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHub<GameHub>("/gameHub");

Console.WriteLine("SignalR host running at http://localhost:5000/gameHub");
app.Run("http://localhost:5000");
