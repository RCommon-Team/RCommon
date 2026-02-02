using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RCommon.Caching;
using RCommon.MemoryCache;
using Xunit;
using MsMemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;

namespace RCommon.MemoryCache.Tests;

public class InMemoryCacheServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidMemoryCache_DoesNotThrow()
    {
        // Arrange
        var mockMemoryCache = new Mock<IMemoryCache>();

        // Act
        var act = () => new InMemoryCacheService(mockMemoryCache.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_CreatesInstanceOfICacheService()
    {
        // Arrange
        var mockMemoryCache = new Mock<IMemoryCache>();

        // Act
        var service = new InMemoryCacheService(mockMemoryCache.Object);

        // Assert
        service.Should().BeAssignableTo<ICacheService>();
    }

    #endregion

    #region GetOrCreate Tests

    [Fact]
    public void GetOrCreate_WhenCacheMiss_ReturnsDataFromFactory()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "test-key";
        var expectedValue = "test-value";

        // Act
        var result = service.GetOrCreate(key, () => expectedValue);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetOrCreate_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "test-key";
        var firstValue = "first-value";
        var secondValue = "second-value";

        // First call to populate cache
        service.GetOrCreate(key, () => firstValue);

        // Act - second call should return cached value
        var result = service.GetOrCreate(key, () => secondValue);

        // Assert
        result.Should().Be(firstValue);
    }

    [Fact]
    public void GetOrCreate_WithDifferentKeys_ReturnsDifferentValues()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key1 = "key1";
        var key2 = "key2";
        var value1 = "value1";
        var value2 = "value2";

        // Act
        var result1 = service.GetOrCreate(key1, () => value1);
        var result2 = service.GetOrCreate(key2, () => value2);

        // Assert
        result1.Should().Be(value1);
        result2.Should().Be(value2);
    }

    [Fact]
    public void GetOrCreate_WithIntegerKey_Works()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = 12345;
        var expectedValue = "test-value";

        // Act
        var result = service.GetOrCreate(key, () => expectedValue);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetOrCreate_WithComplexObjectKey_Works()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = new { Id = 1, Name = "test" };
        var expectedValue = "test-value";

        // Act
        var result = service.GetOrCreate(key, () => expectedValue);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetOrCreate_WithComplexReturnType_Works()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "complex-key";
        var expectedValue = new TestData { Id = 1, Name = "Test", IsActive = true };

        // Act
        var result = service.GetOrCreate(key, () => expectedValue);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
    }

    [Fact]
    public void GetOrCreate_FactoryCalledOnlyOnce_ForSameKey()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "test-key";
        var callCount = 0;

        // Act
        service.GetOrCreate(key, () => { callCount++; return "value"; });
        service.GetOrCreate(key, () => { callCount++; return "value"; });
        service.GetOrCreate(key, () => { callCount++; return "value"; });

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void GetOrCreate_WithNullReturnValue_CachesNull()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "null-key";
        var callCount = 0;

        // Act
        var result1 = service.GetOrCreate<string?>(key, () => { callCount++; return null; });
        var result2 = service.GetOrCreate<string?>(key, () => { callCount++; return "not-null"; });

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull(); // Should still be null from cache
        callCount.Should().Be(1);
    }

    #endregion

    #region GetOrCreateAsync Tests

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_ReturnsDataFromFactory()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "test-key";
        var expectedValue = "test-value";

        // Act
        var result = await service.GetOrCreateAsync(key, () => expectedValue);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "test-key";
        var firstValue = "first-value";
        var secondValue = "second-value";

        // First call to populate cache
        await service.GetOrCreateAsync(key, () => firstValue);

        // Act - second call should return cached value
        var result = await service.GetOrCreateAsync(key, () => secondValue);

        // Assert
        result.Should().Be(firstValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithDifferentKeys_ReturnsDifferentValues()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key1 = "async-key1";
        var key2 = "async-key2";
        var value1 = "async-value1";
        var value2 = "async-value2";

        // Act
        var result1 = await service.GetOrCreateAsync(key1, () => value1);
        var result2 = await service.GetOrCreateAsync(key2, () => value2);

        // Assert
        result1.Should().Be(value1);
        result2.Should().Be(value2);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithComplexReturnType_Works()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "complex-async-key";
        var expectedValue = new TestData { Id = 2, Name = "AsyncTest", IsActive = false };

        // Act
        var result = await service.GetOrCreateAsync(key, () => expectedValue);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_FactoryCalledOnlyOnce_ForSameKey()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "async-test-key";
        var callCount = 0;

        // Act
        await service.GetOrCreateAsync(key, () => { callCount++; return "value"; });
        await service.GetOrCreateAsync(key, () => { callCount++; return "value"; });
        await service.GetOrCreateAsync(key, () => { callCount++; return "value"; });

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithNullReturnValue_CachesNull()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "async-null-key";
        var callCount = 0;

        // Act
        var result1 = await service.GetOrCreateAsync<string?>(key, () => { callCount++; return null; });
        var result2 = await service.GetOrCreateAsync<string?>(key, () => { callCount++; return "not-null"; });

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull(); // Should still be null from cache
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_RunsConcurrently_WithoutErrors()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var tasks = new List<Task<string>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var key = $"concurrent-key-{i % 10}";
            tasks.Add(service.GetOrCreateAsync(key, () => $"value-{i}"));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task GetOrCreate_AndGetOrCreateAsync_ShareSameCache()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "shared-key";
        var syncValue = "sync-value";

        // Act - populate cache synchronously
        service.GetOrCreate(key, () => syncValue);

        // Assert - async should return cached value
        var asyncResult = await service.GetOrCreateAsync(key, () => "async-value");
        asyncResult.Should().Be(syncValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_AndGetOrCreate_ShareSameCache()
    {
        // Arrange
        var memoryCache = new MsMemoryCache(new MemoryCacheOptions());
        var service = new InMemoryCacheService(memoryCache);
        var key = "shared-key-2";
        var asyncValue = "async-value";

        // Act - populate cache asynchronously
        await service.GetOrCreateAsync(key, () => asyncValue);

        // Assert - sync should return cached value
        var syncResult = service.GetOrCreate(key, () => "sync-value");
        syncResult.Should().Be(asyncValue);
    }

    #endregion

    #region Test Helper Classes

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    #endregion
}
