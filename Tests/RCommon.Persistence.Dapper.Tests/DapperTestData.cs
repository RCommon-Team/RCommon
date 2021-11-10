using RCommon.DataServices.Sql;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Dapper.Tests
{
    public class DapperTestData : DisposableResource
    {
        readonly RDbConnection _context;
        readonly IList<Action<RDbConnection>> _entityDeleteActions;

        public DapperTestData(RDbConnection context)
        {
            _context = context;
            _entityDeleteActions = new List<Action<RDbConnection>>();
        }

        public RDbConnection Context
        {
            get { return _context; }
        }

        public IList<Action<RDbConnection>> EntityDeleteActions
        {
            get { return _entityDeleteActions; }
        }

        public void ResetContext()
        {
            this.ExecuteNonQueryCommand($"DELETE OrderItems");
            this.ExecuteNonQueryCommand($"DELETE Products");
            this.ExecuteNonQueryCommand($"DELETE Orders");
            this.ExecuteNonQueryCommand($"DELETE Customers");
        }

        private void ExecuteNonQueryCommand(string sql)
        {
            using (var connection = _context.GetDbConnection())
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();

                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
                
            }
            
        }

        protected override void Dispose(bool disposing)
        {
            if (_entityDeleteActions.Count <= 0)


            _entityDeleteActions.ForEach(x => x(_context));
            _context.Dispose();
        }
    }
}
