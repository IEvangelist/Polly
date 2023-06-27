namespace Polly.Utils;

/// <summary>
/// Extensions for <see cref="TimeProvider"/> used by resilience strategies.
/// </summary>
internal static class TimeProviderExtensions
{
    /// <summary>
    /// Delays the execution for the specified time span.
    /// </summary>
    /// <param name="timeProvider">The instance of <see cref="TimeProvider"/>.</param>
    /// <param name="delay">For how long we should delay.</param>
    /// <param name="context">The resilience context.</param>
    /// <returns>The task.</returns>
    /// <remarks>
    /// The delay is performed synchronously if the <see cref="ResilienceContext.IsSynchronous"/> property is true, otherwise the delay is performed asynchronously.
    /// This method will be public later.
    /// </remarks>
    public static Task DelayAsync(this TimeProvider timeProvider, TimeSpan delay, ResilienceContext context)
    {
        Guard.NotNull(timeProvider);
        Guard.NotNull(context);

        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.IsSynchronous)
        {
#pragma warning disable CA1849
            // For synchronous scenarios we want to return a completed task. We avoid
            // the use of Thread.Sleep() here because it is not cancellable and to
            // simplify the code. Sync-over-async is not a concern here because it
            // only applies in the case of a resilience event and not on the hot path.
#if NET8_0_OR_GREATER
            Task.Delay(delay, timeProvider, context.CancellationToken).GetAwaiter().GetResult();
#else
            timeProvider.Delay(delay, context.CancellationToken).GetAwaiter().GetResult();
#endif
#pragma warning restore CA1849

            return Task.CompletedTask;
        }

#if NET8_0_OR_GREATER
        return Task.Delay(delay, timeProvider, context.CancellationToken);
#else
        return timeProvider.Delay(delay, context.CancellationToken);
#endif
    }
}
