namespace Fig.Web.Javascript;

public interface IJavascriptDisabledDialogCoordinator
{
    Task<bool> ShouldAutoOpen();

    Task SuppressPermanently();
}
