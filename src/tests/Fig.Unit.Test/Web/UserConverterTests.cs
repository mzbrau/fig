using Fig.Client.Abstractions.Data;
using Fig.Web.Converters;
using Fig.Web.Models.Authentication;
using NUnit.Framework;
using Role = Fig.Contracts.Authentication.Role;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class UserConverterTests
{
    private readonly UserConverter _sut = new();

    [Test]
    public void ConvertForUpdate_ShouldNotSendPasswordChangeRequirement_WhenNotOverridden()
    {
        var model = CreateUserModel();

        var result = _sut.ConvertForUpdate(model);

        Assert.That(result.PasswordChangeRequired, Is.Null);
    }

    [Test]
    public void ConvertForUpdate_ShouldSendPasswordChangeRequirement_WhenOverridden()
    {
        var model = CreateUserModel();
        model.PasswordChangeRequired = true;
        model.PasswordChangeRequiredOverridden = true;

        var result = _sut.ConvertForUpdate(model);

        Assert.That(result.PasswordChangeRequired, Is.True);
    }

    [Test]
    public void ConvertForUpdate_ShouldSendPasswordChangeRequirement_WhenOverriddenToFalse()
    {
        var model = CreateUserModel();
        model.PasswordChangeRequired = false;
        model.PasswordChangeRequiredOverridden = true;

        var result = _sut.ConvertForUpdate(model);

        Assert.That(result.PasswordChangeRequired, Is.False);
    }

    [Test]
    public void ConvertForUpdate_ShouldSendPasswordChangeRequirement_WhenAutoApplied()
    {
        var model = CreateUserModel();
        model.PasswordChangeRequired = true;
        model.PasswordChangeRequiredAutoApplied = true;

        var result = _sut.ConvertForUpdate(model);

        Assert.That(result.PasswordChangeRequired, Is.True);
    }

    private static UserModel CreateUserModel()
    {
        return new UserModel
        {
            Id = Guid.NewGuid(),
            Username = "test-user",
            FirstName = "Test",
            LastName = "User",
            Role = Role.Administrator,
            ClientFilter = ".*",
            AllowedClassifications = Enum.GetValues<Classification>().ToList()
        };
    }
}
