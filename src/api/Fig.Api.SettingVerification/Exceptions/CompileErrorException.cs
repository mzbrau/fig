namespace Fig.Api.SettingVerification.Exceptions;

public class CompileErrorException : Exception
{
    public CompileErrorException(IEnumerable<string> compileErrors)
    {
        CompileErrors = compileErrors;
    }

    public IEnumerable<string> CompileErrors { get; }

    public override string ToString()
    {
        return $"Compile Errors:{Environment.NewLine}{string.Join(Environment.NewLine, CompileErrors)}";
    }
}