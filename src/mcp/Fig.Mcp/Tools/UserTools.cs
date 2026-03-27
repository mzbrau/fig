using System.ComponentModel;
using Fig.Contracts.Authentication;
using Fig.Mcp.ApiClient;
using ModelContextProtocol.Server;
using Newtonsoft.Json;

namespace Fig.Mcp.Tools;

[McpServerToolType]
public class UserTools
{
    [McpServerTool, Description("List all Fig user accounts. " +
        "Returns user IDs, usernames, names, roles, and client filter assignments. " +
        "Requires administrator privileges.")]
    public static async Task<string> ListUsers(
        IFigApiClient apiClient,
        CancellationToken cancellationToken)
    {
        var users = await apiClient.GetUsersAsync(cancellationToken);
        return JsonConvert.SerializeObject(users, Formatting.Indented);
    }

    [McpServerTool, Description("Get detailed information about a specific Fig user account including their role, " +
        "client filter, and allowed classifications. Requires administrator privileges.")]
    public static async Task<string> GetUser(
        IFigApiClient apiClient,
        [Description("The unique identifier (GUID) of the user to retrieve. Use list_users to find user IDs.")] string userId,
        CancellationToken cancellationToken)
    {
        var user = await apiClient.GetUserAsync(Guid.Parse(userId), cancellationToken);
        return JsonConvert.SerializeObject(user, Formatting.Indented);
    }

    [McpServerTool, Description("Create a new Fig user account. Requires administrator privileges. " +
        "The user will be able to log in and manage configuration according to their assigned role. " +
        "Roles: Administrator (full access), User (can modify settings), ReadOnly (view only).")]
    public static async Task<string> CreateUser(
        IFigApiClient apiClient,
        [Description("The login username for the new account.")] string username,
        [Description("The user's first name.")] string firstName,
        [Description("The user's last name.")] string lastName,
        [Description("The role to assign. Valid values: Administrator, User, ReadOnly.")] string role,
        [Description("The initial password for the account.")] string password,
        CancellationToken cancellationToken)
    {
        var parsedRole = Enum.Parse<Role>(role);
        var request = new RegisterUserRequestDataContract(username, firstName, lastName, parsedRole, password, ".*", []);
        await apiClient.RegisterUserAsync(request, cancellationToken);
        return $"User '{username}' created successfully with role '{role}'.";
    }

    [McpServerTool, Description("Update an existing Fig user's details including name, role, client filter, " +
        "and allowed classifications. Requires administrator privileges. " +
        "Provide the full user object as JSON — all fields will be updated to match the provided values.")]
    public static async Task<string> UpdateUser(
        IFigApiClient apiClient,
        [Description("The unique identifier (GUID) of the user to update. Use list_users to find user IDs.")] string userId,
        [Description("A JSON representation of the updated user. Must include: Username, FirstName, LastName, Role (Administrator/User/ReadOnly), ClientFilter (regex), AllowedClassifications (array).")] string userJson,
        CancellationToken cancellationToken)
    {
        var user = JsonConvert.DeserializeObject<UpdateUserRequestDataContract>(userJson)
            ?? throw new ArgumentException("Failed to deserialize user JSON. Ensure the format matches the UpdateUserRequestDataContract structure.");

        await apiClient.UpdateUserAsync(Guid.Parse(userId), user, cancellationToken);
        return $"User '{userId}' updated successfully.";
    }

    [McpServerTool, Description("Permanently delete a Fig user account. " +
        "⚠️ WARNING: This action is permanent and cannot be undone. " +
        "The user will immediately lose access to Fig. Requires administrator privileges.")]
    public static async Task<string> DeleteUser(
        IFigApiClient apiClient,
        [Description("The unique identifier (GUID) of the user to delete. Use list_users to find user IDs.")] string userId,
        CancellationToken cancellationToken)
    {
        await apiClient.DeleteUserAsync(Guid.Parse(userId), cancellationToken);
        return $"User '{userId}' has been permanently deleted.";
    }
}
