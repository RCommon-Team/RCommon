
using System.Runtime.Serialization;

namespace RCommon.Models
{
    [DataContract]
    public abstract class PaginatedListRequest : IModel, IPaginatedListRequest
    {
        public PaginatedListRequest()
        {
            PageNumber = 1;
            PageSize = 20;
            SortBy = "id";
            SortDirection = SortDirectionEnum.None;
        }

        public virtual int PageNumber { get; set; }
        public virtual int? PageSize { get; set; }
        public virtual string SortBy { get; set; }
        public virtual SortDirectionEnum SortDirection { get; set; }
    }
}
