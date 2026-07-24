using System.Net.Http.Headers;
using System.Text;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Assistant;

public sealed class OpenAiCompatibleLlmClient : ILlmClient
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEncryptionService _encryptionService;

    public OpenAiCompatibleLlmClient(
        IHttpClientFactory httpClientFactory,
        IConfigurationRepository configurationRepository,
        IEncryptionService encryptionService)
    {
        _httpClientFactory = httpClientFactory;
        _configurationRepository = configurationRepository;
        _encryptionService = encryptionService;
    }

    public async IAsyncEnumerable<LlmStreamChunk> StreamChatAsync(
        IReadOnlyList<JObject> messages,
        IReadOnlyCollection<IAssistantTool> tools,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var configuration = await _configurationRepository.GetConfiguration();
        var token = _encryptionService.Decrypt(
            configuration.FigAssistantAccessTokenEncrypted,
            throwOnFailure: false);
        if (string.IsNullOrWhiteSpace(token) || token == configuration.FigAssistantAccessTokenEncrypted)
            throw new InvalidOperationException("Fig Assistant access token is not configured.");

        var requestBody = new JObject
        {
            ["model"] = configuration.FigAssistantModel,
            ["messages"] = new JArray(messages),
            ["stream"] = true,
            ["tools"] = new JArray(tools.Select(ToToolDefinition)),
            ["tool_choice"] = "auto"
        };

        AssistantTrace.TagLlmHttp(configuration.FigAssistantModel, configuration.FigAssistantEndpoint);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{configuration.FigAssistantEndpoint!.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Content = new StringContent(
            JsonConvert.SerializeObject(requestBody, JsonSettings),
            Encoding.UTF8,
            "application/json");

        var client = _httpClientFactory.CreateClient("FigAssistantLlm");
        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            using var errorReader = new StreamReader(responseStream);
            var error = await errorReader.ReadToEndAsync(cancellationToken);
            throw new HttpRequestException(
                $"LLM endpoint returned {(int)response.StatusCode}: {Truncate(error, 500)}");
        }

        using var reader = new StreamReader(responseStream);
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
                break;
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            var data = line.Substring(5).Trim();
            if (data == "[DONE]")
                yield break;

            JObject packet;
            try
            {
                packet = JObject.Parse(data);
            }
            catch (JsonReaderException)
            {
                continue;
            }

            var choice = packet["choices"]?.First;
            var delta = choice?["delta"];
            var content = delta?["content"]?.Value<string>();
            if (!string.IsNullOrEmpty(content))
                yield return new LlmStreamChunk { Text = content };

            if (delta?["tool_calls"] is JArray toolCalls)
            {
                foreach (var call in toolCalls.OfType<JObject>())
                {
                    yield return new LlmStreamChunk
                    {
                        ToolCallIndex = call["index"]?.Value<int>() ?? 0,
                        ToolCallId = call["id"]?.Value<string>(),
                        ToolName = call["function"]?["name"]?.Value<string>(),
                        ToolArguments = call["function"]?["arguments"]?.Value<string>()
                    };
                }
            }

            var finishReason = choice?["finish_reason"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(finishReason))
                yield return new LlmStreamChunk { FinishReason = finishReason };
        }
    }

    private static JObject ToToolDefinition(IAssistantTool tool)
    {
        return new JObject
        {
            ["type"] = "function",
            ["function"] = new JObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["parameters"] = JObject.Parse(tool.ParameterJsonSchema)
            }
        };
    }

    private static string Truncate(string value, int maximum) =>
        value.Length <= maximum ? value : value.Substring(0, maximum) + "...";
}
