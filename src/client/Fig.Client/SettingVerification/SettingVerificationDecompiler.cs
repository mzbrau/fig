using System;
using System.Reflection;
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
            var decompiler = new CSharpDecompiler(methodInfo.DeclaringType.Assembly.Location, new DecompilerSettings());
            var typeInfo = decompiler.TypeSystem.MainModule.Compilation.FindType(methodInfo.DeclaringType).GetDefinition();
            return decompiler.DecompileTypeAsString(typeInfo.FullTypeName);
        }
    }
}