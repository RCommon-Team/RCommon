namespace RCommon.Persistence
{
    public interface IDataStoreRegistry
    {
        TDataStore GetDataStore<TDataStore>(string dataStoreName) where TDataStore : IDataStore;
        IDataStore GetDataStore(string dataStoreName);
        void RegisterDataStore<TDataStore>(TDataStore dataStore, string dataStoreName) where TDataStore : IDataStore;
        void RemoveRegisteredDataStore(string dataStoreName);
    }
}
