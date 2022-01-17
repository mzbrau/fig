using System;
using Fig.Client.Enums;
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
            VerificationType = VerificationType.Dynamic;
        }

        public VerificationAttribute(string name, params string[] propertyArguments)
        {
            Name = name;
            PropertyArguments = propertyArguments;
            VerificationType = VerificationType.Plugin;
        }

        public string Name { get; }
        public string[] PropertyArguments { get; }
        public string Description { get; }
        public Type ClassDoingVerification { get; }
        public TargetRuntime TargetRuntime { get; }
        
        public VerificationType VerificationType { get; }
    }
}