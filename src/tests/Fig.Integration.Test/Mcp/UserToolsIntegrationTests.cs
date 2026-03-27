using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Authentication;
using Fig.Mcp.Tools;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Mcp;

[TestFixture]
public class UserToolsIntegrationTests : McpToolIntegrationTestBase
{
    [Test]
    public async Task ListUsers_ReturnsAdminUser()
    {
        var result = await UserTools.ListUsers(McpApiClient, CancellationToken.None);

        Assert.That(result, Does.Contain("admin"));
    }

    [Test]
    public async Task CreateUser_ViaTools_AppearsInUserList()
    {
        var result = await UserTools.CreateUser(
            McpApiClient, "newMcpUser", "Mcp", "User", "User",
            "complexPassword123!", CancellationToken.None);

        Assert.That(result, Is.Not.Empty);

        var listResult = await UserTools.ListUsers(McpApiClient, CancellationToken.None);
        Assert.That(listResult, Does.Contain("newMcpUser"));
    }

    [Test]
    public async Task GetUser_ForExistingUser_ReturnsUserDetails()
    {
        var newUser = NewUser("testGetUser", "Test", "User");
        var userId = await CreateUser(newUser);

        var result = await UserTools.GetUser(
            McpApiClient, userId.ToString(), CancellationToken.None);

        Assert.That(result, Does.Contain("testGetUser"));
        Assert.That(result, Does.Contain("Test"));
    }

    [Test]
    public async Task UpdateUser_ModifiesUserDetails()
    {
        var newUser = NewUser("testUpdateUser", "Original", "Name");
        var userId = await CreateUser(newUser);

        var user = await GetUser(userId);
        var updatedUser = new UpdateUserRequestDataContract
        {
            Username = user.Username,
            FirstName = "Updated",
            LastName = user.LastName,
            Role = user.Role,
            ClientFilter = user.ClientFilter,
            AllowedClassifications = user.AllowedClassifications.ToList()
        };

        var userJson = JsonConvert.SerializeObject(updatedUser, JsonSettings.FigDefault);
        var result = await UserTools.UpdateUser(
            McpApiClient, userId.ToString(), userJson, CancellationToken.None);

        Assert.That(result, Does.Contain("updated successfully"));

        var verifyUser = await GetUser(userId);
        Assert.That(verifyUser.FirstName, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task DeleteUser_RemovesUser()
    {
        var newUser = NewUser("testDeleteUser", "Delete", "Me");
        var userId = await CreateUser(newUser);

        await UserTools.DeleteUser(
            McpApiClient, userId.ToString(), CancellationToken.None);

        var users = await GetUsers();
        Assert.That(users.Any(u => u.Username == "testDeleteUser"), Is.False);
    }
}
