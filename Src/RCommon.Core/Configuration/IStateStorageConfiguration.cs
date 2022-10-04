

using RCommon.StateStorage;

namespace RCommon
{
    /// <summary>
    /// Interface that can be implemented by classes that provide state configuration for RCommon.
    /// </summary>
    public interface IStateStorageConfiguration
    {
        IContextStateSelector ContextStateSelector { get; set; }
    }
}
