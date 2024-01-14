using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using NWebDav.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddNWebDav()
    .AddDiskStore(opts =>
    {
        var nwebDavDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "NWebDAV");
        Directory.CreateDirectory(nwebDavDir);
        opts.BaseDirectory = nwebDavDir;
    });
var app = builder.Build();
app.UseNWebDav();
app.Run();
