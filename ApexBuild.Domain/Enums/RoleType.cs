using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBuild.Domain.Enums
{
    public enum RoleType
    {
        SuperAdmin = 1,
        PlatformAdmin = 2,
        ProjectOwner = 3,
        ProjectAdministrator = 4,
        ContractorAdmin = 5,
        DepartmentSupervisor = 6,
        FieldWorker = 7,
        Observer = 8
    }
}