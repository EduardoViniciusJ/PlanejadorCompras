using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Infrastructure.Services.Ai;

/// <summary>
/// Claude (Anthropic) implementation of <see cref="IAiClient"/>, using the official SDK.
/// Without an API key the client stays unconfigured and the API still starts.
/// Provider errors are logged without request content and reach the user only as
/// generic messages.
/// </summary>
public sealed class AnthropicAiClient : IAiClient
{
    private readonly AnthropicClient? _client;
    private readonly AiSettings _settings;
    private readonly ILogger<AnthropicAiClient> _logger;

    public AnthropicAiClient(string? apiKey, AiSettings settings, ILogger<AnthropicAiClient> logger)
    {
        _settings = settings;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _client = new AnthropicClient
            {
                ApiKey = apiKey.Trim(),
                Timeout = TimeSpan.FromSeconds(Math.Clamp(settings.TimeoutSeconds, 5, 600)),
                MaxRetries = 1
            };
        }
    }

    public bool IsConfigured => _client is not null;

    public async Task<string> CompleteJsonAsync(
        AiJsonRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new AiNotConfiguredException();
        }

        var parameters = BuildParameters(request);
        Message message;

        try
        {
            message = await _client.Messages.Create(parameters, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AnthropicRateLimitException exception)
        {
            _logger.LogWarning("AI provider rate limit reached ({ExceptionType}).", exception.GetType().Name);
            throw AiProviderException.Busy(exception);
        }
        catch (AnthropicUnauthorizedException exception)
        {
            _logger.LogError("AI provider rejected the configured API key ({ExceptionType}).", exception.GetType().Name);
            throw AiProviderException.Unavailable(exception);
        }
        catch (AnthropicForbiddenException exception)
        {
            _logger.LogError("AI provider denied access for the configured API key ({ExceptionType}).", exception.GetType().Name);
            throw AiProviderException.Unavailable(exception);
        }
        catch (AnthropicNotFoundException exception)
        {
            _logger.LogError("AI model '{Model}' was not found at the provider ({ExceptionType}).", _settings.Model, exception.GetType().Name);
            throw AiProviderException.Unavailable(exception);
        }
        catch (Anthropic4xxException exception)
        {
            // Usually an unreadable or oversized file. The message can echo user content, so only the type is logged.
            _logger.LogWarning("AI provider rejected the request ({ExceptionType}).", exception.GetType().Name);
            throw AiProviderException.Rejected(exception);
        }
        catch (Anthropic5xxException exception)
        {
            _logger.LogWarning("AI provider failed ({ExceptionType}).", exception.GetType().Name);
            throw AiProviderException.Busy(exception);
        }
        catch (Exception exception) when (exception is AnthropicException or HttpRequestException or OperationCanceledException or IOException)
        {
            // Timeouts and network failures.
            _logger.LogWarning("AI provider call did not complete ({ExceptionType}).", exception.GetType().Name);
            throw AiProviderException.Unavailable(exception);
        }

        _logger.LogInformation(
            "AI call completed. Model: {Model}; input tokens: {InputTokens}; output tokens: {OutputTokens}; stop reason: {StopReason}.",
            _settings.Model,
            message.Usage.InputTokens,
            message.Usage.OutputTokens,
            message.StopReason?.Raw());

        var stopReason = message.StopReason?.Raw();
        if (stopReason == "refusal")
        {
            throw AiUnusableResponseException.Unreadable();
        }

        if (stopReason == "max_tokens")
        {
            throw AiUnusableResponseException.TooLong();
        }

        var builder = new StringBuilder();
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var text))
            {
                builder.Append(text.Text);
            }
        }

        if (builder.Length == 0)
        {
            throw AiUnusableResponseException.Unreadable();
        }

        return builder.ToString();
    }

    private MessageCreateParams BuildParameters(AiJsonRequest request)
    {
        var content = new List<ContentBlockParam>();

        // Documents and images go before the text, as the provider recommends.
        if (request.Attachment is { } attachment)
        {
            var data = Convert.ToBase64String(attachment.Content);
            if (attachment.Kind == AiAttachmentKind.Pdf)
            {
                content.Add(new DocumentBlockParam { Source = new Base64PdfSource { Data = data } });
            }
            else
            {
                content.Add(new ImageBlockParam
                {
                    Source = new Base64ImageSource
                    {
                        Data = data,
                        MediaType = attachment.MediaType == "image/png" ? MediaType.ImagePng : MediaType.ImageJpeg
                    }
                });
            }
        }

        content.Add(new TextBlockParam { Text = request.UserText });

        var schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.JsonSchema)
            ?? throw new InvalidOperationException("AI JSON schema is empty.");

        var outputConfig = new OutputConfig { Format = new JsonOutputFormat { Schema = schema } };
        if (!string.IsNullOrWhiteSpace(_settings.Effort))
        {
            outputConfig = outputConfig with { Effort = _settings.Effort.Trim().ToLowerInvariant() };
        }

        return new MessageCreateParams
        {
            Model = _settings.Model,
            MaxTokens = _settings.MaxOutputTokens,
            System = request.SystemPrompt,
            Messages = [new UserMessageParam { Content = content }],
            OutputConfig = outputConfig
        };
    }
}
