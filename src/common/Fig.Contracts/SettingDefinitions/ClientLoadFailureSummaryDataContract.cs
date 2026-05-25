using System.Collections.Generic;

namespace Fig.Contracts.SettingDefinitions
{
    public class ClientLoadFailureSummaryDataContract
    {
        public ClientLoadFailureSummaryDataContract(int totalFailureCount, List<ClientLoadFailureDataContract> failures)
        {
            TotalFailureCount = totalFailureCount;
            Failures = failures;
        }

        public int TotalFailureCount { get; }

        public List<ClientLoadFailureDataContract> Failures { get; }
    }
}
