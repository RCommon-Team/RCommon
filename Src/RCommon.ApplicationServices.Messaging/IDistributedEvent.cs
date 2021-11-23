namespace RCommon.ApplicationServices.Messaging
{
    public interface IDistributedEvent
    {
        bool Equals(DistributedEvent? other);
        bool Equals(object? obj);
        int GetHashCode();
        string ToString();
    }
}