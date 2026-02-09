using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// A repository that provides SQL-mapped (micro-ORM) CRUD operations for entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type, which must implement <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// Intended for use with lightweight data mappers such as Dapper, where entities are mapped
    /// directly to database tables via the <see cref="TableName"/> property.
    /// Uses <see cref="Sql.RDbConnection"/> for database connectivity.
    /// </remarks>
    public interface ISqlMapperRepository<TEntity> : IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>
        where TEntity : IBusinessEntity
    {
        /// <summary>
        /// Gets or sets the database table name that this repository maps to.
        /// </summary>
        public string TableName { get; set; }

    }
}
