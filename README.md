# ERP Inventory Management API

![CI](https://github.com/faizkhan005/erp-inventory-api/actions/workflows/ci.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)
![Redis](https://img.shields.io/badge/Redis-7-DC382D)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)
![License](https://img.shields.io/badge/license-MIT-green)

A production-grade RESTful API for enterprise inventory management, built with Clean Architecture, Redis caching, and JWT authentication. Inspired by real-world ERP systems I worked on at Epicor Software.

> **🎯 Why this project?**
> Enterprise ERP systems need APIs that are fast, secure, and maintainable at scale. This project demonstrates exactly those principles using a tech stack that maps directly to production .NET backend roles.

---

## ✨ Features

- **Clean Architecture** — strict separation of API / Application / Domain / Infrastructure layers
- **Full CRUD** — Products, Categories, and Warehouses with proper validation
- **JWT Authentication** — register, login, role-based authorization (Admin / User)
- **Refresh Tokens** — HttpOnly cookie-based rotation with reuse detection and full audit trail
- **Custom Middleware** — correlation ID tracing, request timing, global exception handling, request/response logging
- **Redis Caching** — cache-aside pattern on all GET endpoints, automatic invalidation on writes
- **Cursor Pagination** — stable, O(log n) pagination that doesn't break under concurrent writes
- **Filtering & Sorting** — filter by category, warehouse, price range; sort by any field; full-text search
- **OpenAPI / Scalar** — fully documented interactive API at `/scalar/v1`
- **Database Seeding** — 500 products, 10 categories, 5 warehouses, 3 users auto-seeded on first run
- **Docker Compose** — one command to spin up API + PostgreSQL + Redis
- **GitHub Actions CI** — builds and runs tests on every push to `main`

---

## 🏗️ Architecture

```
ErpInventoryApi/
├── ERPInventoryApi.API/                  # Controllers, middleware, DI wiring
│   ├── Controllers/
│   │   ├── AuthController.cs             # register, login, refresh, logout
│   │   ├── ProductController.cs
│   │   ├── CategoriesController.cs
│   │   └── WarehouseController.cs
│   ├── Middleware/
│   │   ├── CorrelationIdMiddleware.cs    # X-Correlation-Id on every request
│   │   ├── RequestTimingMiddleware.cs    # X-Response-Time-Ms header
│   │   ├── GlobalExceptionMiddleware.cs  # RFC 7807 ProblemDetails responses
│   │   └── RequestLoggingMiddleware.cs   # request/response body logging
│   ├── Infrastructure/
│   │   ├── OpenApi/BearerSecuritySchemeTransformer.cs
│   │   └── Seeding/DatabaseSeeder.cs
│   └── Program.cs
│
├── ERPInventoryApi.Application/          # Business logic, interfaces, DTOs
│   ├── Interfaces/
│   │   ├── IProductService.cs
│   │   ├── IProductRepository.cs
│   │   ├── ICacheService.cs
│   │   └── IAuthService.cs
│   ├── Services/
│   │   ├── ProductService.cs
│   │   ├── CategoryService.cs
│   │   └── WarehouseService.cs
│   ├── DTOs/
│   │   ├── Auth/                         # LoginRequest, RegisterRequest, AuthResponse, RefreshResponse
│   │   ├── Products/                     # ProductRequestDto, ProductResponseDto, ProductQueryParams
│   │   ├── Categories/                   # CategoryRequestDto, CategoryResponseDto, CategoryQueryParams
│   │   ├── Warehouses/                   # WarehouseRequestDto, WarehouseResponseDto, WarehouseQueryParams
│   │   └── Common/PagedResult.cs
│   └── Helpers/CacheKeys.cs
│
├── ERPInventoryApi.Domain/               # Entities, value objects (zero dependencies)
│   ├── Entities/
│   │   ├── Product.cs
│   │   ├── Category.cs
│   │   ├── Warehouse.cs
│   │   ├── User.cs
│   │   └── RefreshToken.cs              # token, userId, expiresAt, revokedAt, revokedReason
│   └── Common/BaseEntity.cs
│
└── ERPInventoryApi.Infrastructure/       # EF Core, Redis, repositories
    ├── Data/
    │   ├── AppDbContext.cs
    │   ├── AppDbContextFactory.cs        # design-time factory for EF migrations
    │   └── Migrations/
    ├── Repositories/
    │   ├── ProductRepository.cs
    │   ├── CategoryRepository.cs
    │   └── WarehouseRepository.cs
    └── Services/
        ├── AuthService.cs               # JWT generation, password hashing, token rotation
        └── CacheService.cs              # Redis get/set/invalidate with graceful degradation
```

**Key design decisions:**

- Domain layer has zero external dependencies — pure C# classes only
- All database access goes through the Repository pattern with interfaces defined in Application
- Cache-aside pattern: check Redis → on miss, query PostgreSQL → populate cache → return result
- Cache keys encode all query parameters: `products:paged:cursor=x:size=10:cat=y:sortBy=price`
- Every Redis operation is wrapped in `try/catch` — cache failures never crash the API
- Refresh tokens stored in both PostgreSQL (audit trail) and Redis (fast O(1) lookup)
- Token rotation on every refresh — reuse of a revoked token triggers full session revocation

---

## 🚀 Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Run with Docker (recommended)

```bash
# Clone the repo
git clone https://github.com/faizkhan005/erp-inventory-api.git
cd erp-inventory-api

# Start API + PostgreSQL + Redis
docker compose up --build
```

The API starts at `http://localhost:5000`.
Scalar API docs at `http://localhost:5000/scalar/v1`.
The database is automatically migrated and seeded on first run.

**Seeded credentials:**

| Username | Password | Role |
|---|---|---|
| `admin` | `Admin@1234` | Admin |
| `faizan` | `User@1234` | User |
| `testuser` | `User@1234` | User |

### Run locally (without Docker)

```bash
# 1. Start PostgreSQL and Redis (or use Docker Compose just for dependencies)
docker compose up postgres redis

# 2. Add appsettings.Development.json with your JwtSettings and ConnectionStrings
#    Use Host=localhost for local runs (not Host=postgres which is Docker-only)

# 3. Run
dotnet run --project ERPInventoryApi.API
```

---

## 📡 API Endpoints

### Authentication

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Create account, receive JWT + refresh token cookie |
| POST | `/api/auth/login` | Public | Login, receive JWT + refresh token cookie |
| POST | `/api/auth/refresh` | Cookie | Exchange refresh token for new access token |
| POST | `/api/auth/logout` | JWT | Revoke refresh token, clear cookie |

### Products

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| GET | `/api/product` | JWT | Paginated, filtered, sorted list |
| GET | `/api/product/{id}` | JWT | Single product (Redis cached) |
| POST | `/api/product` | JWT | Create product |
| PUT | `/api/product/{id}` | JWT | Update product |
| DELETE | `/api/product/{id}` | JWT Admin | Delete product |

### Categories & Warehouses

Same CRUD pattern as Products. All GETs are Redis cached. DELETE requires Admin role.

### Query Parameters (GET /api/product)

```
?cursor={guid}               # Cursor for next page (from previous response)
&pageSize=10                 # Items per page (max 50, default 10)
&categoryId={guid}           # Filter by category
&warehouseId={guid}          # Filter by warehouse
&minPrice=10&maxPrice=500    # Price range filter
&search=laptop               # Search name, SKU, description
&sortBy=price                # Sort field: name | price | stockquantity | createdat
&sortOrder=asc               # asc | desc (default: desc)
```

### Example Response (GET /api/product)

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Dell XPS 15",
      "sku": "4006381333931",
      "description": "High performance laptop",
      "price": 1299.99,
      "stockQuantity": 47,
      "reorderPoint": 10,
      "categoryId": "...",
      "categoryName": "Laptops",
      "warehouseId": "...",
      "warehouseName": "Charleston Warehouse",
      "createdAt": "2024-01-20T08:00:00Z",
      "updatedAt": "2024-01-20T08:00:00Z"
    }
  ],
  "nextCursor": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "hasNextPage": true,
  "count": 10
}
```

Pass `nextCursor` as `?cursor=` in the next request to get the next page.
All original filter and sort params must be repeated alongside the cursor.

### Refresh Token Flow

```
POST /api/auth/login
  → Body:   { accessToken, username, role, expiresAt }
  → Cookie: refreshToken (HttpOnly, Strict SameSite, 7 days)

POST /api/auth/refresh   ← no body needed, cookie sent automatically
  → Body:   { accessToken, username, role, expiresAt }
  → Old refresh token revoked, new one issued (rotation)

POST /api/auth/logout
  → Refresh token revoked in DB + Redis
  → Cookie cleared
```

---

## ⚙️ Tech Stack

| Layer | Technology | Why |
|---|---|---|
| Runtime | .NET 10 | Latest, best performance |
| Web Framework | ASP.NET Core 10 | Industry standard for .NET APIs |
| ORM | Entity Framework Core 10 | Type-safe DB access, migrations |
| Database | PostgreSQL 16 | Production-grade, open source |
| Caching | Redis 7 | Sub-millisecond reads, TTL support |
| Authentication | Custom JWT (HS256) + Refresh Tokens | Stateless access, revocable sessions |
| Logging | Serilog | Structured logs, request enrichment |
| Docs | Scalar / OpenAPI | Interactive, JWT-aware |
| Container | Docker + Docker Compose | Reproducible environments |
| CI | GitHub Actions | Automated build + test on push |
| Fake Data | Bogus | Realistic seed data generation |

---

## 🧪 Running Tests

```bash
dotnet test
```

---

## 🌱 What I Learned Building This

**Cache invalidation strategy matters.** A naive "cache everything" approach causes stale data. I implemented explicit key invalidation on every write operation — specific item keys plus all paged result keys by prefix. Redis `SCAN` with pattern matching handles bulk invalidation efficiently.

**Clean Architecture pays off immediately.** When I added Redis caching, I only touched the Infrastructure layer. When I added refresh tokens, the JWT logic stayed in Infrastructure and the interface contract in Application never changed. The Domain layer has never been touched since the initial design.

**Cursor pagination is not just academic.** Offset pagination (`SKIP n TAKE m`) requires the database to scan and discard n rows — O(n) cost that grows with page depth. Cursor pagination uses an indexed WHERE clause — O(log n) regardless. It also stays stable when rows are inserted between page requests, which offset pagination can't guarantee.

**Middleware order in ASP.NET Core is load-bearing.** Putting the exception handler after the logger means unhandled exceptions crash the logger before it can record the error. Putting authentication before correlation ID means the trace ID isn't attached to auth failure logs. The order is architecture.

**Refresh token rotation catches token theft.** When a stolen refresh token is used after the legitimate user already rotated it, the server detects reuse of a revoked token and revokes all active sessions for that user. This is the same approach used by Auth0 and Okta. Storing tokens in both PostgreSQL (audit trail) and Redis (O(1) lookup) gives you security and performance together.

**Docker Compose made environment parity a non-issue.** No more "works on my machine" — the Compose file is the environment definition. `docker compose up --build` gives anyone the full stack in under two minutes.

**`localhost` inside Docker means the container itself.** Every developer hits this once. The fix is using Docker Compose service names as hostnames (`Host=postgres`, `redis:6379`) because Compose creates an internal DNS that resolves service names to container IPs on the shared bridge network.

---

## 🗺️ Roadmap

- [x] Clean Architecture setup
- [x] EF Core + PostgreSQL — Products, Categories, Warehouses
- [x] JWT Authentication with role-based access (Admin / User)
- [x] Refresh tokens — HttpOnly cookies, rotation, reuse detection, dual storage
- [x] Custom middleware pipeline — correlation ID, timing, exception handler, logging
- [x] Redis caching — cache-aside pattern with graceful degradation
- [x] Cursor-based pagination + filtering + sorting on all entities
- [x] Scalar / OpenAPI documentation with JWT support
- [x] Database seeding with Bogus (500 products, realistic data)
- [x] Docker Compose — API + PostgreSQL + Redis
- [x] GitHub Actions CI — build + test on every push to main
- [ ] Unit + integration test suite
- [ ] Rate limiting middleware
- [ ] Azure deployment (coming soon)

---

## 👤 Author

**Faizan Ahmed Khan** — .NET Full Stack Developer
2+ years at Epicor Software building enterprise ERP and mobile applications (.NET MAUI, Azure DevOps).
Incoming MS Computer Engineering @ NYU Tandon, Fall 2026.

[![LinkedIn](https://img.shields.io/badge/LinkedIn-Connect-0077B5)](https://linkedin.com/in/faizan-814521191)
[![GitHub](https://img.shields.io/badge/GitHub-faizkhan005-181717)](https://github.com/faizkhan005)

---

## 📄 License

MIT — see [LICENSE](LICENSE) for details.
