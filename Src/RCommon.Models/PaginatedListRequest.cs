
using System.Runtime.Serialization;

namespace RCommon.Models
{
    [DataContract]
    public abstract class PaginatedListRequest : IModel, IPaginatedListRequest
    {
        public PaginatedListRequest()
        {
            PageIndex = 1;
            PageSize = 20;
            SortBy = null;
            SortDirection = SortDirectionEnum.None;
        }

        public virtual int PageIndex { get; set; }
        public virtual int? PageSize { get; set; }
        public virtual string SortBy { get; set; }
        public virtual SortDirectionEnum SortDirection { get; set; }
    }
}
