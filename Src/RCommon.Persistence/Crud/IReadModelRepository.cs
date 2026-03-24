using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using RCommon;
using RCommon.Models;

namespace RCommon.Persistence.Crud;

public interface IReadModelRepository<TReadModel> : INamedDataSource
    where TReadModel : class, IReadModel
{
    Task<TReadModel?> FindAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TReadModel>> FindAllAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<IPagedResult<TReadModel>> GetPagedAsync(
        IPagedSpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<long> GetCountAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    IReadModelRepository<TReadModel> Include<TProperty>(
        Expression<Func<TReadModel, TProperty>> path);
}
