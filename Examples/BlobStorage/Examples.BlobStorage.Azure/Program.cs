using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Azure.Blobs;
using RCommon.Blobs;
using System.Text;

// "UseDevelopmentStorage=true" is the well-known connection string for the Azurite emulator
// (https://learn.microsoft.com/azure/storage/common/storage-use-azurite) -- no real Azure Storage
// account or credentials are required. Start Azurite locally (e.g. `docker run -p 10000:10000
// mcr.microsoft.com/azure-storage/azurite`) before running this example; without it, the calls
// below fail with a connection error, which the try/catch reports instead of crashing.
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithBlobStorage<AzureBlobStorageBuilder>(azure =>
            {
                azure.AddBlobStore("primary", opts =>
                {
                    opts.ConnectionString = "UseDevelopmentStorage=true";
                });
            });
    })
    .Build();

Console.WriteLine("Example Starting");

try
{
    var factory = host.Services.GetRequiredService<IBlobStoreFactory>();
    var storage = factory.Resolve("primary");

    const string containerName = "example-container";
    const string blobName = "hello.txt";

    if (!await storage.ContainerExistsAsync(containerName))
    {
        await storage.CreateContainerAsync(containerName);
    }

    await using (var content = new MemoryStream(Encoding.UTF8.GetBytes("Hello from RCommon.Azure.Blobs!")))
    {
        await storage.UploadAsync(containerName, blobName, content,
            new BlobUploadOptions { ContentType = "text/plain" });
    }
    Console.WriteLine($"Uploaded {blobName} to {containerName}");

    var exists = await storage.ExistsAsync(containerName, blobName);
    Console.WriteLine($"Blob exists: {exists}");

    await using (var downloaded = await storage.DownloadAsync(containerName, blobName))
    using (var reader = new StreamReader(downloaded))
    {
        Console.WriteLine($"Downloaded content: {await reader.ReadToEndAsync()}");
    }

    await storage.DeleteAsync(containerName, blobName);
    Console.WriteLine("Deleted blob");
}
catch (Exception ex)
{
    Console.WriteLine($"Blob storage operation failed -- is Azurite running? ({ex.Message})");
}

Console.WriteLine("Example Complete");
