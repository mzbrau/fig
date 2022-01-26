using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;

namespace Fig.Web.Services;

public interface IAccountService
{
    UserModel User { get; }
    
    Task Initialize();
    
    Task Login(AuthenticateRequestDataContract model);
    
    Task Logout();
    
    Task Register(RegisterUserModel model);
    
    Task<IList<UserModel>> GetAll();
    
    Task<UserModel> GetById(Guid id);
    
    Task Update(Guid id, EditUserModel model);
    
    Task Delete(Guid id);
}