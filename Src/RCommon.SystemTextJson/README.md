# RCommon.SystemTextJson

System.Text.Json implementation of RCommon's `IJsonSerializer` abstraction. This package registers `TextJsonSerializer` into the dependency injection container and provides fluent configuration of `JsonSerializerOptions`, along with custom enum converters.

## Features

- Implements `IJsonSerializer` using the built-in System.Text.Json library
- Per-call options for camelCase property naming and indented formatting
- Fluent configuration of `JsonSerializerOptions` through the builder pattern
- Custom `JsonByteEnumConverter<T>` for serializing enums as byte values
- Custom `JsonIntEnumConverter<T>` for serializing enums as int values
- Integrates with RCommon's `AddRCommon()` / `WithJsonSerialization<T>()` pipeline
- Registered as a transient service in the DI container

## Installation

```shell
dotnet add package RCommon.SystemTextJson
```

## Usage

Register the System.Text.Json serializer through the RCommon builder:

```csharp
using RCommon;
using RCommon.SystemTextJson;

services.AddRCommon(builder =>
{
    builder.WithJsonSerialization<TextJsonBuilder>(serializer =>
    {
        serializer.Configure(options =>
        {
            options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.Converters.Add(new JsonIntEnumConverter<MyEnum>());
        });
    });
});
```

Then inject and use `IJsonSerializer` in your services:

```csharp
public class OrderService
{
    private readonly IJsonSerializer _serializer;

    public OrderService(IJsonSerializer serializer)
    {
        _serializer = serializer;
    }

    public string SerializeOrder(Order order)
    {
        return _serializer.Serialize(order);
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `TextJsonSerializer` | `IJsonSerializer` implementation backed by System.Text.Json |
| `TextJsonBuilder` | Registers `TextJsonSerializer` into the DI container |
| `ITextJsonBuilder` | Builder interface for System.Text.Json-specific configuration |
| `ITextJsonBuilderExtensions` | Provides `Configure(Action<JsonSerializerOptions>)` for customizing serializer options |
| `JsonIntEnumConverter<T>` | Custom converter that serializes enums as their int numeric value |
| `JsonByteEnumConverter<T>` | Custom converter that serializes enums as their byte numeric value |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Json](https://www.nuget.org/packages/RCommon.Json) - JSON serialization abstractions (IJsonSerializer, options)
- [RCommon.JsonNet](https://www.nuget.org/packages/RCommon.JsonNet) - Alternative implementation using Newtonsoft.Json

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
