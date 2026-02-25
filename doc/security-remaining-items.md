# Security Remaining Items

Items deferred from the initial security audit remediation due to breaking changes, architectural complexity, or the need for coordinated deployment.

## Critical / High Severity — Deferred

### SEC-03: Hardcoded Static Encryption Salt
**Severity**: Critical  
**Breaking Change**: YES — Requires data migration for existing encrypted settings  
**File**: `src/common/Fig.Common.NetStandard/Cryptography/Cryptography.cs`

The `Cryptography` class uses a hardcoded static salt (`0x49, 0x76, 0x61, ...`). A per-operation random salt should be generated and prepended to the ciphertext.

**Recommended approach**:
1. Generate a random 16-byte salt per encryption operation and prepend it to the output.
2. On decryption, extract the salt from the first 16 bytes.
3. Add a legacy fallback that detects old-format ciphertext (no prepended salt) and decrypts with the hardcoded salt.
4. Re-encrypt on read: when legacy data is decrypted successfully, re-encrypt with a new random salt.

**Breaking change detail**: The `Fig.Client` NuGet package uses `Cryptography` for offline settings encryption. Old clients cannot decrypt data encrypted with new salts. New clients can decrypt both old and new formats via the fallback. Requires a coordinated client library update and major version bump.

---

### SEC-04: Weak PBKDF2 Iteration Count (1,000)
**Severity**: Critical  
**Breaking Change**: YES — Coupled with SEC-03 migration  
**File**: `src/common/Fig.Common.NetStandard/Cryptography/Cryptography.cs`

The `DefaultIterationCount` is 1,000. OWASP 2023 recommends 600,000 for PBKDF2-SHA1.

**Recommended approach**:
1. Increase to 600,000.
2. Bundle with SEC-03 — when re-encrypting with a new salt, also use the new iteration count.
3. Legacy decryption uses the old iteration count (1,000) when falling back to the legacy salt.
4. Add iteration count to the ciphertext header format for future-proofing.

---

### SEC-08: JWT Tokens Stored in localStorage
**Severity**: High  
**Breaking Change**: NO (architectural change)  
**Files**: `src/web/Fig.Web/` (Blazor WASM auth infrastructure)

JWT tokens are stored in browser `localStorage`, which is accessible to any JavaScript running on the page (XSS risk).

**Recommended approach**:
1. Implement a Backend-For-Frontend (BFF) pattern where the server holds tokens in httpOnly cookies.
2. As an interim measure, the following mitigations have been applied:
   - SEC-09: Content Security Policy headers (limits XSS attack surface)
   - SEC-11: Reduced token lifetime to 1 hour (limits stolen token window)
3. A full BFF migration should be planned as a separate feature.

---

### SEC-13: No SSRF Protection on Webhook URLs
**Severity**: High  
**Breaking Change**: NO  
**File**: `src/api/Fig.Api/WebHooks/WebHookProcessorWorker.cs`

Webhook URLs are not validated against internal network ranges, allowing Server-Side Request Forgery.

**Recommended approach**:
1. Create a `WebhookUrlValidator` service that blocks RFC 1918 ranges, localhost, link-local, and cloud metadata endpoints (169.254.169.254).
2. Validate at both registration time and request time (to prevent DNS rebinding).
3. Add a configuration option to disable SSRF protection for development/testing.

---

### SEC-14: No JWT Issuer or Audience Validation
**Severity**: High  
**Breaking Change**: YES — Existing tokens are invalidated  
**File**: `src/api/Fig.Api/Authorization/TokenHandler.cs`

JWT tokens are not validated for issuer or audience claims, allowing tokens from other systems to be accepted.

**Recommended approach**:
1. Set a unique issuer (e.g., `"fig-api"`) and audience (e.g., `"fig"`) in token generation.
2. Enable `ValidateIssuer` and `ValidateAudience` in token validation parameters.
3. Make issuer and audience configurable via `appsettings.json`.

**Breaking change detail**: All existing tokens become invalid after deployment. Users will need to re-authenticate. Coordinate with a deployment window.

---

### SEC-15: Webhook Authentication Lacks HMAC Signing
**Severity**: High  
**Breaking Change**: YES — Breaks existing webhook consumers  
**File**: `src/api/Fig.Api/WebHooks/WebHookProcessorWorker.cs`

Webhooks use a simple `Secret` header. HMAC-SHA256 body signing would provide replay protection and body integrity verification.

**Recommended approach**:
1. Compute `HMAC-SHA256(secret, timestamp + "." + body)`.
2. Add `X-Fig-Signature` and `X-Fig-Timestamp` headers.
3. Keep the existing `Secret` header during a transition period.
4. Add signature verification helpers to `Fig.WebHooks.Contracts`.
5. Deprecate the `Secret`-only approach.

---

### SEC-16: File Imports Run with Unrestricted Admin Privileges
**Severity**: High  
**Breaking Change**: NO  
**File**: `src/api/Fig.Api/DataImport/ConfigFileImporter.cs`

File-based imports run with full Administrator privileges, regardless of the actual user's role.

**Recommended approach**:
1. Create a restricted import role (e.g., `Role.Importer`) with limited permissions.
2. Validate import file schema before processing.
3. Add detailed audit logging for file-based imports.
4. Restrict `ClientFilter` to only the clients present in the import file.

---

### SEC-17: No Token Revocation Mechanism
**Severity**: High  
**Breaking Change**: NO  
**Files**: `src/api/Fig.Api/Authorization/TokenHandler.cs`, `src/api/Fig.Api/Middleware/AuthMiddleware.cs`

There is no way to revoke JWT tokens before they expire. Compromised tokens remain valid until expiration.

**Recommended approach**:
1. Add a `jti` (JWT ID) claim to generated tokens.
2. Add a `RevokedTokens` database table (`TokenId`, `ExpiresAt`).
3. Add a `/users/logout` endpoint that blacklists the token's `jti`.
4. Check the blacklist in `AuthMiddleware` during token validation.
5. Add a background cleanup job for expired blacklist entries.

---

### SEC-21: Rate Limiting Bypass via Per-User Limiting
**Severity**: Medium  
**Breaking Change**: NO  
**File**: `src/api/Fig.Api/Program.cs`

Rate limiting is per-IP only. After SEC-05 (IP spoofing fix — completed), the IP-based bypass is resolved. However, per-user/client rate limiting would provide defense-in-depth.

**Recommended approach**:
1. Add per-authenticated-user rate limiting using the `PartitionedRateLimiter`.
2. Add account lockout after N failed authentication attempts (configurable, default 5).

---

### SEC-25: No CSRF Protection
**Severity**: Medium  
**Breaking Change**: NO  
**File**: `src/api/Fig.Api/Middleware/`

No CSRF protection on state-changing API requests.

**Recommended approach**:
1. Verify the `Origin` header on POST/PUT/DELETE requests against configured allowed origins.
2. If cookie-based auth is adopted (SEC-08 BFF), add `SameSite=Strict` to cookies.

**Note**: Blazor WASM with Bearer tokens in `localStorage` is naturally resistant to traditional CSRF (browser doesn't auto-send the token). The risk is residual and low priority.

---

## Implementation Priority

| Priority | Item | Effort | Dependency |
|----------|------|--------|------------|
| 1 | SEC-14 (JWT issuer/audience) | Low | Deployment window needed |
| 2 | SEC-13 (SSRF protection) | Medium | None |
| 3 | SEC-17 (Token revocation) | Medium | Database schema change |
| 4 | SEC-16 (File import privileges) | Medium | None |
| 5 | SEC-25 (CSRF) | Low | None |
| 6 | SEC-21 (Per-user rate limiting) | Medium | SEC-05 (done) |
| 7 | SEC-15 (Webhook HMAC) | Medium | Breaking for consumers |
| 8 | SEC-03 + SEC-04 (Crypto migration) | High | Breaking for NuGet client |
| 9 | SEC-08 (BFF for localStorage) | High | Architectural change |
