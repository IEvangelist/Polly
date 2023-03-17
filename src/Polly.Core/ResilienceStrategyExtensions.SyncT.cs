using Polly;

namespace Polly;

public static partial class ResilienceStrategyExtensions
{
    /// <summary>
    /// Executes the specified callback.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the callback.</typeparam>
    /// <typeparam name="TState">The type of state associated with the callback.</typeparam>
    /// <param name="strategy">The instance of <see cref="IResilienceStrategy"/>.</param>
    /// <param name="callback">The user-provided callback.</param>
    /// <param name="context">The context associated with the callback.</param>
    /// <param name="state">The state associated with the callback.</param>
    /// <returns>An instance of <see cref="ValueTask"/> that represents the asynchronous execution.</returns>
    public static TResult Execute<TResult, TState>(
        this IResilienceStrategy strategy,
        Func<ResilienceContext, TState, TResult> callback,
        ResilienceContext context,
        TState state)
    {
        Guard.NotNull(strategy);
        Guard.NotNull(callback);
        Guard.NotNull(context);

        InitializeSyncContext<TResult>(context);

        return strategy.ExecuteInternalAsync((context, state) => new ValueTask<TResult>(state.callback(context, state.state)), context, (callback, state))
                       .GetResult();
    }

    /// <summary>
    /// Executes the specified callback.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the callback.</typeparam>
    /// <param name="strategy">The instance of <see cref="IResilienceStrategy"/>.</param>
    /// <param name="callback">The user-provided callback.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> associated with the callback.</param>
    /// <returns>An instance of <see cref="ValueTask"/> that represents the asynchronous execution.</returns>
    public static TResult Execute<TResult>(
        this IResilienceStrategy strategy,
        Func<CancellationToken, TResult> callback,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(strategy);
        Guard.NotNull(callback);

        var context = GetSyncContext<TResult>(cancellationToken);

        try
        {
            return strategy.ExecuteInternalAsync((context, state) => new ValueTask<TResult>(state(context.CancellationToken)), context, callback)
                           .GetResult();
        }
        finally
        {
            ResilienceContext.Return(context);
        }
    }

    private static ResilienceContext GetSyncContext<TResult>(CancellationToken cancellationToken)
    {
        var context = ResilienceContext.Get();
        context.CancellationToken = cancellationToken;

        InitializeSyncContext<TResult>(context);

        return context;
    }

    private static void InitializeSyncContext<TResult>(ResilienceContext context) => context.Initialize<TResult>(isSynchronous: true);
}