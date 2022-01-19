namespace Fig.Api.Authorization;

public interface ITokenHandler
{
    public string Generate(Guid userId);
    
    public Guid? Validate(string? token);
}