# Adroit URL Shortener

A high-performance URL shortening service built with C# ASP.NET Core 8 and React TypeScript.

## Live Demo

**Production URL**: https://adroit-api.azurewebsites.net

## Features

- **Create Short URLs**: Generate short, memorable links with custom or auto-generated codes
- **Delete Short URLs**: Remove URLs you no longer need
- **Click Statistics**: Track how many times each short URL is accessed
- **Multiple Mappings**: One long URL can have multiple short codes
- **Thread-Safe**: Uses ConcurrentDictionary for safe concurrent access
- **In-Memory Storage**: Fast, no external database required (data persists until restart)

## Project Structure

```
Adroit/
├── Adroit.sln                     # Solution file
├── Adroit.API/                    # ASP.NET Core Web API
├── Adroit.Core/                   # Domain entities and interfaces
├── Adroit.Data/                   # Data access (in-memory repository)
├── Adroit.Infrastructure/         # Business logic and services
├── Adroit.Tests/                  # Unit tests (xUnit)
└── Adroit.Web/                    # React TypeScript frontend
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- npm or yarn

## Quick Start

### Backend (API)

```bash
# Navigate to the solution directory
cd Adroit

# Restore dependencies
dotnet restore

# Run the API (from solution root)
dotnet run --project Adroit.API

# The API will be available at:
# - http://localhost:5000 (HTTP)
# - Swagger UI: http://localhost:5000/swagger
```

### Frontend (React)

```bash
# Navigate to the web project
cd Adroit.Web

# Install dependencies
npm install

# Start the development server
npm start

# The frontend will be available at:
# - http://localhost:3000
```

### Run Tests

```bash
# From solution root
dotnet test
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/urls` | Create a short URL |
| GET | `/api/urls` | List all URLs |
| GET | `/api/urls/{shortCode}` | Get URL details |
| DELETE | `/api/urls/{shortCode}` | Delete a URL |
| GET | `/api/urls/{shortCode}/stats` | Get click statistics |
| GET | `/api/urls/lookup?longUrl=...` | Find URLs by long URL |
| GET | `/{shortCode}` | Redirect to long URL |

### Example: Create a Short URL

```bash
curl -X POST http://localhost:5000/api/urls \
  -H "Content-Type: application/json" \
  -d '{
    "longUrl": "https://www.example.com/very/long/path",
    "customShortCode": "mycode"
  }'
```

Response:
```json
{
  "success": true,
  "data": {
    "id": "abc123...",
    "shortCode": "mycode",
    "shortUrl": "http://localhost:5000/mycode",
    "longUrl": "https://www.example.com/very/long/path",
    "clickCount": 0,
    "createdAt": "2024-01-15T10:30:00Z"
  }
}
```

## Architecture

### Backend Layers

1. **Adroit.Core** - Domain layer
   - Entities: `ShortUrl`, `BaseEntity`
   - Interfaces: `IUrlRepository`, `IUrlService`, `IShortCodeGenerator`
   - Custom exceptions

2. **Adroit.Data** - Data access layer
   - `InMemoryUrlRepository`: Thread-safe storage using `ConcurrentDictionary`

3. **Adroit.Infrastructure** - Business logic
   - `UrlService`: URL creation, deletion, and statistics
   - `ShortCodeGenerator`: Base62 random code generation

4. **Adroit.API** - Presentation layer
   - REST API controllers
   - DTOs for request/response
   - Swagger documentation

### Frontend

- React 18 with TypeScript
- Redux Toolkit for state management
- Axios for API calls

## Design Decisions

1. **In-Memory Storage**: Uses `ConcurrentDictionary` for thread-safe, high-performance storage. Data is not persisted between restarts (suitable for POC).

2. **Base62 Encoding**: Short codes use `a-z`, `A-Z`, `0-9` characters. 7 characters = 3.5 trillion combinations.

3. **Case-Insensitive Lookup**: Short codes are stored normalized to lowercase for consistent lookups.

4. **Fire-and-Forget Clicks**: Click counting is done asynchronously to minimize redirect latency.

## Azure Deployment

The application is deployed to Azure:
- **Backend + Frontend**: Azure App Service (https://adroit-api.azurewebsites.net)
- **Database**: Azure SQL Database

### CI/CD

GitHub Actions workflow is configured for automated deployment on push to `main` branch.

### Manual Deployment

```bash
# Build for release
dotnet publish Adroit.API -c Release -o ./publish

# Deploy to Azure App Service using Azure CLI
az webapp deploy --resource-group rg-adroit --name adroit-api --src-path ./publish
```

## Tech Stack

| Layer | Technology |
|-------|------------|
| Backend | ASP.NET Core 8, C# |
| Frontend | React 18, TypeScript, Redux Toolkit |
| Database | Azure SQL (EF Core) / In-Memory (dev) |
| Hosting | Azure App Service |
| CI/CD | GitHub Actions |

## License

MIT License
