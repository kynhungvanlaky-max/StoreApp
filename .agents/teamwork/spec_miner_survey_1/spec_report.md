# Authoritative Specification Report: Authentication, User Identity, and API Contract

- **Source Code & Test Repository**: `tests/GameStore.API.ProductKeyTests/` and `src/NexusKeys.*`
- **Execution Target**: .NET 10.0 xUnit Integration Test Suite
- **Test Baseline Status**: 72 passed, 0 failed, 0 skipped (Duration: ~25s)
- **Authoritative Date**: 2026-10-16

---

## 1. Executive Summary

This report provides the authoritative specification mined directly from the integration tests in `tests/GameStore.API.ProductKeyTests/` and the underlying Clean Architecture implementation across `NexusKeys.Domain`, `NexusKeys.Infrastructure`, and `NexusKeys.Api`.

The test suite acts as an executable contract enforcing:
1. Strict RESTful v1 conventions (`/api/v1/...`) with explicit route deprecation for legacy `/api/...` routes.
2. Strict RFC 7807 `application/problem+json` format for all error responses (HTTP status >= 400).
3. JWT Bearer token generation with standard claims (`sub`/`nameid`, `email`, `role`, `jti`, `name`) and 30-minute access token lifetime.
4. Cryptographically secure 7-day refresh tokens with rotation upon use and complete revocation on password reset or password change.
5. Account locking enforcement (`isLocked == true`) rejecting login with `400 Bad Request` (with `isLocked: true` extension) and rejecting refresh requests with `401 Unauthorized`.
6. Role-Based Access Control (RBAC) ensuring unprivileged customers cannot escalate privileges to Administrator, access other customers' orders/tickets/reviews, or call administration endpoints.

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Auth | Customer Registration | Register a new customer account | `POST /api/v1/auth/register`<br>Body: `{ username, email, password, secretCode? }` | `201 Created`<br>`Location: /api/v1/users/{id}`<br>`{ id: int, message: string }` | Invalid username (<3 chars), invalid email (no '@' or '.'), or short password (<6 chars) -> `400 Bad Request` (`application/problem+json`). Duplicate email -> `400 Bad Request`. `secretCode == "NEXUSADMIN"` -> `403 Forbidden`. | `AuthAndUserApiTests.cs:12-54`<br>`ImprovementRegressionTests.cs:79-97` |
| 2 | Auth | Account Login | Authenticate with email/password and obtain JWT + Refresh Token | `POST /api/v1/auth/login`<br>Body: `{ email, password }` | `200 OK`<br>`AuthResponse { token, refreshToken, username, email, role }`<br>Persists RefreshToken in DB | Invalid credentials -> `401 Unauthorized` (`ProblemDetails`). Account locked (`isLocked == true`) -> `400 Bad Request` (`ProblemDetails` with `isLocked: true`, 0 tokens saved). | `AuthAndUserApiTests.cs:56-92` |
| 3 | Auth | Refresh Token Rotation | Rotate refresh token to receive a new JWT and replacement refresh token | `POST /api/v1/auth/refresh`<br>Body: `{ refreshToken }` | `200 OK`<br>`AuthResponse { token, refreshToken, username, email, role }`<br>Old token deleted, new token stored | Expired token / unknown token -> `401 Unauthorized`. Locked user -> `401 Unauthorized`. Parallel race -> safely returns latest valid token or `401 Unauthorized`. | `AuthAndUserApiTests.cs:94-124`<br>`ImprovementRegressionTests.cs:14-30` |
| 4 | Auth | Password Reset Confirmation | Reset password using a valid reset token and revoke all sessions | `POST /api/v1/auth/reset-password`<br>Body: `{ token, newPassword }` | `200 OK`<br>`{ message: string }`<br>Updates `PasswordHash`, marks reset token `isUsed = true`, deletes all user refresh tokens | Invalid token, expired token (>1h), or already used token -> `400 Bad Request`. Password < 6 chars -> `400 Bad Request`. | `AuthAndUserApiTests.cs:126-158`<br>`ImprovementRegressionTests.cs:51-76` |
| 5 | Auth | Forgot Password Initiation | Generate password reset token and send email link | `POST /api/v1/auth/forgot-password`<br>Body: `{ email }` | `200 OK`<br>`{ message: "Nếu email tồn tại, một liên kết khôi phục sẽ được gửi." }` | Returns generic `200 OK` even if email does not exist to prevent enumeration. Token expires in 1 hour. | `AuthEndpoints.cs:202-242` |
| 6 | Auth | External Login (OAuth) | Authenticate or register via Google or Facebook access token | `POST /api/v1/auth/external-login`<br>Body: `{ provider, accessToken }` | `200 OK`<br>`AuthResponse { token, refreshToken, username, email, role }` | Invalid OAuth token -> `400 Bad Request`. Locked user -> `400 Bad Request` (`ProblemDetails` with `isLocked: true`). | `AuthEndpoints.cs:276-393` |
| 7 | User | Current User Profile | Retrieve authenticated user profile based on JWT claims | `GET /api/v1/users/me`<br>Header: `Authorization: Bearer <token>` | `200 OK`<br>`UserDto { id, username, email, walletBalance, roleName, createdAt, avatarUrl, isLocked }` | Unauthenticated -> `401 Unauthorized`. User not found in DB -> `404 NotFound`. | `AuthAndUserApiTests.cs:160-173`<br>`RestRouteContractTests.cs` |
| 8 | User | Self Password Change | Change authenticated user's password and revoke all active refresh tokens | `POST /api/v1/users/me/password-changes`<br>Header: `Authorization: Bearer <token>`<br>Body: `{ oldPassword, newPassword }` | `204 NoContent`<br>Updates `PasswordHash`, purges all `db.RefreshTokens` for user | Unauthenticated -> `401 Unauthorized`. Incorrect `oldPassword` -> `400 Bad Request`. `newPassword` < 6 chars -> `400 Bad Request`. | `AuthAndUserApiTests.cs:175-188`<br>`ImprovementRegressionTests.cs:32-49`<br>`RestRouteContractTests.cs:100,117` |
| 9 | User | Self Profile Update | Update profile details (username, avatar) | `PATCH /api/v1/users/me`<br>Body: `{ username?, avatarUrl? }` | `204 NoContent` in v1 | Unauthenticated -> `401 Unauthorized`. User not found -> `404 NotFound`. | `UserEndpoints.cs:124-139` |
| 10 | User | Admin Manage User Lock | Explicitly lock or unlock a user account | `PATCH /api/v1/users/{id:int}`<br>Header: `Authorization: Bearer <admin-token>`<br>Body: `{ isLocked: bool, ... }` | `204 NoContent` in v1<br>Sets `user.IsLocked` in DB | Unauthenticated -> `401 Unauthorized`. Non-Admin -> `403 Forbidden`. User not found -> `404 NotFound`. Note: Route `/toggle-lock` is disallowed in v1. | `AuthAndUserApiTests.cs:190-202`<br>`RestRouteContractTests.cs:89,113-114` |
| 11 | User | Admin List Users | List and search all users with pagination | `GET /api/v1/users?page=1&pageSize=10&search=...`<br>Header: `Authorization: Bearer <admin-token>` | `200 OK`<br>`{ data: UserDto[], totalItems, totalPages, currentPage, pageSize }` | Unauthenticated -> `401 Unauthorized`. Non-Admin -> `403 Forbidden`. | `UserEndpoints.cs:16-54` |
| 12 | User | Admin Get User by ID | Retrieve detailed user profile by ID | `GET /api/v1/users/{id:int}`<br>Header: `Authorization: Bearer <admin-token>` | `200 OK`<br>`UserDto` | Non-Admin -> `403 Forbidden`. User not found -> `404 NotFound`. | `UserEndpoints.cs:175-190` |
| 13 | User | Admin Change User Role | Reassign a user's role | `PUT /api/v1/users/{id:int}/role`<br>Header: `Authorization: Bearer <admin-token>`<br>Body: `{ roleName: string }` | `200 OK`<br>`{ message: string }` | Non-Admin -> `403 Forbidden`. User not found -> `404 NotFound`. Invalid role name -> `400 Bad Request`. | `UserEndpoints.cs:86-98` |
| 14 | Admin Portal | Admin Bridge | Single sign-on bridge from API token into Admin portal session cookies | `POST /Auth/Bridge`<br>Form Body: `token=<jwt>` | `302 Found` (Redirect to `Home/Index`)<br>Sets 3 cookies: `access_token` (HttpOnly, SameSite=Lax), `admin_name`, `admin_email` | Missing token -> Redirect `Auth/Login` (API not called). Expired token (401) -> Redirect `Auth/Login`. Non-Admin role (e.g. Customer) -> Redirect `Auth/Login`. Malformed profile -> Redirect `Auth/Login`. Locked user -> Redirect `Auth/Login`. Zero cookies set on failure. | `AdminBridgeTests.cs:13-86` |
| 15 | Admin Portal | Admin Token Forwarding | Forward Bearer token from cookie or explicit header to API calls | Incoming request to backend API client | Attaches `Authorization: Bearer <token>` | Explicit header is not overwritten by stale cookie. | `AdminBridgeTests.cs:88-106` |
| 16 | Product Keys | List Keys Paginated | Retrieve product keys grouped/paginated by product with search | `GET /api/v1/product-keys?page=1&pageSize=10&search=...` (or legacy `/api/keys/`) | `200 OK`<br>`KeyListResponse { data: ProductKeyDto[], totalItems, totalPages, currentPage, pageSize }` | Unauthenticated -> `401 Unauthorized`. Non-Admin -> `403 Forbidden`. | `ProductKeyApiTests.cs:25-94,211-227` |
| 17 | Product Keys | Create Product Key | Create a product key; optionally bind to an existing order | `POST /api/v1/product-keys/`<br>Body: `CreateProductKeyDto { productId, keyCode, isSold?, orderId? }` | `201 Created`<br>Returns created key `Id: int`<br>If `orderId` is passed, `isSold` is forced to `true` | Product not found -> `404 NotFound` (`application/problem+json`) in v1, `400 Bad Request` in legacy. | `ProductKeyApiTests.cs:96-136,229-244` |
| 18 | Product Keys | Update Product Key | Update product key code, target product, or order assignment | `PUT /api/v1/product-keys/{id}`<br>Body: `UpdateProductKeyDto { productId, keyCode, orderId? }` | `204 NoContent`<br>If `orderId` is set, `isSold` is updated to `true` | Key not found -> `404 NotFound`. Target product not found -> `404 NotFound`. | `ProductKeyApiTests.cs:138-179`<br>`ImprovementRegressionTests.cs:159-173` |
| 19 | Product Keys | Delete Product Key | Delete an unsold product key | `DELETE /api/keys/{id}` | `204 NoContent`<br>Key removed from DB | Key is sold (`isSold == true`) -> `409 Conflict`, key retained. | `ProductKeyApiTests.cs:181-208` |
| 20 | Customer | Wishlist Management | Put, check, and delete items from authenticated user's wishlist | `PUT /api/v1/users/me/wishlist-items/{productId:int}`<br>`GET /api/v1/users/me/wishlist-items/{productId:int}`<br>`DELETE /api/v1/users/me/wishlist-items/{productId:int}` | PUT/DELETE -> `204 NoContent` (Idempotent).<br>GET -> `200 OK` `{ isWishlisted: bool }` | Product not found -> `404 NotFound`. Unauthenticated -> `401 Unauthorized`. | `CustomerWorkflowApiTests.cs:12-43` |
| 21 | Customer | Support Tickets | Create support tickets, view own tickets, admin resolves | `POST /api/v1/support-tickets`<br>`GET /api/v1/users/me/support-tickets`<br>`PATCH /api/v1/support-tickets/{id:int}` | POST -> `201 Created`.<br>GET -> `200 OK` (scoped to user).<br>PATCH -> `204 NoContent` (Status="Resolved") | Invalid email -> `400 Bad Request` (`ProblemDetails`). Unknown status -> `400 Bad Request`. | `CustomerWorkflowApiTests.cs:45-93`<br>`ImprovementRegressionTests.cs:201-229` |
| 22 | Customer | Wallet Deposit | Deposit funds into user wallet and log transaction | `POST /api/v1/users/me/wallet-deposits`<br>Body: `{ amount: decimal }` | `200 OK`<br>Increments `WalletBalance`, creates `Transaction` (`TransactionType="Deposit"`) | Amount <= 0 or > 10,000,000 -> `400 Bad Request`, no mutation. | `CustomerWorkflowApiTests.cs:94-133` |
| 23 | Commerce | Checkout & Key Fulfillment | Create completed order and allocate distinct key per item | `POST /api/v1/checkouts`<br>Body: `{ productIds: int[], paymentMethod }` | `201 Created`<br>Creates Order (`Completed`), marks keys `isSold=true`, assigns `orderId`, creates License & Transaction | Out of stock -> `400 Bad Request`. Duplicate product IDs with insufficient keys -> `400 Bad Request`. | `CustomerWorkflowApiTests.cs:146-185`<br>`ImprovementRegressionTests.cs:266-282` |
| 24 | Commerce | Order Ownership Protection | View order details | `GET /api/v1/orders/{id:int}` | `200 OK`<br>`OrderDetails` | Request by customer who does not own the order -> `403 Forbidden`. | `CustomerWorkflowApiTests.cs:134-145` |
| 25 | Community | Review & Purchase Validation | Submit review for purchased product, edit review, moderate replies | `POST /api/v1/products/{productId:int}/reviews`<br>`PATCH /api/v1/reviews/{id:int}`<br>`GET /api/v1/products/{id:int}/reviews` | POST -> `201 Created`.<br>PATCH -> `204 NoContent`.<br>GET -> `200 OK` | Review without completed purchase -> `403 Forbidden`. Duplicate review -> `409 Conflict`. Edit by non-owner -> `403 Forbidden`. Moderated replies (`isHidden=true`) excluded from public GET. | `CustomerWorkflowApiTests.cs:186-237`<br>`ImprovementRegressionTests.cs:231-265` |
| 26 | Catalog | Product CRUD & Validation | Admin manages catalog; public searches and views | `GET /api/v1/products?search=&sortBy=&page=&pageSize=`<br>`POST /api/v1/products`<br>`PUT /api/v1/products/{id}`<br>`DELETE /api/v1/products/{id}` | GET -> `200 OK`.<br>POST -> `201 Created` (`Location: /api/v1/products/{id}`).<br>DELETE -> `200 OK` or `204 NoContent`. | Create by Customer -> `403 Forbidden`. Unauthenticated -> `401 Unauthorized`. Negative basePrice -> `400 Bad Request`. Deleting sold product -> `409 Conflict` (`ProblemDetails`). | `CatalogAndCommerceApiTests.cs:13-112`<br>`ImprovementRegressionTests.cs:99-123` |
| 27 | Promotions | Coupons & Validation | Admin manages coupons; public validates discount | `POST /api/v1/coupons`<br>`PUT /api/v1/coupons/{id}`<br>`POST /api/v1/coupon-validations` | POST -> `201 Created` (Code uppercase normalized).<br>Validation -> `200 OK` `{ discountPercent, couponId }` | Duplicate code -> `409 Conflict`. Discount > 100% -> `400 Bad Request`. Expired, inactive, or usage maxed coupon validation -> `400 Bad Request` (`ProblemDetails`). | `CatalogAndCommerceApiTests.cs:114-215`<br>`ImprovementRegressionTests.cs:125-157` |
| 28 | Integrations | MoMo Webhook Idempotency | Payment provider notifies of successful transaction | `POST /api/webhooks/momo`<br>Body: MoMo payment callback payload | `204 NoContent`<br>Updates Order to "Completed", assigns key, creates 1 Transaction | Repeated callback is strictly idempotent: returns `204 NoContent`, creates 0 duplicate transactions or keys. DB enforces unique index on `[PaymentGateway, GatewayTransactionId]`. | `PaymentWebhookApiTests.cs:13-100`<br>`RestRouteContractTests.cs:120-127` |
| 29 | Architecture | Legacy API Deprecation | Identify and signal legacy `/api/...` routes | Any request starting with `/api/` (excluding `/api/v1`, `/api/integrations`, `/api/webhooks`) | Headers added: `Deprecation: true`, `Sunset: Fri, 31 Dec 2027 23:59:59 GMT`, `Link: </api/v1>; rel="successor-version"` | N/A | `ProductKeyApiTests.cs:246-257`<br>`ApiConventions.cs:66-94` |

---

## 3. Edge Cases Discovered and Verified

| # | Feature | Input | Observed Behavior |
|---|---------|-------|-------------------|
| 1 | Register Validation | `username: "ab"` (< 3 characters) | Returns `400 Bad Request` (`application/problem+json`), 0 users persisted. |
| 2 | Register Validation | `email: "invalid-email"` (missing '@' or '.') | Returns `400 Bad Request` (`application/problem+json`), 0 users persisted. |
| 3 | Register Validation | `password: "123"` (< 6 characters) | Returns `400 Bad Request` (`application/problem+json`), 0 users persisted. |
| 4 | Register Privilege Escalation | `secretCode: "NEXUSADMIN"` on public register endpoint | Returns `403 Forbidden` (`application/problem+json`), no Administrator created. |
| 5 | Login Locked User | Valid credentials for user with `isLocked == true` | Returns `400 Bad Request` (`application/problem+json` with extension `isLocked: true`), 0 refresh tokens saved. |
| 6 | Refresh Token Locked User | Valid refresh token belonging to user with `isLocked == true` | Returns `401 Unauthorized` (`application/problem+json`), rotation aborted. |
| 7 | Refresh Token Concurrent Race | Parallel requests attempting to rotate the same refresh token | EF Core concurrency handling retrieves latest unexpired token for user or returns `401 Unauthorized`. |
| 8 | Password Change Token Cleanup | User changes password via `POST /api/v1/users/me/password-changes` | Updates `PasswordHash` and revokes all active refresh tokens for the user (`Count == 0`). |
| 9 | Password Reset Token Cleanup | User resets password via `POST /api/v1/auth/reset-password` | Updates `PasswordHash`, marks reset token `isUsed = true`, and deletes all user refresh tokens (`Count == 0`). |
| 10 | Admin Bridge Authorization | Non-administrator JWT (e.g., Customer) passed to `POST /Auth/Bridge` | Rejects access, redirects to `/Auth/Login`, creates 0 cookies. |
| 11 | Admin Bridge Malformed Response | API returns non-JSON or HTTP 401 to `POST /Auth/Bridge` | Rejects access, redirects to `/Auth/Login`, creates 0 cookies. |
| 12 | Product Key Deletion Conflict | Admin calls `DELETE /api/keys/{id}` on sold key (`isSold == true`) | Returns `409 Conflict`, key remains in database. |
| 13 | Product Key Product Missing | Admin calls `POST /api/v1/product-keys/` with non-existent `productId` | Returns `404 NotFound` (`application/problem+json`) in v1 contract. |
| 14 | Checkout Stock Starvation | Customer checks out product with 0 available keys | Returns `400 Bad Request`, 0 orders, 0 transactions created. |
| 15 | Checkout Duplicate Items | Customer checks out `productIds: [1, 1]` with only 1 available key in stock | Returns `400 Bad Request`, 0 orders, 0 licenses, key remains available (`isSold == false`). |
| 16 | Order Cross-Tenant Access | Customer A (userId 2) requests `GET /api/v1/orders/{id}` owned by Customer B (userId 1) | Returns `403 Forbidden`. |
| 17 | Review Without Purchase | Customer attempts `POST /api/v1/products/{id}/reviews` before purchasing product | Returns `403 Forbidden`. |
| 18 | Review Duplicate Attempt | Customer attempts second review on same purchased product | Returns `409 Conflict`. |
| 19 | Review Tamper Attempt | Customer A attempts `PATCH /api/v1/reviews/{id}` owned by Customer B | Returns `403 Forbidden`. |
| 20 | Wallet Invalid Amount | Deposit amounts `0`, `-1`, or `10000001` (> 10,000,000) | Returns `400 Bad Request`, wallet balance unaltered, 0 transactions created. |
| 21 | Coupon Case Normalization | Coupon created with lowercase code `summer20` | Saved in DB as uppercase `SUMMER20`. |
| 22 | Coupon Over-Discount | Admin updates coupon with `discountPercent: 150` (> 100) | Returns `400 Bad Request`, discount remains unchanged. |
| 23 | Coupon Validation Inactive | Validating expired, usage-maxed, or deactivated coupon | Returns `400 Bad Request` (`application/problem+json`). |
| 24 | Webhook Repeated Callback | MoMo webhook invoked multiple times with identical `transId` | Idempotent `204 NoContent`, exactly 1 transaction and 1 order completion recorded. |
| 25 | Legacy Route Deprecation | Client requests `/api/keys/?page=1&pageSize=10` | Response contains headers `Deprecation: true`, `Sunset: Fri, 31 Dec 2027...`, `Link: </api/v1>; rel="successor-version"`. |

---

## 4. Detailed Specification: Endpoints, Verbs, and Parameters

### 4.1 Authentication Endpoints (`/api/v1/auth`)

#### 1. `POST /api/v1/auth/register`
- **Purpose**: Self-service user registration.
- **Access**: Public / Anonymous.
- **Route**: `/api/v1/auth/register` (v1) or `/api/auth/register` (legacy).
- **Request Body**:
  ```json
  {
    "username": "string (min: 3)",
    "email": "string (valid email with @ and .)",
    "password": "string (min: 6)",
    "secretCode": "string (optional; must NOT be NEXUSADMIN)"
  }
  ```
- **Responses**:
  - `201 Created`: Location header `/api/v1/users/{id}`, Body: `{ "id": 1, "message": "Đăng ký thành công!" }`.
  - `400 Bad Request`: Validation failure (short username/password, invalid email, duplicate email) formatted as RFC 7807 `ProblemDetails`.
  - `403 Forbidden`: `secretCode == "NEXUSADMIN"` to prevent public administrator account creation.

#### 2. `POST /api/v1/auth/login`
- **Purpose**: Authenticate user credentials and issue JWT + Refresh Token.
- **Access**: Public / Anonymous.
- **Route**: `/api/v1/auth/login`.
- **Request Body**:
  ```json
  {
    "email": "string (required)",
    "password": "string (required)"
  }
  ```
- **Responses**:
  - `200 OK`:
    ```json
    {
      "token": "eyJhbGciOi...",
      "refreshToken": "base64-random-string",
      "username": "customer",
      "email": "customer@example.com",
      "role": "Customer"
    }
    ```
  - `400 Bad Request`: If account is locked (`isLocked == true`), returns `ProblemDetails` with title `"Tài khoản bị khoá"`, detail `"Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ."`, extension `"isLocked": true`.
  - `401 Unauthorized`: Bad password or non-existent email, returns `ProblemDetails` with title `"Đăng nhập thất bại"`.

#### 3. `POST /api/v1/auth/refresh`
- **Purpose**: Rotate refresh token and issue new access token.
- **Access**: Public / Anonymous with refresh token.
- **Route**: `/api/v1/auth/refresh`.
- **Request Body**:
  ```json
  {
    "refreshToken": "string (required)"
  }
  ```
- **Responses**:
  - `200 OK`: `AuthResponse` with fresh JWT and newly rotated `refreshToken`.
  - `401 Unauthorized`: Token expired (`Expires < UtcNow`), token does not exist, or associated user is locked (`isLocked == true`).

#### 4. `POST /api/v1/auth/forgot-password`
- **Purpose**: Trigger password reset email.
- **Access**: Public / Anonymous.
- **Route**: `/api/v1/auth/forgot-password`.
- **Request Body**:
  ```json
  {
    "email": "string (required)"
  }
  ```
- **Responses**:
  - `200 OK`: `{ "message": "Nếu email tồn tại, một liên kết khôi phục sẽ được gửi." }`. (Constant response to prevent user enumeration).

#### 5. `POST /api/v1/auth/reset-password`
- **Purpose**: Complete password reset using a reset token.
- **Access**: Public / Anonymous with token.
- **Route**: `/api/v1/auth/reset-password`.
- **Request Body**:
  ```json
  {
    "token": "string (required)",
    "newPassword": "string (min: 6)"
  }
  ```
- **Responses**:
  - `200 OK`: `{ "message": "Mật khẩu đã được thay đổi thành công." }`.
  - `400 Bad Request`: Token invalid/expired/already used, or `newPassword` < 6 characters.

#### 6. `POST /api/v1/auth/external-login`
- **Purpose**: Sign in / register via Google or Facebook OAuth.
- **Access**: Public / Anonymous.
- **Route**: `/api/v1/auth/external-login`.
- **Request Body**:
  ```json
  {
    "provider": "Google | Facebook",
    "accessToken": "string"
  }
  ```
- **Responses**:
  - `200 OK`: `AuthResponse` with JWT and Refresh Token.
  - `400 Bad Request`: OAuth verification failure or locked account (`isLocked == true`).

---

### 4.2 User Identity Endpoints (`/api/v1/users`)

#### 1. `GET /api/v1/users/me`
- **Purpose**: Retrieve authenticated user's profile.
- **Access**: Authenticated (`Customer`, `Administrator`, `Staff`, `Developer`).
- **Headers**: `Authorization: Bearer <token>`.
- **Responses**:
  - `200 OK`:
    ```json
    {
      "id": 1,
      "username": "customer",
      "email": "customer@example.com",
      "walletBalance": 100000.0,
      "roleName": "Customer",
      "createdAt": "2026-10-16T08:00:00Z",
      "avatarUrl": null,
      "isLocked": false
    }
    ```
  - `401 Unauthorized`: Missing or invalid Bearer token.
  - `404 NotFound`: User record no longer exists.

#### 2. `POST /api/v1/users/me/password-changes`
- **Purpose**: Authenticated password change and session purge.
- **Access**: Authenticated (`Customer`, `Administrator`, etc.).
- **Route**: `/api/v1/users/me/password-changes` (v1 canonical; route `/api/v1/auth/change-password` is disallowed).
- **Request Body**:
  ```json
  {
    "oldPassword": "string (required)",
    "newPassword": "string (min: 6)"
  }
  ```
- **Responses**:
  - `204 NoContent`: Password updated in DB; all active refresh tokens deleted.
  - `400 Bad Request`: Incorrect `oldPassword` or `newPassword` < 6 characters.
  - `401 Unauthorized`: Unauthenticated.

#### 3. `PATCH /api/v1/users/me`
- **Purpose**: Update user profile (username, avatar).
- **Access**: Authenticated.
- **Request Body**:
  ```json
  {
    "username": "string (optional)",
    "avatarUrl": "string (optional)"
  }
  ```
- **Responses**:
  - `204 NoContent`: Profile updated.
  - `401 Unauthorized`: Unauthenticated.

#### 4. `PATCH /api/v1/users/{id:int}`
- **Purpose**: Administrator management of user account (explicit lock state, email, username).
- **Access**: `Administrator` only.
- **Route**: `/api/v1/users/{id:int}` (v1 canonical; route `toggle-lock` is disallowed).
- **Request Body**:
  ```json
  {
    "isLocked": true,
    "username": "string (optional)",
    "email": "string (optional)",
    "avatarUrl": "string (optional)"
  }
  ```
- **Responses**:
  - `204 NoContent`: User properties and lock state updated in DB.
  - `401 Unauthorized`: Unauthenticated.
  - `403 Forbidden`: Non-Administrator.
  - `404 NotFound`: User `id` does not exist.

#### 5. `GET /api/v1/users`
- **Purpose**: Admin user listing.
- **Access**: `Administrator` only.
- **Query Parameters**:
  - `page`: int (default: 1, min: 1)
  - `pageSize`: int (default: 10, min: 1, max: 100)
  - `search`: string (optional, searches username or email)
- **Responses**:
  - `200 OK`:
    ```json
    {
      "data": [ /* UserDto items */ ],
      "totalItems": 10,
      "totalPages": 1,
      "currentPage": 1,
      "pageSize": 10
    }
    ```
  - `401 Unauthorized`, `403 Forbidden`.

#### 6. `PUT /api/v1/users/{id:int}/role`
- **Purpose**: Admin role reassignment.
- **Access**: `Administrator` only.
- **Request Body**: `{ "roleName": "Customer" | "Administrator" }`
- **Responses**: `200 OK`, `400 Bad Request` (invalid role), `404 NotFound`.

---

## 5. Token Claims, Headers, Cookies, and Session Security

### 5.1 JWT Token Specification
- **Token Format**: JWT signed with HMAC-SHA256 (`SecurityAlgorithms.HmacSha256`).
- **Configuration Keys**:
  - `Jwt:Key`: 256-bit symmetric key (`integration-test-key-with-at-least-thirty-two-characters` in test).
  - `Jwt:Issuer`: `"GameStore.Tests"` (or configured API issuer).
  - `Jwt:Audience`: `"GameStore.Tests"` (or configured API audience).
- **Standard Claims**:
  1. `sub`: `user.Id.ToString()` (maps to `ClaimTypes.NameIdentifier`)
  2. `email`: `user.Email`
  3. `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`: `user.Username`
  4. `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`: `user.Role.RoleName` (`"Customer"`, `"Administrator"`)
  5. `jti`: `Guid.NewGuid().ToString()` (unique token identifier)
- **Lifespan**:
  - Access Token: **30 minutes** (`DateTime.UtcNow.AddMinutes(30)`).
  - Validated with `ClockSkew = TimeSpan.Zero`.

### 5.2 Refresh Token Specification
- **Generation**: Cryptographically secure random 32 bytes (`RandomNumberGenerator`), Base64 encoded.
- **Storage**: `RefreshTokens` table in database:
  - `Id`: int (PK)
  - `UserId`: int (FK to `Users.Id`)
  - `Token`: string (unique token string)
  - `CreatedAt`: DateTime (UTC)
  - `Expires`: DateTime (UTC, set to **7 days** from generation)
- **Rotation Rule**: Every call to `POST /api/v1/auth/refresh` removes the presented token and creates a brand new token with a refreshed 7-day expiration.
- **Revocation Triggers**:
  1. **Password Change**: `POST /api/v1/users/me/password-changes` removes all refresh tokens for that `UserId`.
  2. **Password Reset**: `POST /api/v1/auth/reset-password` removes all refresh tokens for that `UserId`.
  3. **Account Lock**: Any attempt to use a refresh token for a user with `isLocked == true` is rejected with `401 Unauthorized`.
  4. **Expiration**: Any token where `Expires < DateTime.UtcNow` is rejected with `401 Unauthorized`.

### 5.3 Admin Portal Cookies & Session Bridge
- **Endpoint**: `POST /Auth/Bridge` in `Backend.Controllers.AuthController`.
- **Validation**: Sends internal `GET /api/v1/users/me` with `Authorization: Bearer <submitted_token>`.
- **Role Requirement**: Returned user must have `roleName` of `"Administrator"`, `"Staff"`, or `"Developer"`.
- **Cookies Issued**:
  1. `access_token`: Bearer token string, `HttpOnly = true`, `SameSite = SameSiteMode.Lax`, `Secure = Request.IsHttps`, `Expires = UtcNow + 12 hours`, `Path = "/"`.
  2. `admin_name`: User's display name or email, `HttpOnly = true`, `SameSite = SameSiteMode.Lax`.
  3. `admin_email`: User's email, `HttpOnly = true`, `SameSite = SameSiteMode.Lax`.
- **Logout Action**: `GET /Auth/Logout` or `POST /Auth/Logout` deletes `access_token`, `admin_name`, and `admin_email` cookies and redirects to frontend login with `loggedOut=true`.

---

## 6. RFC 7807 ProblemDetails Specification

The application enforces RFC 7807 via `V1ProblemDetailsFilter` and `builder.Services.AddProblemDetails()`:

- **Response Content-Type**: `application/problem+json`
- **Standard Properties**:
  - `status`: integer (e.g. `400`, `401`, `403`, `404`, `409`, `422`).
  - `title`: short summary string (e.g. `"Bad Request"`, `"Unauthorized"`, `"Forbidden"`, `"Resource not found"`, `"Resource conflict"`, `"Validation failed"`).
  - `detail`: specific human-readable explanation (extracted from error string or exception message).
  - `type`: optional URI reference or standard HTTP status URI.
  - `instance`: URI of the request path (configured in `options.CustomizeProblemDetails`).
  - `extensions`:
    - `traceId`: `HttpContext.TraceIdentifier`.
    - `isLocked`: boolean `true` when account is locked.
    - `errors`: dictionary of field-level validation errors (if validation fails).

---

## 7. Role-Based Access Control (RBAC) Matrix

| Endpoint Route | HTTP Method | Public | Customer | Administrator | Verified In Test File |
|---|---|:---:|:---:|:---:|---|
| `/api/v1/auth/register` | POST | Yes | Yes | N/A (SecretCode blocked) | `AuthAndUserApiTests.cs`, `ImprovementRegressionTests.cs` |
| `/api/v1/auth/login` | POST | Yes | Yes | Yes | `AuthAndUserApiTests.cs` |
| `/api/v1/auth/refresh` | POST | Yes | Yes | Yes | `AuthAndUserApiTests.cs`, `ImprovementRegressionTests.cs` |
| `/api/v1/auth/reset-password` | POST | Yes | Yes | Yes | `AuthAndUserApiTests.cs`, `ImprovementRegressionTests.cs` |
| `/api/v1/auth/forgot-password` | POST | Yes | Yes | Yes | `AuthEndpoints.cs` |
| `/api/v1/users/me` | GET | No (401) | Yes (own) | Yes (own) | `AuthAndUserApiTests.cs`, `AdminBridgeTests.cs` |
| `/api/v1/users/me/password-changes` | POST | No (401) | Yes (own) | Yes (own) | `AuthAndUserApiTests.cs`, `ImprovementRegressionTests.cs` |
| `/api/v1/users/me/wishlist-items/{productId}` | PUT, GET, DELETE | No (401) | Yes (own) | Yes (own) | `CustomerWorkflowApiTests.cs` |
| `/api/v1/users/me/support-tickets` | GET | No (401) | Yes (own) | Yes (own) | `CustomerWorkflowApiTests.cs` |
| `/api/v1/users/me/wallet-deposits` | POST | No (401) | Yes (own) | Yes (own) | `CustomerWorkflowApiTests.cs` |
| `/api/v1/checkouts` | POST | No (401) | Yes | Yes | `CustomerWorkflowApiTests.cs` |
| `/api/v1/orders/{id}` | GET | No (401) | Only Owner (Non-owner 403) | Yes | `CustomerWorkflowApiTests.cs` |
| `/api/v1/products/{id}/reviews` | POST | No (401) | Only If Purchased (Else 403) | Only If Purchased | `CustomerWorkflowApiTests.cs` |
| `/api/v1/reviews/{id}` | PATCH | No (401) | Only Owner (Non-owner 403) | Only Owner | `CustomerWorkflowApiTests.cs` |
| `/api/v1/users` | GET | No (401) | No (403) | Yes | `UserEndpoints.cs` |
| `/api/v1/users/{id}` | PATCH | No (401) | No (403) | Yes | `AuthAndUserApiTests.cs` |
| `/api/v1/users/{id}/role` | PUT | No (401) | No (403) | Yes | `UserEndpoints.cs` |
| `/api/v1/product-keys` | GET, POST | No (401) | No (403) | Yes | `ProductKeyApiTests.cs` |
| `/api/v1/product-keys/{id}` | PUT, DELETE | No (401) | No (403) | Yes | `ProductKeyApiTests.cs`, `ImprovementRegressionTests.cs` |
| `/api/v1/products` | POST | No (401) | No (403) | Yes | `CatalogAndCommerceApiTests.cs` |
| `/api/v1/coupons` | POST, PUT | No (401) | No (403) | Yes | `CatalogAndCommerceApiTests.cs` |
| `/api/v1/payment-configurations` | PUT | No (401) | No (403) | Yes | `CatalogAndCommerceApiTests.cs` |
| `/Auth/Bridge` | POST | No (Redirect) | No (Redirect) | Yes (Sets Cookies) | `AdminBridgeTests.cs` |

---

## 8. Database Model & Integrity Rules

1. **`Users` Table**:
   - `Id`: int (PK, identity)
   - `Username`: nvarchar, required
   - `Email`: nvarchar, unique index, required
   - `PasswordHash`: BCrypt hash string
   - `WalletBalance`: decimal, defaults to 0
   - `RoleId`: int (FK to `Roles.Id`)
   - `IsLocked`: boolean, defaults to false
   - `CreatedAt`: DateTime (UTC)
   - `AvatarUrl`: nvarchar, nullable
   - `GoogleId`: nvarchar, nullable
2. **`Roles` Table**:
   - `Id`: int (PK)
   - `RoleName`: nvarchar (1 = `Customer`, 2 = `Administrator`)
3. **`RefreshTokens` Table**:
   - `Id`: int (PK)
   - `UserId`: int (FK to `Users.Id`, Cascade delete)
   - `Token`: string, required
   - `Expires`: DateTime (UTC)
   - `CreatedAt`: DateTime (UTC)
4. **`PasswordResetTokens` Table**:
   - `Id`: int (PK)
   - `UserId`: int (FK to `Users.Id`, Cascade delete)
   - `Token`: string, required
   - `CreatedAt`: DateTime (UTC)
   - `ExpiresAt`: DateTime (UTC)
   - `IsUsed`: boolean, defaults to false
5. **`Transactions` Table**:
   - Unique composite index on `(PaymentGateway, GatewayTransactionId)` preventing duplicate payment webhook records.
6. **`ProductKeys` Table**:
   - Cannot delete sold keys (`IsSold == true` -> Conflict).
   - Setting `OrderId` automatically forces `IsSold = true`.
