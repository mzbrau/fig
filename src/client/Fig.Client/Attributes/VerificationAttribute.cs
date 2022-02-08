using System;
using Fig.Client.Enums;
using Fig.Contracts.SettingVerification;

namespace Fig.Client.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class VerificationAttribute : Attribute
    {
        public VerificationAttribute(string name, string description, Type classDoingVerification,
            TargetRuntime targetRuntime, params string[] settingsVerified)
        {
            Name = name;
            Description = description;
            ClassDoingVerification = classDoingVerification;
            TargetRuntime = targetRuntime;
            VerificationType = VerificationType.Dynamic;
            SettingNames = settingsVerified;
        }

        public VerificationAttribute(string name, params string[] propertyArguments)
        {
            Name = name;
            SettingNames = propertyArguments;
            VerificationType = VerificationType.Plugin;
        }

        public string Name { get; }
        public string[] SettingNames { get; }
        public string Description { get; }
        public Type ClassDoingVerification { get; }
        public TargetRuntime TargetRuntime { get; }

        public VerificationType VerificationType { get; }
    }
}