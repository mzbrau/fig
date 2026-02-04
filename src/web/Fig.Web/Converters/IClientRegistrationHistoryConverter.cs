using Fig.Contracts.ClientRegistrationHistory;
using Fig.Web.Models.ClientHistory;

namespace Fig.Web.Converters;

public interface IClientRegistrationHistoryConverter
{
    ClientRegistrationHistoryModel Convert(ClientRegistrationHistoryDataContract dataContract);
    
    SettingDefaultValueModel Convert(SettingDefaultValueDataContract dataContract);
}
