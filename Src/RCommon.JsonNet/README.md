# RCommon.JsonNet

Newtonsoft.Json (Json.NET) implementation of RCommon's `IJsonSerializer` abstraction. This package registers `JsonNetSerializer` into the dependency injection container and provides fluent configuration of `JsonSerializerSettings`.

## Features

- Implements `IJsonSerializer` using Newtonsoft.Json for serialization and deserialization
- Per-call options for camelCase property naming and indented formatting
- Fluent configuration of `JsonSerializerSettings` through the builder pattern
- Integrates with RCommon's `AddRCommon()` / `WithJsonSerialization<T>()` pipeline
- Registered as a transient service in the DI container

## Installation

```shell
dotnet add package RCommon.JsonNet
```

## Usage

Register the Newtonsoft.Json serializer through the RCommon builder:

```csharp
using RCommon;
using RCommon.JsonNet;

services.AddRCommon(builder =>
{
    builder.WithJsonSerialization<JsonNetBuilder>(serializer =>
    {
        serializer.Configure(settings =>
        {
            settings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            settings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
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
| `JsonNetSerializer` | `IJsonSerializer` implementation backed by Newtonsoft.Json |
| `JsonNetBuilder` | Registers `JsonNetSerializer` into the DI container |
| `IJsonNetBuilder` | Builder interface for Newtonsoft.Json-specific configuration |
| `IJsonNetBuilderExtensions` | Provides `Configure(Action<JsonSerializerSettings>)` for customizing serializer settings |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Json](https://www.nuget.org/packages/RCommon.Json) - JSON serialization abstractions (IJsonSerializer, options)
- [RCommon.SystemTextJson](https://www.nuget.org/packages/RCommon.SystemTextJson) - Alternative implementation using System.Text.Json

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
