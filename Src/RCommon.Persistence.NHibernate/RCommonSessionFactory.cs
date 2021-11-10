using NHibernate;
using RCommon.DataServices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Mapping.ByCode;
using System.Data;

namespace RCommon.Persistence.NHibernate
{
    public abstract class RCommonSessionFactory : IDataStore
    {
        private readonly ISessionFactory _sessionFactory;

       

        public void PersistChanges()
        {
            this.OpenSession().Flush();
        }

        public async Task PersistChangesAsync()
        {
            await this.OpenSession().FlushAsync();
        }

        public void Dispose()
        {
            this.OpenSession().Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            this.OpenSession().Dispose();
            await Task.CompletedTask;
        }

        

		public RCommonSessionFactory(ISessionFactory sessionFactory, INHibernateLoggerFactory loggerFactory)
		{
			NHibernateLogger.SetLoggersFactory(loggerFactory);
            this._sessionFactory = sessionFactory;
        }

		public ISession OpenSession()
		{
            return this.SessionFactory.OpenSession();
		}

        public IDbConnection GetDbConnection()
        {
            return this.OpenSession().Connection;
        }

        public ISessionFactory SessionFactory => _sessionFactory;


    }
}
