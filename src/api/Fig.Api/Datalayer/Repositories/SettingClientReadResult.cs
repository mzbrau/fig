using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public record SettingClientReadResult(IList<SettingClientBusinessEntity> Clients, IList<SettingClientReadFailure> Failures);
