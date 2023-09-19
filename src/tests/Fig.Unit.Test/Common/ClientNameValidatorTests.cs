using Fig.Common.NetStandard.Exceptions;
using Fig.Common.NetStandard.Validation;
using NUnit.Framework;

namespace Fig.Unit.Test.Common;

[TestFixture]
public class ClientNameValidatorTests
{
    [Test]
    public void ShallNotThrowExceptionForValidString()
    {
        Assert.DoesNotThrow(() => new ClientNameValidator().Validate("Hello World"));
    }

    [Test]
    public void ShallThrowExceptionWhenClientNameIncludesReservedCharacters()
    {
        Assert.Throws<InvalidClientNameException>(
            () => new ClientNameValidator().Validate("This [string] has *reserved* characters."));
    }

    [Test]
    public void ShallNotAcceptEmptyString()
    {
        Assert.Throws<InvalidClientNameException>(() => new ClientNameValidator().Validate(""));
    }
}