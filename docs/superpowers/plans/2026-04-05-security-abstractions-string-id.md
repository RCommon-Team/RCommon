# Security Abstractions: String-Based User Identity — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Change `ICurrentUser.Id` from `Guid?` to `string?`, align `IsAuthenticated` with `ClaimsIdentity.IsAuthenticated`, replace mutable `ClaimTypesConst` statics with a configure-once freeze pattern, remove `FindClaimValue<T>`, and update all tests, examples, and website docs.

**Architecture:** This is a breaking change to the `RCommon.Security` public API surface. All changes flow from the interface (`ICurrentUser.Id` type change) outward through the implementation, extensions, tests, examples, and documentation. The `ClaimTypesConst` freeze pattern is an independent but co-shipped change.

**Tech Stack:** C# / .NET (multi-target net8.0/net9.0/net10.0), xUnit, FluentAssertions, Moq

**Spec:** `docs/superpowers/specs/2026-04-05-security-abstractions-string-id-design.md`

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `Src/RCommon.Security/Claims/ClaimTypesOptions.cs` | Create | Options POCO with settable claim type properties and defaults |
| `Src/RCommon.Security/Claims/ClaimTypesConst.cs` | Modify | Replace mutable statics with `Configure`/freeze pattern backed by `ClaimTypesOptions` |
| `Src/RCommon.Security/Users/ICurrentUser.cs` | Modify | `Id` type: `Guid?` → `string?` |
| `Src/RCommon.Security/ClaimsIdentityExtensions.cs` | Modify | `FindUserId` overloads return `string?` |
| `Src/RCommon.Security/Users/CurrentUser.cs` | Modify | `Id` return type, `IsAuthenticated` derivation |
| `Src/RCommon.Security/Users/CurrentUserExtensions.cs` | Modify | `GetId()` returns `string`, remove `FindClaimValue<T>` |
| `Src/RCommon.Security/RCommon.Security.csproj` | Modify | Add `InternalsVisibleTo` for test project |
| `Tests/RCommon.Security.Tests/ClaimTypesConstTests.cs` | Create | Configure/freeze/reset tests |
| `Tests/RCommon.Security.Tests/ClaimsIdentityExtensionsTests.cs` | Create | `FindUserId` string return tests, non-GUID cases |
| `Tests/RCommon.Security.Tests/CurrentUserTests.cs` | Modify | Update `Id`/`IsAuthenticated` assertions |
| `Examples/CleanWithCQRS/HR.LeaveManagement.Persistence/AuditableDbContext.cs` | Modify | Simplify `Id` usage |
| `website/docs/security-web/authorization.mdx` | Modify | Update code examples and API summary |
| `website/docs/security-web/web-utilities.mdx` | Modify | Update middleware example |
| `website/versioned_docs/version-2.4.1/security-web/authorization.mdx` | Modify | Mirror authorization.mdx changes |
| `website/versioned_docs/version-2.4.1/security-web/web-utilities.mdx` | Modify | Mirror web-utilities.mdx changes |

---

## Task 1: ClaimTypesOptions and ClaimTypesConst freeze pattern

**Files:**
- Create: `Src/RCommon.Security/Claims/ClaimTypesOptions.cs`
- Modify: `Src/RCommon.Security/Claims/ClaimTypesConst.cs`
- Modify: `Src/RCommon.Security/RCommon.Security.csproj`
- Create: `Tests/RCommon.Security.Tests/ClaimTypesConstTests.cs`

### Step 1: Write ClaimTypesConst tests

- [ ] **Create `Tests/RCommon.Security.Tests/ClaimTypesConstTests.cs`**

```csharp
using System.Security.Claims;
using FluentAssertions;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Security.Tests;

public class ClaimTypesConstTests : IDisposable
{
    public ClaimTypesConstTests()
    {
        ClaimTypesConst.Reset();
    }

    public void Dispose()
    {
        ClaimTypesConst.Reset();
    }

    [Fact]
    public void Defaults_MatchStandardClaimTypes()
    {
        ClaimTypesConst.UserId.Should().Be(ClaimTypes.NameIdentifier);
        ClaimTypesConst.UserName.Should().Be(ClaimTypes.Name);
        ClaimTypesConst.Name.Should().Be(ClaimTypes.GivenName);
        ClaimTypesConst.SurName.Should().Be(ClaimTypes.Surname);
        ClaimTypesConst.Role.Should().Be(ClaimTypes.Role);
        ClaimTypesConst.Email.Should().Be(ClaimTypes.Email);
        ClaimTypesConst.TenantId.Should().Be("tenantid");
        ClaimTypesConst.ClientId.Should().Be("client_id");
    }

    [Fact]
    public void Configure_AppliesCustomValues()
    {
        ClaimTypesConst.Configure(options =>
        {
            options.UserId = "sub";
            options.Role = "roles";
            options.TenantId = "tenant_id";
        });

        ClaimTypesConst.UserId.Should().Be("sub");
        ClaimTypesConst.Role.Should().Be("roles");
        ClaimTypesConst.TenantId.Should().Be("tenant_id");
        // Unchanged values keep defaults
        ClaimTypesConst.Email.Should().Be(ClaimTypes.Email);
    }

    [Fact]
    public void Configure_CalledTwice_ThrowsInvalidOperationException()
    {
        ClaimTypesConst.Configure(options => { options.UserId = "sub"; });

        var action = () => ClaimTypesConst.Configure(options => { options.Role = "roles"; });

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Configure_WithNullAction_ThrowsArgumentNullException()
    {
        var action = () => ClaimTypesConst.Configure(null!);

        action.Should().Throw<ArgumentNullException>();
    }
}
```

- [ ] **Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Security.Tests/ --filter "FullyQualifiedName~ClaimTypesConstTests" --no-restore -v quiet`
Expected: Build errors — `Reset()` and `Configure()` don't exist yet.

### Step 2: Add InternalsVisibleTo for test project

- [ ] **Modify `Src/RCommon.Security/RCommon.Security.csproj`** — add a new `<ItemGroup>` block:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="RCommon.Security.Tests" />
</ItemGroup>
```

### Step 3: Create ClaimTypesOptions

- [ ] **Create `Src/RCommon.Security/Claims/ClaimTypesOptions.cs`**

```csharp
using System.Security.Claims;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Configurable options for claim type URI mappings.
    /// Set properties to match the claim types issued by your identity provider.
    /// </summary>
    public class ClaimTypesOptions
    {
        public string UserName { get; set; } = ClaimTypes.Name;
        public string Name { get; set; } = ClaimTypes.GivenName;
        public string SurName { get; set; } = ClaimTypes.Surname;
        public string UserId { get; set; } = ClaimTypes.NameIdentifier;
        public string Role { get; set; } = ClaimTypes.Role;
        public string Email { get; set; } = ClaimTypes.Email;
        public string TenantId { get; set; } = "tenantid";
        public string ClientId { get; set; } = "client_id";
    }
}
```

### Step 4: Rewrite ClaimTypesConst with freeze pattern

- [ ] **Replace contents of `Src/RCommon.Security/Claims/ClaimTypesConst.cs`**

```csharp
using System;
using System.Security.Claims;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Provides claim type URI constants used throughout the security subsystem.
    /// Call <see cref="Configure"/> once at startup to override defaults.
    /// After configuration (or first property access), values are frozen.
    /// </summary>
    public static class ClaimTypesConst
    {
        private static ClaimTypesOptions? _options;
        private static bool _frozen;
        private static readonly object _lock = new();

        public static string UserName => GetOptions().UserName;
        public static string Name => GetOptions().Name;
        public static string SurName => GetOptions().SurName;
        public static string UserId => GetOptions().UserId;
        public static string Role => GetOptions().Role;
        public static string Email => GetOptions().Email;
        public static string TenantId => GetOptions().TenantId;
        public static string ClientId => GetOptions().ClientId;

        /// <summary>
        /// Configures claim type mappings. May only be called once, before any property is accessed.
        /// </summary>
        public static void Configure(Action<ClaimTypesOptions> configure)
        {
            Guard.IsNotNull(configure, nameof(configure));

            lock (_lock)
            {
                if (_frozen)
                {
                    throw new InvalidOperationException(
                        "ClaimTypesConst has already been configured or accessed. Configure may only be called once, before any property is read.");
                }

                var options = new ClaimTypesOptions();
                configure(options);
                _options = options;
                _frozen = true;
            }
        }

        private static ClaimTypesOptions GetOptions()
        {
            if (_options != null)
                return _options;

            lock (_lock)
            {
                if (_options != null)
                    return _options;

                _options = new ClaimTypesOptions();
                _frozen = true;
                return _options;
            }
        }

        /// <summary>
        /// Resets configuration to allow reconfiguration. Internal — for test isolation only.
        /// </summary>
        internal static void Reset()
        {
            lock (_lock)
            {
                _options = null;
                _frozen = false;
            }
        }
    }
}
```

- [ ] **Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Security.Tests/ --filter "FullyQualifiedName~ClaimTypesConstTests" --no-restore -v quiet`
Expected: All 4 tests PASS.

- [ ] **Commit**

```bash
git add Src/RCommon.Security/Claims/ClaimTypesOptions.cs Src/RCommon.Security/Claims/ClaimTypesConst.cs Src/RCommon.Security/RCommon.Security.csproj Tests/RCommon.Security.Tests/ClaimTypesConstTests.cs
git commit -m "Replace mutable ClaimTypesConst statics with configure-once freeze pattern"
```

---

## Task 2: FindUserId returns string

**Files:**
- Modify: `Src/RCommon.Security/ClaimsIdentityExtensions.cs`
- Create: `Tests/RCommon.Security.Tests/ClaimsIdentityExtensionsTests.cs`

### Step 1: Write FindUserId tests

- [ ] **Create `Tests/RCommon.Security.Tests/ClaimsIdentityExtensionsTests.cs`**

```csharp
using System.Security.Claims;
using FluentAssertions;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Security.Tests;

public class ClaimsIdentityExtensionsTests : IDisposable
{
    public ClaimsIdentityExtensionsTests()
    {
        ClaimTypesConst.Reset();
    }

    public void Dispose()
    {
        ClaimTypesConst.Reset();
    }

    [Fact]
    public void FindUserId_WithGuidClaim_ReturnsGuidString()
    {
        var guid = Guid.NewGuid().ToString();
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, guid);

        var result = principal.FindUserId();

        result.Should().Be(guid);
    }

    [Fact]
    public void FindUserId_WithIntegerClaim_ReturnsIntegerString()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "12345");

        var result = principal.FindUserId();

        result.Should().Be("12345");
    }

    [Fact]
    public void FindUserId_WithAuth0StyleClaim_ReturnsFullString()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "auth0|abc123def456");

        var result = principal.FindUserId();

        result.Should().Be("auth0|abc123def456");
    }

    [Fact]
    public void FindUserId_WhenClaimMissing_ReturnsNull()
    {
        var principal = CreatePrincipal("other-claim", "value");

        var result = principal.FindUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void FindUserId_WhenClaimValueEmpty_ReturnsNull()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "");

        var result = principal.FindUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void FindUserId_WhenClaimValueWhitespace_ReturnsNull()
    {
        var principal = CreatePrincipal(ClaimTypes.NameIdentifier, "   ");

        var result = principal.FindUserId();

        result.Should().BeNull();
    }

    [Fact]
    public void FindUserId_OnIIdentity_WithStringClaim_ReturnsString()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-42") }, "test");

        var result = ((System.Security.Principal.IIdentity)identity).FindUserId();

        result.Should().Be("user-42");
    }

    [Fact]
    public void FindUserId_OnIIdentity_WhenClaimMissing_ReturnsNull()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("other", "value") }, "test");

        var result = ((System.Security.Principal.IIdentity)identity).FindUserId();

        result.Should().BeNull();
    }

    private static ClaimsPrincipal CreatePrincipal(string claimType, string claimValue)
    {
        var claims = new[] { new Claim(claimType, claimValue) };
        var identity = new ClaimsIdentity(claims, "test");
        return new ClaimsPrincipal(identity);
    }
}
```

- [ ] **Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Security.Tests/ --filter "FullyQualifiedName~ClaimsIdentityExtensionsTests" --no-restore -v quiet`
Expected: FAIL — `FindUserId` returns `Guid?` not `string?`, type mismatches.

### Step 2: Change FindUserId to return string

- [ ] **Modify `Src/RCommon.Security/ClaimsIdentityExtensions.cs`** — replace the two `FindUserId` methods.

Replace the `ClaimsPrincipal` overload (lines 23-38) with:

```csharp
/// <summary>
/// Extracts the user identifier from the principal's claims as a raw string value.
/// </summary>
/// <param name="principal">The claims principal to search.</param>
/// <returns>The user ID string, or <c>null</c> if the claim is missing or empty.</returns>
public static string? FindUserId(this ClaimsPrincipal principal)
{
    Guard.IsNotNull(principal, nameof(principal));

    var userIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.UserId);
    if (userIdOrNull == null || userIdOrNull.Value.IsNullOrWhiteSpace())
    {
        return null;
    }

    return userIdOrNull.Value;
}
```

Replace the `IIdentity` overload (lines 46-63) with:

```csharp
/// <summary>
/// Extracts the user identifier from the identity's claims as a raw string value.
/// </summary>
/// <param name="identity">The identity to search. Must be castable to <see cref="ClaimsIdentity"/>.</param>
/// <returns>The user ID string, or <c>null</c> if the claim is missing or empty.</returns>
public static string? FindUserId(this IIdentity identity)
{
    Guard.IsNotNull(identity, nameof(identity));

    var claimsIdentity = identity as ClaimsIdentity;

    var userIdOrNull = claimsIdentity?.Claims?.FirstOrDefault(c => c.Type == ClaimTypesConst.UserId);
    if (userIdOrNull == null || userIdOrNull.Value.IsNullOrWhiteSpace())
    {
        return null;
    }

    return userIdOrNull.Value;
}
```

- [ ] **Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Security.Tests/ --filter "FullyQualifiedName~ClaimsIdentityExtensionsTests" --no-restore -v quiet`
Expected: All 8 tests PASS.

- [ ] **Commit**

```bash
git add Src/RCommon.Security/ClaimsIdentityExtensions.cs Tests/RCommon.Security.Tests/ClaimsIdentityExtensionsTests.cs
git commit -m "Change FindUserId to return string instead of Guid"
```

---

## Task 3: ICurrentUser.Id to string and IsAuthenticated alignment

**Files:**
- Modify: `Src/RCommon.Security/Users/ICurrentUser.cs`
- Modify: `Src/RCommon.Security/Users/CurrentUser.cs`
- Modify: `Src/RCommon.Security/Users/CurrentUserExtensions.cs`
- Modify: `Tests/RCommon.Security.Tests/CurrentUserTests.cs`

### Step 1: Update existing tests and add new IsAuthenticated tests

- [ ] **Modify `Tests/RCommon.Security.Tests/CurrentUserTests.cs`**

Replace the `IsAuthenticated_WhenIdIsNull_ReturnsFalse` test (line 35-46) with:

```csharp
[Fact]
public void IsAuthenticated_WhenPrincipalIsNull_ReturnsFalse()
{
    // Arrange
    _mockPrincipalAccessor.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
    var currentUser = CreateCurrentUser();

    // Act
    var result = currentUser.IsAuthenticated;

    // Assert
    result.Should().BeFalse();
}
```

Replace the `IsAuthenticated_WhenIdHasValue_ReturnsTrue` test (line 48-64) with:

```csharp
[Fact]
public void IsAuthenticated_WhenIdentityIsAuthenticated_ReturnsTrue()
{
    // Arrange - identity with AuthenticationType set (makes IsAuthenticated true)
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-42") };
    var identity = new ClaimsIdentity(claims, "Bearer");
    var principal = new ClaimsPrincipal(identity);
    _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
    var currentUser = CreateCurrentUser();

    // Act
    var result = currentUser.IsAuthenticated;

    // Assert
    result.Should().BeTrue();
}

[Fact]
public void IsAuthenticated_WhenAuthenticatedButNoNameIdentifier_ReturnsTrue()
{
    // Arrange - authenticated identity with roles but no NameIdentifier
    var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
    var identity = new ClaimsIdentity(claims, "Bearer");
    var principal = new ClaimsPrincipal(identity);
    _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
    var currentUser = CreateCurrentUser();

    // Act
    var result = currentUser.IsAuthenticated;

    // Assert
    result.Should().BeTrue();
}

[Fact]
public void IsAuthenticated_WhenIdentityNotAuthenticated_ReturnsFalse()
{
    // Arrange - identity without AuthenticationType (unauthenticated)
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-42") };
    var identity = new ClaimsIdentity(claims); // no authenticationType
    var principal = new ClaimsPrincipal(identity);
    _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
    var currentUser = CreateCurrentUser();

    // Act
    var result = currentUser.IsAuthenticated;

    // Assert
    result.Should().BeFalse();
}
```

Update the `Id_WhenPrincipalIsNull_ReturnsNull` test — no changes needed (assertion is `BeNull()` which works for both types).

Add a new test for string-based Id:

```csharp
[Fact]
public void Id_WithStringIdentifier_ReturnsRawString()
{
    // Arrange
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "auth0|abc123") };
    var identity = new ClaimsIdentity(claims, "test");
    var principal = new ClaimsPrincipal(identity);
    _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
    var currentUser = CreateCurrentUser();

    // Act
    var result = currentUser.Id;

    // Assert
    result.Should().Be("auth0|abc123");
}

[Fact]
public void Id_WithGuidIdentifier_ReturnsGuidAsString()
{
    // Arrange
    var userId = Guid.NewGuid();
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
    var identity = new ClaimsIdentity(claims, "test");
    var principal = new ClaimsPrincipal(identity);
    _mockPrincipalAccessor.Setup(x => x.Principal).Returns(principal);
    var currentUser = CreateCurrentUser();

    // Act
    var result = currentUser.Id;

    // Assert
    result.Should().Be(userId.ToString());
}
```

- [ ] **Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Security.Tests/ --filter "FullyQualifiedName~CurrentUserTests" --no-restore -v quiet`
Expected: FAIL — `IsAuthenticated` still uses `Id.HasValue`, `Id` still returns `Guid?`.

### Step 2: Change ICurrentUser interface

- [ ] **Modify `Src/RCommon.Security/Users/ICurrentUser.cs`** — change line 14:

From: `Guid? Id { get; }`
To: `string? Id { get; }`

Remove `using System;` if it becomes unused (it won't be needed after removing `Guid`).

### Step 3: Change CurrentUser implementation

- [ ] **Modify `Src/RCommon.Security/Users/CurrentUser.cs`**

Change `IsAuthenticated` (line 34):
From: `public virtual bool IsAuthenticated => Id.HasValue;`
To: `public virtual bool IsAuthenticated => _principalAccessor.Principal?.Identity?.IsAuthenticated ?? false;`

Change `Id` (line 37):
From: `public virtual Guid? Id => _principalAccessor.Principal?.FindUserId();`
To: `public virtual string? Id => _principalAccessor.Principal?.FindUserId();`

The return type now matches because `FindUserId()` was changed to `string?` in Task 2.

### Step 4: Change CurrentUserExtensions

- [ ] **Modify `Src/RCommon.Security/Users/CurrentUserExtensions.cs`**

Replace `GetId()` method (lines 54-59) with:

```csharp
/// <summary>
/// Gets the current user's ID, asserting that it is not <c>null</c>.
/// </summary>
/// <param name="currentUser">The current user instance.</param>
/// <returns>The user's string identifier.</returns>
/// <exception cref="InvalidOperationException">Thrown if <see cref="ICurrentUser.Id"/> is <c>null</c>.</exception>
public static string GetId(this ICurrentUser currentUser)
{
    return currentUser.Id
        ?? throw new InvalidOperationException("The current user ID is null. Ensure the user is authenticated and has a NameIdentifier claim.");
}
```

Remove the `FindClaimValue<T>` method (lines 37-46) entirely.

Remove `using System.Diagnostics;` (no longer needed after removing `Debug.Assert`).

- [ ] **Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Security.Tests/ --filter "FullyQualifiedName~CurrentUserTests" --no-restore -v quiet`
Expected: All tests PASS.

- [ ] **Commit**

```bash
git add Src/RCommon.Security/Users/ICurrentUser.cs Src/RCommon.Security/Users/CurrentUser.cs Src/RCommon.Security/Users/CurrentUserExtensions.cs Tests/RCommon.Security.Tests/CurrentUserTests.cs
git commit -m "Change ICurrentUser.Id to string, align IsAuthenticated with ClaimsIdentity, remove FindClaimValue<T>"
```

---

## Task 4: Update example code

**Files:**
- Modify: `Examples/CleanWithCQRS/HR.LeaveManagement.Persistence/AuditableDbContext.cs`

- [ ] **Modify line 38 of `AuditableDbContext.cs`**

From:
```csharp
string userId = (_currentUser == null || _currentUser.Id == null ? "System" : _currentUser.Id.ToString());
```

To:
```csharp
string userId = _currentUser?.Id ?? "System";
```

- [ ] **Build the example to verify compilation**

Run: `dotnet build Examples/CleanWithCQRS/HR.LeaveManagement.Persistence/ --no-restore -v quiet`
Expected: Build succeeded.

- [ ] **Commit**

```bash
git add Examples/CleanWithCQRS/HR.LeaveManagement.Persistence/AuditableDbContext.cs
git commit -m "Simplify AuditableDbContext to use string-based ICurrentUser.Id"
```

---

## Task 5: Run full test suite

- [ ] **Run all security tests**

Run: `dotnet test Tests/RCommon.Security.Tests/ --no-restore -v quiet`
Expected: All tests PASS, zero failures.

- [ ] **Build entire solution to catch any remaining compilation errors**

Run: `dotnet build RCommon.sln --no-restore -v quiet`
Expected: Build succeeded. If any other projects reference `ICurrentUser.Id` as `Guid?`, they will surface here.

---

## Task 6: Update website documentation

**Files:**
- Modify: `website/docs/security-web/authorization.mdx`
- Modify: `website/docs/security-web/web-utilities.mdx`
- Modify: `website/versioned_docs/version-2.4.1/security-web/authorization.mdx`
- Modify: `website/versioned_docs/version-2.4.1/security-web/web-utilities.mdx`

### Step 1: Update authorization.mdx (current docs)

- [ ] **Modify `website/docs/security-web/authorization.mdx`** — apply the following changes:

**Line 82-89** — replace the "Overriding claim type URIs" code block:

From:
```csharp
using RCommon.Security.Claims;

// Map to the short claim names issued by your OIDC provider.
ClaimTypesConst.UserId   = "sub";
ClaimTypesConst.Role     = "roles";
ClaimTypesConst.Email    = "email";
ClaimTypesConst.TenantId = "tenant_id";
ClaimTypesConst.ClientId = "client_id";
```

To:
```csharp
using RCommon.Security.Claims;

// Map to the short claim names issued by your OIDC provider.
// Configure must be called once at startup, before any property is accessed.
ClaimTypesConst.Configure(options =>
{
    options.UserId   = "sub";
    options.Role     = "roles";
    options.Email    = "email";
    options.TenantId = "tenant_id";
    options.ClientId = "client_id";
});
```

**Line 148** — in the "Accessing the current user" example:

From: `CustomerId   = _currentUser.Id!.Value,`
To: `CustomerId   = _currentUser.Id!,`

**Lines 178-181** — in the "Reading arbitrary claims" section, remove:

```csharp
// Find a claim value converted to a specific struct type.
int employeeNumber = _currentUser.FindClaimValue<int>("custom:employee_number");
```

**Lines 282-284** — in the "Claims identity helpers" section:

From:
```csharp
Guid? userId   = principal.FindUserId();    // parses ClaimTypesConst.UserId as Guid
string? tenant = principal.FindTenantId();  // reads ClaimTypesConst.TenantId
string? client = principal.FindClientId();  // reads ClaimTypesConst.ClientId
```

To:
```csharp
string? userId = principal.FindUserId();    // reads ClaimTypesConst.UserId
string? tenant = principal.FindTenantId();  // reads ClaimTypesConst.TenantId
string? client = principal.FindClientId();  // reads ClaimTypesConst.ClientId
```

**Line 298** — in the API summary table, `CurrentUserExtensions` row:

From: `FindClaimValue(string)`, `FindClaimValue<T>(string)`, `GetId()`
To: `FindClaimValue(string)`, `GetId()`

**Line 301** — in the API summary table, `ClaimTypesConst` row:

From: `Configurable claim type URIs: UserName, Name, SurName, UserId, Role, Email, TenantId, ClientId`
To: `Configure-once claim type URIs via Configure(Action<ClaimTypesOptions>): UserName, Name, SurName, UserId, Role, Email, TenantId, ClientId`

### Step 2: Update web-utilities.mdx (current docs)

- [ ] **Modify `website/docs/security-web/web-utilities.mdx`** — line 139:

From: `var userId = currentUser.Id?.ToString() ?? "anonymous";`
To: `var userId = currentUser.Id ?? "anonymous";`

### Step 3: Mirror changes to versioned docs

- [ ] **Apply the same changes from Step 1 to `website/versioned_docs/version-2.4.1/security-web/authorization.mdx`**

The versioned file has identical content — apply the same edits.

- [ ] **Apply the same change from Step 2 to `website/versioned_docs/version-2.4.1/security-web/web-utilities.mdx`**

### Step 4: Commit

- [ ] **Commit**

```bash
git add website/docs/security-web/authorization.mdx website/docs/security-web/web-utilities.mdx website/versioned_docs/version-2.4.1/security-web/authorization.mdx website/versioned_docs/version-2.4.1/security-web/web-utilities.mdx
git commit -m "Update website docs for string-based ICurrentUser.Id and ClaimTypesConst.Configure"
```

---

## Task 7: Final verification and squash

- [ ] **Run full test suite one more time**

Run: `dotnet test Tests/RCommon.Security.Tests/ -v quiet`
Expected: All tests PASS.

- [ ] **Run full solution build**

Run: `dotnet build RCommon.sln -v quiet`
Expected: Build succeeded.

- [ ] **Rebase and squash interim commits into a single commit**

```bash
git rebase -i main
```

Squash all task commits into one with message:

```
Breaking: Change ICurrentUser.Id from Guid? to string?

Aligns security abstractions with .NET claims identity standards:
- ICurrentUser.Id is now string? (was Guid?), matching how claims work
- IsAuthenticated delegates to ClaimsIdentity.IsAuthenticated
- FindUserId() returns raw string claim value (no Guid parsing)
- ClaimTypesConst uses configure-once freeze pattern
- Removed FindClaimValue<T> (consumers parse claim strings themselves)
- Updated website documentation and examples
```
