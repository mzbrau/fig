using Fig.Common.NetStandard.Data;
using Fig.Contracts.Authentication;

namespace Fig.Api.Utils;

internal class ServiceUser : UserDataContract
{
    public ServiceUser() 
        : base(Guid.NewGuid(),
        "SERVICE",
        "SERVICE",
        "USER",
        Role.Administrator,
        ".*",
        Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList())
    {
    }
}