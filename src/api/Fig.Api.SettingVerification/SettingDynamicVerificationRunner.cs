using System.Reflection;
using Basic.Reference.Assemblies;
using Fig.Api.SettingVerification.Exceptions;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fig.Api.SettingVerification;

public class SettingDynamicVerificationRunner : ISettingDynamicVerificationRunner
{
   public async Task<VerificationResultDataContract> Run(SettingVerificationDefinitionDataContract verification, IEnumerable<SettingDefinitionDataContract> settings)
   {
      try
      {
         return await Task.Run(() =>
         {
            var parsedCode = Parse(verification.Code);
            var compiledCode = Compile(parsedCode, verification.TargetRuntime);
            var result = (VerificationResultDataContract) Invoke(compiledCode,
               nameof(ISettingVerification.PerformVerification),
               settings.ToDictionary(a => a.Name, b => b.DefaultValue));

            return result;
         });
      }
      catch (CompileErrorException ex)
      {
         return VerificationResultDataContract.Failure("Compile Error, see logs for details", ex.CompileErrors.ToList());
      }
      catch (ObjectCreationException ex)
      {
         return VerificationResultDataContract.Failure($"Unable to create object from provided test type. {ex.Message}");
      }
      catch (DynamicCodeExecutionException ex)
      {
         return VerificationResultDataContract.Failure($"Exception during code execution. {ex.InnerException}");
      }
      catch (Exception ex)
      {
         return VerificationResultDataContract.Failure($"Unknown error. {ex.Message}");
      }
   }

   private static Assembly Compile(SyntaxTree syntaxTree, TargetRuntime targetRuntime)
   {
      var references = new List<MetadataReference>();
      Assembly.GetExecutingAssembly().GetReferencedAssemblies()
         .ToList()
         .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

      switch (targetRuntime)
      {
         case TargetRuntime.Framework472:
            references.AddRange(ReferenceAssemblies.Net472);
            break;
         case TargetRuntime.Core31:
            references.AddRange(ReferenceAssemblies.NetCoreApp31);
            break;
         case TargetRuntime.Dotnet5:
            references.AddRange(ReferenceAssemblies.Net50);
            break;
         case TargetRuntime.Dotnet6:
            references.AddRange(ReferenceAssemblies.Net60);
            break;
      }

      var compilation = CSharpCompilation.Create(Path.GetRandomFileName(), new[] {syntaxTree}, references,
         new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

      using var ms = new MemoryStream();
      var result = compilation.Emit(ms);
      if (result.Success)
      {
         ms.Seek(0, SeekOrigin.Begin);
         return Assembly.Load(ms.ToArray());
      }
      
      throw new CompileErrorException(result.Diagnostics
         .Where(diagnostic =>
            diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
         .Select(d => $"{d.Id}: {d.GetMessage()}"));
   }

   private static object Invoke(Assembly assembly, string methodName, params object[] args)
   {
      Type? verificationType;
      object? createdObject;
      try
      {
         verificationType = assembly.DefinedTypes.FirstOrDefault(a =>
            a.ImplementedInterfaces.Contains(typeof(ISettingVerification)));
         createdObject = Activator.CreateInstance(verificationType);
      }
      catch (Exception e)
      {
         throw new ObjectCreationException(e);
      }

      try
      {
         return verificationType.InvokeMember(methodName,
            BindingFlags.Default | BindingFlags.InvokeMethod,
            null,
            createdObject,
            args);
      }
      catch (Exception e)
      {
         throw new DynamicCodeExecutionException(e);
      }
   }

   private static SyntaxTree Parse(string snippet) => CSharpSyntaxTree.ParseText(snippet);
   
}