using Xunit;

// Several test classes in this assembly exercise ClaimTypesConst, which is a process-wide mutable
// static (configure-once-at-startup). ClaimTypesConstTests and ClaimsIdentityExtensionsTests both
// call the internal ClaimTypesConst.Reset() in their constructor/Dispose for isolation. Under xUnit's
// default per-class parallelism these classes race on that global: a parallel Reset() can null the
// options mid-test, so a subsequent property read rebuilds defaults and a configured value (e.g. a
// custom Role) reverts to the default claim URI. Serialize the assembly's tests to make the shared
// static state deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
