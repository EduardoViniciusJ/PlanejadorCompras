namespace PlanejadorCompras.Application.Services.Interfaces;

public enum AiAttachmentKind
{
    Image,
    Pdf
}

/// <summary>Binary input sent to the model next to the text (photo or PDF).</summary>
public sealed record AiAttachment(AiAttachmentKind Kind, string MediaType, byte[] Content);

/// <summary>
/// One structured request: the model must answer with JSON that follows the schema.
/// </summary>
public sealed record AiJsonRequest(
    string SystemPrompt,
    string UserText,
    AiAttachment? Attachment,
    string JsonSchema);

/// <summary>
/// Provider-agnostic entry point for AI calls. Implementations throw
/// AiNotConfiguredException when there is no key, AiProviderException when the call
/// failed and AiUnusableResponseException when the provider answered but the answer
/// cannot be used (refusal or truncated output).
/// </summary>
public interface IAiClient
{
    bool IsConfigured { get; }

    Task<string> CompleteJsonAsync(AiJsonRequest request, CancellationToken cancellationToken = default);
}
