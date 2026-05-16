using System;

namespace RCommon
{
    /// <summary>
    /// Tracks whether a singleton-style RCommon verb has been configured and which implementation
    /// type was chosen. Used by verbs like <c>WithSimpleGuidGenerator</c> and <c>WithDateTimeSystem</c>
    /// to enforce same-type-idempotent / different-type-throw semantics across modular calls.
    /// </summary>
    internal struct SingletonRegistration
    {
        public bool Configured;
        public Type? ImplementationType;
    }
}
