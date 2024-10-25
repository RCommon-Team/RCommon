namespace RCommon.Models
{
    public interface ISearchPaginatedListRequest : IModel
    {
        string SearchString { get; set; }
    }
}
