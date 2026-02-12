# Smart Task Management API (Open Source)

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)
![Auth0](https://img.shields.io/badge/Auth0-JWT-eb5424?logo=auth0)
![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-00a1e0)
![License](https://img.shields.io/badge/license-MIT-blue)
![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)
![Open Source](https://badges.frapsoft.com/os/v1/open-source.svg?v=103)

---

## 🚀 Smart Task Management API

Enterprise-grade, open-source Task Management API built with Clean Architecture, .NET 8, PostgreSQL, Auth0, and Hangfire.

This project demonstrates a production-ready, multi-tenant task management system that you can use, learn from, and contribute to. It strictly follows Clean Architecture and SOLID principles, with zero external dependencies in the Domain layer. The codebase is fully tested and containerized — ready for cloud deployment.

---

## ✨ Why Open Source?

We believe in sharing knowledge and building better software together.

This project is free and open source because:

* 🧠 **Learn** — Study a real-world Clean Architecture implementation
* 🛠️ **Use** — Integrate it into your own projects, modify it, and extend it
* 🤝 **Contribute** — Help fix bugs, add features, and improve documentation
* 📈 **Grow** — Build your portfolio with a production-grade .NET project

Every contribution, no matter how small, is welcome.

---

## 🌟 Features

### Core Domain

* ✅ **WorkItem** (instead of Task) — title, description, priority, due date, tags
* ✅ **WorkItemState lifecycle** — Draft → InProgress → Completed → Archived
* ✅ **Reminders** — scheduled per WorkItem, triggered via Hangfire
* ✅ **Multi-tenancy** — full tenant isolation via Auth0 claims
* ✅ **Audit fields** — CreatedBy, UpdatedBy, DeletedBy, timestamps
* ✅ **Soft delete** — data is never permanently removed (only admins can permanently delete)

### API & Architecture

* ✅ Clean Architecture — Domain, Application, Infrastructure, API layers
* ✅ CQRS with MediatR — commands and queries separated
* ✅ FluentValidation — request validation
* ✅ AutoMapper — object mapping
* ✅ Result pattern — consistent error handling
* ✅ API versioning — URL, header, query string
* ✅ Swagger / OpenAPI — fully documented and JWT-aware
* ✅ Global exception handling — RFC 7807 Problem Details

### Infrastructure

* ✅ PostgreSQL 16 — main and Hangfire databases
* ✅ Entity Framework Core 8 — code-first with migrations
* ✅ Hangfire — background job processing for reminders
* ✅ Auth0 — JWT bearer authentication with role and tenant claims
* ✅ Serilog — structured logging (Console, File, Seq)
* ✅ Health checks — database, Hangfire, custom
* ✅ Rate limiting — sliding window per IP
* ✅ CORS — configurable allowed origins

### Testing

* ✅ Unit tests — Domain & Application (xUnit, Moq, FluentAssertions)
* ✅ Integration tests — Infrastructure & API (Testcontainers, Respawn, WebApplicationFactory)
* ✅ Testcontainers — real PostgreSQL container for integration tests
* ✅ Respawn — fast database cleanup between tests

### DevOps & Deployment

* ✅ Docker — multi-stage build with Docker Compose
* ✅ CI/CD ready — GitHub Actions workflow example
* ✅ Production hardening — `appsettings.Production.json`, secrets via environment variables
* ✅ Reverse proxy ready — designed for Traefik or Nginx with Let's Encrypt

---

## 🏗️ Architecture Overview

```text
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Domain Layer  │────▶│ Application     │────▶│ Infrastructure  │────▶│ API Layer       │
│   (Pure C#)     │     │ Layer           │     │ Layer           │     │ (Controllers)   │
│                 │     │ (Use Cases)     │     │ (EF, Hangfire,  │     │                 │
│                 │     │                 │     │ Auth0, etc.)    │     │                 │
└─────────────────┘     └─────────────────┘     └─────────────────┘     └─────────────────┘
         ▲                       ▲                       ▲                       ▲
         └───────────────────────┴───────────────────────┴───────────────────────┘
                           Dependency Direction (Inward)
```

**Domain** — No external dependencies. Contains entities, value objects, enums, domain services, and domain events. <br/>
**Application** — Depends only on Domain. Contains MediatR handlers, DTOs, validators, mapping profiles, and repository interfaces.<br/>
**Infrastructure** — Implements Application interfaces. Contains EF Core DbContext, repositories, Hangfire jobs, Auth0 JWT configuration, email services, etc.<br/>
**API** — Entry point. Depends on Application and Infrastructure. Contains controllers, middleware, filters, and Swagger configuration.

---

## 🛠️ Technology Stack

| Category          | Technology                                            |
| ----------------- | ----------------------------------------------------- |
| Framework         | .NET 8                                                |
| Database          | PostgreSQL 16                                         |
| ORM               | Entity Framework Core 8                               |
| Authentication    | Auth0 / JWT Bearer                                    |
| Background Jobs   | Hangfire                                              |
| Mapping           | AutoMapper                                            |
| Validation        | FluentValidation                                      |
| CQRS / Mediator   | MediatR                                               |
| Logging           | Serilog (Console, File, Seq)                          |
| Testing           | xUnit, Moq, FluentAssertions, Testcontainers, Respawn |
| Containerization  | Docker / Docker Compose                               |
| API Documentation | Swashbuckle / Swagger                                 |

---

## 📁 Project Structure

```text
SmartTaskManagement/
├── src/
│   ├── SmartTaskManagement.Domain/
│   ├── SmartTaskManagement.Application/
│   ├── SmartTaskManagement.Infrastructure/
│   └── SmartTaskManagement.API/
├── tests/
│   ├── SmartTaskManagement.Domain.UnitTests/
│   ├── SmartTaskManagement.Application.UnitTests/
│   ├── SmartTaskManagement.Infrastructure.IntegrationTests/
│   └── SmartTaskManagement.API.IntegrationTests/
├── scripts/
├── docker-compose.yml
├── Dockerfile
├── LICENSE
└── SmartTaskManagement.sln
```

---

## 🚀 Getting Started

### Prerequisites

* .NET 8 SDK
* Docker Desktop (or Docker Engine + Compose)
* Auth0 account (free tier works)

---

### Quick Start (Local with Docker Compose)

#### Clone the repository

```bash
git clone git@github.com:Emmanuel-Ejeagha/Smart_Task_Management_API.git
cd Smart_Task_Management_API
```

#### Configure Auth0

1. Create an API in Auth0 with identifier:
   `https://api.smarttaskmanagement.com`

2. Create a Machine-to-Machine application and authorize it for your API.

3. Copy `Domain`, `ClientId`, and `ClientSecret` into your `.env` file (from `.env.example`).

---

#### Run the automated setup script

```bash
chmod +x scripts/setup-and-run.sh
./scripts/setup-and-run.sh
```

This script will:

* Clean old containers and volumes
* Start PostgreSQL and create the Hangfire database
* Build the project and apply EF Core migrations
* Start the full stack (API and Seq)
* Wait for health checks and verify Swagger UI

---

### Access the API

* Swagger UI: [http://127.0.0.1:5000/swagger](http://127.0.0.1:5000/swagger)
* API Base URL: [http://127.0.0.1:5000](http://127.0.0.1:5000)
* Seq (logs): [http://localhost:8081](http://localhost:8081)
* Hangfire Dashboard: [http://127.0.0.1:5000/hangfire](http://127.0.0.1:5000/hangfire) (admin role required)

---

## 🧪 Testing

### Unit Tests

```bash
dotnet test tests/SmartTaskManagement.Domain.UnitTests
dotnet test tests/SmartTaskManagement.Application.UnitTests
```

### Integration Tests (Docker required)

```bash
dotnet test tests/SmartTaskManagement.Infrastructure.IntegrationTests
dotnet test tests/SmartTaskManagement.API.IntegrationTests
```

Integration tests use Testcontainers — a real PostgreSQL container is spun up automatically.
Ensure Docker is running before execution.

---

## 🤝 Contributing

We welcome contributions.

1. Fork the repository
2. Create a feature branch

   ```bash
   git checkout -b feature/amazing-feature
   ```
3. Commit your changes
4. Push to your branch
5. Open a Pull Request

Please read `CONTRIBUTING.md` for development guidelines.

---

## 📜 License

This project is licensed under the MIT License — see the `LICENSE` file for details.
You are free to use, modify, and distribute it for any purpose, including commercial applications.

---

## 🙏 Acknowledgements

* Clean Architecture by Robert C. Martin
* Jason Taylor’s Clean Architecture template
* Auth0 for identity services
* All open-source libraries used in this project

---

## 📬 Contact & Community

Maintainer: **Your Name** — [your.email@example.com](mailto:your.email@example.com)

* GitHub Issues — report bugs
* Discussions — start a discussion
* Twitter — @Emma_Ejeagha

If you find this project useful, consider giving it a ⭐.

---