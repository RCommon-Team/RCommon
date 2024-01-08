﻿
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.EventHandling
{
    public interface ILocalEventHandler<TLocalEvent>
        where TLocalEvent : ILocalEvent
    {
    }
}
