using Wism.SignalR.Host.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHub<GameHub>("/gameHub");

Console.WriteLine("SignalR host running at http://localhost:5000/gameHub");
app.Run("http://localhost:5000");
