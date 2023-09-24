using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class UserIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task ShallCreateUser()
    {
        var user = NewUser();
        var id = await CreateUser(user);

        var users = (await GetUsers()).Where(a => a.Username != UserName).ToList();
        
        Assert.That(users.Count, Is.EqualTo(1));
        var firstUser = users.First();
        firstUser.Should().BeEquivalentTo(user, config => config.Excluding(prop =>
            prop.Password));
        Assert.That(firstUser.Id, Is.EqualTo(id));
    }

    [Test]
    public async Task ShallGetUserById()
    {
        var user = NewUser();
        var id = await CreateUser(user);
        var createdUser = await GetUser(id);

        createdUser.Should().BeEquivalentTo(user,
            config => config.Excluding(prop =>
                prop.Password));
        Assert.That(createdUser.Id, Is.EqualTo(id));
    }

    [Test]
    public async Task ShallGetAllUsers()
    {
        var user1 = NewUser();
        var user2 = NewUser("number2", "num", "two");
        var user3 = NewUser("number3", "numb", "three", Role.Administrator, "long long complex pass?");
        var id1 = await CreateUser(user1);
        var id2 = await CreateUser(user2);
        var id3 = await CreateUser(user3);

        var users = (await GetUsers()).Where(a => a.Username != UserName).ToList();
        
        Assert.That(users.Count, Is.EqualTo(3));
        users.Single(a => a.Id == id1).Should().BeEquivalentTo(user1, config => config.Excluding(prop =>
            prop.Password));
        users.Single(a => a.Id == id2).Should().BeEquivalentTo(user2, config => config.Excluding(prop =>
            prop.Password));
        users.Single(a => a.Id == id3).Should().BeEquivalentTo(user3, config => config.Excluding(prop =>
            prop.Password));
    }

    [Test]
    public async Task ShallDeleteUser()
    {
        var user = NewUser();
        var id = await CreateUser(user);
        var createdUser = await GetUser(id);
        Assert.That(createdUser, Is.Not.Null);
        await DeleteUser(id);
        
        var users = (await GetUsers()).Where(a => a.Username != UserName).ToList();
        Assert.That(users.Count, Is.Zero);
    }

    [Test]
    public async Task ShallUpdateUserDetailsNotPassword()
    {
        var user = NewUser();
        var id = await CreateUser(user);

        var update = new UpdateUserRequestDataContract
        {
            Username = "changedUser",
            FirstName = "Changed",
            LastName = "Userz",
            Role = Role.User,
            ClientFilter = "Some Updated Filter"
        };
        await UpdateUser(id, update);
        var updatedUser = await GetUser(id);

        updatedUser.Should().BeEquivalentTo(update,
            config => config.Excluding(prop =>
                prop.Password));
    }

    [Test]
    public async Task ShallUpdateUserPassword()
    {
        var user = NewUser();
        var id = await CreateUser(user);

        var result = await Login(user.Username, user.Password);
        Assert.That(result.Token, Is.Not.Null, "Original password should work for logins");
        
        var update = new UpdateUserRequestDataContract
        {
            Password = "newPassword123$$"
        };
        await UpdateUser(id, update);

        var result2 = await Login(user.Username, update.Password);
        Assert.That(result2.Token, Is.Not.Null, "Updated password should work for logins");
    }
    
    [Test]
    public async Task ShallPreventWeakPasswordsOnUpdates()
    {
        var user = NewUser();
        var id = await CreateUser(user);
        var uri = $"/users/{id}";

        var update = new UpdateUserRequestDataContract
        {
            Password = "xxx"
        };

        await ApiClient.PutAndVerify(uri, update, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShallPreventWeakPasswordsOnCreation()
    {
        const string uri = "/users/register";
        
        var user = NewUser();
        user.Password = "yyy";

        await ApiClient.PostAndVerify(uri, user, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShallUpdateAllUserProperties()
    {
        var user = NewUser();
        var id = await CreateUser(user);

        var update = new UpdateUserRequestDataContract
        {
            Username = "changedUser",
            FirstName = "Changed",
            LastName = "Userz",
            Role = Role.User,
            Password = "what is the password!",
            ClientFilter = "Some Updated Filter"
        };
        await UpdateUser(id, update);
        var updatedUser = await GetUser(id);

        updatedUser.Should().BeEquivalentTo(update,
            config => config.Excluding(prop =>
                prop.Password));
        
        var result = await Login(update.Username, update.Password);
        Assert.That(result.Token, Is.Not.Null, "Updated credentials should work for logins");
    }

    [Test]
    public async Task ShallNotAllowUserRoleToCreateUser()
    {
        var naughtyUser = NewUser("naughtyUser");
        await CreateUser(naughtyUser);

        var loginResult = await Login(naughtyUser.Username, naughtyUser.Password);
        
        var userToCreate = NewUser();
        var json = JsonConvert.SerializeObject(userToCreate);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", loginResult.Token);
        var uri = "/users/register";
        var result = await httpClient.PostAsync(uri, data);

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized),
            "Users should not be able to create other users");
    }

    [Test]
    public async Task ShallNotAllowOtherUserUpdatesAsUserRole()
    {
        var naughtyUser = NewUser("naughtyUser");
        await CreateUser(naughtyUser);

        var userToEdit = NewUser();
        var id = await CreateUser(userToEdit);
        
        var loginResult = await Login(naughtyUser.Username, naughtyUser.Password);
        
        var update = new UpdateUserRequestDataContract
        {
            Username = "changedUser",
            FirstName = "Changed",
            LastName = "Userz",
            Role = Role.User,
            Password = "xxx"
        };

        var json = JsonConvert.SerializeObject(update);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", loginResult.Token);
        var uri = $"/users/{id}";
        var result = await httpClient.PutAsync(uri, data);

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized),
            "Users are not allowed to edit other users");
    }

    [Test]
    public async Task ShallAllowUserToEditThemselves()
    {
        var editingUser = NewUser();
        var id = await CreateUser(editingUser);

        var loginResult = await Login(editingUser.Username, editingUser.Password);
        
        var update = new UpdateUserRequestDataContract
        {
            Username = "changedUser",
            FirstName = "Changed",
            LastName = "Userz",
            Password = "this is an updated password!"
        };

        var json = JsonConvert.SerializeObject(update);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", loginResult.Token);
        var uri = $"/users/{id}";
        var result = await httpClient.PutAsync(uri, data);

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status200OK),
            "Users should be able to edit themselves.");
    }

    [Test]
    public async Task ShallPreventUsersFromUpgradingTheirRoleToAdministrator()
    {
        var editingUser = NewUser();
        var id = await CreateUser(editingUser);

        var loginResult = await Login(editingUser.Username, editingUser.Password);
        
        var update = new UpdateUserRequestDataContract
        {
            Username = "changedUser",
            FirstName = "Changed",
            LastName = "Userz",
            Role = Role.Administrator,
            Password = "super long password..."
        };

        var json = JsonConvert.SerializeObject(update);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", loginResult.Token);
        var uri = $"/users/{id}";
        var result = await httpClient.PutAsync(uri, data);

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized),
            "Users should not be able to change their role.");
    }

    [Test]
    public async Task ShallSetUserClientFilter()
    {
        const string filter = "someFilter";
        var user = NewUser(role: Role.User, clientFilter: filter);
        var id = await CreateUser(user);

        var users = await GetUsers();
        var matchingUser = users.FirstOrDefault(a => a.Id == id);
        
        Assert.That(matchingUser?.ClientFilter, Is.EqualTo(filter));
    }

    [Test]
    public async Task ShallAcceptPreviousServerSecretWhenValidatingUserAuthentication()
    {
        await RegisterSettings<ThreeSettings>();
        
        Settings.PreviousSecret = Settings.Secret;
        Settings.Secret = "c11210c0fe854bdba85f1119e4d4df9a";

        // Should be able to get clients even though secret has changed.
        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients.Count, Is.EqualTo(1));
    }
}