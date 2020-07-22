using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public interface ICommonFactory<T>
    {
        T Create();
    }
}
