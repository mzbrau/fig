using System.Net;
using Fig.Api.Exceptions;
using Fig.Api.Reports;
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

            var reference = Guid.NewGuid().ToString();
            // Can't modify response if it has already started
            if (response.HasStarted)
            {
                _logger.LogError(ex,
                    "Reference: {Reference}. Unhandled exception after response started. Status code: {StatusCode}",
                    reference,
                    response.StatusCode.ToString());
                return;
            }

            response.ContentType = "application/json";

            var mappedStatusCode = ex switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,

                UserExistsException => (int)HttpStatusCode.BadRequest,
                InvalidSettingException => (int)HttpStatusCode.BadRequest,
                InvalidClientSecretException => (int)HttpStatusCode.BadRequest,
                InvalidPasswordException => (int)HttpStatusCode.BadRequest,
                InvalidClientSecretChangeException => (int)HttpStatusCode.BadRequest,
                InvalidUserDeletionException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                ApplicationException => (int)HttpStatusCode.BadRequest,
                InvalidImportException => (int)HttpStatusCode.BadRequest,
                InvalidClientNameException => (int)HttpStatusCode.BadRequest,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                ReportParameterValidationException => (int)HttpStatusCode.BadRequest,

                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnknownUserException => (int)HttpStatusCode.NotFound,
                UnknownClientException => (int)HttpStatusCode.NotFound,
                ChangeNotFoundException => (int)HttpStatusCode.NotFound,
                ActionExecutionNotFoundException => (int)HttpStatusCode.NotFound,
                ReportNotFoundException => (int)HttpStatusCode.NotFound,

                _ => (int)HttpStatusCode.InternalServerError
            };

            response.StatusCode = mappedStatusCode;
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