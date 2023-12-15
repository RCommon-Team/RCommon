using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities.MongoDb
{
    public class DocumentEntity : MongoDbGenericRepository.Models.Document, IDocumentEntity
    {
        public IReadOnlyCollection<ILocalEvent> LocalEvents => throw new NotImplementedException();

        public bool AllowEventTracking { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void AddLocalEvent(ILocalEvent eventItem)
        {
            throw new NotImplementedException();
        }

        public void ClearLocalEvents()
        {
            throw new NotImplementedException();
        }

        public bool EntityEquals(IBusinessEntity other)
        {
            throw new NotImplementedException();
        }

        public object[] GetKeys()
        {
            throw new NotImplementedException();
        }

        public void RemoveLocalEvent(ILocalEvent eventItem)
        {
            throw new NotImplementedException();
        }
    }
}
