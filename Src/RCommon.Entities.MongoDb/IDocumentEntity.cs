namespace RCommon.Entities.MongoDb
{
    public interface IDocumentEntity : MongoDbGenericRepository.Models.IDocument, IBusinessEntity
    {

    }

    public interface IBusinessEntityDocument<TKey> : MongoDbGenericRepository.Models.IDocument, IBusinessEntity<TKey>
    {

    }
}
