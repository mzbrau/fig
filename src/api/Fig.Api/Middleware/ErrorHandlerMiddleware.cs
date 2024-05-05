using System.Net;
using Fig.Api.Exceptions;
using Fig.Common.NetStandard.Exceptions;
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
        catch (Exception ex)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            switch (ex)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int) HttpStatusCode.Unauthorized;
                    break;
                case UserExistsException:
                case InvalidSettingException:
                case InvalidClientSecretException:
                case InvalidPasswordException:
                case InvalidClientSecretChangeException:
                case InvalidUserDeletionException:
                case ApplicationException:
                case InvalidImportException:
                case InvalidClientNameException:
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
            _logger.LogError(ex, "Reference: {Reference}. Status code: {StatusCode}", reference, response.StatusCode.ToString());

            var detail = _hostEnvironment.IsDevelopment() ? ex?.ToString() : null;
            var result = new ErrorResultDataContract(response.StatusCode.ToString(), 
                ex?.Message ?? "Unknown",
                detail, reference);

            var serializedResult = JsonConvert.SerializeObject(result);
            await response.WriteAsync(serializedResult);
        }
    }
}