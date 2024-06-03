namespace RCommon.Persistence
{
    public interface IDataStoreFactory
    {
        IDataStore Resolve(string name);
        TDataStore Resolve<TDataStore>(string name)
            where TDataStore : IDataStore;
    }
}
