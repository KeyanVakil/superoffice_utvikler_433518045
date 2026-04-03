# MarketFlow — CRM Marketing Automation Platform

A full-stack CRM marketing automation platform for customer segmentation, email campaigns, automated journeys, and engagement analytics.

Built as a demonstration project for [SuperOffice Utvikler (Finn.no #433518045)](https://www.finn.no/job/ad/433518045).

## Skills Demonstrated

| Requirement | Implementation |
|-------------|---------------|
| **C# / .NET 8 / ASP.NET Core** | Backend API with EF Core, SQL Server, service-layer architecture |
| **TypeScript / React** | SPA with interactive segment builder, journey designer, analytics dashboards |
| **SQL / Database Design** | Normalized schema, parameterized queries, EF Core code-first data model |
| **REST API Design** | Clean resource-based endpoints with proper HTTP semantics |
| **Testing** | xUnit unit + integration tests, Vitest frontend tests |
| **Docker** | Multi-container compose setup with SQL Server, .NET API, React frontend |
| **CI/CD** | GitHub Actions pipeline with test, lint, and build steps |
| **AI Integration** | Rule-based + optional LLM content suggestions |

## Architecture Overview

```
Browser (React/TypeScript) ──> REST API (ASP.NET Core) ──> SQL Server 2022
                                        |
                                  AI Service
                            (rule-based / optional LLM)
```

Three-tier architecture: React SPA frontend, .NET 8 API backend, SQL Server database. All services run in Docker with a single command.

## Quick Start

```bash
docker compose up
```

Then open [http://localhost:3000](http://localhost:3000).

The app starts with ~100 demo contacts, sample segments, campaigns, and a customer journey pre-loaded via EF Core seed data.

## Running Tests

### Backend

```bash
# Requires SQL Server running (start it with docker compose up db)
dotnet test tests/MarketFlow.Tests/
```

### Frontend

```bash
cd web
npm ci
npm test
```

## Features

- **Contact Management** -- Searchable contact database with CSV import, tags, and activity timeline
- **Segment Builder** -- Visual rule builder with AND/OR groups and real-time audience preview
- **Email Campaigns** -- Create, preview, and send campaigns with personalization tokens
- **Customer Journeys** -- Multi-step automation with visual designer (send email, wait, condition split)
- **Analytics Dashboard** -- Engagement metrics, trend charts, and campaign performance tracking
- **AI Content Assistance** -- Smart subject line suggestions and optimal send-time recommendations

## Tech Stack

| Technology | Role | Rationale |
|-----------|------|-----------|
| C# / .NET 8 | Backend | Primary language in job requirements; LTS release |
| ASP.NET Core | Web API | Standard .NET web framework |
| Entity Framework Core | ORM | Type-safe data access with code-first modeling |
| SQL Server 2022 | Database | Required skill; natural .NET pairing |
| TypeScript / React 18 | Frontend | Required in job listing |
| Vite | Build tool | Fast TypeScript bundling |
| Docker Compose | Infrastructure | Single-command local setup |
| GitHub Actions | CI/CD | Automated test + lint + build |

## Project Structure

```
src/
  MarketFlow.Api/          .NET 8 Web API
    Controllers/            API endpoints
    Models/                 EF Core entities
    Services/               Business logic
    Data/                   DbContext, seed data
    Dockerfile
tests/
  MarketFlow.Tests/         xUnit test project
web/                        React/TypeScript SPA
  src/
    components/             UI components (contacts, campaigns, segments, journeys, analytics)
    api/                    API client
    hooks/                  Custom React hooks
    types/                  TypeScript interfaces
  tests/                    Vitest frontend tests
  Dockerfile
docker-compose.yml          Full-stack local environment
.github/workflows/ci.yml   CI pipeline
```

## Job Listing

[https://www.finn.no/job/ad/433518045](https://www.finn.no/job/ad/433518045)
