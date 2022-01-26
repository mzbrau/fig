namespace Fig.Api.SettingVerification.Exceptions;

public class CompileErrorException : Exception
{
    public CompileErrorException(IEnumerable<string> compileErrors)
        : base(
            $"Compile error(s) detected in settings verification code:{Environment.NewLine}{string.Join(Environment.NewLine, compileErrors)}")
    {
    }
}