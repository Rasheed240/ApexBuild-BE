using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Domain.Enums
{

    public enum ProjectStatus
    {
        Planning = 1,
        Active = 2,
        OnHold = 3,
        Completed = 4,
        Cancelled = 5
    }

}