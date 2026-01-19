# Smart Task Management API

A production-ready multi-tenant task management system built with ASP.NET Core, Clean Architecture, and PostgreSQL.

## Features

- 🔐 Multi-tenancy with data isolation
- 👥 User authentication (JWT + Refresh Tokens)
- 🎯 Role-based authorization (Admin/User)
- 📝 Task lifecycle management
- 🔄 Background jobs for reminders
- 📊 Audit logging
- 📄 Pagination, filtering, sorting
- 🗃️ Soft deletes
- 📈 API versioning
- 📚 Swagger documentation

## Tech Stack

- .NET 8 LTS
- ASP.NET Core Web API
- PostgreSQL with EF Core
- Clean Architecture
- JWT Authentication
- Hangfire for background jobs
- FluentValidation
- AutoMapper
- Serilog for logging

## Getting Started

### Prerequisites
- .NET 8 SDK
- Docker Desktop (for PostgreSQL)
- Visual Studio 2022 / VS Code / Rider

### Quick Start

1. Clone the repository
2. Run `docker-compose up -d` to start PostgreSQL
3. Update connection string in appsettings.Development.json
4. Run the API project

## Architecture

The project follows Clean Architecture principles:

Domain → Application → Infrastructure → API

text

- **Domain**: Entities, value objects, domain events
- **Application**: Use cases, DTOs, validation, interfaces
- **Infrastructure**: Database, external services, background jobs
- **API**: Controllers, middleware, dependency injection

## API Documentation

Once running, access Swagger UI at:
- `https://localhost:5001/swagger` (or your configured port)

## License

MIT
