using RCommon.BusinessEntities;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface ILinqRepository<TEntity>: IQueryable<TEntity>, IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>
        where TEntity : IBusinessEntity
    {
        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);
    }
}
