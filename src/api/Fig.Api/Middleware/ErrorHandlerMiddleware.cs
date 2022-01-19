using System.Net;
using Fig.Api.Exceptions;
using Newtonsoft.Json;

namespace Fig.Api.Middleware;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
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

            switch(error)
            {
                case UserExistsException:
                case InvalidSettingException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case KeyNotFoundException:
                case UnknownUserException:
                case UnknownClientException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            // TODO: Exception shielding...
            var result = JsonConvert.SerializeObject(new { message = error?.Message });
            await response.WriteAsync(result);
        }
    }
}