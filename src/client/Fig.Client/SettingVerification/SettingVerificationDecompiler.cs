using System;
using Fig.Client.Exceptions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace Fig.Client.SettingVerification
{
    internal class SettingVerificationDecompiler : ISettingVerificationDecompiler
    {
        public string Decompile(Type classToDecompile, string methodName)
        {
            var methodInfo = classToDecompile.GetMethod(methodName);
            if (methodInfo is null || methodInfo.DeclaringType is null)
                throw new InvalidSettingVerificationException($"Method {methodName} could not be found");
            
            var decompiler = new CSharpDecompiler(methodInfo.DeclaringType.Assembly.Location, new DecompilerSettings());
            var typeInfo = decompiler.TypeSystem.MainModule.Compilation.FindType(methodInfo.DeclaringType).GetDefinition();
            if (typeInfo is null)
                throw new InvalidSettingVerificationException($"Could not decompile type {methodInfo.DeclaringType.Name}");
            
            return decompiler.DecompileTypeAsString(typeInfo.FullTypeName);
        }
    }
}