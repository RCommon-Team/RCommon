using FluentAssertions;
using Moq;
using Xunit;

namespace RCommon.Core.Tests;

public class CommonFactoryTests
{
    #region CommonFactory<TResult> Tests

    [Fact]
    public void CommonFactory_Create_ReturnsResultFromInitFunc()
    {
        // Arrange
        var expectedResult = new TestService();
        var factory = new CommonFactory<TestService>(() => expectedResult);

        // Act
        var result = factory.Create();

        // Assert
        result.Should().BeSameAs(expectedResult);
    }

    [Fact]
    public void CommonFactory_Create_CallsInitFuncEachTime()
    {
        // Arrange
        var callCount = 0;
        var factory = new CommonFactory<TestService>(() =>
        {
            callCount++;
            return new TestService();
        });

        // Act
        factory.Create();
        factory.Create();
        factory.Create();

        // Assert
        callCount.Should().Be(3);
    }

    [Fact]
    public void CommonFactory_Create_WithCustomize_AppliesCustomization()
    {
        // Arrange
        var factory = new CommonFactory<TestService>(() => new TestService());

        // Act
        var result = factory.Create(service => service.Name = "Customized");

        // Assert
        result.Name.Should().Be("Customized");
    }

    [Fact]
    public void CommonFactory_Create_WithCustomize_ReturnsConfiguredObject()
    {
        // Arrange
        var factory = new CommonFactory<TestService>(() => new TestService { Name = "Initial" });

        // Act
        var result = factory.Create(service =>
        {
            service.Name = "Modified";
            service.Value = 42;
        });

        // Assert
        result.Name.Should().Be("Modified");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void CommonFactory_ImplementsICommonFactory()
    {
        // Arrange & Act
        var factory = new CommonFactory<TestService>(() => new TestService());

        // Assert
        factory.Should().BeAssignableTo<ICommonFactory<TestService>>();
    }

    #endregion

    #region CommonFactory<T, TResult> Tests

    [Fact]
    public void CommonFactoryWithArg_Create_PassesArgumentToInitFunc()
    {
        // Arrange
        string? capturedArg = null;
        var factory = new CommonFactory<string, TestService>(arg =>
        {
            capturedArg = arg;
            return new TestService { Name = arg };
        });

        // Act
        var result = factory.Create("TestArgument");

        // Assert
        capturedArg.Should().Be("TestArgument");
        result.Name.Should().Be("TestArgument");
    }

    [Fact]
    public void CommonFactoryWithArg_Create_ReturnsResultFromInitFunc()
    {
        // Arrange
        var factory = new CommonFactory<int, TestService>(value => new TestService { Value = value });

        // Act
        var result = factory.Create(100);

        // Assert
        result.Value.Should().Be(100);
    }

    [Fact]
    public void CommonFactoryWithArg_Create_WithCustomize_AppliesCustomization()
    {
        // Arrange
        var factory = new CommonFactory<string, TestService>(name => new TestService { Name = name });

        // Act
        var result = factory.Create("InitialName", service => service.Value = 50);

        // Assert
        result.Name.Should().Be("InitialName");
        result.Value.Should().Be(50);
    }

    [Fact]
    public void CommonFactoryWithArg_ImplementsICommonFactory()
    {
        // Arrange & Act
        var factory = new CommonFactory<string, TestService>(arg => new TestService());

        // Assert
        factory.Should().BeAssignableTo<ICommonFactory<string, TestService>>();
    }

    #endregion

    #region CommonFactory<T, T2, TResult> Tests

    [Fact]
    public void CommonFactoryWithTwoArgs_Create_PassesBothArgumentsToInitFunc()
    {
        // Arrange
        string? capturedArg1 = null;
        int capturedArg2 = 0;
        var factory = new CommonFactory<string, int, TestService>((arg1, arg2) =>
        {
            capturedArg1 = arg1;
            capturedArg2 = arg2;
            return new TestService { Name = arg1, Value = arg2 };
        });

        // Act
        var result = factory.Create("TestName", 42);

        // Assert
        capturedArg1.Should().Be("TestName");
        capturedArg2.Should().Be(42);
        result.Name.Should().Be("TestName");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void CommonFactoryWithTwoArgs_Create_ReturnsResultFromInitFunc()
    {
        // Arrange
        var factory = new CommonFactory<string, bool, TestService>((name, isActive) =>
            new TestService { Name = name, IsActive = isActive });

        // Act
        var result = factory.Create("Service1", true);

        // Assert
        result.Name.Should().Be("Service1");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CommonFactoryWithTwoArgs_Create_WithCustomize_AppliesCustomization()
    {
        // Arrange
        var factory = new CommonFactory<string, int, TestService>((name, value) =>
            new TestService { Name = name, Value = value });

        // Act
        var result = factory.Create("InitialName", 10, service =>
        {
            service.IsActive = true;
            service.Value = service.Value * 2;
        });

        // Assert
        result.Name.Should().Be("InitialName");
        result.Value.Should().Be(20);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CommonFactoryWithTwoArgs_ImplementsICommonFactory()
    {
        // Arrange & Act
        var factory = new CommonFactory<string, int, TestService>((arg1, arg2) => new TestService());

        // Assert
        factory.Should().BeAssignableTo<ICommonFactory<string, int, TestService>>();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void CommonFactory_Create_WithNullReturningInitFunc_ReturnsNull()
    {
        // Arrange
        var factory = new CommonFactory<TestService?>(() => null);

        // Act
        var result = factory.Create();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CommonFactory_Create_WithValueType_WorksCorrectly()
    {
        // Arrange
        var factory = new CommonFactory<int>(() => 42);

        // Act
        var result = factory.Create();

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void CommonFactoryWithArg_Create_WithNullArgument_PassesNull()
    {
        // Arrange
        string? capturedArg = "not null";
        var factory = new CommonFactory<string?, TestService>(arg =>
        {
            capturedArg = arg;
            return new TestService { Name = arg ?? "default" };
        });

        // Act
        var result = factory.Create(null);

        // Assert
        capturedArg.Should().BeNull();
        result.Name.Should().Be("default");
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void CommonFactory_Create_WithComplexInitialization_WorksCorrectly()
    {
        // Arrange
        var factory = new CommonFactory<ComplexService>(() =>
        {
            var service = new ComplexService();
            service.Initialize();
            return service;
        });

        // Act
        var result = factory.Create();

        // Assert
        result.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void CommonFactory_Create_MultipleCustomizations_ApplyInOrder()
    {
        // Arrange
        var factory = new CommonFactory<TestService>(() => new TestService { Value = 1 });

        // Act
        var result = factory.Create(service =>
        {
            service.Value = service.Value + 10;
            service.Name = $"Value_{service.Value}";
        });

        // Assert
        result.Value.Should().Be(11);
        result.Name.Should().Be("Value_11");
    }

    #endregion

    #region Test Helper Classes

    public class TestService
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public bool IsActive { get; set; }
    }

    public class ComplexService
    {
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            IsInitialized = true;
        }
    }

    #endregion
}
