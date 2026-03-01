# Solution Design & Trade-offs

This document describes the architectural decisions, design patterns, and deliberate trade-offs made when building the Product Catalog Management System.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Backend Design Decisions](#backend-design-decisions)
- [Frontend Design Decisions](#frontend-design-decisions)
- [Trade-offs](#trade-offs)

---

## Architecture Overview

The system follows a clean layered architecture on the backend, with each layer having a single responsibility and communicating only with the layer directly below it.

```
HTTP Request
     ↓
Controller       — routes, HTTP status mapping, no business logic
     ↓
Service          — business rules, validation, mapping, cache coordination
     ↓
Repository       — data access only, no business logic
     ↓
Storage          — EF Core in-memory DB (products) or Dictionary (categories)
```

On the frontend, each component owns its own template, styles, and change detection — no shared state store is needed for an application of this scope.

---

## Backend Design Decisions

### 1. Result\<T\> instead of exceptions for expected failures

**Decision:** All service methods return `Result<T>` rather than throwing exceptions for validation failures or not-found conditions.

**Why:** Exceptions carry significant overhead and are semantically wrong for expected outcomes. A missing product or an invalid SKU is not an exceptional event — it is a normal application flow. `Result<T>` makes the failure path visible in the method signature and forces every caller to explicitly handle it.

The controller becomes a thin mapping layer with no try-catch blocks anywhere.

---

### 2. Pattern matching for validation

**Decision:** Validation logic in service methods uses C# 9 switch expressions with property pattern matching.

**Why:** A chain of `if` statements grows linearly and requires reading every condition to understand the full set of rules. A switch expression reads like a policy table — each arm is one rule, the compiler enforces exhaustiveness, and extending it means adding one line.

---

### 3. Two repository strategies — intentional, not accidental

**Decision:** `ProductRepository` uses EF Core with `IQueryable`. `InMemoryCategoryRepository` uses a plain `Dictionary<int, Category>` with no EF dependency at all.

**Why:** The spec required demonstrating both strategies. Beyond the spec, this also shows the value of the repository pattern — both classes satisfy the same `IRepository<T, TKey>` contract, and the `CategoryService` has no knowledge of which storage mechanism it is talking to.

The `ProductRepository` needs `IQueryable` because the LINQ extension methods (`FilterBySearch`, `FilterByPriceRange`, `SortProducts`) must translate to SQL in a production deployment. Categories are queried in full on every request and benefit from in-memory speed.

---

### 4. Generic ProductSearchEngine\<T\> with no external packages

**Decision:** The search engine is built using only the .NET Base Class Library. No external NuGet packages are used.

**Why:** The spec explicitly required this. Beyond compliance, writing the algorithm from scratch demonstrates understanding of the underlying mechanics.

The engine supports:
- **Exact match** — score 1.0 per field (multiplied by weight)
- **Prefix match** — score 0.8 (user typed the start of the word)
- **Substring match** — score 0.6 (term appears anywhere in the field)
- **Token match** — score 0.65–0.75 (multi-word fields, matches individual words)
- **Fuzzy match** — Levenshtein distance ≤ 2, score scaled by edit distance

The Levenshtein implementation uses **two arrays** instead of a full `m×n` matrix, keeping memory allocations at O(min(m,n)) rather than O(m×n). This matters when searching large datasets.

---

### 5. Custom middleware from scratch

**Decision:** `RequestLoggingMiddleware` is written as a class implementing the `InvokeAsync` convention, not using `IMiddleware` or any framework helper.

**Why:** Using the convention-based approach (constructor injection of `RequestDelegate` and logger) avoids needing to register the class as `IMiddleware` in the DI container, which keeps `Program.cs` simpler. The middleware is registered via an extension method `app.UseRequestLogging()` — following ASP.NET Core's established convention.

The middleware provides:
1. Correlation ID propagation via `X-Correlation-Id` header
2. Structured request/response logging with elapsed time
3. Unhandled exception catching with JSON error responses — replacing the default HTML error page

---

### 6. SearchCacheService with TTL-based eviction

**Decision:** Search results are cached in a `ConcurrentDictionary` with a configurable TTL (default 30 seconds). Eviction is lazy — entries are removed on access, not on a background timer.

**Why:** A background eviction timer requires a hosted service and thread coordination. Lazy eviction is simpler, has no background threads, and is appropriate for this scale. The cache is fully invalidated (cleared) on any product mutation — add, update, or delete — so stale data from mutations is never served.

The cache key is built from all query parameters, meaning each unique combination of filters, page, and sort gets its own cache entry.

---

### 7. Category tree built in O(n) with two passes

**Decision:** The tree is built in `CategoryService.GetTreeAsync()` using a Dictionary for O(1) parent lookup rather than recursive queries or nested loops.

```
Pass 1: Build Dictionary<int, CategoryTreeNodeDto> — O(n)
Pass 2: Wire parent-child by looking up each node's parent — O(n)
```

Total: O(n) with no database round-trips per level. The alternative — loading children lazily per parent — would issue one query per tree node, which is the N+1 problem.

---

## Frontend Design Decisions

### 1. Standalone components with lazy-loaded routes

**Decision:** Every component uses `standalone: true`. All routes use `loadComponent` for lazy loading.

**Why:** Standalone components eliminate NgModule boilerplate and make each component's dependencies explicit in its own `imports` array. Lazy loading via `loadComponent` means each route's JavaScript is only downloaded when first navigated to — the router handles code splitting automatically.

---

### 2. BehaviorSubject + refreshSubject merged pipeline

**Decision:** The product list manages query state with two RxJS subjects feeding a single `merge()` → `switchMap()` pipeline.

`querySubject` carries filter/sort/page state and passes through `distinctUntilChanged` — preventing duplicate API calls when nothing has changed.

`refreshSubject` is a plain `Subject<void>` that bypasses `distinctUntilChanged` entirely — used after delete to guarantee the server is always re-queried even when the query parameters are identical.

**Why this specific design:** An earlier implementation used only `querySubject` for everything. After a delete, calling `querySubject.next(this.query)` was silently swallowed because `distinctUntilChanged` saw no change in the query object — so page 2 items never filled the gap. The two-subject pattern solves this cleanly without removing deduplication from the normal filter flow.

`switchMap` ensures that if the user changes filters rapidly, any in-flight HTTP request is automatically cancelled before the next one fires — no stale responses or race conditions.

---

### 3. Functional HTTP interceptor

**Decision:** The error interceptor is a function (`HttpInterceptorFn`) rather than a class implementing `HttpInterceptor`.

**Why:** Angular 15 introduced functional interceptors as the preferred approach for standalone applications. They require no class, no DI registration as a class, and integrate directly into `provideHttpClient(withInterceptors([...]))`. The interceptor enriches every HTTP error with a `friendlyMessage` property — every component reads this single property rather than mapping status codes themselves.

---

### 4. ToastService as an RxJS Subject event bus

**Decision:** All toast notifications flow through a singleton `Subject<ToastMessage>`. Components subscribe to `toast$` with `takeUntil(destroy$)` for automatic cleanup.

Two usage modes:
- **Same-page toast** (categories, delete): call `toastService.show(message)` — the current subscriber renders it immediately.
- **Cross-route toast** (after adding/editing a product and navigating back to the list): call `toastService.setPending(message)` before `router.navigate()`, then `consumePending()` in the destination's `ngOnInit`.

**Why:** The alternative — passing state through route extras or a shared service with a BehaviorSubject — either pollutes the URL or requires components to clean up stale state. The pending message pattern is intentionally simple: write once, read once, auto-cleared.

---

### 5. Typed reactive forms with nonNullable.group()

**Decision:** All forms use `FormBuilder.nonNullable.group()` rather than `FormBuilder.group()`.

**Why:** The standard `group()` allows control values to be `null` even when initialised with a string. `nonNullable.group()` ensures form control values always match their declared types — no `string | null` widening. The submit button is bound to `[disabled]="form.invalid"` so it is grayed out until all required fields pass validation, matching the same UX pattern as the category form.

---

### 6. ShareReplay(1) cache in CategoryService

**Decision:** `CategoryService.getCategories()` uses `shareReplay(1)` to cache the HTTP response in memory.

**Why:** Categories are read by three different places — the product form dropdown, the category filter dropdown, and the category management page. Without caching, each would trigger an independent HTTP request. With `shareReplay(1)`, the first subscriber triggers the request; all subsequent subscribers receive the cached result immediately. The cache is explicitly invalidated (`this.categories$ = null`) after any mutation — create, update, or delete — so the next subscriber always gets fresh data.

---

## Trade-offs

### Trade-off 1: InMemoryCategoryRepository + EF seed data duplication

The category data exists in two places: the `InMemoryCategoryRepository` constructor (seeded directly into the Dictionary) and `AppDbContext.SeedData()` (seeded into EF Core's in-memory database). This is a sync point — if you add a seed category to EF but forget the Dictionary, the data will be inconsistent.

**Why accepted:** The spec explicitly required demonstrating both storage strategies. Splitting categories to the in-memory repository and products to EF Core was the most natural division. In a production system, there would be one source of truth (a real database) and no in-memory store for categories.

---

### Trade-off 2: No authentication or authorisation

The API has no authentication. Any request to any endpoint is accepted. The CORS policy is locked to `http://localhost:4200` as a minimal guard.

**Why accepted:** The spec explicitly said to skip authentication unless time permits. The architecture supports adding it — `[Authorize]` attributes on controllers and `provideHttpClient(withInterceptors([authInterceptor]))` on the frontend are the natural insertion points.

---

### Trade-off 3: In-memory EF database resets on restart

Every restart of the API wipes all data and reseeds from the model configuration. This means products added through the UI are lost when the server restarts.

**Why accepted:** The in-memory provider was specified in the requirements. Migrating to SQL Server requires changing one line in `Program.cs` (the EF provider) and running `dotnet ef migrations add InitialCreate`. All repository interfaces and service logic remain unchanged.

---

### Trade-off 4: Category lazy eviction vs background eviction timer

The `SearchCacheService` evicts stale entries lazily on access rather than running a background timer to sweep expired entries.

**Why accepted:** A background timer requires a hosted service and careful thread coordination. For an assessment at this scale, lazy eviction is correct and simpler. The cache is also fully invalidated on any write operation, which is the main protection against stale data.

---

### Trade-off 5: No price range filter UI (backend supports it)

The `ProductQueryDto` and `ProductRepository` both support `minPrice` and `maxPrice` filtering. The LINQ extension `FilterByPriceRange` is implemented and unit tested. The Angular `ProductQuery` interface also defines these fields. However, there is no price range input in the product list UI.

**Why accepted:** Connecting the existing backend filter to the UI is a small, well-defined addition — the groundwork is entirely in place. Adding it requires a min/max input component, wiring it into `ProductListComponent.updateQuery()`, and passing it through to `ProductService.getProducts()`. This was deprioritised to invest time in the category edit/delete flow and test coverage.
