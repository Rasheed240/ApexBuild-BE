using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Domain.Enums
{
    public enum InvitationStatus
    {
        Pending = 1,
        Accepted = 2,
        Expired = 3,
        Cancelled = 4
    }
}