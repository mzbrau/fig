using System.Reflection;
using Basic.Reference.Assemblies;
using Fig.Api.SettingVerification.Exceptions;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fig.Api.SettingVerification.Dynamic;

// https://stackoverflow.com/a/56742550
public class SettingDynamicVerifier : ISettingDynamicVerifier
{
    private readonly ICodeHasher _codeHasher;

    public SettingDynamicVerifier(ICodeHasher codeHasher)
    {
        _codeHasher = codeHasher;
    }

    public async Task<VerificationResultDataContract> RunVerification(
        SettingDynamicVerificationBusinessEntity verification,
        IDictionary<string, object?> settings)
    {
        if (!_codeHasher.IsValid(verification.CodeHash, verification.Code))
            throw new CodeTamperedException(verification.Name);

        try
        {
            return await Task.Run(() =>
            {
                var parsedCode = Parse(verification.Code);
                var compiledCode = Compile(parsedCode, verification.TargetRuntime);
                var result = (VerificationResultDataContract) Invoke(compiledCode,
                    nameof(ISettingVerification.PerformVerification),
                    settings);

                return result;
            });
        }
        catch (CompileErrorException ex)
        {
            return VerificationResultDataContract.Failure("Compile Error, see logs for details",
                ex.CompileErrors.ToList());
        }
        catch (ObjectCreationException ex)
        {
            return VerificationResultDataContract.Failure(
                $"Unable to create object from provided test type. {ex.Message}");
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

    public async Task Compile(SettingDynamicVerificationBusinessEntity verification)
    {
        await Task.Run(() =>
        {
            var parsedCode = Parse(verification.Code);
            var _ = Compile(parsedCode, verification.TargetRuntime);
        });
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

    private static SyntaxTree Parse(string snippet)
    {
        return CSharpSyntaxTree.ParseText(snippet);
    }
}