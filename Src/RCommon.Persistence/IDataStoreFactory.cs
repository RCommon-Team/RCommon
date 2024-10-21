namespace RCommon.Persistence
{
    /// <summary>
    /// Abstraction for allowing repositories to find Data Stores
    /// </summary>
    public interface IDataStoreFactory
    {
        /// <summary>
        /// Resolves a data store value to it's concrete type
        /// </summary>
        /// <typeparam name="B">Name of base type used for <see cref="DataStoreValue"/></typeparam>
        /// <typeparam name="C">Name of concrete type used for <see cref="DataStoreValue"/></typeparam>
        /// <param name="name">Name of <see cref="DataStoreValue"/></param>
        /// <returns>Concrete Type of <see cref="DataStoreValue"/></returns>
        /// <remarks>The combination of name and base type will be unique and there should not be duplicates</remarks>
        C Resolve<B, C>(string name)
            where B : IDataStore
            where C : IDataStore, B;
        /// <summary>
        /// Resolves a data store value to it's base type
        /// </summary>
        /// <typeparam name="B">Name of base type used for <see cref="DataStoreValue"/></typeparam>
        /// <param name="name">Name of <see cref="DataStoreValue"/></param>
        /// <returns>Base Type of <see cref="DataStoreValue"/></returns>
        /// <remarks>The combination of name and base type will be unique and there should not be duplicates</remarks>
        B Resolve<B>(string name) where B : IDataStore;
    }
}
