using System;
using Fig.Contracts.SettingVerification;

namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class VerificationAttribute : Attribute
    {
        public VerificationAttribute(string name, string description, Type classDoingVerification, TargetRuntime targetRuntime)
        {
            Name = name;
            Description = description;
            ClassDoingVerification = classDoingVerification;
            TargetRuntime = targetRuntime;
        }

        public string Name { get; }
        public string Description { get; }
        public Type ClassDoingVerification { get; }
        public TargetRuntime TargetRuntime { get; }
    }
}