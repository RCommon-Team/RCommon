using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Caching;
using RCommon.Json;
using RCommon.RedisCache;
using System.Text;
using Xunit;

namespace RCommon.RedisCache.Tests;

public class RedisCacheServiceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();

        // Act
        var act = () => new RedisCacheService(mockDistributedCache.Object, mockJsonSerializer.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_CreatesInstanceOfICacheService()
    {
        // Arrange
        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();

        // Act
        var service = new RedisCacheService(mockDistributedCache.Object, mockJsonSerializer.Object);

        // Assert
        service.Should().BeAssignableTo<ICacheService>();
    }

    #endregion

    #region GetOrCreate Tests with Real MemoryDistributedCache

    [Fact]
    public void GetOrCreate_WhenCacheMiss_ReturnsDataFromFactory()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";
        var expectedValue = "test-value";
        var serializedValue = "\"test-value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(expectedValue, null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate(key, () => expectedValue);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetOrCreate_WhenCacheMiss_StoresValueInCache()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";
        var expectedValue = "test-value";
        var serializedValue = "\"test-value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(expectedValue, null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        service.GetOrCreate(key, () => expectedValue);

        // Assert - verify the value was stored in cache
        var cachedBytes = distributedCache.Get(key);
        cachedBytes.Should().NotBeNull();
        Encoding.UTF8.GetString(cachedBytes!).Should().Be(serializedValue);
    }

    [Fact]
    public void GetOrCreate_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";
        var cachedJson = "\"cached-value\"";
        var expectedValue = "cached-value";

        // Pre-populate cache
        distributedCache.SetString(key, cachedJson);

        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(cachedJson, null))
            .Returns(expectedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate(key, () => "new-value");

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void GetOrCreate_WhenCacheHit_DoesNotCallFactory()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";
        var cachedJson = "\"cached-value\"";
        var factoryCalled = false;

        // Pre-populate cache
        distributedCache.SetString(key, cachedJson);

        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(cachedJson, null))
            .Returns("cached-value");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        service.GetOrCreate(key, () => { factoryCalled = true; return "new-value"; });

        // Assert
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public void GetOrCreate_WithComplexType_SerializesCorrectly()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "complex-key";
        var expectedValue = new TestData { Id = 1, Name = "Test", IsActive = true };
        var serializedValue = "{\"Id\":1,\"Name\":\"Test\",\"IsActive\":true}";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<TestData>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate(key, () => expectedValue);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
        mockJsonSerializer.Verify(x => x.Serialize(It.IsAny<TestData>(), null), Times.Once);
    }

    [Fact]
    public void GetOrCreate_UsesToStringOnKey()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = 12345;
        var expectedKeyString = "12345";
        var serializedValue = "\"value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        service.GetOrCreate(key, () => "value");

        // Assert - verify the value was stored with the string key
        var cachedBytes = distributedCache.Get(expectedKeyString);
        cachedBytes.Should().NotBeNull();
    }

    [Fact]
    public void GetOrCreate_WithDifferentKeys_ReturnsDifferentValues()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key1 = "key1";
        var key2 = "key2";
        var value1 = "value1";
        var value2 = "value2";

        mockJsonSerializer
            .Setup(x => x.Serialize(value1, null))
            .Returns("\"value1\"");
        mockJsonSerializer
            .Setup(x => x.Serialize(value2, null))
            .Returns("\"value2\"");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result1 = service.GetOrCreate(key1, () => value1);
        var result2 = service.GetOrCreate(key2, () => value2);

        // Assert
        result1.Should().Be(value1);
        result2.Should().Be(value2);
    }

    [Fact]
    public void GetOrCreate_FactoryNotCalledOnSubsequentCalls_ForSameKey()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";
        var callCount = 0;
        var serializedValue = "\"value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);
        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(serializedValue, null))
            .Returns("value");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act - First call triggers factory (note: implementation calls factory twice on cache miss)
        service.GetOrCreate(key, () => { callCount++; return "value"; });
        var callCountAfterFirstCall = callCount;

        // Subsequent calls should return cached value without calling factory
        service.GetOrCreate(key, () => { callCount++; return "value"; });
        service.GetOrCreate(key, () => { callCount++; return "value"; });

        // Assert - factory should not be called after first cache population
        callCount.Should().Be(callCountAfterFirstCall);
    }

    [Fact]
    public void GetOrCreate_WithGuidKey_Works()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = Guid.NewGuid();
        var serializedValue = "\"value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate(key, () => "value");

        // Assert
        result.Should().Be("value");
    }

    [Fact]
    public void GetOrCreate_WithComplexObjectKey_UsesToString()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = new CustomKeyObject { Id = 42 };
        var serializedValue = "\"value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate(key, () => "value");

        // Assert
        result.Should().Be("value");
        var cachedBytes = distributedCache.Get(key.ToString()!);
        cachedBytes.Should().NotBeNull();
    }

    #endregion

    #region GetOrCreateAsync Tests with Real MemoryDistributedCache

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_ReturnsDataFromFactory()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-test-key";
        var expectedValue = "async-test-value";
        var serializedValue = "\"async-test-value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(expectedValue, null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = await service.GetOrCreateAsync(key, () => expectedValue);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_StoresValueInCache()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-test-key";
        var expectedValue = "async-test-value";
        var serializedValue = "\"async-test-value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(expectedValue, null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        await service.GetOrCreateAsync(key, () => expectedValue);

        // Assert - verify the value was stored in cache
        var cachedBytes = await distributedCache.GetAsync(key);
        cachedBytes.Should().NotBeNull();
        Encoding.UTF8.GetString(cachedBytes!).Should().Be(serializedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-test-key";
        var cachedJson = "\"cached-async-value\"";
        var expectedValue = "cached-async-value";

        // Pre-populate cache
        await distributedCache.SetStringAsync(key, cachedJson);

        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(cachedJson, null))
            .Returns(expectedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = await service.GetOrCreateAsync(key, () => "new-async-value");

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheHit_DoesNotCallFactory()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-test-key";
        var cachedJson = "\"cached-async-value\"";
        var factoryCalled = false;

        // Pre-populate cache
        await distributedCache.SetStringAsync(key, cachedJson);

        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(cachedJson, null))
            .Returns("cached-async-value");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        await service.GetOrCreateAsync(key, () => { factoryCalled = true; return "new-async-value"; });

        // Assert
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateAsync_WithComplexType_SerializesCorrectly()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-complex-key";
        var expectedValue = new TestData { Id = 2, Name = "AsyncTest", IsActive = false };
        var serializedValue = "{\"Id\":2,\"Name\":\"AsyncTest\",\"IsActive\":false}";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<TestData>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = await service.GetOrCreateAsync(key, () => expectedValue);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
        mockJsonSerializer.Verify(x => x.Serialize(It.IsAny<TestData>(), null), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_UsesToStringOnKey()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = 98765;
        var expectedKeyString = "98765";
        var serializedValue = "\"value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        await service.GetOrCreateAsync(key, () => "value");

        // Assert - verify the value was stored with the string key
        var cachedBytes = await distributedCache.GetAsync(expectedKeyString);
        cachedBytes.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrCreateAsync_WithDifferentKeys_ReturnsDifferentValues()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key1 = "async-key1";
        var key2 = "async-key2";
        var value1 = "async-value1";
        var value2 = "async-value2";

        mockJsonSerializer
            .Setup(x => x.Serialize(value1, null))
            .Returns("\"async-value1\"");
        mockJsonSerializer
            .Setup(x => x.Serialize(value2, null))
            .Returns("\"async-value2\"");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result1 = await service.GetOrCreateAsync(key1, () => value1);
        var result2 = await service.GetOrCreateAsync(key2, () => value2);

        // Assert
        result1.Should().Be(value1);
        result2.Should().Be(value2);
    }

    [Fact]
    public async Task GetOrCreateAsync_FactoryNotCalledOnSubsequentCalls_ForSameKey()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-test-key";
        var callCount = 0;
        var serializedValue = "\"value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);
        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(serializedValue, null))
            .Returns("value");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act - First call triggers factory (note: implementation calls factory twice on cache miss)
        await service.GetOrCreateAsync(key, () => { callCount++; return "value"; });
        var callCountAfterFirstCall = callCount;

        // Subsequent calls should return cached value without calling factory
        await service.GetOrCreateAsync(key, () => { callCount++; return "value"; });
        await service.GetOrCreateAsync(key, () => { callCount++; return "value"; });

        // Assert - factory should not be called after first cache population
        callCount.Should().Be(callCountAfterFirstCall);
    }

    [Fact]
    public async Task GetOrCreateAsync_RunsConcurrently_WithoutErrors()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns<object, JsonSerializeOptions?>((obj, _) => $"\"{obj}\"");
        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(It.IsAny<string>(), null))
            .Returns<string, JsonDeserializeOptions?>((json, _) => json.Trim('"'));

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);
        var tasks = new List<Task<string>>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var value = $"value-{i}";
            var key = $"concurrent-key-{i % 10}";
            tasks.Add(service.GetOrCreateAsync(key, () => value));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    [Fact]
    public async Task GetOrCreateAsync_WithGuidKey_Works()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = Guid.NewGuid();
        var serializedValue = "\"value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = await service.GetOrCreateAsync(key, () => "value");

        // Assert
        result.Should().Be("value");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void GetOrCreate_WhenFactoryReturnsNull_StoresNullInCache()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "null-key";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns("null");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate<string?>(key, () => null);

        // Assert
        result.Should().BeNull();
        var cachedBytes = distributedCache.Get(key);
        cachedBytes.Should().NotBeNull();
        Encoding.UTF8.GetString(cachedBytes!).Should().Be("null");
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenFactoryReturnsNull_StoresNullInCache()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-null-key";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns("null");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = await service.GetOrCreateAsync<string?>(key, () => null);

        // Assert
        result.Should().BeNull();
        var cachedBytes = await distributedCache.GetAsync(key);
        cachedBytes.Should().NotBeNull();
        Encoding.UTF8.GetString(cachedBytes!).Should().Be("null");
    }

    [Fact]
    public void GetOrCreate_WithEmptyStringKey_Works()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "";
        var serializedValue = "\"value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate(key, () => "value");

        // Assert
        result.Should().Be("value");
    }

    [Fact]
    public void GetOrCreate_WithEmptyStringValue_Works()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "empty-value-key";
        var serializedValue = "\"\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(string.Empty, null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate(key, () => string.Empty);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void GetOrCreate_WithListReturnType_Works()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "list-key";
        var expectedValue = new List<int> { 1, 2, 3, 4, 5 };
        var serializedValue = "[1,2,3,4,5]";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<List<int>>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = service.GetOrCreate(key, () => expectedValue);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithListReturnType_Works()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-list-key";
        var expectedValue = new List<string> { "a", "b", "c" };
        var serializedValue = "[\"a\",\"b\",\"c\"]";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<List<string>>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        var result = await service.GetOrCreateAsync(key, () => expectedValue);

        // Assert
        result.Should().BeEquivalentTo(expectedValue);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task GetOrCreate_AndGetOrCreateAsync_ShareSameCache()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "shared-key";
        var syncValue = "sync-value";
        var serializedValue = "\"sync-value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(syncValue, null))
            .Returns(serializedValue);
        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(serializedValue, null))
            .Returns(syncValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

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
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "shared-key-2";
        var asyncValue = "async-value";
        var serializedValue = "\"async-value\"";

        mockJsonSerializer
            .Setup(x => x.Serialize(asyncValue, null))
            .Returns(serializedValue);
        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(serializedValue, null))
            .Returns(asyncValue);

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act - populate cache asynchronously
        await service.GetOrCreateAsync(key, () => asyncValue);

        // Assert - sync should return cached value
        var syncResult = service.GetOrCreate(key, () => "sync-value");
        syncResult.Should().Be(asyncValue);
    }

    #endregion

    #region Mock-Based Tests

    [Fact]
    public void GetOrCreate_CallsDistributedCache_GetString()
    {
        // Arrange
        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";

        mockDistributedCache
            .Setup(x => x.Get(key))
            .Returns((byte[]?)null);
        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns("\"value\"");

        var service = new RedisCacheService(mockDistributedCache.Object, mockJsonSerializer.Object);

        // Act
        service.GetOrCreate(key, () => "value");

        // Assert
        mockDistributedCache.Verify(x => x.Get(key), Times.Once);
    }

    [Fact]
    public void GetOrCreate_WhenCacheMiss_CallsDistributedCache_SetString()
    {
        // Arrange
        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";
        var serializedValue = "\"value\"";

        mockDistributedCache
            .Setup(x => x.Get(key))
            .Returns((byte[]?)null);
        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(mockDistributedCache.Object, mockJsonSerializer.Object);

        // Act
        service.GetOrCreate(key, () => "value");

        // Assert
        mockDistributedCache.Verify(x => x.Set(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_CallsDistributedCache_GetStringAsync()
    {
        // Arrange
        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-test-key";

        mockDistributedCache
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns("\"value\"");

        var service = new RedisCacheService(mockDistributedCache.Object, mockJsonSerializer.Object);

        // Act
        await service.GetOrCreateAsync(key, () => "value");

        // Assert
        mockDistributedCache.Verify(x => x.GetAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_CallsDistributedCache_SetStringAsync()
    {
        // Arrange
        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "async-test-key";
        var serializedValue = "\"value\"";

        mockDistributedCache
            .Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedValue);

        var service = new RedisCacheService(mockDistributedCache.Object, mockJsonSerializer.Object);

        // Act
        await service.GetOrCreateAsync(key, () => "value");

        // Assert
        mockDistributedCache.Verify(x => x.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetOrCreate_CallsJsonSerializer_Serialize()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";
        var value = "test-value";

        mockJsonSerializer
            .Setup(x => x.Serialize(value, null))
            .Returns("\"test-value\"");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        service.GetOrCreate(key, () => value);

        // Assert
        mockJsonSerializer.Verify(x => x.Serialize(value, null), Times.Once);
    }

    [Fact]
    public void GetOrCreate_WhenCacheHit_CallsJsonSerializer_Deserialize()
    {
        // Arrange
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        var key = "test-key";
        var cachedJson = "\"cached-value\"";

        // Pre-populate cache
        distributedCache.SetString(key, cachedJson);

        mockJsonSerializer
            .Setup(x => x.Deserialize<string>(cachedJson, null))
            .Returns("cached-value");

        var service = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        // Act
        service.GetOrCreate(key, () => "new-value");

        // Assert
        mockJsonSerializer.Verify(x => x.Deserialize<string>(cachedJson, null), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static MemoryDistributedCache CreateMemoryDistributedCache()
    {
        var options = Options.Create(new MemoryDistributedCacheOptions());
        return new MemoryDistributedCache(options);
    }

    #endregion

    #region Test Helper Classes

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private class CustomKeyObject
    {
        public int Id { get; set; }

        public override string ToString()
        {
            return $"CustomKey_{Id}";
        }
    }

    #endregion
}
