using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Amazon.S3Objects;
using RCommon.Blobs;
using System.Text;

// Points at a local MinIO/LocalStack-style S3-compatible endpoint, so no real AWS account or
// credentials are required. Start MinIO locally (e.g. `docker run -p 9000:9000 minio/minio server
// /data`) before running this example; without it, the calls below fail with a connection error,
// which the try/catch reports instead of crashing. See persistence/... blob-storage/s3.mdx's
// "Using MinIO or LocalStack" section for the full explanation of ForcePathStyle.
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithBlobStorage<AmazonS3ObjectsBuilder>(s3 =>
            {
                s3.AddBlobStore("primary", opts =>
                {
                    opts.ServiceUrl = "http://localhost:9000";
                    opts.ForcePathStyle = true;
                    opts.AccessKeyId = "minioadmin";
                    opts.SecretAccessKey = "minioadmin";
                });
            });
    })
    .Build();

Console.WriteLine("Example Starting");

try
{
    var factory = host.Services.GetRequiredService<IBlobStoreFactory>();
    var storage = factory.Resolve("primary");

    const string containerName = "example-bucket";
    const string blobName = "hello.txt";

    if (!await storage.ContainerExistsAsync(containerName))
    {
        await storage.CreateContainerAsync(containerName);
    }

    await using (var content = new MemoryStream(Encoding.UTF8.GetBytes("Hello from RCommon.Amazon.S3Objects!")))
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

    var presignedUrl = await storage.GetPresignedDownloadUrlAsync(containerName, blobName, TimeSpan.FromMinutes(15));
    Console.WriteLine($"Presigned download URL: {presignedUrl}");

    await storage.DeleteAsync(containerName, blobName);
    Console.WriteLine("Deleted blob");
}
catch (Exception ex)
{
    Console.WriteLine($"Blob storage operation failed -- is a local S3-compatible server running? ({ex.Message})");
}

Console.WriteLine("Example Complete");
