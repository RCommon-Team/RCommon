using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities.Document
{
    public class PartitionedDocumentEntity : MongoDbGenericRepository.Models.PartitionedDocument
    {
        public PartitionedDocumentEntity(string partitionKey) : base(partitionKey)
        {
        }
    }
}
