# RCommon.Models

Shared model interfaces and base types for the RCommon framework, providing CQRS command and query contracts, event marker interfaces for sync/async dispatch, pagination models, and execution result types for conveying operation outcomes.

## Features

- `ICommand` and `ICommand<TResult>` marker interfaces for CQRS command segregation
- `IQuery` and `IQuery<TResult>` marker interfaces for CQRS query segregation
- `ISerializableEvent`, `ISyncEvent`, and `IAsyncEvent` marker interfaces for controlling event dispatch behavior
- `CommandResult<TExecutionResult>` record for wrapping command handler outcomes
- `ExecutionResult` with static factory methods for `Success()` and `Failed(errors)` using cached singletons
- `PaginatedListModel<TSource>` and `PaginatedListModel<TSource, TOut>` abstract records for building paginated DTOs with built-in page calculation
- `PaginatedListRequest` and `SearchPaginatedListRequest` for encapsulating paging, sorting, and search parameters
- `SortDirectionEnum` for specifying ascending, descending, or no-sort ordering
- All models are decorated with `DataContract`/`DataMember` attributes for serialization support

## Installation

```shell
dotnet add package RCommon.Models
```

## Usage

```csharp
using RCommon.Models.Commands;
using RCommon.Models.Queries;
using RCommon.Models.Events;
using RCommon.Models.ExecutionResults;

// Define a command
public class CreateOrderCommand : ICommand<IExecutionResult>
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}

// Define an event for async dispatch
public class OrderCreatedEvent : IAsyncEvent
{
    public Guid OrderId { get; set; }
}

// Return execution results from handlers
IExecutionResult result = ExecutionResult.Success();
IExecutionResult failure = ExecutionResult.Failed("Insufficient inventory", "Payment declined");

// Use paginated requests
var request = new SearchPaginatedListRequest
{
    PageNumber = 1,
    PageSize = 25,
    SortBy = "CreatedDate",
    SortDirection = SortDirectionEnum.Descending,
    SearchString = "widget"
};
```

## Key Types

| Type | Description |
|------|-------------|
| `IModel` | Base marker interface for all RCommon models |
| `ICommand` / `ICommand<TResult>` | Marker interfaces for CQRS commands, optionally specifying a result type |
| `IQuery` / `IQuery<TResult>` | Marker interfaces for CQRS queries, optionally specifying a result type |
| `ISerializableEvent` | Marker interface for events that can be serialized across process boundaries |
| `ISyncEvent` | Marker for events dispatched and handled synchronously within the same process |
| `IAsyncEvent` | Marker for events dispatched asynchronously via a message bus or queue |
| `IExecutionResult` | Represents the outcome of an operation with an `IsSuccess` flag |
| `ExecutionResult` | Abstract base with `Success()` and `Failed()` factory methods |
| `CommandResult<TExecutionResult>` | Wraps an execution result returned from a command handler |
| `PaginatedListModel<TSource>` | Abstract paginated DTO with page size, count, and navigation properties |
| `PaginatedListRequest` | Base request with page number, page size, sort field, and sort direction |
| `SearchPaginatedListRequest` | Extends `PaginatedListRequest` with a free-text `SearchString` property |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Foundation package with event bus, builder pattern, guards, and extensions
- [RCommon.Entities](https://www.nuget.org/packages/RCommon.Entities) - Domain entity base classes with auditing and transactional event tracking

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
