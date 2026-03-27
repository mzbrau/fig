using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class UserToolsTests
{
    [Test]
    public async Task ListUsers_CallsApiAndReturnsSerializedUsers()
    {
        var mock = new Mock<IFigApiClient>();
        var userId = Guid.NewGuid();
        var users = new List<UserDataContract>
        {
            new(userId, "jdoe", "John", "Doe", Role.Administrator, ".*",
                new List<Classification> { Classification.Technical })
        };

        mock.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(users);

        var result = await UserTools.ListUsers(mock.Object, CancellationToken.None);

        mock.Verify(x => x.GetUsersAsync(), Times.Once);
        Assert.That(result, Does.Contain("jdoe"));
        Assert.That(result, Does.Contain("John"));
    }

    [Test]
    public async Task GetUser_CallsApiWithParsedGuid()
    {
        var mock = new Mock<IFigApiClient>();
        var userId = Guid.NewGuid();
        var user = new UserDataContract(userId, "asmith", "Alice", "Smith", Role.User, ".*",
            new List<Classification> { Classification.Functional });

        mock.Setup(x => x.GetUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await UserTools.GetUser(mock.Object, userId.ToString(), CancellationToken.None);

        mock.Verify(x => x.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("asmith"));
        Assert.That(result, Does.Contain("Alice"));
    }

    [Test]
    public async Task CreateUser_ParsesRoleAndCallsApi()
    {
        var mock = new Mock<IFigApiClient>();

        mock.Setup(x => x.RegisterUserAsync(
                It.IsAny<RegisterUserRequestDataContract>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await UserTools.CreateUser(
            mock.Object, "newuser", "New", "User", "Administrator", "P@ssw0rd!", CancellationToken.None);

        mock.Verify(x => x.RegisterUserAsync(
            It.Is<RegisterUserRequestDataContract>(r =>
                r.Username == "newuser" &&
                r.FirstName == "New" &&
                r.LastName == "User" &&
                r.Role == Role.Administrator &&
                r.Password == "P@ssw0rd!" &&
                r.ClientFilter == ".*"),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result, Does.Contain("newuser").IgnoreCase);
    }

    [Test]
    public async Task UpdateUser_DeserializesJsonAndCallsApi()
    {
        var mock = new Mock<IFigApiClient>();
        var userId = Guid.NewGuid();
        var user = new UpdateUserRequestDataContract
        {
            Username = "updateduser",
            FirstName = "Updated",
            LastName = "User",
            Role = Role.ReadOnly,
            ClientFilter = ".*",
            AllowedClassifications = new List<Classification> { Classification.Special }
        };
        var userJson = JsonConvert.SerializeObject(user);

        mock.Setup(x => x.UpdateUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<UpdateUserRequestDataContract>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await UserTools.UpdateUser(
            mock.Object, userId.ToString(), userJson, CancellationToken.None);

        mock.Verify(x => x.UpdateUserAsync(
            userId,
            It.Is<UpdateUserRequestDataContract>(u => u.Username == "updateduser"),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result, Does.Contain(userId.ToString()));
    }

    [Test]
    public async Task DeleteUser_ParsesGuidAndCallsApi()
    {
        var mock = new Mock<IFigApiClient>();
        var userId = Guid.NewGuid();

        mock.Setup(x => x.DeleteUserAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await UserTools.DeleteUser(
            mock.Object, userId.ToString(), CancellationToken.None);

        mock.Verify(x => x.DeleteUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain(userId.ToString()).IgnoreCase);
    }
}
