using System;
using System.Net;

namespace Fig.Contracts.WebHook;

public class TestResultDataContract
{
    public TestResultDataContract(WebHookType webHookType, string result, HttpStatusCode? statusCode, string? message, TimeSpan testDuration)
    {
        WebHookType = webHookType;
        Result = result;
        StatusCode = statusCode;
        Message = message;
        TestDuration = testDuration;
    }

    public WebHookType WebHookType { get; }
    
    public string Result { get; set; }
    
    public HttpStatusCode? StatusCode { get; set; }
    
    public string? Message { get; set; }
    
    public TimeSpan TestDuration { get; set; }
}