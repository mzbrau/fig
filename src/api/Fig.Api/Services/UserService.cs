using Fig.Api.Authorization;
using Fig.Api.Constants;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Validators;
using Fig.Api.WebHooks;
using Fig.Contracts.Authentication;
using Fig.WebHooks.Contracts;
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
    private readonly IWebHookDisseminationService _webHookDisseminationService;

    public UserService(
        IUserRepository userRepository,
        ITokenHandler tokenHandler,
        IUserConverter userConverter,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IPasswordValidator passwordValidator,
        IOptions<ApiSettings> apiSettings,
        IWebHookDisseminationService webHookDisseminationService)
    {
        _userRepository = userRepository;
        _tokenHandler = tokenHandler;
        _userConverter = userConverter;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _passwordValidator = passwordValidator;
        _apiSettings = apiSettings;
        _webHookDisseminationService = webHookDisseminationService;
    }

    public async Task<AuthenticateResponseDataContract> Authenticate(AuthenticateRequestDataContract model)
    {
        var user = await _userRepository.GetUser(model.Username, false);
        if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(model.Password, user.PasswordHash))
        {
            // Log failed login attempt
            var failureReason = user == null ? "User not found" : "Invalid password";
            await _eventLogRepository.Add(_eventLogFactory.LogInFailed(model.Username, failureReason));
            
            // Fire security webhook for failed login
            var failedLoginEvent = new SecurityEventWebHookData(
                "Login",
                DateTime.UtcNow,
                model.Username,
                false,
                _eventLogFactory.GetRequestIpAddress(),
                _eventLogFactory.GetRequestHostname(),
                failureReason);
            
            await _webHookDisseminationService.SecurityEvent(failedLoginEvent);
            
            throw new UnauthorizedAccessException("Username or password is incorrect");
        }

        var token = _tokenHandler.Generate(user);
        var passwordChangeRequired = IsPasswordChangeRequired(model);

        var response = _userConverter.ConvertToResponse(user, token, passwordChangeRequired);
        
        // Log successful login
        await _eventLogRepository.Add(_eventLogFactory.LogIn(user));
        
        // Fire security webhook for successful login
        var successfulLoginEvent = new SecurityEventWebHookData(
            "Login",
            DateTime.UtcNow,
            user.Username,
            true,
            _eventLogFactory.GetRequestIpAddress(),
            _eventLogFactory.GetRequestHostname());
        
        await _webHookDisseminationService.SecurityEvent(successfulLoginEvent);

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
        if (string.IsNullOrEmpty(request.Password))
            throw new InvalidDataException("Password is required");
        
        var existingUser = await _userRepository.GetUser(request.Username, false);
        if (existingUser != null)
            throw new UserExistsException(request.Username);

        _passwordValidator.Validate(request.Password);

        var user = _userConverter.ConvertFromRequest(request);
        user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password);

        await _userRepository.SaveUser(user);

        await _eventLogRepository.Add(_eventLogFactory.NewUser(user, AuthenticatedUser));
        return user.Id;
    }

    public async Task Update(Guid id, UpdateUserRequestDataContract request)
    {
        var user = await _userRepository.GetUser(id, true);

        if (user == null)
            throw new UnknownUserException();

        if (request.Username != user.Username && request.Username != null &&
            await _userRepository.GetUser(request.Username, true) != null)
            throw new UserExistsException(request.Username);

        if (AuthenticatedUser?.Role != Role.Administrator && AuthenticatedUser?.Username != user.Username)
            throw new UnauthorizedAccessException("Only administrators can update other users");

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