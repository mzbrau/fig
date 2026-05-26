using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class ClientLoadFailureSummaryDataContract
    {
        public ClientLoadFailureSummaryDataContract(int totalFailureCount, List<ClientLoadFailureDataContract> failures, bool truncated = false)
        {
            TotalFailureCount = totalFailureCount;
            Failures = failures;
            Truncated = truncated;
        }

        public int TotalFailureCount { get; }

        public List<ClientLoadFailureDataContract> Failures { get; }

        public bool Truncated { get; }
    }
}
