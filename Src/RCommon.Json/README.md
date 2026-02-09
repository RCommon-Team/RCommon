# RCommon.Json

Provides the JSON serialization abstraction layer for RCommon. This package defines the `IJsonSerializer` interface and configuration options, allowing your application to serialize and deserialize JSON without coupling to a specific library.

## Features

- `IJsonSerializer` interface with generic and non-generic serialize/deserialize methods
- `JsonSerializeOptions` for controlling camelCase naming and indented output
- `JsonDeserializeOptions` for controlling camelCase naming during deserialization
- `IJsonBuilder` interface for pluggable DI registration of JSON providers
- Fluent `WithJsonSerialization<T>()` extension method on `IRCommonBuilder` for easy setup

## Installation

```shell
dotnet add package RCommon.Json
```

## Usage

This package is typically not used directly. Instead, install a concrete implementation such as `RCommon.JsonNet` or `RCommon.SystemTextJson` and register it through the RCommon builder:

```csharp
using RCommon;
using RCommon.Json;

services.AddRCommon(builder =>
{
    // Use one of the concrete implementations:
    // builder.WithJsonSerialization<JsonNetBuilder>();
    // builder.WithJsonSerialization<TextJsonBuilder>();
});
```

Once registered, inject `IJsonSerializer` anywhere in your application:

```csharp
public class MyService
{
    private readonly IJsonSerializer _serializer;

    public MyService(IJsonSerializer serializer)
    {
        _serializer = serializer;
    }

    public string ToJson(Order order)
    {
        return _serializer.Serialize(order, new JsonSerializeOptions
        {
            CamelCase = true,
            Indented = true
        });
    }

    public Order FromJson(string json)
    {
        return _serializer.Deserialize<Order>(json);
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `IJsonSerializer` | Abstraction for JSON serialization and deserialization operations |
| `JsonSerializeOptions` | Options for camelCase naming and indented formatting during serialization |
| `JsonDeserializeOptions` | Options for camelCase naming during deserialization |
| `IJsonBuilder` | Builder interface for registering a JSON provider into DI |
| `JsonBuilderExtensions` | Extension methods providing `WithJsonSerialization<T>()` on `IRCommonBuilder` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.JsonNet](https://www.nuget.org/packages/RCommon.JsonNet) - Newtonsoft.Json implementation of IJsonSerializer
- [RCommon.SystemTextJson](https://www.nuget.org/packages/RCommon.SystemTextJson) - System.Text.Json implementation of IJsonSerializer

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
