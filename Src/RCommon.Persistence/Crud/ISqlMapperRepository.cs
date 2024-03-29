﻿using RCommon.Entities;
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
    public interface ISqlMapperRepository<TEntity> : IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>
        where TEntity : IBusinessEntity
    {
        public string TableName { get; set; }
        
    }
}
