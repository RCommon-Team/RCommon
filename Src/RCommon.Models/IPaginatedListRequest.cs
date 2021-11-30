namespace RCommon.Models
{
    public interface IPaginatedListRequest
    {
        int PageIndex { get; set; }
        int? PageSize { get; set; }
        string SortBy { get; set; }
        SortDirectionEnum SortDirection { get; set; }
    }
}