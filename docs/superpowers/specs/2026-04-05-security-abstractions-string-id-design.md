# Security Abstractions: String-Based User Identity

**Date:** 2026-04-05
**Status:** Approved
**Breaking Change:** Yes (alpha)

## Problem

`ICurrentUser.Id` exposes `Guid?`, but the underlying claim (`NameIdentifier`) is a string in .NET's claims model. The `FindUserId()` extension silently parses the claim to `Guid` via `Guid.TryParse`, returning `null` for any non-GUID identifier (integers, Auth0 `auth0|abc123`, Firebase UIDs, etc.). This is a lossy, opinionated conversion that doesn't belong in a framework abstraction.

Additional issues:
- `IsAuthenticated` is derived from `Id.HasValue`, meaning a user with a valid authenticated identity but a non-GUID NameIdentifier appears unauthenticated.
- `FindClaimValue<T>` uses `Convert.ChangeType` which handles many types poorly.
- `ClaimTypesConst` uses mutable static setters, allowing runtime mutation of global claim type mappings at any time. Not thread-safe.
- `ICurrentClient.Id` is already `string?`, making the `Guid?` on `ICurrentUser.Id` inconsistent.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| `ICurrentUser.Id` type | `string?` | Aligns with claims standard; consumers parse downstream |
| `IsAuthenticated` derivation | `ClaimsIdentity.IsAuthenticated` | Decouples authentication from ID presence; aligns with .NET standard |
| `FindUserId()` return type | `string?` | Consistent with `FindTenantId()` and `FindClientId()` |
| `GetId()` return type | `string` | Convenience stays, type follows `Id` |
| `ClaimTypesConst` mutation | `Configure(Action<ClaimTypesOptions>)` freeze pattern | One-time config at startup, frozen after first use |
| `FindClaimValue<T>` | Remove | YAGNI; consumers parse claim strings themselves |
| `ICurrentClient.IsAuthenticated` | No change (`Id != null`) | Correct for client credential flows |
| Migration approach | Single breaking change | Alpha stage; clean break, no deprecation |

## Design

### Interface Changes

**`ICurrentUser`**
- `Guid? Id` becomes `string? Id`
- `bool IsAuthenticated` signature unchanged; contract changes to reflect `ClaimsIdentity.IsAuthenticated`

**`ClaimTypesConst` → freeze-on-first-use pattern**

Replace mutable static `{ get; set; }` properties with a configure-once pattern:

1. Introduce a `ClaimTypesOptions` class with settable properties for each claim type mapping (`UserId`, `Role`, `Email`, `TenantId`, `ClientId`, `UserName`, `Name`, `SurName`). Defaults match the current `ClaimTypes.*` values.
2. `ClaimTypesConst` gets a `Configure(Action<ClaimTypesOptions> configure)` static method. Calling it applies the action to the options and freezes the configuration. A second call throws `InvalidOperationException`.
3. The existing static getters (`ClaimTypesConst.UserId`, etc.) become read-only properties backed by the frozen options instance. On first access, if `Configure` was never called, they resolve to defaults (same behavior as today for apps that don't customize).

**Consumer migration:**

```csharp
// Before (mutable statics):
ClaimTypesConst.UserId   = "sub";
ClaimTypesConst.Role     = "roles";
ClaimTypesConst.TenantId = "tenant_id";

// After (configure-once):
ClaimTypesConst.Configure(options =>
{
    options.UserId   = "sub";
    options.Role     = "roles";
    options.TenantId = "tenant_id";
});
```

No changes to `ICurrentClient`.

### Implementation Changes

**`CurrentUser.cs`**
- `Id`: returns `_principalAccessor.Principal?.FindUserId()` (now `string?`)
- `IsAuthenticated`: changes from `Id.HasValue` to `_principalAccessor.Principal?.Identity?.IsAuthenticated ?? false`

**`ClaimsIdentityExtensions.cs`**
- `FindUserId(this ClaimsPrincipal)`: returns `string?`. Removes `Guid.TryParse`, returns raw claim value. Same pattern as `FindTenantId`/`FindClientId`.
- `FindUserId(this IIdentity)`: same change.

**`CurrentUserExtensions.cs`**
- `GetId()`: returns `string`. Throws `InvalidOperationException` if `Id` is null (replaces current `Debug.Assert` + `Nullable<Guid>.Value` pattern with an explicit throw).
- `FindClaimValue<T>()`: removed entirely.
- `FindClaimValue(string)` (non-generic): unchanged.

**`ClaimTypesConst.cs`**
- Add `ClaimTypesOptions` class with settable properties and defaults.
- Add `Configure(Action<ClaimTypesOptions>)` static method with freeze semantics.
- Replace `{ get; set; }` properties with read-only getters backed by the frozen options.

### Test Changes

**`CurrentUserTests.cs`**
- Update `Id` assertions from `Guid` to `string?`
- Add `IsAuthenticated` cases: authenticated principal with no NameIdentifier (expect `true`), unauthenticated principal with NameIdentifier (expect `false`)

**`ClaimsIdentityExtensionsTests.cs`** (new file)
- `FindUserId` tests asserting `string?` return
- Non-GUID identifier cases (integer IDs, Auth0-style `auth0|abc123`)
- Null/empty claim handling

**`ClaimTypesConstTests`** (new or updated)
- Test `Configure` applies values correctly
- Test double-call to `Configure` throws `InvalidOperationException`
- Test default values work when `Configure` is never called
- `ClaimTypesConst` needs an internal `Reset()` method (or `[InternalsVisibleTo]` test access) to allow test isolation, since the freeze is process-global static state

**`AuditableDbContext.cs`** (example)
- Simplify from `_currentUser.Id == null ? "System" : _currentUser.Id.ToString()` to `_currentUser.Id ?? "System"`

### Website Documentation Changes

**`website/docs/security-web/authorization.mdx`** (and `versioned_docs/version-2.4.1/` copy)
- "Accessing the current user" example: `_currentUser.Id!.Value` → `_currentUser.Id!` (no `.Value`, no longer `Guid?`)
- "Reading arbitrary claims" section: remove `FindClaimValue<int>` example (method deleted)
- "Claims identity helpers" section: `Guid? userId = principal.FindUserId()` → `string? userId = principal.FindUserId()`; remove "parses as Guid" comment
- "Overriding claim type URIs" section: replace direct assignment examples with `ClaimTypesConst.Configure(...)` pattern
- API summary table: remove `FindClaimValue<T>(string)` from `CurrentUserExtensions` description
- API summary table: update `ClaimTypesConst` description to mention configure-once pattern

**`website/docs/security-web/web-utilities.mdx`** (and `versioned_docs/version-2.4.1/` copy)
- "Using ICurrentUser in middleware" example: `currentUser.Id?.ToString() ?? "anonymous"` → `currentUser.Id ?? "anonymous"`

## Files Affected

| File | Change |
|---|---|
| `Src/RCommon.Security/Users/ICurrentUser.cs` | `Id` type: `Guid?` to `string?` |
| `Src/RCommon.Security/Users/CurrentUser.cs` | `Id` return type, `IsAuthenticated` derivation |
| `Src/RCommon.Security/Users/CurrentUserExtensions.cs` | `GetId()` return type, remove `FindClaimValue<T>` |
| `Src/RCommon.Security/ClaimsIdentityExtensions.cs` | `FindUserId` overloads return `string?` |
| `Src/RCommon.Security/Claims/ClaimTypesConst.cs` | Replace mutable statics with `Configure`/freeze pattern |
| `Src/RCommon.Security/Claims/ClaimTypesOptions.cs` | New: options class for claim type mappings |
| `Tests/RCommon.Security.Tests/CurrentUserTests.cs` | Update assertions |
| `Tests/RCommon.Security.Tests/ClaimsIdentityExtensionsTests.cs` | New: `FindUserId` tests with string return, non-GUID cases |
| `Tests/RCommon.Security.Tests/ClaimTypesConstTests.cs` | New: configure/freeze tests |
| `Examples/CleanWithCQRS/HR.LeaveManagement.Persistence/AuditableDbContext.cs` | Simplify Id usage |
| `website/docs/security-web/authorization.mdx` | Update code examples and API summary |
| `website/docs/security-web/web-utilities.mdx` | Update middleware example |
| `website/versioned_docs/version-2.4.1/security-web/authorization.mdx` | Mirror changes from current docs |
| `website/versioned_docs/version-2.4.1/security-web/web-utilities.mdx` | Mirror changes from current docs |
