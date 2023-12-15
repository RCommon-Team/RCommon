namespace RCommon.Persistence.MongoDb
{
    public class MongoDbRepository : MongoDbGenericRepository.BaseMongoRepository
    {
        public MongoDbRepository(string connectionString, string databaseName = null) 
            : base(connectionString, databaseName)
        {
        }
    }
}
