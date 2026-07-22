using System;
using RCommon.EventHandling;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Thrown when the pre-commit domain-event drain exceeds the configured cascade generation limit
    /// (<see cref="EventHandlingOptions.MaxDispatchGenerations"/>). A generation is the cascade depth of
    /// events-raising-events; exceeding the limit indicates an unbounded cascade (e.g. A→B→A…). This is a
    /// fail-loud safety net, not the drain's termination mechanism (empty-queue is).
    /// </summary>
    public class DispatchGenerationLimitException : GeneralException
    {
        /// <summary>The configured maximum number of cascade generations that was exceeded.</summary>
        public int MaxDispatchGenerations { get; }

        public DispatchGenerationLimitException(int maxDispatchGenerations)
            : base($"The pre-commit domain-event dispatch cascade exceeded the configured limit of " +
                   $"{maxDispatchGenerations} generation(s). This usually means a handler cycle " +
                   $"(e.g. A raises B, B raises A). Break the cycle or raise " +
                   $"{nameof(EventHandlingOptions)}.{nameof(EventHandlingOptions.MaxDispatchGenerations)} " +
                   $"if the cascade is legitimately deep.")
        {
            MaxDispatchGenerations = maxDispatchGenerations;
        }
    }
}
