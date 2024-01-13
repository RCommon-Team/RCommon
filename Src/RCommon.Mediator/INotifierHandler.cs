﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator
{
    public interface INotifierHandler<in T>
    {
        Task HandleAsync(T notification, CancellationToken cancellationToken = default(CancellationToken));
    }
}
