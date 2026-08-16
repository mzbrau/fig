using System;
using System.Threading.Tasks;
using Fig.Api;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.CustomActions;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class CustomActionServiceTests
{
    [Test]
    public void RequestExecution_ThrowsUnauthorized_WhenUserLacksClassificationAccess()
    {
        const string clientName = "MyClient";
        const string actionName = "DangerousAction";

        var customActionRepository = new Mock<ICustomActionRepository>();
        var executionRepository = new Mock<ICustomActionExecutionRepository>();

        customActionRepository
            .Setup(r => r.GetByName(clientName, actionName))
            .ReturnsAsync(new CustomActionBusinessEntity
            {
                Name = actionName,
                ClientName = clientName,
                Classification = Classification.Special,
                ButtonName = "Run",
                Description = "desc",
                SettingsUsed = string.Empty
            });

        var sut = new CustomActionService(
            customActionRepository.Object,
            executionRepository.Object,
            Mock.Of<ISettingClientRepository>(),
            Mock.Of<IEventLogFactory>(),
            Mock.Of<IEventLogRepository>(),
            NullLogger<CustomActionService>.Instance,
            Mock.Of<IRegistrationStatusValidator>());

        sut.SetAuthenticatedUser(new UserDataContract(
            Guid.NewGuid(),
            "limited-user",
            "Limited",
            "User",
            Role.User,
            ".*",
            [Classification.Technical]));

        Assert.That(
            async () => await sut.RequestExecution(clientName, new CustomActionExecutionRequestDataContract(actionName)),
            Throws.TypeOf<UnauthorizedAccessException>()
                .With.Message.Contains("does not have access to custom action"));

        executionRepository.Verify(
            r => r.AddExecutionRequest(It.IsAny<CustomActionExecutionBusinessEntity>()),
            Times.Never);
    }
}
