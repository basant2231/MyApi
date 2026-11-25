# MyApi — Production-Ready Book API

A .NET Web API with JWT Authentication, designed for Flutter consumption.

## Features

- **JWT Authentication** with refresh token rotation
- **Account security**: lockout after 5 failed attempts, cascade token revocation
- **Book CRUD**: create, read, update, delete with ownership control
- **Search & Filter**: by title/author, genre, with pagination and sorting
- **Favorites**: toggle favorite, list favorites
- **Reading Progress**: track pages read, status (WantToRead/Reading/Finished/Dropped), personal rating
- **Rate Limiting**: 100 req/min general, 10 req/min auth endpoints
- **Security Headers**: CSP, X-Frame-Options, X-Content-Type-Options, etc.
- **Structured Logging**: Serilog with file rotation
- **Health Checks**: `/health` endpoint
- **Swagger UI**: interactive API docs in development mode

## Tech Stack

- .NET 11 / ASP.NET Core
- SQLite (EF Core)
- JWT + BCrypt
- FluentValidation
- Serilog

## Quick Start

```bash
# 1. Clone
git clone https://github.com/basant2231/MyApi.git
cd MyApi

# 2. Run (dev mode)
dotnet run

# 3. Open Swagger UI
# http://localhost:5120
```

## API Endpoints

### Auth (Rate Limited: 10 req/min)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | ❌ | Create account |
| POST | `/api/auth/login` | ❌ | Login |
| POST | `/api/auth/refresh-token` | ❌ | Refresh access token |
| POST | `/api/auth/logout` | ✅ | Logout (revoke token) |
| GET | `/api/auth/profile` | ✅ | Get profile |
| POST | `/api/auth/change-password` | ✅ | Change password |

### Books (Rate Limited: 100 req/min)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/books` | ❌ | Browse books (search, filter, paginate) |
| GET | `/api/books/{id}` | ❌ | Get book details |
| GET | `/api/books/genres` | ❌ | List all genres |
| POST | `/api/books` | ✅ | Add a book |
| PUT | `/api/books/{id}` | ✅ | Update a book (owner only) |
| DELETE | `/api/books/{id}` | ✅ | Delete a book (owner only) |
| POST | `/api/books/{id}/favorite` | ✅ | Toggle favorite |
| GET | `/api/books/my/favorites` | ✅ | My favorites |
| PUT | `/api/books/{id}/progress` | ✅ | Update reading progress |
| GET | `/api/books/my/library` | ✅ | My reading library |

### System

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check |

## Flutter Integration

```dart
// 1. Register
final response = await http.post(
  Uri.parse('$baseUrl/api/auth/register'),
  headers: {'Content-Type': 'application/json'},
  body: jsonEncode({
    'username': 'john',
    'email': 'john@example.com',
    'password': 'SecurePass123!',
  }),
);

// 2. Use the access token for authenticated requests
final token = jsonDecode(response.body)['data']['accessToken'];
final books = await http.get(
  Uri.parse('$baseUrl/api/books?page=1&pageSize=20'),
  headers: {'Authorization': 'Bearer $token'},
);
```

## Production Checklist

- [ ] Change JWT key in `appsettings.Production.json`
- [ ] Update CORS origins for your domain
- [ ] Switch from SQLite to PostgreSQL/SQL Server
- [ ] Use EF Core Migrations instead of `EnsureCreated()`
- [ ] Deploy behind a reverse proxy (Nginx/Caddy)
- [ ] Enable HTTPS
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`

## License

MIT
