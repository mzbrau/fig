using System.Net;
using Fig.Api.Exceptions;
using Fig.Api.SettingVerification.Exceptions;
using Fig.Contracts;
using Newtonsoft.Json;

namespace Fig.Api.Middleware;

public class ErrorHandlerMiddleware
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ErrorHandlerMiddleware(RequestDelegate next, IHostEnvironment hostEnvironment,
        ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            switch (error)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    break;
                case UserExistsException:
                case InvalidSettingException:
                case InvalidClientSecretException:
                case CompileErrorException:
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
                    break;
                case KeyNotFoundException:
                case UnknownUserException:
                case UnknownClientException:
                case UnknownVerificationException:
                    response.StatusCode = (int) HttpStatusCode.NotFound;
                    break;
                default:
                    response.StatusCode = (int) HttpStatusCode.InternalServerError;
                    break;
            }

            var reference = Guid.NewGuid().ToString();
            _logger.LogError($"Reference: {reference}{Environment.NewLine}{error}");

            var result = new ErrorResultDataContract
            {
                ErrorType = response.StatusCode.ToString(),
                Message = error?.Message,
                Reference = reference
            };

            if (_hostEnvironment.IsDevelopment())
                result.Detail = error?.ToString();

            var serializedResult = JsonConvert.SerializeObject(result);
            await response.WriteAsync(serializedResult);
        }
    }
}