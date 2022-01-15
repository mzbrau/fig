using System;
using System.Reflection;

namespace Fig.Client.SettingVerification
{
    public interface ISettingVerificationDecompiler
    {
        string Decompile(Type classToDecompile, string methodName);
    }
}