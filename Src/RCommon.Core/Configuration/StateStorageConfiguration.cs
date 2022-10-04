using RCommon.StateStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public class StateStorageConfiguration : IStateStorageConfiguration
    {

        public StateStorageConfiguration()
        {

        }

        public IContextStateSelector ContextStateSelector { get; set; }
    }
}
