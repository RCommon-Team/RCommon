using LinqToDB.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db
{
    public class Linq2DbOptions
    {
        public Linq2DbOptions()
        {

        }

        public LinqToDBConnectionOptions Settings { get; set; }
    }
}
