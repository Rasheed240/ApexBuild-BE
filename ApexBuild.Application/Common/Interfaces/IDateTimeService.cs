using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Application.Common.Interfaces
{
    public interface IDateTimeService
    {
        DateTime UtcNow { get; }
        DateTime Now { get; }
    }
}