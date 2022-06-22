using Fig.Contracts.Status;
using Fig.Web.Models.Api;

namespace Fig.Web.Converters;

public interface IApiStatusConverter
{
    ApiStatusModel Convert(ApiStatusDataContract status);
}