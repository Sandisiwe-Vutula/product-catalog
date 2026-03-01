# Product Catalog Management System

A full-stack product catalog management system built with **ASP.NET Core** (backend) and **Angular** (ui). Administrators can manage products and categories through a web interface with search, filtering, pagination, and inventory tracking.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Running the Backend](#running-the-backend)
- [Running the Frontend](#running-the-frontend)
- [Running the Tests](#running-the-tests)
- [API Reference](#api-reference)
- [Seeded Data](#seeded-data)

---

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 10+ | https://dotnet.microsoft.com/download |
| Node.js | 24+ | https://nodejs.org |
| Angular CLI | 21+ | `npm install -g @angular/cli` |

Verify your versions:

```bash
dotnet --version   # should be 10.x
node --version     # should be 24.x or higher
ng version         # should be 21.x or higher
```

---

## Project Structure

```
product-catalog/
├── backend/          				  # ASP.NET Core Web API
│   ├── ProductCatalog.API/
│   │   ├── Controllers/              # ProductsController, CategoriesController
│   │   ├── Data/                     # AppDbContext + EF Core seed data
│   │   ├── DTOs/                     # C# 9 record types (request/response shapes)
│   │   ├── Extensions/               # Custom LINQ extension methods
│   │   ├── Interfaces/               # IRepository<T>, IProductService, ICategoryService…
│   │   ├── Middleware/               # RequestLoggingMiddleware (custom, from scratch)
│   │   ├── Models/                   # Product (IComparable<T>), Category
│   │   ├── Repositories/             # RepositoryBase<T> (EF) + InMemoryCategoryRepository
│   │   ├── Services/                 # ProductService, CategoryService, SearchCacheService
│   │   ├── Utilities/                # ProductSearchEngine<T>, Result<T>
│   │   └── Program.cs
│   └── ProductCatalog.Tests/         # xUnit test project
│       ├── ProductServiceTests.cs
│       ├── CategoryServiceTests.cs
│       ├── ProductSearchEngineTests.cs
│       ├── ProductQueryExtensionsTests.cs
│       └── SearchCacheServiceTests.cs
│
└── ui/               				  # Angular SPA
    └── src/app/
        ├── components/
        │   ├── product-list/         # Product grid with search, filter, pagination
        │   ├── product-form/         # Add / edit product (single shared component)
        │   ├── category-management/  # Category tree with add, edit, delete
        │   ├── category-filter/      # Category dropdown filter
        │   ├── search-bar/           # Debounced search input
        │   └── confirm-dialog/       # Reusable confirmation dialog
        ├── services/
        │   ├── product.service.ts
        │   ├── category.service.ts
        │   └── toast.service.ts      # Cross-route notification service
        ├── interceptors/
        │   └── error.interceptor.ts  # Global HTTP error handler
        └── models/
            └── models.ts             # TypeScript interfaces for all entities
```

---

## Running the Backend

### 1. Navigate to the API project

```bash
cd backend/ProductCatalog.API
```

### 2. Restore dependencies and run

```bash
dotnet restore
dotnet run
```

The API will start at:

```
https://localhost:44345/   (HTTPS)
```

### 3. Open Swagger UI

Navigate to **https://localhost:44345/index.html** in your browser.

Swagger UI loads and lists all available endpoints.

> **Note:** The application uses an **in-memory database**. All seeded data (27 products, 6 categories) is loaded automatically on startup. Data resets when the API restarts — this is by design.

### Updating the proxy port (if your port differs)

If your API starts on a different port, update `product-catalog-ui/proxy.conf.json`:

```json
{
  "/api": {
    "target": "https://localhost:YOUR_PORT",
    "secure": false,
    "changeOrigin": true
  }
}
```

---

## Running the Frontend

Open a **new terminal** (keep the API running in the first one).

### 1. Navigate to the Angular project

```bash
cd ui
```

### 2. Install dependencies

```bash
npm install
```

> If you see errors about missing Karma packages, run:
> ```bash
> npm install --save-dev karma karma-jasmine karma-jasmine-html-reporter karma-chrome-launcher jasmine-core karma-coverage @angular/animations
> ```

### 3. Start the development server

```bash
ng serve --open
```

The app opens automatically at **http://localhost:4200**.

The Angular dev server proxies all `/api/*` requests to the ASP.NET Core API via `proxy.conf.json`, which avoids CORS and HTTPS certificate issues in development.

> **Both the API and the Angular dev server must be running at the same time.**

---

## Running the Tests

### Backend (xUnit)

```bash
cd backend
dotnet test
```

To see test names and results in the console:

```bash
dotnet test --logger "console;verbosity=normal"
```

**Test coverage:**

| Test Class | Tests | What it covers |
|---|---|---|
| `ProductSearchEngineTests` | 9 | Exact, prefix, fuzzy, case-insensitive, weighted scoring |
| `ProductServiceTests` | 11 | All CRUD operations, cache hit/miss, validation |
| `CategoryServiceTests` | 12 | Tree building, CRUD, circular-parent guard, child-delete guard |
| `ProductQueryExtensionsTests` | 12 | Custom LINQ filter and sort methods |
| `SearchCacheServiceTests` | 8 | TTL expiry, key isolation, invalidation |

### Frontend (Karma / Jasmine)

```bash
cd ui
ng test
```

**Frontend test coverage:**

| Test Class | Tests | What it covers |
|---|---|---|
| `SearchBarComponent` | 6 | Debounce timing, distinctUntilChanged, clear, DOM |
| `ProductService` | 7 | HTTP calls, query params, error propagation |
| `AppComponent` | 3 | Bootstrap, title, nav render |

---

## API Reference

### Products

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/products` | Paged list with filtering and sorting |
| `GET` | `/api/products/{id}` | Single product by ID |
| `GET` | `/api/products/search?q=` | Fuzzy search with weighted scoring |
| `POST` | `/api/products` | Create a new product |
| `PUT` | `/api/products/{id}` | Update an existing product |
| `DELETE` | `/api/products/{id}` | Delete a product |

**GET /api/products — query parameters:**

| Parameter | Type | Example | Description |
|-----------|------|---------|-------------|
| `search` | string | `laptop` | Search name, description, and SKU |
| `categoryId` | int | `2` | Filter by category |
| `minPrice` | decimal | `100` | Minimum price (inclusive) |
| `maxPrice` | decimal | `500` | Maximum price (inclusive) |
| `inStock` | bool | `true` | Filter by stock availability |
| `sortBy` | string | `price` | `name`, `price`, `quantity`, `createdat`, `sku` |
| `sortDesc` | bool | `true` | Reverse sort direction |
| `page` | int | `1` | Page number (1-based) |
| `pageSize` | int | `12` | Items per page (max 100) |

### Categories

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/categories` | Flat list of all categories |
| `GET` | `/api/categories/tree` | Hierarchical tree structure |
| `GET` | `/api/categories/{id}` | Single category by ID |
| `POST` | `/api/categories` | Create a new category |
| `PUT` | `/api/categories/{id}` | Update a category |
| `DELETE` | `/api/categories/{id}` | Delete a category (blocked if it has children) |

---

## Seeded Data

The application seeds the following data on startup:

**Categories (6):**
- Electronics → Computers, Phones
- Clothing → Mens, Womens

**Products (27):**
- 6 × Computers (laptops, desktop, monitor, mouse, keyboard)
- 5 × Phones (iPhone, Samsung, Pixel, case, charger)
- 5 × Mens clothing (t-shirt, jeans, jacket, shoes, shirt)
- 5 × Womens clothing (dress, heels, handbag, yoga pants, coat)
- 6 × Electronics (speaker, smartwatch, headphones, hub, SSD, tablet)

> The `InMemoryCategoryRepository` and the EF seed data are kept in sync manually. Categories deleted through the UI will not reappear until the API restarts.
