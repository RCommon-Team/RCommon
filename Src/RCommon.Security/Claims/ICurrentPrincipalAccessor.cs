﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Claims
{
    public interface ICurrentPrincipalAccessor
    {
        ClaimsPrincipal Principal { get; }

        IDisposable Change(ClaimsPrincipal principal);
    }
}
