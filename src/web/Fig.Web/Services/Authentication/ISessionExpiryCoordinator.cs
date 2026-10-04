namespace Fig.Web.Services.Authentication;

public interface ISessionExpiryCoordinator : IDisposable
{
    bool IsReauthenticationPending { get; }

    /// <summary>
    /// When true, abandoning the re-auth dialog should fully log the user out
    /// (session already expired). When false, the dialog was opened to extend
    /// a still-valid session and cancel only dismisses the prompt.
    /// </summary>
    bool LogoutOnAbandon { get; }

    event Action? ReauthenticationRequired;

    event Action? SessionExpiringSoon;

    void NotifySessionExpired();

    void RequestReauthentication();

    void MarkReauthenticationCompleted();

    void MarkReauthenticationAbandoned();

    void StartMonitoring();

    void StopMonitoring();
}
