using Fig.Web.Converters;
using Fig.Web.Models.Authentication;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class UsersFacade : IUsersFacade
{
    private readonly IAccountService _accountService;
    private readonly IUserConverter _userConverter;

    public UsersFacade(IAccountService accountService, IUserConverter userConverter)
    {
        _accountService = accountService;
        _userConverter = userConverter;
    }
    
    public List<UserModel> UserCollection { get; set; }

    public async Task LoadAllUsers()
    {
        var users = await _accountService.GetAll();
        UserCollection = users.Select(a => _userConverter.Convert(a)).ToList();
    }
    
    public async Task SaveUser(UserModel user)
    {
        if (user.Id != null)
            await _accountService.Update(user.Id.Value, _userConverter.ConvertForUpdate(user));
    }

    public async Task DeleteUser(UserModel user)
    {
        if (user.Id != null)
            await _accountService.Delete(user.Id.Value);
    }

    public async Task AddUser(UserModel user)
    {
        user.Id ??= await _accountService.Register(_userConverter.ConvertForRegistration(user));
    }
}