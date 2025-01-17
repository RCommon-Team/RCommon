
using System.Runtime.Serialization;

namespace RCommon.Models
{
    [DataContract]
    public abstract record PaginatedListRequest : IPaginatedListRequest
    {
        public PaginatedListRequest()
        {
            PageNumber = 1;
            PageSize = 20;
            SortBy = "id";
            SortDirection = SortDirectionEnum.None;
        }

        [DataMember]
        public virtual int PageNumber { get; set; }

        [DataMember]
        public virtual int PageSize { get; set; }

        [DataMember]
        public virtual string SortBy { get; set; }

        [DataMember]
        public virtual SortDirectionEnum SortDirection { get; set; }
    }
}
