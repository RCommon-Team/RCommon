using RCommon.DataServices.Transactions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace RCommon
{
    public class CommonFactory<T> : IUnitOfWorkScopeFactory<T>
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

    public class CommonFactory<T, T2, TResult> : ICommonFactory<T, T2, TResult>
    {
        private readonly Func<T, T2, TResult> _initFunc;

        public CommonFactory(Func<T, T2, TResult> initFunc)
        {
            _initFunc = initFunc;
        }

        public TResult Create(T arg, T2 arg2)
        {
            return _initFunc(arg, arg2);
        }

        public TResult Create(T arg, T2 arg2, Action<TResult> customize)
        {
            var concreteObject = _initFunc(arg, arg2);
            customize(concreteObject);
            return concreteObject;
        }


    }

    /*public class Test
    {
        public Test()
        {
            var factory = new CommonFactory<IUnitOfWorkManager, TransactionMode, IUnitOfWorkScope>
                (x => new UnitOfWorkManager(null, null, null, null) y=> new UnitOfWorkScope(x)).Create(y, TransactionMode.Default);
        }
    }*/
}
