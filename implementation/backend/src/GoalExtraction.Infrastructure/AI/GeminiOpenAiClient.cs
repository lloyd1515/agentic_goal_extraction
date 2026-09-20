namespace GoalExtraction.Infrastructure.AI;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GoalExtraction.Application.AI;
using GoalExtraction.Application.AI.Models;
using GoalExtraction.Infrastructure.AI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class GeminiOpenAiClient : IGeminiOpenAiClient
{
    private readonly HttpClient _httpClient;
    private readonly GeminiAiOptions _options;
    private readonly ILogger<GeminiOpenAiClient> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public GeminiOpenAiClient(
        HttpClient httpClient,
        IOptions<GeminiAiOptions> options,
        ILogger<GeminiOpenAiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        EnsureClientConfigured();
    }

    private void EnsureClientConfigured()
    {
        if (_httpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        if (_httpClient.DefaultRequestHeaders.Authorization == null && !string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Model))
        {
            request.Model = _options.Model;
        }

        // Gemini OpenAI-compatible endpoint does not support response_format alongside tool calling
        if (request.Tools != null && request.Tools.Count > 0)
        {
            request.ResponseFormat = null;
        }

        var jsonPayload = JsonSerializer.Serialize(request, SerializerOptions);
        using var requestContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var endpoint = _httpClient.BaseAddress != null 
            ? "chat/completions" 
            : new Uri(new Uri(_options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/"), "chat/completions").ToString();

        var response = await _httpClient.PostAsync(endpoint, requestContent, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Gemini OpenAI API call failed with status code {StatusCode}: {ErrorBody}", 
                response.StatusCode, errorBody);
            throw new HttpRequestException($"Gemini API error ({response.StatusCode}): {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, SerializerOptions);
        if (completion == null)
        {
            throw new InvalidOperationException("Failed to deserialize Gemini OpenAI chat completion response.");
        }

        return completion;
    }
}
