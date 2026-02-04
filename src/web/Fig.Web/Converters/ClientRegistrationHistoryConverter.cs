using Fig.Contracts.ClientRegistrationHistory;
using Fig.Web.Models.ClientHistory;

namespace Fig.Web.Converters;

public class ClientRegistrationHistoryConverter : IClientRegistrationHistoryConverter
{
    public ClientRegistrationHistoryModel Convert(ClientRegistrationHistoryDataContract dataContract)
    {
        return new ClientRegistrationHistoryModel
        {
            Id = dataContract.Id,
            RegistrationDateUtc = dataContract.RegistrationDateUtc,
            ClientName = dataContract.ClientName,
            ClientVersion = dataContract.ClientVersion,
            Settings = dataContract.Settings?.Select(Convert).ToList() ?? new List<SettingDefaultValueModel>()
        };
    }

    public SettingDefaultValueModel Convert(SettingDefaultValueDataContract dataContract)
    {
        return new SettingDefaultValueModel
        {
            Name = dataContract.Name,
            DefaultValue = dataContract.DefaultValue,
            Advanced = dataContract.Advanced
        };
    }
}
