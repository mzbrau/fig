using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingVerification
{
    public class VerificationResultDataContract
    {
        public VerificationResultDataContract()
        {
            Logs = new List<string>();
            ExecutionTime = DateTime.UtcNow;
        }

        public bool Success { get; set; }

        public string Message { get; set; }

        public List<string> Logs { get; set; }

        public string RequestingUser { get; set; }

        public DateTime ExecutionTime { get; set; }

        public static VerificationResultDataContract Failure(string message, List<string>? logs = null)
        {
            return new VerificationResultDataContract
            {
                Success = false,
                Message = message,
                Logs = logs ?? new List<string>(),
            };
        }

        public void AddLog(string message)
        {
            Logs.Add(message);
        }
    }
}