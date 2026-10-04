namespace Fig.Test.Common;

/// <summary>
/// Generates client secrets that always satisfy <c>ClientSecretValidator</c>
/// (length ≥ 32 and ≥ 10 unique characters). A bare <c>Guid.NewGuid().ToString()</c>
/// can occasionally fail uniqueness because the hex alphabet is small.
/// </summary>
public static class TestClientSecrets
{
    public static string New()
    {
        // Prefix contributes 9 unique chars (f,i,g,-,t,e,s,c,r); GUID v4 always includes '4'.
        return $"fig-test-secret-{Guid.NewGuid():N}";
    }
}
