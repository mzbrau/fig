using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;
using Fig.Web.Services.Authentication;

namespace Fig.Web.Services;

public class AccountService : IAccountService
{
    private readonly IWebAuthenticationModeService _webAuthenticationModeService;

    public AccountService(
        IWebAuthenticationModeService webAuthenticationModeService)
    {
        _webAuthenticationModeService = webAuthenticationModeService;
    }

    public WebAuthMode AuthenticationMode => _webAuthenticationModeService.Mode;

    public AuthenticatedUserModel? AuthenticatedUser => _webAuthenticationModeService.AuthenticatedUser;

    public bool IsInitialized => _webAuthenticationModeService.IsInitialized;

    public async Task Initialize()
    {
        await _webAuthenticationModeService.Initialize();
    }

    public async Task Login(LoginModel model)
    {
        await _webAuthenticationModeService.Login(model);
    }

    public async Task Logout()
    {
        await _webAuthenticationModeService.Logout();
    }

    public async Task<Guid> Register(RegisterUserRequestDataContract model)
    {
        return await _webAuthenticationModeService.Register(model);
    }

    public async Task<IList<UserDataContract>> GetAll()
    {
        return await _webAuthenticationModeService.GetAll();
    }

    public async Task Update(Guid id, UpdateUserRequestDataContract model)
    {
        await _webAuthenticationModeService.Update(id, model);
    }

    public async Task Delete(Guid id)
    {
        await _webAuthenticationModeService.Delete(id);
    }
}