namespace RCommon.Persistence;

/// <summary>
/// Marker interface for read-model/projection types used in CQRS query-side repositories.
/// Read models are optimized for querying and do not participate in domain event tracking.
/// </summary>
public interface IReadModel { }
