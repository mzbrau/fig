using Sentry;

namespace Fig.Common.Sentry;

public class ExceptionCapture : IExceptionCapture
{
    private readonly bool _sentryEnabled;

    public ExceptionCapture(bool sentryEnabled)
    {
        _sentryEnabled = sentryEnabled;
    }
    
    public async Task Capture(Exception ex, bool flush = false)
    {
        if (!_sentryEnabled)
            return;
        
        SentrySdk.CaptureException(ex);
        
        if (flush)
            await SentrySdk.FlushAsync();
    }
}