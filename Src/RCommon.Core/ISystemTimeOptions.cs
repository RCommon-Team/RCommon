using System;

namespace RCommon
{
    public interface ISystemTimeOptions
    {
        DateTimeKind Kind { get; set; }
    }
}