using Fig.Api.Authorization;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.Authentication;

namespace Fig.Api.Services;

public class UserService : AuthenticatedService, IUserService
{
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly ITokenHandler _tokenHandler;
    private readonly IUserConverter _userConverter;
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserRepository userRepository,
        ITokenHandler tokenHandler,
        IUserConverter userConverter,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory)
    {
        _userRepository = userRepository;
        _tokenHandler = tokenHandler;
        _userConverter = userConverter;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
    }

    public AuthenticateResponseDataContract Authenticate(AuthenticateRequestDataContract model)
    {
        var user = _userRepository.GetUser(model.Username);
        if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(model.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Username or password is incorrect");

        var response = _userConverter.ConvertToResponse(user);
        response.Token = _tokenHandler.Generate(user);
        _eventLogRepository.Add(_eventLogFactory.LogIn(user));

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

        _eventLogRepository.Add(_eventLogFactory.NewUser(user, AuthenticatedUser));
    }

    public void Update(Guid id, UpdateUserRequestDataContract request)
    {
        var user = _userRepository.GetUser(id);

        if (user == null)
            throw new UnknownUserException();

        if (request.Username != user.Username && _userRepository.GetUser(request.Username) != null)
            throw new UserExistsException();

        if (AuthenticatedUser?.Role != Role.Administrator && AuthenticatedUser?.Username != user.Username)
            throw new UnauthorizedAccessException();

        // hash password if it was entered
        var passwordUpdated = false;
        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);
            passwordUpdated = true;
        }

        var originalDetails = user.Details();

        user.Username = request.Username;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Role = request.Role;

        _userRepository.UpdateUser(user);

        _eventLogRepository.Add(_eventLogFactory.UpdateUser(user, originalDetails, passwordUpdated, AuthenticatedUser));
    }

    public void Delete(Guid id)
    {
        var user = _userRepository.GetUser(id);

        if (user != null)
        {
            _userRepository.DeleteUser(user);
            _eventLogRepository.Add(_eventLogFactory.DeleteUser(user, AuthenticatedUser));
        }
    }
}