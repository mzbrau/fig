namespace Fig.Web.Services.Authentication;

public interface IFigApiAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync();
}
