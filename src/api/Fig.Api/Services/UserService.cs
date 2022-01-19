using Fig.Api.Authorization;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Contracts.Authentication;

namespace Fig.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenHandler _tokenHandler;
    private readonly IUserConverter _userConverter;

    public UserService(
        IUserRepository userRepository,
        ITokenHandler tokenHandler,
        IUserConverter userConverter)
    {
        _userRepository = userRepository;
        _tokenHandler = tokenHandler;
        _userConverter = userConverter;
    }

    public AuthenticateResponseDataContract Authenticate(AuthenticateRequestDataContract model)
    {
        var user = _userRepository.GetUser(model.Username);
        if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(model.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Username or password is incorrect");

        var response = _userConverter.ConvertToResponse(user);
        response.Token = _tokenHandler.Generate(response.Id);
        return response;
    }

    public IEnumerable<UserDataContract> GetAll()
    {
        return _userRepository.GetAllUsers().Select(user => _userConverter.Convert(user));
    }

    public UserDataContract GetById(Guid id)
    {
        var user = _userRepository.GetUser(id);

        if (user == null)
            throw new UnknownUserException();
        
        return _userConverter.Convert(user);
    }

    public void Register(RegisterUserRequestDataContract request)
    {
        var existingUser = _userRepository.GetUser(request.Username);
        if (existingUser != null)
            throw new UserExistsException();

        var user = _userConverter.ConvertFromRequest(request);
        user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);

        _userRepository.SaveUser(user);
    }

    public void Update(Guid id, UpdateUserRequestDataContract request)
    {
        var user = _userRepository.GetUser(id);

        if (user == null)
            throw new UnknownUserException();
        
        if (request.Username != user.Username && _userRepository.GetUser(request.Username) != null)
            throw new UserExistsException();

        // hash password if it was entered
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);

        user.Username = request.Username;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Role = request.Role;

        _userRepository.UpdateUser(user);
    }

    public void Delete(Guid id)
    {
        var user = _userRepository.GetUser(id);

        if (user != null)
        {
            _userRepository.DeleteUser(user);
        }
    }
}