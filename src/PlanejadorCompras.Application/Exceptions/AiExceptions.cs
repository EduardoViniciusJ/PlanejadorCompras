using System.Net;

namespace PlanejadorCompras.Application.Exceptions;

public sealed class AiNotConfiguredException : AppException
{
    public AiNotConfiguredException()
        : base(
            "IA não configurada neste servidor.",
            HttpStatusCode.ServiceUnavailable,
            "ai_not_configured")
    {
    }
}

public sealed class AiUsageLimitExceededException : AppException
{
    public AiUsageLimitExceededException(int limit, DateOnly renewsOn)
        : base(
            $"Você já usou os {limit} usos de IA deste mês. O limite renova em {renewsOn:dd/MM/yyyy}.",
            HttpStatusCode.TooManyRequests,
            "ai_usage_limit_exceeded")
    {
        Limit = limit;
        RenewsOn = renewsOn;
    }

    public int Limit { get; }

    public DateOnly RenewsOn { get; }

    public override IReadOnlyDictionary<string, object?> Extensions =>
        new Dictionary<string, object?>
        {
            ["limit"] = Limit,
            ["renewsOn"] = RenewsOn.ToString("yyyy-MM-dd")
        };
}

/// <summary>The provider call failed (network, timeout, provider error). The use is not counted.</summary>
public sealed class AiProviderException : AppException
{
    private AiProviderException(string message, HttpStatusCode statusCode, string errorCode, Exception? innerException)
        : base(message, statusCode, errorCode, innerException)
    {
    }

    public static AiProviderException Unavailable(Exception? innerException = null) =>
        new(
            "A IA está indisponível no momento. Tente novamente em instantes.",
            HttpStatusCode.ServiceUnavailable,
            "ai_unavailable",
            innerException);

    public static AiProviderException Busy(Exception? innerException = null) =>
        new(
            "A IA está sobrecarregada no momento. Tente novamente em alguns minutos.",
            HttpStatusCode.ServiceUnavailable,
            "ai_busy",
            innerException);

    public static AiProviderException Rejected(Exception? innerException = null) =>
        new(
            "A IA não conseguiu processar este conteúdo. Confira o arquivo ou envie o texto.",
            HttpStatusCode.UnprocessableEntity,
            "ai_rejected_input",
            innerException);
}

/// <summary>The provider answered, but the answer cannot be used. The use is counted.</summary>
public sealed class AiUnusableResponseException : AppException
{
    private AiUnusableResponseException(string message, string errorCode, Exception? innerException)
        : base(message, HttpStatusCode.UnprocessableEntity, errorCode, innerException)
    {
    }

    public static AiUnusableResponseException Unreadable(Exception? innerException = null) =>
        new(
            "A IA não conseguiu ler este conteúdo. Tente outro arquivo ou cole o texto.",
            "ai_unusable_response",
            innerException);

    public static AiUnusableResponseException TooLong() =>
        new(
            "O conteúdo é longo demais para uma leitura só. Envie em partes menores.",
            "ai_response_truncated",
            null);
}
