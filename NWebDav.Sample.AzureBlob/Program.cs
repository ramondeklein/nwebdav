using System;
using System.Linq;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NWebDav.Sample.AzureBlob.AzureBlob;
using NWebDav.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddNWebDav()
    .AddAzureBlob()
    .AddSingleton<BlobServiceClient>(_ =>
    {
        var uri = builder.Configuration.GetValue<Uri>("BlobServiceUri");
        return new BlobServiceClient(uri, new DefaultAzureCredential());
    });

var app = builder.Build();

try
{
    Console.WriteLine("Checking for Azure access...");
    await new DefaultAzureCredential().GetTokenAsync(new TokenRequestContext(scopes: new[] { "https://management.azure.com/.default" })).ConfigureAwait(false);
}
catch
{
    Console.Error.WriteLine("Cannot obtain Azure access token (run 'az login' first).");
    throw;
}

try
{
    Console.WriteLine("Checking for Azure Storage Account BLOB access...");
    var blobServiceClient = app.Services.GetRequiredService<BlobServiceClient>();
    await blobServiceClient.GetBlobContainersAsync().CountAsync().ConfigureAwait(false);
}
catch
{
    Console.Error.WriteLine("Cannot access Azure BLOB container (check IAM rights on the container).");
    throw;
}

app.UseNWebDav();
app.Run();