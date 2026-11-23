using PlanejadorCompras.Application.Exceptions;
using PlanejadorCompras.Application.Features.Ai;
using PlanejadorCompras.Application.Features.Ai.Contracts;
using PlanejadorCompras.Application.Services.Interfaces;

namespace PlanejadorCompras.Application.UseCases.Ai;

/// <summary>
/// Single door for every AI call: checks configuration, takes one unit of the user's
/// monthly quota before calling the provider and gives it back when the call fails.
/// The limit is the same for every user; there are no plans.
/// </summary>
public sealed class AiUsageService(
    IAiClient aiClient,
    IAiUsageStore usageStore,
    ICurrentUser currentUser,
    AiSettings settings,
    TimeProvider timeProvider)
{
    public bool IsEnabled => settings.IsConfigured && aiClient.IsConfigured;

    public void EnsureEnabled()
    {
        if (!IsEnabled)
        {
            throw new AiNotConfiguredException();
        }
    }

    public async Task<AiStatusResponseDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var period = CurrentPeriod();
        var limit = Math.Max(settings.MonthlyLimitPerUser, 0);
        var used = await usageStore.GetUsedAsync(currentUser.UserId, period, cancellationToken);

        return new AiStatusResponseDto(
            IsEnabled,
            used,
            limit,
            Math.Max(limit - used, 0),
            period.AddMonths(1),
            settings.MaxFileBytes,
            settings.MaxInputCharacters);
    }

    /// <summary>
    /// Runs one provider call under the monthly limit. The quota unit is reserved first, so
    /// concurrent requests cannot go over the limit, and released if the provider call fails.
    /// A call the provider answered counts even when the answer turns out unusable, because
    /// it was billed.
    /// </summary>
    public async Task<string> CompleteJsonAsync(AiJsonRequest request, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();

        var userId = currentUser.UserId;
        var period = CurrentPeriod();
        var limit = Math.Max(settings.MonthlyLimitPerUser, 0);

        if (limit == 0 || !await usageStore.TryReserveAsync(userId, period, limit, cancellationToken))
        {
            throw new AiUsageLimitExceededException(limit, period.AddMonths(1));
        }

        try
        {
            return await aiClient.CompleteJsonAsync(request, cancellationToken);
        }
        catch (AiUnusableResponseException)
        {
            throw;
        }
        catch
        {
            // The request may already be cancelled; the release must still happen.
            await usageStore.ReleaseAsync(userId, period, CancellationToken.None);
            throw;
        }
    }

    private DateOnly CurrentPeriod()
    {
        var now = timeProvider.GetUtcNow();
        return new DateOnly(now.Year, now.Month, 1);
    }
}
