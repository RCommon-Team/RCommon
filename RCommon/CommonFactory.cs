using RCommon.DataServices.Transactions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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

        public T Create(Action<T> customize)
        {
            var concreteObject = _initFunc();
            customize(concreteObject);
            return concreteObject;
        }
    }

    public class CommonFactory<T, TResult> : ICommonFactory<T, TResult>
    {
        private readonly Func<T, TResult> _initFunc;

        public CommonFactory(Func<T, TResult> initFunc)
        {
            _initFunc = initFunc;
        }

        public TResult Create(T arg)
        {
            return _initFunc(arg);
        }

        public TResult Create(T arg, Action<TResult> customize)
        {
            var concreteObject = _initFunc(arg);
            customize(concreteObject);
            return concreteObject;
        }


    }

    public class Test
    {
        public Test()
        {
            var factory = new CommonFactory<TransactionMode, IUnitOfWorkScope>(x => new UnitOfWorkScope(null)).Create(TransactionMode.Default);
        }
    }
}
