using Fig.Api.Authorization;
using Fig.Api.Constants;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Validators;
using Fig.Contracts.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class UserService : AuthenticatedService, IUserService
{
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IOptions<ApiSettings> _apiSettings;
    private readonly ITokenHandler _tokenHandler;
    private readonly IUserConverter _userConverter;
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserRepository userRepository,
        ITokenHandler tokenHandler,
        IUserConverter userConverter,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IPasswordValidator passwordValidator,
        IOptions<ApiSettings> apiSettings)
    {
        _userRepository = userRepository;
        _tokenHandler = tokenHandler;
        _userConverter = userConverter;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _passwordValidator = passwordValidator;
        _apiSettings = apiSettings;
    }

    public async Task<AuthenticateResponseDataContract> Authenticate(AuthenticateRequestDataContract model)
    {
        if (_apiSettings.Value.UseKeycloak)
        {
            throw new InvalidOperationException("Authentication is handled by Keycloak when enabled.");
        }
        
        var user = await _userRepository.GetUser(model.Username, false);
        if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(model.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Username or password is incorrect");

        var token = _tokenHandler.Generate(user);
        var passwordChangeRequired = IsPasswordChangeRequired(model);

        var response = _userConverter.ConvertToResponse(user, token, passwordChangeRequired);
        
        await _eventLogRepository.Add(_eventLogFactory.LogIn(user));

        return response;
    }

    public async Task<IEnumerable<UserDataContract>> GetAll()
    {
        var users = await _userRepository.GetAllUsers();
        return users.Select(user => _userConverter.Convert(user));
    }

    public async Task<UserDataContract> GetById(Guid id)
    {
        var user = await _userRepository.GetUser(id, false);

        if (user == null)
            throw new UnknownUserException();

        return _userConverter.Convert(user);
    }

    public async Task<Guid> Register(RegisterUserRequestDataContract request)
    {
        if (_apiSettings.Value.UseKeycloak)
        {
            throw new InvalidOperationException("User registration is handled by Keycloak when enabled.");
        }
        
        if (string.IsNullOrEmpty(request.Password))
            throw new InvalidDataException("Password is required");
        
        var existingUser = await _userRepository.GetUser(request.Username, false);
        if (existingUser != null)
            throw new UserExistsException();

        _passwordValidator.Validate(request.Password);

        var user = _userConverter.ConvertFromRequest(request);
        user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);

        await _userRepository.SaveUser(user);

        await _eventLogRepository.Add(_eventLogFactory.NewUser(user, AuthenticatedUser));
        return user.Id;
    }

    public async Task Update(Guid id, UpdateUserRequestDataContract request)
    {
        if (_apiSettings.Value.UseKeycloak)
        {
            // User updates (especially roles/passwords) should be managed in Keycloak.
            // Specific profile updates might be allowed if Fig stores additional user data.
            // For now, disabling all modifications via this service.
            throw new InvalidOperationException("User updates should be managed in Keycloak when enabled.");
        }
        
        var user = await _userRepository.GetUser(id, true);

        if (user == null)
            throw new UnknownUserException();

        if (request.Username != user.Username && request.Username != null &&
            await _userRepository.GetUser(request.Username, true) != null)
            throw new UserExistsException();

        if (AuthenticatedUser?.Role != Role.Administrator && AuthenticatedUser?.Username != user.Username)
            throw new UnauthorizedAccessException();

        // hash password if it was entered
        var passwordUpdated = false;
        if (!string.IsNullOrEmpty(request.Password))
        {
            _passwordValidator.Validate(request.Password);
            user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);
            passwordUpdated = true;
        }

        var originalDetails = user.Details();

        if (!string.IsNullOrEmpty(request.Username))
            user.Username = request.Username;

        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;

        if (!string.IsNullOrWhiteSpace(request.ClientFilter))
            user.ClientFilter = request.ClientFilter;

        if (JsonConvert.SerializeObject(user.AllowedClassifications) !=
            JsonConvert.SerializeObject(request.AllowedClassifications))
        {
            if (AuthenticatedUser?.Role != Role.Administrator)
                throw new UnauthorizedAccessException();
            
            user.AllowedClassifications = request.AllowedClassifications ?? [];
        }

        if (request.Role != null)
        {
            if (AuthenticatedUser?.Role != Role.Administrator)
                throw new UnauthorizedAccessException();

            user.Role = request.Role.Value;
        }
        
        await _userRepository.UpdateUser(user);

        await _eventLogRepository.Add(_eventLogFactory.UpdateUser(user, originalDetails, passwordUpdated, AuthenticatedUser));
    }

    public async Task Delete(Guid id)
    {
        if (_apiSettings.Value.UseKeycloak)
        {
            throw new InvalidOperationException("User deletion is handled by Keycloak when enabled.");
        }
        
        var user = await _userRepository.GetUser(id, true);

        if (user is null)
            return;

        if (user.Role == Role.Administrator)
        {
            var allUsers = await _userRepository.GetAllUsers();
            if (allUsers.Count(a => a.Role == Role.Administrator) == 1)
            {
                throw new InvalidUserDeletionException();
            }
        }
        
        await _userRepository.DeleteUser(user);
        await _eventLogRepository.Add(_eventLogFactory.DeleteUser(user, AuthenticatedUser));
    }

    private bool IsPasswordChangeRequired(AuthenticateRequestDataContract model)
    {
        return _apiSettings.Value.ForceAdminDefaultPasswordChange &&
               model.Username == DefaultUser.UserName &&
               model.Password == DefaultUser.Password;
    }
}