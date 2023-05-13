using Fig.Contracts.Authentication;
using Fig.Web.Converters;
using Fig.Web.Models.Authentication;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Services;

public class AccountService : IAccountService
{
    private readonly IHttpService _httpService;
    private readonly ILocalStorageService _localStorageService;
    private readonly NavigationManager _navigationManager;
    private readonly IUserConverter _userConverter;
    private readonly string _userKey = "user";

    public AccountService(
        IHttpService httpService,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService,
        IUserConverter userConverter)
    {
        _httpService = httpService;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _userConverter = userConverter;
    }

    public AuthenticatedUserModel? AuthenticatedUser { get; private set; }

    public async Task Initialize()
    {
        AuthenticatedUser = await _localStorageService.GetItem<AuthenticatedUserModel>(_userKey);
    }

    public async Task Login(LoginModel model)
    {
        var dataContract = new AuthenticateRequestDataContract(model.Username!, model.Password!);
        var user = await _httpService.Post<AuthenticateResponseDataContract>("/users/authenticate", dataContract);

        if (user == null)
            throw new Exception("Invalid user");

        AuthenticatedUser = _userConverter.Convert(user);
        await _localStorageService.SetItem(_userKey, AuthenticatedUser);
    }

    public async Task Logout()
    {
        AuthenticatedUser = null;
        await _localStorageService.RemoveItem(_userKey);
        _navigationManager.NavigateTo("account/login");
    }

    public async Task<Guid> Register(RegisterUserRequestDataContract userRegistration)
    {
        return await _httpService.Post<Guid>("/users/register", userRegistration);
    }

    public async Task<IList<UserDataContract>> GetAll()
    {
        return await _httpService.Get<IList<UserDataContract>>("/users");
    }

    public async Task<UserDataContract> GetById(Guid id)
    {
        return await _httpService.Get<UserDataContract>($"/users/{id}");
    }

    public async Task Update(Guid id, UpdateUserRequestDataContract update)
    {
        await _httpService.Put($"/users/{id}", update);

        // update stored user if the logged in user updated their own record
        if (id == AuthenticatedUser?.Id)
        {
            // update local storage
            AuthenticatedUser.FirstName = update.FirstName;
            AuthenticatedUser.LastName = update.LastName;
            AuthenticatedUser.Username = update.Username;
            await _localStorageService.SetItem(_userKey, AuthenticatedUser);
        }

        if (AuthenticatedUser?.PasswordChangeRequired == true)
        {
            await Logout();
        }
    }

    public async Task Delete(Guid id)
    {
        await _httpService.Delete($"/users/{id}");

        // auto logout if the logged in user deleted their own record
        if (id == AuthenticatedUser?.Id)
            await Logout();
    }
}