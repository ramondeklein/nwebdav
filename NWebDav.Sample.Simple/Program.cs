using Microsoft.AspNetCore.Builder;
using NWebDav.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddNWebDav()
    .AddDiskStore();

var app = builder.Build();
app.UseNWebDav();
app.Run();