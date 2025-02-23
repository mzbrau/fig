using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;
using System.IO;
using System.IO.Compression;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class CompressionTests : IntegrationTestBase
{
    [TestCase("gzip")]
    [TestCase("br")]
    public async Task WhenCompressionIsRequested_ResponseShouldBeCompressed(string encoding)
    {
        await RegisterSettings<ThreeSettings>();
        var user = NewUser(role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        var client = GetHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue(encoding));

        // Act
        var response = await client.GetAsync("/clients");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(response.Content.Headers.ContentEncoding, Does.Contain(encoding));
    }

    [Test]
    public async Task WhenCompressionIsNotRequested_ResponseShouldNotBeCompressed()
    {
        await RegisterSettings<ThreeSettings>();
        var user = NewUser(role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        var client = GetHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        
        // Act
        var response = await client.GetAsync("/clients");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(response.Content.Headers.ContentEncoding, Is.Empty);
    }

    [Test]
    public async Task WhenMultipleCompressionAlgorithmsAreSupported_ShouldPreferBrotli()
    {
        await RegisterSettings<ThreeSettings>();
        var user = NewUser(role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        var client = GetHttpClient();
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        // Act
        var response = await client.GetAsync("/clients");

        // Assert
        Assert.That(response.IsSuccessStatusCode, Is.True);
        Assert.That(response.Content.Headers.ContentEncoding, Has.Count.EqualTo(1));
        Assert.That(response.Content.Headers.ContentEncoding, Does.Contain("br"));
    }
    
    
    [TestCase("gzip")]
    [TestCase("br")]
    public async Task CompressedAndUncompressedResponses_ShouldHaveMatchingContent(string encoding)
    {
        await RegisterSettings<ThreeSettings>();
        var user = NewUser(role: Role.User);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);
        
        var client = GetHttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        
        // Get uncompressed response (clear Accept-Encoding header)
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        var uncompressedResponse = await client.GetAsync("/clients");
        var uncompressedContent = await uncompressedResponse.Content.ReadAsStringAsync();
        
        // Add compression request header and get compressed response
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue(encoding));
        var compressedResponse = await client.GetAsync("/clients");
        var compressedContent = await ReadDecompressedContentAsync(compressedResponse);

        Assert.That(uncompressedResponse.IsSuccessStatusCode, Is.True);
        Assert.That(compressedResponse.IsSuccessStatusCode, Is.True);
        Assert.That(uncompressedContent, Is.EqualTo(compressedContent));
    }

    private async Task<string> ReadDecompressedContentAsync(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentEncoding.Count == 0)
        {
            return await response.Content.ReadAsStringAsync();
        }

        var stream = await response.Content.ReadAsStreamAsync();
        if (response.Content.Headers.ContentEncoding.Contains("br"))
        {
            await using var brotli = new BrotliStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(brotli);
            return await reader.ReadToEndAsync();
        }

        if (response.Content.Headers.ContentEncoding.Contains("gzip"))
        {
            await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            return await reader.ReadToEndAsync();
        }

        // Fallback: read raw.
        return await response.Content.ReadAsStringAsync();
    }
}