using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Services;

public class AccountService : IAccountService
{
    private IHttpService _httpService;
    private NavigationManager _navigationManager;
    private ILocalStorageService _localStorageService;
    private string _userKey = "user";
    
    public AccountService(
        IHttpService httpService,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService)
    {
        _httpService = httpService;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
    }
    
    public UserModel? User { get; private set; }
    
    public async Task Initialize()
    {
        User = await _localStorageService.GetItem<UserModel>(_userKey);
    }

    public async Task Login(AuthenticateRequestDataContract model)
    {
        User = await _httpService.Post<UserModel>("/users/authenticate", model);
        await _localStorageService.SetItem(_userKey, User);
    }

    public async Task Logout()
    {
        User = null;
        await _localStorageService.RemoveItem(_userKey);
        _navigationManager.NavigateTo("account/login");
    }

    public async Task Register(RegisterUserModel model)
    {
        await _httpService.Post("/users/register", model);
    }

    public async Task<IList<UserModel>> GetAll()
    {
        return await _httpService.Get<IList<UserModel>>("/users");
    }

    public async Task<UserModel> GetById(Guid id)
    {
        return await _httpService.Get<UserModel>($"/users/{id}");
    }
    
    public async Task Update(Guid id, EditUserModel model)
    {
        await _httpService.Put($"/users/{id}", model);

        // update stored user if the logged in user updated their own record
        if (id == User?.Id) 
        {
            // update local storage
            User.FirstName = model.FirstName;
            User.LastName = model.LastName;
            User.Username = model.Username;
            await _localStorageService.SetItem(_userKey, User);
        }
    }

    public async Task Delete(Guid id)
    {
        await _httpService.Delete($"/users/{id}");

        // auto logout if the logged in user deleted their own record
        if (id == User?.Id)
            await Logout();
    }
}