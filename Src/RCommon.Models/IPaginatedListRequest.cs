namespace RCommon.Models
{
    public interface IPaginatedListRequest
    {
        int PageNumber { get; set; }
        int? PageSize { get; set; }
        string SortBy { get; set; }
        SortDirectionEnum SortDirection { get; set; }
    }
}
