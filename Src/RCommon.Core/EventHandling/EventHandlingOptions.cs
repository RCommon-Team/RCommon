namespace RCommon.EventHandling
{
    /// <summary>
    /// Options controlling in-process event dispatch behaviour.
    /// </summary>
    public class EventHandlingOptions
    {
        /// <summary>
        /// The maximum number of cascade generations allowed during a single pre-commit domain-event
        /// drain before the cycle-breaker fails loud. A generation is the cascade depth: the event that
        /// triggered a handler is generation <c>n</c>; events that handler raises are generation
        /// <c>n + 1</c>. A wide fan-out at one generation is fine — only unbounded chains (A→B→A…) trip
        /// this limit. Seeds are generation 0; enqueuing an event beyond this limit throws a
        /// DispatchGenerationLimitException (added in the next task). Default: 16.
        /// </summary>
        public int MaxDispatchGenerations { get; set; } = 16;
    }
}
