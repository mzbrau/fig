namespace Fig.Common.Sentry;

public interface IExceptionCapture
{
    Task Capture(Exception ex, bool flush = false);
}