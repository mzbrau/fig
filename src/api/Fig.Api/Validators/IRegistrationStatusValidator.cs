using Fig.Api.Enums;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Validators;

public interface IRegistrationStatusValidator
{
    CurrentRegistrationStatus GetStatus(List<SettingClientBusinessEntity>? existingRegistrations, string clientSecret);

    CurrentRegistrationStatus GetStatus(ClientBase client, string clientSecret);
}
