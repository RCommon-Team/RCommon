using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon
{
    public class CommonFactory<T> : ICommonFactory<T>
    {
        private readonly Func<T> _initFunc;

        public CommonFactory(Func<T> initFunc)
        {
            _initFunc = initFunc;
        }

        public T Create()
        {
            return _initFunc();
        }
    }
}
