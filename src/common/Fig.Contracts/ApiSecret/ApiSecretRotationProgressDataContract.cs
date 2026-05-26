using System.Collections.Generic;

namespace Fig.Contracts.ApiSecret;

public class ApiSecretRotationProgressDataContract
{
    public string? CurrentStageId { get; set; }

    public string? CurrentProgressMessage { get; set; }

    public IList<ApiSecretRotationStageProgressDataContract> Stages { get; set; } =
        new List<ApiSecretRotationStageProgressDataContract>();
}
