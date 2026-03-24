# Blob Storage Abstractions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add provider-agnostic blob storage abstractions to RCommon with Azure Blob Storage and AWS S3 implementations, including multi-store factory support.

**Architecture:** Single `IBlobStorageService` interface in a new `RCommon.Blobs` abstraction project, with `RCommon.Azure.Blobs` and `RCommon.Amazon.S3Objects` implementation projects. Multi-store support via `IBlobStoreFactory` using name-keyed factory delegates. All three projects integrate through the existing `IRCommonBuilder` extension pattern.

**Tech Stack:** .NET 8/9/10, Azure.Storage.Blobs, AWSSDK.S3, xUnit, FluentAssertions, Moq

**Spec:** `docs/superpowers/specs/2026-03-23-blob-storage-design.md`

---

## File Map

### New Projects

| Project | Location | Purpose |
|---------|----------|---------|
| `RCommon.Blobs` | `Src/RCommon.Blobs/` | Abstraction: interface, factory, builder, supporting types |
| `RCommon.Azure.Blobs` | `Src/RCommon.Azure.Blobs/` | Azure Blob Storage implementation |
| `RCommon.Amazon.S3Objects` | `Src/RCommon.Amazon.S3Objects/` | AWS S3 implementation |
| `RCommon.Blobs.Tests` | `Tests/RCommon.Blobs.Tests/` | Unit tests for abstraction layer |
| `RCommon.Azure.Blobs.Tests` | `Tests/RCommon.Azure.Blobs.Tests/` | Integration tests for Azure implementation |
| `RCommon.Amazon.S3Objects.Tests` | `Tests/RCommon.Amazon.S3Objects.Tests/` | Integration tests for S3 implementation |

### New Files — RCommon.Blobs

| File | Responsibility |
|------|---------------|
| `RCommon.Blobs.csproj` | Project file, depends on `RCommon.Core` |
| `IBlobStorageService.cs` | Core interface: container ops, blob CRUD, metadata, transfer, presigned URLs |
| `BlobItem.cs` | DTO for listing results |
| `BlobProperties.cs` | DTO for blob metadata/properties |
| `BlobUploadOptions.cs` | Upload configuration (content type, metadata, overwrite) |
| `IBlobStorageBuilder.cs` | Builder interface with `IServiceCollection Services` |
| `BlobStorageBuilderExtensions.cs` | `WithBlobStorage<T>` extension on `IRCommonBuilder` |
| `IBlobStoreFactory.cs` | Factory interface: `Resolve(string name)` |
| `BlobStoreFactory.cs` | Implementation with lazy-cached factory delegates |
| `BlobStoreFactoryOptions.cs` | `ConcurrentDictionary<string, Func<IServiceProvider, IBlobStorageService>>` |
| `BlobStoreNotFoundException.cs` | Exception for unresolved store names |

### New Files — RCommon.Azure.Blobs

| File | Responsibility |
|------|---------------|
| `RCommon.Azure.Blobs.csproj` | Project file, depends on `RCommon.Blobs` + `Azure.Storage.Blobs` |
| `IAzureBlobStorageBuilder.cs` | Builder interface with `AddBlobStore` method |
| `AzureBlobStorageBuilder.cs` | Concrete builder: registers factory delegates per named store |
| `AzureBlobStoreOptions.cs` | Options: ConnectionString or ServiceUri + TokenCredential |
| `AzureBlobStorageService.cs` | `IBlobStorageService` implementation wrapping `BlobServiceClient` |

### New Files — RCommon.Amazon.S3Objects

| File | Responsibility |
|------|---------------|
| `RCommon.Amazon.S3Objects.csproj` | Project file, depends on `RCommon.Blobs` + `AWSSDK.S3` |
| `IAmazonS3ObjectsBuilder.cs` | Builder interface with `AddBlobStore` method |
| `AmazonS3ObjectsBuilder.cs` | Concrete builder: registers factory delegates per named store |
| `AmazonS3StoreOptions.cs` | Options: Region, credentials, ServiceUrl, ForcePathStyle |
| `AmazonS3StorageService.cs` | `IBlobStorageService` implementation wrapping `IAmazonS3` |

### Modified Files

| File | Change |
|------|--------|
| `Src/RCommon.sln` | Add 6 new projects (3 src + 3 test) to "Blobs" and "Tests" solution folders |

---

## Task 1: Create RCommon.Blobs Project and Supporting Types

**Files:**
- Create: `Src/RCommon.Blobs/RCommon.Blobs.csproj`
- Create: `Src/RCommon.Blobs/BlobItem.cs`
- Create: `Src/RCommon.Blobs/BlobProperties.cs`
- Create: `Src/RCommon.Blobs/BlobUploadOptions.cs`
- Create: `Src/RCommon.Blobs/BlobStoreNotFoundException.cs`
- Create: `Tests/RCommon.Blobs.Tests/RCommon.Blobs.Tests.csproj`

- [ ] **Step 1: Create the RCommon.Blobs project directory and csproj**

```xml
<!-- Src/RCommon.Blobs/RCommon.Blobs.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
		<Nullable>enable</Nullable>
		<GeneratePackageOnBuild>True</GeneratePackageOnBuild>
		<Title>RCommon.Blobs</Title>
		<PackageProjectUrl>https://rcommon.com</PackageProjectUrl>
		<PackageTags>RCommon; Blob Storage Abstractions; Blob; Cloud Storage</PackageTags>
		<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
		<PackageRequireLicenseAcceptance>True</PackageRequireLicenseAcceptance>
		<Description>A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more</Description>
		<RepositoryUrl>https://github.com/RCommon-Team/RCommon</RepositoryUrl>
		<Company>RCommon</Company>
		<Authors>Jason Webb</Authors>
		<PackageIcon>RCommon-Icon.jpg</PackageIcon>
		<PackageReadmeFile>README.md</PackageReadmeFile>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="..\RCommon.Core\RCommon.Core.csproj" />
	</ItemGroup>

	<ItemGroup>
		<None Include="..\..\RCommon-Icon.jpg">
			<Pack>True</Pack>
			<PackagePath>\</PackagePath>
		</None>
	</ItemGroup>

	<ItemGroup>
		<Content Include="README.md">
			<Pack>True</Pack>
			<PackagePath>\</PackagePath>
		</Content>
	</ItemGroup>

	<ItemGroup>
		<PackageReference Update="MinVer" Version="7.0.0" />
	</ItemGroup>

</Project>
```

- [ ] **Step 2: Create the supporting type files**

```csharp
// Src/RCommon.Blobs/BlobItem.cs
namespace RCommon.Blobs;

/// <summary>
/// Represents a blob item returned from listing operations.
/// </summary>
public class BlobItem
{
    public string Name { get; set; } = default!;
    public long? Size { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
```

```csharp
// Src/RCommon.Blobs/BlobProperties.cs
namespace RCommon.Blobs;

/// <summary>
/// Detailed properties of a blob, returned from GetPropertiesAsync.
/// </summary>
public class BlobProperties
{
    public string ContentType { get; set; } = default!;
    public long ContentLength { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string? ETag { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
```

```csharp
// Src/RCommon.Blobs/BlobUploadOptions.cs
namespace RCommon.Blobs;

/// <summary>
/// Options for upload operations.
/// </summary>
public class BlobUploadOptions
{
    public string? ContentType { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
    public bool Overwrite { get; set; } = true;
}
```

```csharp
// Src/RCommon.Blobs/BlobStoreNotFoundException.cs
namespace RCommon.Blobs;

/// <summary>
/// Thrown when a named blob store cannot be resolved from the factory.
/// </summary>
public class BlobStoreNotFoundException : Exception
{
    public BlobStoreNotFoundException(string message) : base(message) { }
}
```

- [ ] **Step 3: Create the test project**

```xml
<!-- Tests/RCommon.Blobs.Tests/RCommon.Blobs.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\..\Src\RCommon.Blobs\RCommon.Blobs.csproj" />
    <ProjectReference Include="..\RCommon.TestBase.XUnit\RCommon.TestBase.XUnit.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.2" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="10.0.2" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Write tests for BlobUploadOptions defaults**

```csharp
// Tests/RCommon.Blobs.Tests/BlobUploadOptionsTests.cs
using FluentAssertions;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Blobs.Tests;

public class BlobUploadOptionsTests
{
    [Fact]
    public void Overwrite_DefaultsToTrue()
    {
        var options = new BlobUploadOptions();
        options.Overwrite.Should().BeTrue();
    }

    [Fact]
    public void ContentType_DefaultsToNull()
    {
        var options = new BlobUploadOptions();
        options.ContentType.Should().BeNull();
    }

    [Fact]
    public void Metadata_DefaultsToNull()
    {
        var options = new BlobUploadOptions();
        options.Metadata.Should().BeNull();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Blobs.Tests/ --filter "FullyQualifiedName~BlobUploadOptionsTests" -v minimal`
Expected: 3 tests PASS

- [ ] **Step 6: Commit**

```bash
git add Src/RCommon.Blobs/ Tests/RCommon.Blobs.Tests/
git commit -m "feat(blobs): add RCommon.Blobs project with supporting types"
```

---

## Task 2: Add IBlobStorageService Interface

**Files:**
- Create: `Src/RCommon.Blobs/IBlobStorageService.cs`

- [ ] **Step 1: Create the interface**

```csharp
// Src/RCommon.Blobs/IBlobStorageService.cs
namespace RCommon.Blobs;

/// <summary>
/// Provider-agnostic interface for blob/object storage operations.
/// Covers container management, blob CRUD, metadata, transfer, and presigned URL generation.
/// </summary>
public interface IBlobStorageService
{
    // Container operations
    Task CreateContainerAsync(string containerName, CancellationToken token = default);
    Task DeleteContainerAsync(string containerName, CancellationToken token = default);
    Task<bool> ContainerExistsAsync(string containerName, CancellationToken token = default);
    Task<IEnumerable<string>> ListContainersAsync(CancellationToken token = default);

    // Blob CRUD
    Task UploadAsync(string containerName, string blobName, Stream content,
        BlobUploadOptions? options = null, CancellationToken token = default);
    Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken token = default);
    Task DeleteAsync(string containerName, string blobName, CancellationToken token = default);
    Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken token = default);
    Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName,
        string? prefix = null, CancellationToken token = default);

    // Metadata
    Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName,
        CancellationToken token = default);
    Task SetMetadataAsync(string containerName, string blobName,
        IDictionary<string, string> metadata, CancellationToken token = default);

    // Transfer
    Task CopyAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default);
    Task MoveAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default);

    // Presigned URLs
    Task<Uri> GetPresignedDownloadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default);
    Task<Uri> GetPresignedUploadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default);
}
```

- [ ] **Step 2: Verify the project compiles**

Run: `dotnet build Src/RCommon.Blobs/`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Src/RCommon.Blobs/IBlobStorageService.cs
git commit -m "feat(blobs): add IBlobStorageService interface"
```

---

## Task 3: Add BlobStoreFactory and Factory Infrastructure

**Files:**
- Create: `Src/RCommon.Blobs/IBlobStoreFactory.cs`
- Create: `Src/RCommon.Blobs/BlobStoreFactory.cs`
- Create: `Src/RCommon.Blobs/BlobStoreFactoryOptions.cs`
- Test: `Tests/RCommon.Blobs.Tests/BlobStoreFactoryTests.cs`

- [ ] **Step 1: Write the failing tests for BlobStoreFactory**

```csharp
// Tests/RCommon.Blobs.Tests/BlobStoreFactoryTests.cs
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Blobs.Tests;

public class BlobStoreFactoryTests
{
    private readonly Mock<IServiceProvider> _serviceProvider;

    public BlobStoreFactoryTests()
    {
        _serviceProvider = new Mock<IServiceProvider>();
    }

    private BlobStoreFactory CreateFactory(BlobStoreFactoryOptions factoryOptions)
    {
        var options = Options.Create(factoryOptions);
        return new BlobStoreFactory(_serviceProvider.Object, options);
    }

    [Fact]
    public void Resolve_WithRegisteredName_ReturnsService()
    {
        // Arrange
        var mockService = new Mock<IBlobStorageService>();
        var factoryOptions = new BlobStoreFactoryOptions();
        factoryOptions.Stores.TryAdd("test-store", sp => mockService.Object);
        var factory = CreateFactory(factoryOptions);

        // Act
        var result = factory.Resolve("test-store");

        // Assert
        result.Should().BeSameAs(mockService.Object);
    }

    [Fact]
    public void Resolve_WithUnregisteredName_ThrowsBlobStoreNotFoundException()
    {
        // Arrange
        var factory = CreateFactory(new BlobStoreFactoryOptions());

        // Act
        var act = () => factory.Resolve("nonexistent");

        // Assert
        act.Should().Throw<BlobStoreNotFoundException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public void Resolve_CalledTwiceWithSameName_ReturnsCachedInstance()
    {
        // Arrange
        var callCount = 0;
        var mockService = new Mock<IBlobStorageService>();
        var factoryOptions = new BlobStoreFactoryOptions();
        factoryOptions.Stores.TryAdd("cached", sp =>
        {
            callCount++;
            return mockService.Object;
        });
        var factory = CreateFactory(factoryOptions);

        // Act
        var first = factory.Resolve("cached");
        var second = factory.Resolve("cached");

        // Assert
        first.Should().BeSameAs(second);
        callCount.Should().Be(1);
    }

    [Fact]
    public void Resolve_MultipleNames_ReturnsDistinctInstances()
    {
        // Arrange
        var mockServiceA = new Mock<IBlobStorageService>();
        var mockServiceB = new Mock<IBlobStorageService>();
        var factoryOptions = new BlobStoreFactoryOptions();
        factoryOptions.Stores.TryAdd("store-a", sp => mockServiceA.Object);
        factoryOptions.Stores.TryAdd("store-b", sp => mockServiceB.Object);
        var factory = CreateFactory(factoryOptions);

        // Act
        var resultA = factory.Resolve("store-a");
        var resultB = factory.Resolve("store-b");

        // Assert
        resultA.Should().BeSameAs(mockServiceA.Object);
        resultB.Should().BeSameAs(mockServiceB.Object);
        resultA.Should().NotBeSameAs(resultB);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Blobs.Tests/ --filter "FullyQualifiedName~BlobStoreFactoryTests" -v minimal`
Expected: FAIL — `IBlobStoreFactory`, `BlobStoreFactory`, `BlobStoreFactoryOptions` do not exist yet

- [ ] **Step 3: Create the factory interface, options, and implementation**

```csharp
// Src/RCommon.Blobs/IBlobStoreFactory.cs
namespace RCommon.Blobs;

/// <summary>
/// Resolves named <see cref="IBlobStorageService"/> instances from registered blob stores.
/// </summary>
public interface IBlobStoreFactory
{
    /// <summary>
    /// Resolves a blob storage service by its registered name.
    /// </summary>
    /// <param name="name">The name used when the store was registered via AddBlobStore.</param>
    /// <returns>The <see cref="IBlobStorageService"/> instance for the named store.</returns>
    /// <exception cref="BlobStoreNotFoundException">Thrown when no store is registered with the given name.</exception>
    IBlobStorageService Resolve(string name);
}
```

```csharp
// Src/RCommon.Blobs/BlobStoreFactoryOptions.cs
using System.Collections.Concurrent;

namespace RCommon.Blobs;

/// <summary>
/// Configuration options for <see cref="BlobStoreFactory"/>.
/// Stores factory delegates keyed by blob store name.
/// </summary>
public class BlobStoreFactoryOptions
{
    /// <summary>
    /// Name-keyed factory delegates. Each delegate creates an <see cref="IBlobStorageService"/>
    /// for the named store when first resolved.
    /// </summary>
    public ConcurrentDictionary<string, Func<IServiceProvider, IBlobStorageService>> Stores { get; } = new();
}
```

```csharp
// Src/RCommon.Blobs/BlobStoreFactory.cs
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace RCommon.Blobs;

/// <summary>
/// Resolves named <see cref="IBlobStorageService"/> instances by invoking registered factory delegates.
/// Instances are created lazily on first resolution and cached for subsequent calls.
/// </summary>
public class BlobStoreFactory : IBlobStoreFactory
{
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<string, Func<IServiceProvider, IBlobStorageService>> _stores;
    private readonly ConcurrentDictionary<string, IBlobStorageService> _instances = new();

    public BlobStoreFactory(IServiceProvider provider, IOptions<BlobStoreFactoryOptions> options)
    {
        _provider = provider;
        _stores = options.Value.Stores;
    }

    /// <inheritdoc />
    public IBlobStorageService Resolve(string name)
    {
        return _instances.GetOrAdd(name, key =>
        {
            if (!_stores.TryGetValue(key, out var factory))
            {
                throw new BlobStoreNotFoundException(
                    $"Blob store with name '{key}' not found.");
            }

            return factory(_provider);
        });
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Blobs.Tests/ --filter "FullyQualifiedName~BlobStoreFactoryTests" -v minimal`
Expected: 4 tests PASS

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Blobs/IBlobStoreFactory.cs Src/RCommon.Blobs/BlobStoreFactory.cs Src/RCommon.Blobs/BlobStoreFactoryOptions.cs Tests/RCommon.Blobs.Tests/BlobStoreFactoryTests.cs
git commit -m "feat(blobs): add BlobStoreFactory with name-keyed factory delegates"
```

---

## Task 4: Add Builder Infrastructure and WithBlobStorage Extension

**Files:**
- Create: `Src/RCommon.Blobs/IBlobStorageBuilder.cs`
- Create: `Src/RCommon.Blobs/BlobStorageBuilderExtensions.cs`
- Test: `Tests/RCommon.Blobs.Tests/BlobStorageBuilderExtensionsTests.cs`

- [ ] **Step 1: Write the failing tests for builder extensions**

```csharp
// Tests/RCommon.Blobs.Tests/BlobStorageBuilderExtensionsTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Blobs.Tests;

public class BlobStorageBuilderExtensionsTests
{
    [Fact]
    public void WithBlobStorage_RegistersBlobStoreFactoryAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<TestBlobStorageBuilder>(b => { });
        var provider = services.BuildServiceProvider();

        // Assert
        var factory1 = provider.GetService<IBlobStoreFactory>();
        var factory2 = provider.GetService<IBlobStoreFactory>();
        factory1.Should().NotBeNull();
        factory1.Should().BeSameAs(factory2);
    }

    [Fact]
    public void WithBlobStorage_CalledMultipleTimes_DoesNotDuplicateFactoryRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<TestBlobStorageBuilder>(b => { });
        builder.WithBlobStorage<TestBlobStorageBuilder>(b => { });
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<IBlobStoreFactory>();
        factory.Should().NotBeNull();
        factory.Should().BeOfType<BlobStoreFactory>();
    }

    [Fact]
    public void WithBlobStorage_InvokesConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);
        var actionCalled = false;

        // Act
        builder.WithBlobStorage<TestBlobStorageBuilder>(b =>
        {
            actionCalled = true;
            b.Services.Should().BeSameAs(services);
        });

        // Assert
        actionCalled.Should().BeTrue();
    }

    [Fact]
    public void WithBlobStorage_ReturnsBuilder_ForFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        var result = builder.WithBlobStorage<TestBlobStorageBuilder>(b => { });

        // Assert
        result.Should().BeSameAs(builder);
    }

    /// <summary>
    /// Minimal test builder that satisfies the IBlobStorageBuilder contract.
    /// Constructor must accept IRCommonBuilder (used by Activator.CreateInstance).
    /// </summary>
    private class TestBlobStorageBuilder : IBlobStorageBuilder
    {
        public IServiceCollection Services { get; }

        public TestBlobStorageBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Blobs.Tests/ --filter "FullyQualifiedName~BlobStorageBuilderExtensionsTests" -v minimal`
Expected: FAIL — `IBlobStorageBuilder` and `BlobStorageBuilderExtensions` do not exist

- [ ] **Step 3: Create the builder interface and extensions**

```csharp
// Src/RCommon.Blobs/IBlobStorageBuilder.cs
using Microsoft.Extensions.DependencyInjection;

namespace RCommon.Blobs;

/// <summary>
/// Base builder interface for configuring blob storage providers within the RCommon builder pipeline.
/// </summary>
public interface IBlobStorageBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection"/> used to register blob storage dependencies.
    /// </summary>
    IServiceCollection Services { get; }
}
```

```csharp
// Src/RCommon.Blobs/BlobStorageBuilderExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RCommon.Blobs;

/// <summary>
/// Extension methods on <see cref="IRCommonBuilder"/> for registering blob storage infrastructure.
/// </summary>
public static class BlobStorageBuilderExtensions
{
    /// <summary>
    /// Registers a blob storage provider with default configuration.
    /// </summary>
    public static IRCommonBuilder WithBlobStorage<T>(this IRCommonBuilder builder)
        where T : IBlobStorageBuilder
    {
        return WithBlobStorage<T>(builder, x => { });
    }

    /// <summary>
    /// Registers a blob storage provider and applies the specified configuration actions.
    /// Can be called multiple times to register multiple providers (e.g. Azure + S3).
    /// </summary>
    public static IRCommonBuilder WithBlobStorage<T>(this IRCommonBuilder builder, Action<T> actions)
        where T : IBlobStorageBuilder
    {
        Guard.IsNotNull(actions, nameof(actions));

        // Register factory infrastructure (idempotent — safe to call multiple times)
        builder.Services.TryAddSingleton<IBlobStoreFactory, BlobStoreFactory>();

        var blobConfig = (T)(Activator.CreateInstance(typeof(T), new object[] { builder })
            ?? throw new InvalidOperationException($"Failed to create instance of {typeof(T).Name}."));
        actions(blobConfig);
        return builder;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Blobs.Tests/ -v minimal`
Expected: All tests PASS (4 factory + 4 builder + 3 options = 11 tests)

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Blobs/IBlobStorageBuilder.cs Src/RCommon.Blobs/BlobStorageBuilderExtensions.cs Tests/RCommon.Blobs.Tests/BlobStorageBuilderExtensionsTests.cs
git commit -m "feat(blobs): add builder infrastructure and WithBlobStorage extension"
```

---

## Task 5: Add RCommon.Blobs and Tests to Solution

**Files:**
- Modify: `Src/RCommon.sln`

- [ ] **Step 1: Add projects to the solution with a "Blobs" solution folder**

Run the following commands from the `Src/` directory:

```bash
cd Src
dotnet sln add RCommon.Blobs/RCommon.Blobs.csproj --solution-folder "Blobs"
dotnet sln add ../Tests/RCommon.Blobs.Tests/RCommon.Blobs.Tests.csproj --solution-folder "Tests"
cd ..
```

- [ ] **Step 2: Build the solution to verify everything compiles**

Run: `dotnet build Src/RCommon.sln`
Expected: Build succeeded

- [ ] **Step 3: Run all blob tests**

Run: `dotnet test Tests/RCommon.Blobs.Tests/ -v minimal`
Expected: All 11 tests PASS

- [ ] **Step 4: Commit**

```bash
git add Src/RCommon.sln
git commit -m "feat(blobs): add RCommon.Blobs and tests to solution"
```

---

## Task 6: Create RCommon.Azure.Blobs Project and Builder

**Files:**
- Create: `Src/RCommon.Azure.Blobs/RCommon.Azure.Blobs.csproj`
- Create: `Src/RCommon.Azure.Blobs/AzureBlobStoreOptions.cs`
- Create: `Src/RCommon.Azure.Blobs/IAzureBlobStorageBuilder.cs`
- Create: `Src/RCommon.Azure.Blobs/AzureBlobStorageBuilder.cs`
- Create: `Tests/RCommon.Azure.Blobs.Tests/RCommon.Azure.Blobs.Tests.csproj`

- [ ] **Step 1: Create the Azure project csproj**

```xml
<!-- Src/RCommon.Azure.Blobs/RCommon.Azure.Blobs.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
		<Nullable>enable</Nullable>
		<GeneratePackageOnBuild>True</GeneratePackageOnBuild>
		<Title>RCommon.Azure.Blobs</Title>
		<PackageProjectUrl>https://rcommon.com</PackageProjectUrl>
		<PackageTags>RCommon; Azure Blob Storage; Blob; Cloud Storage</PackageTags>
		<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
		<PackageRequireLicenseAcceptance>True</PackageRequireLicenseAcceptance>
		<Description>A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more</Description>
		<RepositoryUrl>https://github.com/RCommon-Team/RCommon</RepositoryUrl>
		<Company>RCommon</Company>
		<Authors>Jason Webb</Authors>
		<PackageIcon>RCommon-Icon.jpg</PackageIcon>
		<PackageReadmeFile>README.md</PackageReadmeFile>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Azure.Storage.Blobs" Version="12.24.0" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\RCommon.Blobs\RCommon.Blobs.csproj" />
	</ItemGroup>

	<ItemGroup>
		<None Include="..\..\RCommon-Icon.jpg">
			<Pack>True</Pack>
			<PackagePath>\</PackagePath>
		</None>
	</ItemGroup>

	<ItemGroup>
		<Content Include="README.md">
			<Pack>True</Pack>
			<PackagePath>\</PackagePath>
		</Content>
	</ItemGroup>

	<ItemGroup>
		<PackageReference Update="MinVer" Version="7.0.0" />
	</ItemGroup>

</Project>
```

- [ ] **Step 2: Create the options, builder interface, and builder**

```csharp
// Src/RCommon.Azure.Blobs/AzureBlobStoreOptions.cs
using Azure.Core;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Configuration options for a named Azure Blob Storage store.
/// Provide either <see cref="ConnectionString"/> or <see cref="ServiceUri"/> + <see cref="Credential"/>.
/// </summary>
public class AzureBlobStoreOptions
{
    public string? ConnectionString { get; set; }
    public Uri? ServiceUri { get; set; }
    public TokenCredential? Credential { get; set; }
}
```

```csharp
// Src/RCommon.Azure.Blobs/IAzureBlobStorageBuilder.cs
using RCommon.Blobs;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Builder interface for configuring Azure Blob Storage as an <see cref="IBlobStorageService"/> provider.
/// </summary>
public interface IAzureBlobStorageBuilder : IBlobStorageBuilder
{
    /// <summary>
    /// Registers a named Azure blob store with the specified options.
    /// </summary>
    IAzureBlobStorageBuilder AddBlobStore(string name, Action<AzureBlobStoreOptions> options);
}
```

```csharp
// Src/RCommon.Azure.Blobs/AzureBlobStorageBuilder.cs
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Blobs;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Concrete builder that registers Azure Blob Storage services into the DI container.
/// Constructor accepts <see cref="IRCommonBuilder"/> (required by Activator.CreateInstance pattern).
/// </summary>
public class AzureBlobStorageBuilder : IAzureBlobStorageBuilder
{
    public IServiceCollection Services { get; }

    public AzureBlobStorageBuilder(IRCommonBuilder builder)
    {
        Services = builder.Services;
    }

    /// <inheritdoc />
    public IAzureBlobStorageBuilder AddBlobStore(string name, Action<AzureBlobStoreOptions> options)
    {
        var storeOptions = new AzureBlobStoreOptions();
        options(storeOptions);

        Services.Configure<BlobStoreFactoryOptions>(o =>
            o.Stores.TryAdd(name, sp =>
            {
                BlobServiceClient client;
                if (storeOptions.ConnectionString != null)
                {
                    client = new BlobServiceClient(storeOptions.ConnectionString);
                }
                else if (storeOptions.ServiceUri != null && storeOptions.Credential != null)
                {
                    client = new BlobServiceClient(storeOptions.ServiceUri, storeOptions.Credential);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Blob store '{name}' requires either ConnectionString or ServiceUri + Credential.");
                }

                return new AzureBlobStorageService(client);
            }));

        return this;
    }
}
```

- [ ] **Step 3: Create test project (stub for now — integration tests in Task 8)**

```xml
<!-- Tests/RCommon.Azure.Blobs.Tests/RCommon.Azure.Blobs.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\..\Src\RCommon.Azure.Blobs\RCommon.Azure.Blobs.csproj" />
    <ProjectReference Include="..\RCommon.TestBase.XUnit\RCommon.TestBase.XUnit.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.2" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="10.0.2" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create AzureBlobStorageService stub (full implementation in Task 8)**

The builder references `AzureBlobStorageService` in the factory delegate, so it must exist for the project to compile. Create a stub with `NotImplementedException` for all methods:

```csharp
// Src/RCommon.Azure.Blobs/AzureBlobStorageService.cs
using Azure.Storage.Blobs;
using RCommon.Blobs;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Stub — full implementation in Task 8.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorageService(BlobServiceClient client)
    {
        _client = client;
    }

    public Task CreateContainerAsync(string containerName, CancellationToken token = default) => throw new NotImplementedException();
    public Task DeleteContainerAsync(string containerName, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> ContainerExistsAsync(string containerName, CancellationToken token = default) => throw new NotImplementedException();
    public Task<IEnumerable<string>> ListContainersAsync(CancellationToken token = default) => throw new NotImplementedException();
    public Task UploadAsync(string containerName, string blobName, Stream content, BlobUploadOptions? options = null, CancellationToken token = default) => throw new NotImplementedException();
    public Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken token = default) => throw new NotImplementedException();
    public Task DeleteAsync(string containerName, string blobName, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken token = default) => throw new NotImplementedException();
    public Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken token = default) => throw new NotImplementedException();
    public Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName, CancellationToken token = default) => throw new NotImplementedException();
    public Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken token = default) => throw new NotImplementedException();
    public Task CopyAsync(string sourceContainer, string sourceBlob, string destContainer, string destBlob, CancellationToken token = default) => throw new NotImplementedException();
    public Task MoveAsync(string sourceContainer, string sourceBlob, string destContainer, string destBlob, CancellationToken token = default) => throw new NotImplementedException();
    public Task<Uri> GetPresignedDownloadUrlAsync(string containerName, string blobName, TimeSpan expiry, CancellationToken token = default) => throw new NotImplementedException();
    public Task<Uri> GetPresignedUploadUrlAsync(string containerName, string blobName, TimeSpan expiry, CancellationToken token = default) => throw new NotImplementedException();
}
```

- [ ] **Step 5: Write builder registration tests**

```csharp
// Tests/RCommon.Azure.Blobs.Tests/AzureBlobStorageBuilderTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.Azure.Blobs;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Azure.Blobs.Tests;

public class AzureBlobStorageBuilderTests
{
    [Fact]
    public void AddBlobStore_WithConnectionString_RegistersFactoryDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AzureBlobStorageBuilder>(blob =>
        {
            blob.AddBlobStore("test", options =>
            {
                options.ConnectionString = "UseDevelopmentStorage=true";
            });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("test");
    }

    [Fact]
    public void AddBlobStore_WithoutConnectionStringOrCredential_ThrowsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        builder.WithBlobStorage<AzureBlobStorageBuilder>(blob =>
        {
            blob.AddBlobStore("invalid", options => { });
        });
        var provider = services.BuildServiceProvider();

        // Act
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        var factory = factoryOptions.Value.Stores["invalid"];
        var act = () => factory(provider);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires either ConnectionString or ServiceUri*");
    }

    [Fact]
    public void AddBlobStore_MultipleStores_RegistersBothDelegates()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AzureBlobStorageBuilder>(blob =>
        {
            blob.AddBlobStore("primary", o => o.ConnectionString = "UseDevelopmentStorage=true");
            blob.AddBlobStore("backup", o => o.ConnectionString = "UseDevelopmentStorage=true");
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("primary").And.ContainKey("backup");
    }
}
```

- [ ] **Step 6: Verify the project compiles and tests pass**

Run: `dotnet build Src/RCommon.Azure.Blobs/`
Expected: Build succeeded

Run: `dotnet test Tests/RCommon.Azure.Blobs.Tests/ --filter "FullyQualifiedName~AzureBlobStorageBuilderTests" -v minimal`
Expected: 3 tests PASS

- [ ] **Step 7: Commit**

```bash
git add Src/RCommon.Azure.Blobs/ Tests/RCommon.Azure.Blobs.Tests/
git commit -m "feat(azure-blobs): add Azure blob storage builder and project"
```

---

## Task 7: Create RCommon.Amazon.S3Objects Project and Builder

**Files:**
- Create: `Src/RCommon.Amazon.S3Objects/RCommon.Amazon.S3Objects.csproj`
- Create: `Src/RCommon.Amazon.S3Objects/AmazonS3StoreOptions.cs`
- Create: `Src/RCommon.Amazon.S3Objects/IAmazonS3ObjectsBuilder.cs`
- Create: `Src/RCommon.Amazon.S3Objects/AmazonS3ObjectsBuilder.cs`
- Create: `Tests/RCommon.Amazon.S3Objects.Tests/RCommon.Amazon.S3Objects.Tests.csproj`

- [ ] **Step 1: Create the S3 project csproj**

```xml
<!-- Src/RCommon.Amazon.S3Objects/RCommon.Amazon.S3Objects.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
		<Nullable>enable</Nullable>
		<GeneratePackageOnBuild>True</GeneratePackageOnBuild>
		<Title>RCommon.Amazon.S3Objects</Title>
		<PackageProjectUrl>https://rcommon.com</PackageProjectUrl>
		<PackageTags>RCommon; Amazon S3; AWS; Blob; Cloud Storage; Object Storage</PackageTags>
		<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
		<PackageRequireLicenseAcceptance>True</PackageRequireLicenseAcceptance>
		<Description>A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more</Description>
		<RepositoryUrl>https://github.com/RCommon-Team/RCommon</RepositoryUrl>
		<Company>RCommon</Company>
		<Authors>Jason Webb</Authors>
		<PackageIcon>RCommon-Icon.jpg</PackageIcon>
		<PackageReadmeFile>README.md</PackageReadmeFile>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="AWSSDK.S3" Version="3.7.410.7" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\RCommon.Blobs\RCommon.Blobs.csproj" />
	</ItemGroup>

	<ItemGroup>
		<None Include="..\..\RCommon-Icon.jpg">
			<Pack>True</Pack>
			<PackagePath>\</PackagePath>
		</None>
	</ItemGroup>

	<ItemGroup>
		<Content Include="README.md">
			<Pack>True</Pack>
			<PackagePath>\</PackagePath>
		</Content>
	</ItemGroup>

	<ItemGroup>
		<PackageReference Update="MinVer" Version="7.0.0" />
	</ItemGroup>

</Project>
```

- [ ] **Step 2: Create the options, builder interface, and builder**

```csharp
// Src/RCommon.Amazon.S3Objects/AmazonS3StoreOptions.cs
namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Configuration options for a named Amazon S3 blob store.
/// </summary>
public class AmazonS3StoreOptions
{
    public string? Region { get; set; }
    /// <summary>
    /// AWS credentials profile name. Uses CredentialProfileStoreChain for resolution.
    /// </summary>
    public string? Profile { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    /// <summary>
    /// Service URL for S3-compatible stores (MinIO, LocalStack).
    /// </summary>
    public string? ServiceUrl { get; set; }
    /// <summary>
    /// Required for S3-compatible stores that don't support virtual-hosted-style addressing.
    /// </summary>
    public bool ForcePathStyle { get; set; }
}
```

```csharp
// Src/RCommon.Amazon.S3Objects/IAmazonS3ObjectsBuilder.cs
using RCommon.Blobs;

namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Builder interface for configuring Amazon S3 as an <see cref="IBlobStorageService"/> provider.
/// </summary>
public interface IAmazonS3ObjectsBuilder : IBlobStorageBuilder
{
    /// <summary>
    /// Registers a named S3 blob store with the specified options.
    /// </summary>
    IAmazonS3ObjectsBuilder AddBlobStore(string name, Action<AmazonS3StoreOptions> options);
}
```

```csharp
// Src/RCommon.Amazon.S3Objects/AmazonS3ObjectsBuilder.cs
using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Blobs;

namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Concrete builder that registers Amazon S3 services into the DI container.
/// Constructor accepts <see cref="IRCommonBuilder"/> (required by Activator.CreateInstance pattern).
/// </summary>
public class AmazonS3ObjectsBuilder : IAmazonS3ObjectsBuilder
{
    public IServiceCollection Services { get; }

    public AmazonS3ObjectsBuilder(IRCommonBuilder builder)
    {
        Services = builder.Services;
    }

    /// <inheritdoc />
    public IAmazonS3ObjectsBuilder AddBlobStore(string name, Action<AmazonS3StoreOptions> options)
    {
        var storeOptions = new AmazonS3StoreOptions();
        options(storeOptions);

        Services.Configure<BlobStoreFactoryOptions>(o =>
            o.Stores.TryAdd(name, sp =>
            {
                var config = new AmazonS3Config();
                if (storeOptions.Region != null)
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(storeOptions.Region);
                if (storeOptions.ServiceUrl != null)
                    config.ServiceURL = storeOptions.ServiceUrl;
                config.ForcePathStyle = storeOptions.ForcePathStyle;

                IAmazonS3 client;
                if (storeOptions.AccessKeyId != null && storeOptions.SecretAccessKey != null)
                {
                    client = new AmazonS3Client(
                        storeOptions.AccessKeyId, storeOptions.SecretAccessKey, config);
                }
                else if (storeOptions.Profile != null)
                {
                    var chain = new CredentialProfileStoreChain();
                    if (!chain.TryGetAWSCredentials(storeOptions.Profile, out var credentials))
                    {
                        throw new InvalidOperationException(
                            $"AWS profile '{storeOptions.Profile}' not found.");
                    }
                    client = new AmazonS3Client(credentials, config);
                }
                else
                {
                    // Falls back to default credential chain (env vars, instance profile, etc.)
                    client = new AmazonS3Client(config);
                }

                return new AmazonS3StorageService(client);
            }));

        return this;
    }
}
```

- [ ] **Step 3: Create the test project (stub for now — integration tests in Task 9)**

```xml
<!-- Tests/RCommon.Amazon.S3Objects.Tests/RCommon.Amazon.S3Objects.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\..\Src\RCommon.Amazon.S3Objects\RCommon.Amazon.S3Objects.csproj" />
    <ProjectReference Include="..\RCommon.TestBase.XUnit\RCommon.TestBase.XUnit.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.2" />
    <PackageReference Include="Microsoft.Extensions.Options" Version="10.0.2" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Create AmazonS3StorageService stub (full implementation in Task 9)**

```csharp
// Src/RCommon.Amazon.S3Objects/AmazonS3StorageService.cs
using Amazon.S3;
using RCommon.Blobs;

namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Amazon S3 implementation of <see cref="IBlobStorageService"/>.
/// Stub — full implementation in Task 9.
/// </summary>
public class AmazonS3StorageService : IBlobStorageService
{
    private readonly IAmazonS3 _client;

    public AmazonS3StorageService(IAmazonS3 client)
    {
        _client = client;
    }

    public Task CreateContainerAsync(string containerName, CancellationToken token = default) => throw new NotImplementedException();
    public Task DeleteContainerAsync(string containerName, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> ContainerExistsAsync(string containerName, CancellationToken token = default) => throw new NotImplementedException();
    public Task<IEnumerable<string>> ListContainersAsync(CancellationToken token = default) => throw new NotImplementedException();
    public Task UploadAsync(string containerName, string blobName, Stream content, BlobUploadOptions? options = null, CancellationToken token = default) => throw new NotImplementedException();
    public Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken token = default) => throw new NotImplementedException();
    public Task DeleteAsync(string containerName, string blobName, CancellationToken token = default) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken token = default) => throw new NotImplementedException();
    public Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken token = default) => throw new NotImplementedException();
    public Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName, CancellationToken token = default) => throw new NotImplementedException();
    public Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata, CancellationToken token = default) => throw new NotImplementedException();
    public Task CopyAsync(string sourceContainer, string sourceBlob, string destContainer, string destBlob, CancellationToken token = default) => throw new NotImplementedException();
    public Task MoveAsync(string sourceContainer, string sourceBlob, string destContainer, string destBlob, CancellationToken token = default) => throw new NotImplementedException();
    public Task<Uri> GetPresignedDownloadUrlAsync(string containerName, string blobName, TimeSpan expiry, CancellationToken token = default) => throw new NotImplementedException();
    public Task<Uri> GetPresignedUploadUrlAsync(string containerName, string blobName, TimeSpan expiry, CancellationToken token = default) => throw new NotImplementedException();
}
```

- [ ] **Step 5: Write builder registration tests**

```csharp
// Tests/RCommon.Amazon.S3Objects.Tests/AmazonS3ObjectsBuilderTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.Amazon.S3Objects;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Amazon.S3Objects.Tests;

public class AmazonS3ObjectsBuilderTests
{
    [Fact]
    public void AddBlobStore_WithExplicitCredentials_RegistersFactoryDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AmazonS3ObjectsBuilder>(blob =>
        {
            blob.AddBlobStore("test", options =>
            {
                options.AccessKeyId = "test";
                options.SecretAccessKey = "test";
                options.Region = "us-east-1";
            });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("test");
    }

    [Fact]
    public void AddBlobStore_WithDefaultCredentialChain_RegistersFactoryDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AmazonS3ObjectsBuilder>(blob =>
        {
            blob.AddBlobStore("default", options =>
            {
                options.ServiceUrl = "http://localhost:4566";
                options.ForcePathStyle = true;
            });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("default");
    }

    [Fact]
    public void AddBlobStore_MultipleStores_RegistersBothDelegates()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithBlobStorage<AmazonS3ObjectsBuilder>(blob =>
        {
            blob.AddBlobStore("primary", o => { o.Region = "us-east-1"; });
            blob.AddBlobStore("archive", o => { o.Region = "eu-west-1"; });
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var factoryOptions = provider.GetRequiredService<IOptions<BlobStoreFactoryOptions>>();
        factoryOptions.Value.Stores.Should().ContainKey("primary").And.ContainKey("archive");
    }
}
```

- [ ] **Step 6: Verify the project compiles and tests pass**

Run: `dotnet build Src/RCommon.Amazon.S3Objects/`
Expected: Build succeeded

Run: `dotnet test Tests/RCommon.Amazon.S3Objects.Tests/ --filter "FullyQualifiedName~AmazonS3ObjectsBuilderTests" -v minimal`
Expected: 3 tests PASS

- [ ] **Step 7: Commit**

```bash
git add Src/RCommon.Amazon.S3Objects/ Tests/RCommon.Amazon.S3Objects.Tests/
git commit -m "feat(s3): add Amazon S3 builder and project"
```

---

## Task 8: Implement AzureBlobStorageService

**Files:**
- Create: `Src/RCommon.Azure.Blobs/AzureBlobStorageService.cs`
- Test: `Tests/RCommon.Azure.Blobs.Tests/AzureBlobStorageServiceTests.cs`

This is the largest task. The implementation wraps `BlobServiceClient` from `Azure.Storage.Blobs`.

- [ ] **Step 1: Write integration tests for the Azure implementation**

These tests require Azurite (Azure Storage Emulator). Use the well-known Azurite connection string. Tests should be marked with a `[Trait("Category", "Integration")]` so they can be excluded when Azurite is not running.

```csharp
// Tests/RCommon.Azure.Blobs.Tests/AzureBlobStorageServiceTests.cs
using Azure.Storage.Blobs;
using FluentAssertions;
using RCommon.Azure.Blobs;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Azure.Blobs.Tests;

[Trait("Category", "Integration")]
public class AzureBlobStorageServiceTests : IAsyncLifetime
{
    // Azurite default connection string
    private const string ConnectionString = "UseDevelopmentStorage=true";
    private readonly AzureBlobStorageService _service;
    private readonly string _testContainer;

    public AzureBlobStorageServiceTests()
    {
        var client = new BlobServiceClient(ConnectionString);
        _service = new AzureBlobStorageService(client);
        _testContainer = $"test-{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        await _service.CreateContainerAsync(_testContainer);
    }

    public async Task DisposeAsync()
    {
        try { await _service.DeleteContainerAsync(_testContainer); } catch { }
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsTrue_ForExistingContainer()
    {
        var exists = await _service.ContainerExistsAsync(_testContainer);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsFalse_ForNonExistentContainer()
    {
        var exists = await _service.ContainerExistsAsync("does-not-exist-" + Guid.NewGuid());
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ListContainersAsync_IncludesTestContainer()
    {
        var containers = await _service.ListContainersAsync();
        containers.Should().Contain(_testContainer);
    }

    [Fact]
    public async Task UploadAndDownload_RoundTripsContent()
    {
        var content = "Hello, blob!"u8.ToArray();
        using var uploadStream = new MemoryStream(content);

        await _service.UploadAsync(_testContainer, "test.txt", uploadStream,
            new BlobUploadOptions { ContentType = "text/plain" });

        using var downloadStream = await _service.DownloadAsync(_testContainer, "test.txt");
        using var ms = new MemoryStream();
        await downloadStream.CopyToAsync(ms);
        ms.ToArray().Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_AfterUpload()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        await _service.UploadAsync(_testContainer, "exists-test.txt", stream);

        var exists = await _service.ExistsAsync(_testContainer, "exists-test.txt");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_ForNonExistentBlob()
    {
        var exists = await _service.ExistsAsync(_testContainer, "nope.txt");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesBlob()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        await _service.UploadAsync(_testContainer, "delete-me.txt", stream);

        await _service.DeleteAsync(_testContainer, "delete-me.txt");

        var exists = await _service.ExistsAsync(_testContainer, "delete-me.txt");
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ListBlobsAsync_ReturnsUploadedBlobs()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        await _service.UploadAsync(_testContainer, "list/one.txt", s1);
        await _service.UploadAsync(_testContainer, "list/two.txt", s2);

        var blobs = (await _service.ListBlobsAsync(_testContainer, "list/")).ToList();
        blobs.Should().HaveCount(2);
        blobs.Select(b => b.Name).Should().Contain("list/one.txt").And.Contain("list/two.txt");
    }

    [Fact]
    public async Task GetPropertiesAsync_ReturnsMetadata()
    {
        using var stream = new MemoryStream("props"u8.ToArray());
        await _service.UploadAsync(_testContainer, "props.txt", stream,
            new BlobUploadOptions
            {
                ContentType = "text/plain",
                Metadata = new Dictionary<string, string> { ["env"] = "test" }
            });

        var props = await _service.GetPropertiesAsync(_testContainer, "props.txt");
        props.ContentType.Should().Be("text/plain");
        props.ContentLength.Should().Be(5);
        props.Metadata.Should().ContainKey("env").WhoseValue.Should().Be("test");
    }

    [Fact]
    public async Task SetMetadataAsync_UpdatesMetadata()
    {
        using var stream = new MemoryStream("m"u8.ToArray());
        await _service.UploadAsync(_testContainer, "meta.txt", stream);

        await _service.SetMetadataAsync(_testContainer, "meta.txt",
            new Dictionary<string, string> { ["key"] = "value" });

        var props = await _service.GetPropertiesAsync(_testContainer, "meta.txt");
        props.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public async Task CopyAsync_CopiesBlobToNewLocation()
    {
        using var stream = new MemoryStream("copy-me"u8.ToArray());
        await _service.UploadAsync(_testContainer, "original.txt", stream);

        await _service.CopyAsync(_testContainer, "original.txt", _testContainer, "copy.txt");

        var exists = await _service.ExistsAsync(_testContainer, "copy.txt");
        exists.Should().BeTrue();
        // Original still exists
        (await _service.ExistsAsync(_testContainer, "original.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task MoveAsync_MovesBlob()
    {
        using var stream = new MemoryStream("move-me"u8.ToArray());
        await _service.UploadAsync(_testContainer, "move-src.txt", stream);

        await _service.MoveAsync(_testContainer, "move-src.txt", _testContainer, "move-dest.txt");

        (await _service.ExistsAsync(_testContainer, "move-dest.txt")).Should().BeTrue();
        (await _service.ExistsAsync(_testContainer, "move-src.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ReturnsValidUri()
    {
        using var stream = new MemoryStream("signed"u8.ToArray());
        await _service.UploadAsync(_testContainer, "signed.txt", stream);

        var uri = await _service.GetPresignedDownloadUrlAsync(_testContainer, "signed.txt", TimeSpan.FromMinutes(5));

        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Contain("signed.txt");
        uri.AbsoluteUri.Should().Contain("sig=");
    }

    [Fact]
    public async Task GetPresignedUploadUrlAsync_ReturnsValidUri()
    {
        var uri = await _service.GetPresignedUploadUrlAsync(_testContainer, "upload-target.txt", TimeSpan.FromMinutes(5));

        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Contain("upload-target.txt");
        uri.AbsoluteUri.Should().Contain("sig=");
    }
}
```

- [ ] **Step 2: Implement AzureBlobStorageService**

```csharp
// Src/RCommon.Azure.Blobs/AzureBlobStorageService.cs
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using RCommon.Blobs;

namespace RCommon.Azure.Blobs;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobStorageService"/>.
/// Wraps <see cref="BlobServiceClient"/> from the Azure.Storage.Blobs SDK.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;

    public AzureBlobStorageService(BlobServiceClient client)
    {
        _client = client;
    }

    public async Task CreateContainerAsync(string containerName, CancellationToken token = default)
    {
        await _client.CreateBlobContainerAsync(containerName, cancellationToken: token);
    }

    public async Task DeleteContainerAsync(string containerName, CancellationToken token = default)
    {
        await _client.DeleteBlobContainerAsync(containerName, cancellationToken: token);
    }

    public async Task<bool> ContainerExistsAsync(string containerName, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var response = await container.ExistsAsync(token);
        return response.Value;
    }

    public async Task<IEnumerable<string>> ListContainersAsync(CancellationToken token = default)
    {
        var containers = new List<string>();
        await foreach (var item in _client.GetBlobContainersAsync(cancellationToken: token))
        {
            containers.Add(item.Name);
        }
        return containers;
    }

    public async Task UploadAsync(string containerName, string blobName, Stream content,
        BlobUploadOptions? options = null, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        var uploadOptions = new Azure.Storage.Blobs.Models.BlobUploadOptions
        {
            HttpHeaders = options?.ContentType != null
                ? new BlobHttpHeaders { ContentType = options.ContentType }
                : null,
            Metadata = options?.Metadata,
        };

        // Overwrite behavior: if Overwrite is false and blob exists, throw
        if (options != null && !options.Overwrite)
        {
            uploadOptions.Conditions = new Azure.Storage.Blobs.Models.BlobRequestConditions
            {
                IfNoneMatch = Azure.ETag.All
            };
        }

        await blob.UploadAsync(content, uploadOptions, token);
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        return await blob.OpenReadAsync(cancellationToken: token);
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: token);
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        var response = await blob.ExistsAsync(token);
        return response.Value;
    }

    public async Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName,
        string? prefix = null, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var items = new List<BlobItem>();
        await foreach (var item in container.GetBlobsAsync(
            traits: BlobTraits.Metadata,
            prefix: prefix,
            cancellationToken: token))
        {
            items.Add(new BlobItem
            {
                Name = item.Name,
                Size = item.Properties.ContentLength,
                ContentType = item.Properties.ContentType,
                LastModified = item.Properties.LastModified,
                Metadata = item.Metadata ?? new Dictionary<string, string>()
            });
        }
        return items;
    }

    public async Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName,
        CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        var response = await blob.GetPropertiesAsync(cancellationToken: token);
        var props = response.Value;

        return new BlobProperties
        {
            ContentType = props.ContentType,
            ContentLength = props.ContentLength,
            LastModified = props.LastModified,
            ETag = props.ETag.ToString(),
            Metadata = props.Metadata ?? new Dictionary<string, string>()
        };
    }

    public async Task SetMetadataAsync(string containerName, string blobName,
        IDictionary<string, string> metadata, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.SetMetadataAsync(metadata, cancellationToken: token);
    }

    public async Task CopyAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default)
    {
        var sourceClient = _client.GetBlobContainerClient(sourceContainer).GetBlobClient(sourceBlob);
        var destClient = _client.GetBlobContainerClient(destContainer).GetBlobClient(destBlob);

        var operation = await destClient.StartCopyFromUriAsync(sourceClient.Uri, cancellationToken: token);
        await operation.WaitForCompletionAsync(token);
    }

    public async Task MoveAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default)
    {
        await CopyAsync(sourceContainer, sourceBlob, destContainer, destBlob, token);
        await DeleteAsync(sourceContainer, sourceBlob, token);
    }

    public Task<Uri> GetPresignedDownloadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var uri = blob.GenerateSasUri(sasBuilder);
        return Task.FromResult(uri);
    }

    public Task<Uri> GetPresignedUploadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var uri = blob.GenerateSasUri(sasBuilder);
        return Task.FromResult(uri);
    }
}
```

- [ ] **Step 3: Run integration tests (requires Azurite)**

Run: `dotnet test Tests/RCommon.Azure.Blobs.Tests/ -v minimal`
Expected: All tests PASS (if Azurite is running)

If Azurite is not available, verify the project builds: `dotnet build Src/RCommon.Azure.Blobs/`

- [ ] **Step 4: Commit**

```bash
git add Src/RCommon.Azure.Blobs/AzureBlobStorageService.cs Tests/RCommon.Azure.Blobs.Tests/AzureBlobStorageServiceTests.cs
git commit -m "feat(azure-blobs): implement AzureBlobStorageService with full IBlobStorageService"
```

---

## Task 9: Implement AmazonS3StorageService

**Files:**
- Create: `Src/RCommon.Amazon.S3Objects/AmazonS3StorageService.cs`
- Test: `Tests/RCommon.Amazon.S3Objects.Tests/AmazonS3StorageServiceTests.cs`

- [ ] **Step 1: Write integration tests for the S3 implementation**

These tests require LocalStack (S3-compatible emulator). Use `http://localhost:4566` with path-style addressing.

```csharp
// Tests/RCommon.Amazon.S3Objects.Tests/AmazonS3StorageServiceTests.cs
using Amazon.S3;
using FluentAssertions;
using RCommon.Amazon.S3Objects;
using RCommon.Blobs;
using Xunit;

namespace RCommon.Amazon.S3Objects.Tests;

[Trait("Category", "Integration")]
public class AmazonS3StorageServiceTests : IAsyncLifetime
{
    private readonly AmazonS3StorageService _service;
    private readonly string _testBucket;

    public AmazonS3StorageServiceTests()
    {
        var client = new AmazonS3Client(
            "test", "test",
            new AmazonS3Config
            {
                ServiceURL = "http://localhost:4566",
                ForcePathStyle = true
            });
        _service = new AmazonS3StorageService(client);
        _testBucket = $"test-{Guid.NewGuid():N}";
    }

    public async Task InitializeAsync()
    {
        await _service.CreateContainerAsync(_testBucket);
    }

    public async Task DisposeAsync()
    {
        // Delete all objects then the bucket
        try
        {
            var blobs = await _service.ListBlobsAsync(_testBucket);
            foreach (var blob in blobs)
                await _service.DeleteAsync(_testBucket, blob.Name);
            await _service.DeleteContainerAsync(_testBucket);
        }
        catch { }
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsTrue_ForExistingBucket()
    {
        var exists = await _service.ContainerExistsAsync(_testBucket);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ContainerExistsAsync_ReturnsFalse_ForNonExistentBucket()
    {
        var exists = await _service.ContainerExistsAsync("nope-" + Guid.NewGuid());
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ListContainersAsync_IncludesTestBucket()
    {
        var buckets = await _service.ListContainersAsync();
        buckets.Should().Contain(_testBucket);
    }

    [Fact]
    public async Task UploadAndDownload_RoundTripsContent()
    {
        var content = "Hello, S3!"u8.ToArray();
        using var uploadStream = new MemoryStream(content);

        await _service.UploadAsync(_testBucket, "test.txt", uploadStream,
            new BlobUploadOptions { ContentType = "text/plain" });

        using var downloadStream = await _service.DownloadAsync(_testBucket, "test.txt");
        using var ms = new MemoryStream();
        await downloadStream.CopyToAsync(ms);
        ms.ToArray().Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_AfterUpload()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        await _service.UploadAsync(_testBucket, "exists.txt", stream);

        (await _service.ExistsAsync(_testBucket, "exists.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_ForNonExistentObject()
    {
        (await _service.ExistsAsync(_testBucket, "nope.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovesObject()
    {
        using var stream = new MemoryStream("data"u8.ToArray());
        await _service.UploadAsync(_testBucket, "delete-me.txt", stream);

        await _service.DeleteAsync(_testBucket, "delete-me.txt");
        (await _service.ExistsAsync(_testBucket, "delete-me.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task ListBlobsAsync_ReturnsUploadedObjects()
    {
        using var s1 = new MemoryStream("a"u8.ToArray());
        using var s2 = new MemoryStream("b"u8.ToArray());
        await _service.UploadAsync(_testBucket, "list/one.txt", s1);
        await _service.UploadAsync(_testBucket, "list/two.txt", s2);

        var blobs = (await _service.ListBlobsAsync(_testBucket, "list/")).ToList();
        blobs.Should().HaveCount(2);
        blobs.Select(b => b.Name).Should().Contain("list/one.txt").And.Contain("list/two.txt");
    }

    [Fact]
    public async Task GetPropertiesAsync_ReturnsObjectMetadata()
    {
        using var stream = new MemoryStream("props"u8.ToArray());
        await _service.UploadAsync(_testBucket, "props.txt", stream,
            new BlobUploadOptions
            {
                ContentType = "text/plain",
                Metadata = new Dictionary<string, string> { ["env"] = "test" }
            });

        var props = await _service.GetPropertiesAsync(_testBucket, "props.txt");
        props.ContentType.Should().Be("text/plain");
        props.ContentLength.Should().Be(5);
        props.Metadata.Should().ContainKey("env").WhoseValue.Should().Be("test");
    }

    [Fact]
    public async Task SetMetadataAsync_UpdatesMetadataViaCopyToSelf()
    {
        using var stream = new MemoryStream("m"u8.ToArray());
        await _service.UploadAsync(_testBucket, "meta.txt", stream);

        await _service.SetMetadataAsync(_testBucket, "meta.txt",
            new Dictionary<string, string> { ["key"] = "value" });

        var props = await _service.GetPropertiesAsync(_testBucket, "meta.txt");
        props.Metadata.Should().ContainKey("key").WhoseValue.Should().Be("value");
    }

    [Fact]
    public async Task CopyAsync_CopiesObject()
    {
        using var stream = new MemoryStream("copy"u8.ToArray());
        await _service.UploadAsync(_testBucket, "original.txt", stream);

        await _service.CopyAsync(_testBucket, "original.txt", _testBucket, "copy.txt");

        (await _service.ExistsAsync(_testBucket, "copy.txt")).Should().BeTrue();
        (await _service.ExistsAsync(_testBucket, "original.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task MoveAsync_MovesObject()
    {
        using var stream = new MemoryStream("move"u8.ToArray());
        await _service.UploadAsync(_testBucket, "src.txt", stream);

        await _service.MoveAsync(_testBucket, "src.txt", _testBucket, "dest.txt");

        (await _service.ExistsAsync(_testBucket, "dest.txt")).Should().BeTrue();
        (await _service.ExistsAsync(_testBucket, "src.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task GetPresignedDownloadUrlAsync_ReturnsValidUri()
    {
        using var stream = new MemoryStream("signed"u8.ToArray());
        await _service.UploadAsync(_testBucket, "signed.txt", stream);

        var uri = await _service.GetPresignedDownloadUrlAsync(_testBucket, "signed.txt", TimeSpan.FromMinutes(5));
        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Contain("signed.txt");
    }

    [Fact]
    public async Task GetPresignedUploadUrlAsync_ReturnsValidUri()
    {
        var uri = await _service.GetPresignedUploadUrlAsync(_testBucket, "upload.txt", TimeSpan.FromMinutes(5));
        uri.Should().NotBeNull();
        uri.AbsoluteUri.Should().Contain("upload.txt");
    }
}
```

- [ ] **Step 2: Implement AmazonS3StorageService**

```csharp
// Src/RCommon.Amazon.S3Objects/AmazonS3StorageService.cs
using Amazon.S3;
using Amazon.S3.Model;
using RCommon.Blobs;

namespace RCommon.Amazon.S3Objects;

/// <summary>
/// Amazon S3 implementation of <see cref="IBlobStorageService"/>.
/// Wraps <see cref="IAmazonS3"/> from the AWSSDK.S3 package.
/// </summary>
public class AmazonS3StorageService : IBlobStorageService
{
    private readonly IAmazonS3 _client;

    public AmazonS3StorageService(IAmazonS3 client)
    {
        _client = client;
    }

    public async Task CreateContainerAsync(string containerName, CancellationToken token = default)
    {
        await _client.PutBucketAsync(containerName, token);
    }

    public async Task DeleteContainerAsync(string containerName, CancellationToken token = default)
    {
        await _client.DeleteBucketAsync(containerName, token);
    }

    public async Task<bool> ContainerExistsAsync(string containerName, CancellationToken token = default)
    {
        try
        {
            await _client.GetBucketLocationAsync(new GetBucketLocationRequest
            {
                BucketName = containerName
            }, token);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListContainersAsync(CancellationToken token = default)
    {
        var response = await _client.ListBucketsAsync(token);
        return response.Buckets.Select(b => b.BucketName);
    }

    public async Task UploadAsync(string containerName, string blobName, Stream content,
        BlobUploadOptions? options = null, CancellationToken token = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = containerName,
            Key = blobName,
            InputStream = content
        };

        if (options?.ContentType != null)
            request.ContentType = options.ContentType;

        if (options?.Metadata != null)
        {
            foreach (var kvp in options.Metadata)
                request.Metadata.Add(kvp.Key, kvp.Value);
        }

        await _client.PutObjectAsync(request, token);
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken token = default)
    {
        var response = await _client.GetObjectAsync(containerName, blobName, token);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken token = default)
    {
        await _client.DeleteObjectAsync(containerName, blobName, token);
    }

    public async Task<bool> ExistsAsync(string containerName, string blobName, CancellationToken token = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(containerName, blobName, token);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<IEnumerable<BlobItem>> ListBlobsAsync(string containerName,
        string? prefix = null, CancellationToken token = default)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = containerName,
            Prefix = prefix
        };

        var items = new List<BlobItem>();
        ListObjectsV2Response response;
        do
        {
            response = await _client.ListObjectsV2Async(request, token);
            foreach (var obj in response.S3Objects)
            {
                items.Add(new BlobItem
                {
                    Name = obj.Key,
                    Size = obj.Size,
                    LastModified = obj.LastModified
                });
            }
            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);

        return items;
    }

    public async Task<BlobProperties> GetPropertiesAsync(string containerName, string blobName,
        CancellationToken token = default)
    {
        var response = await _client.GetObjectMetadataAsync(containerName, blobName, token);
        var metadata = new Dictionary<string, string>();
        foreach (var key in response.Metadata.Keys)
        {
            metadata[key] = response.Metadata[key];
        }

        return new BlobProperties
        {
            ContentType = response.Headers.ContentType,
            ContentLength = response.ContentLength,
            LastModified = response.LastModified,
            ETag = response.ETag,
            Metadata = metadata
        };
    }

    public async Task SetMetadataAsync(string containerName, string blobName,
        IDictionary<string, string> metadata, CancellationToken token = default)
    {
        // S3 does not support updating metadata in place — copy-to-self with REPLACE directive
        var copyRequest = new CopyObjectRequest
        {
            SourceBucket = containerName,
            SourceKey = blobName,
            DestinationBucket = containerName,
            DestinationKey = blobName,
            MetadataDirective = S3MetadataDirective.REPLACE
        };

        foreach (var kvp in metadata)
            copyRequest.Metadata.Add(kvp.Key, kvp.Value);

        await _client.CopyObjectAsync(copyRequest, token);
    }

    public async Task CopyAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default)
    {
        await _client.CopyObjectAsync(sourceContainer, sourceBlob, destContainer, destBlob, token);
    }

    public async Task MoveAsync(string sourceContainer, string sourceBlob,
        string destContainer, string destBlob, CancellationToken token = default)
    {
        await CopyAsync(sourceContainer, sourceBlob, destContainer, destBlob, token);
        await DeleteAsync(sourceContainer, sourceBlob, token);
    }

    public Task<Uri> GetPresignedDownloadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = containerName,
            Key = blobName,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(new Uri(url));
    }

    public Task<Uri> GetPresignedUploadUrlAsync(string containerName, string blobName,
        TimeSpan expiry, CancellationToken token = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = containerName,
            Key = blobName,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.PUT
        };

        var url = _client.GetPreSignedURL(request);
        return Task.FromResult(new Uri(url));
    }
}
```

- [ ] **Step 3: Run integration tests (requires LocalStack)**

Run: `dotnet test Tests/RCommon.Amazon.S3Objects.Tests/ -v minimal`
Expected: All tests PASS (if LocalStack is running)

If LocalStack is not available, verify the project builds: `dotnet build Src/RCommon.Amazon.S3Objects/`

- [ ] **Step 4: Commit**

```bash
git add Src/RCommon.Amazon.S3Objects/AmazonS3StorageService.cs Tests/RCommon.Amazon.S3Objects.Tests/AmazonS3StorageServiceTests.cs
git commit -m "feat(s3): implement AmazonS3StorageService with full IBlobStorageService"
```

---

## Task 10: Add All Projects to Solution and Final Build Verification

**Files:**
- Modify: `Src/RCommon.sln`

- [ ] **Step 1: Add remaining projects to solution**

```bash
cd Src
dotnet sln add RCommon.Azure.Blobs/RCommon.Azure.Blobs.csproj --solution-folder "Blobs"
dotnet sln add RCommon.Amazon.S3Objects/RCommon.Amazon.S3Objects.csproj --solution-folder "Blobs"
dotnet sln add ../Tests/RCommon.Azure.Blobs.Tests/RCommon.Azure.Blobs.Tests.csproj --solution-folder "Tests"
dotnet sln add ../Tests/RCommon.Amazon.S3Objects.Tests/RCommon.Amazon.S3Objects.Tests.csproj --solution-folder "Tests"
cd ..
```

- [ ] **Step 2: Full solution build**

Run: `dotnet build Src/RCommon.sln`
Expected: Build succeeded with 0 errors

- [ ] **Step 3: Run all blob-related unit tests**

Run: `dotnet test Tests/RCommon.Blobs.Tests/ -v minimal`
Expected: All 11 unit tests PASS

- [ ] **Step 4: Commit**

```bash
git add Src/RCommon.sln
git commit -m "feat(blobs): add all blob projects to solution"
```

---

## Task 11: Squash and Final Commit

- [ ] **Step 1: Rebase and squash interim commits**

```bash
git reset --soft main
git add -A
git commit -m "<message below>"
```

Squash all blob-related commits into a single commit with message:

```
feat: add blob storage abstractions with Azure and S3 implementations

Add provider-agnostic blob storage abstraction (RCommon.Blobs) with
IBlobStorageService interface covering container management, blob CRUD,
metadata, copy/move, and presigned URL generation.

Implementations:
- RCommon.Azure.Blobs: wraps Azure.Storage.Blobs SDK
- RCommon.Amazon.S3Objects: wraps AWSSDK.S3

Multi-store support via IBlobStoreFactory with name-keyed factory
delegates, enabling cross-provider scenarios.
```

- [ ] **Step 2: Verify final build after rebase**

Run: `dotnet build Src/RCommon.sln`
Expected: Build succeeded
