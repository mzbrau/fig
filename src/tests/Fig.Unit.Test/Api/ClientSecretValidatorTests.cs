using Fig.Api.Validators;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ClientSecretValidatorTests
{
    private ClientSecretValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ClientSecretValidator();
    }

    [TestCaseSource(nameof(ValidityMatrix))]
    public void IsValid_ShouldMatchLengthAndUniqueCharacterRules(string clientSecret, bool expected)
    {
        Assert.That(_sut.IsValid(clientSecret), Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> ValidityMatrix()
    {
        // Too short regardless of uniqueness
        yield return new TestCaseData("short", false).SetName("TooShort_FewChars");
        yield return new TestCaseData(new string('a', 31), false).SetName("Length31_RepeatedChar");
        yield return new TestCaseData("abcdefghijabcdefghijabcdefghij1", false).SetName("Length31_EnoughUnique");

        // Length ok but uniqueness below 10
        yield return new TestCaseData(new string('a', 32), false).SetName("Length32_OneUnique");
        yield return new TestCaseData(string.Concat(Enumerable.Repeat("abcdefghi", 4)), false)
            .SetName("Length36_NineUnique");

        // Digits 0-9 = 10 unique, length 32 -> valid
        yield return new TestCaseData("01234567890123456789012345678901", true)
            .SetName("Length32_TenUniqueDigits");

        // Exactly 10 unique, length 32
        yield return new TestCaseData("abcdefghijabcdefghijabcdefghijab", true)
            .SetName("Length32_TenUniqueLetters");

        // 9 unique, length 32
        yield return new TestCaseData("abcdefghiabcdefghiabcdefghiabcde", false)
            .SetName("Length32_NineUniqueLetters");

        // Longer than min with enough uniqueness
        yield return new TestCaseData("abcdefghijklmnopqrstuvwxyz012345", true)
            .SetName("Length32_ManyUnique");
        yield return new TestCaseData("aB3$dE5^gH7*jK9#mN1!pQ2@rS4%tU6&", true)
            .SetName("Length32_MixedUnique");

        // Boundary: length 32 with exactly 10 unique including symbols
        yield return new TestCaseData("!@#$%^&*()!@#$%^&*()!@#$%^&*()!@", true)
            .SetName("Length32_TenUniqueSymbols");
    }
}
