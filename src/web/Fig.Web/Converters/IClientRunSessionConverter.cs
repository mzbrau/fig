using Fig.Contracts.Status;
using Fig.Web.Models.Clients;

namespace Fig.Web.Converters;

public interface IClientRunSessionConverter
{
    IEnumerable<ClientRunSessionModel> Convert(List<ClientStatusDataContract> clients);
}