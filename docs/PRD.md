# PRD: CRM Marketing Automation Platform

## 1. Project Overview

**MarketFlow** is a CRM marketing automation platform that enables marketers to create customer segments, build email campaigns, define automated customer journeys, and track engagement analytics — the core workflow SuperOffice's CRM Marketing team builds every day.

### Why This Project

SuperOffice's CRM Marketing team owns email campaigns, marketing automation, analytics, and customer journeys. This project is a focused, working implementation of exactly those capabilities. It also integrates AI for smarter content suggestions — directly aligned with SuperOffice's initiative to bring AI into the CRM platform.

### The Problem It Solves

Marketers need a unified tool to:
- Segment their customer base by attributes and behavior
- Create and send targeted email campaigns to those segments
- Automate multi-step customer journeys (e.g., welcome series, re-engagement flows)
- Measure campaign performance with real-time analytics

MarketFlow delivers all four in a single, self-contained application.

---

## 2. Technical Architecture

```
┌─────────────────────────────────────────────────────┐
│                   Browser (TypeScript/React)         │
│  ┌──────────┐ ┌──────────┐ ┌────────┐ ┌──────────┐ │
│  │Campaigns │ │Segments  │ │Journeys│ │Analytics │ │
│  └────┬─────┘ └────┬─────┘ └───┬────┘ └────┬─────┘ │
│       └─────────────┴───────────┴───────────┘       │
│                         │ REST API                   │
└─────────────────────────┼───────────────────────────┘
                          │
┌─────────────────────────┼───────────────────────────┐
│            ASP.NET Core Web API (C#/.NET 8)          │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────┐  │
│  │Campaign Svc  │  │Segment Svc   │  │Journey Svc│  │
│  │  - CRUD      │  │  - Builder   │  │  - Engine  │  │
│  │  - Sending   │  │  - Evaluation│  │  - Steps   │  │
│  └──────┬───────┘  └──────┬───────┘  └─────┬─────┘  │
│         │                 │                 │         │
│  ┌──────┴─────────────────┴─────────────────┴─────┐  │
│  │              Data Access (EF Core)              │  │
│  └─────────────────────┬───────────────────────────┘  │
│                        │                              │
│  ┌─────────────────────┴───────────────────────────┐  │
│  │          AI Service (content suggestions)       │  │
│  └─────────────────────────────────────────────────┘  │
└────────────────────────┬────────────────────────────┘
                         │
              ┌──────────┴──────────┐
              │   SQL Server 2022   │
              │   (Docker)          │
              └─────────────────────┘
```

### Key Components

| Component | Responsibility |
|-----------|---------------|
| **React Frontend** | SPA for campaign management, segment builder, journey designer, analytics dashboards |
| **ASP.NET Core API** | REST endpoints, business logic, data validation, background job processing |
| **Campaign Service** | CRUD for email campaigns, template rendering, simulated send execution |
| **Segment Service** | Dynamic segment builder with rule-based filtering, segment evaluation against contacts |
| **Journey Service** | Multi-step automation engine — triggers, delays, conditions, email actions |
| **AI Service** | Email subject line suggestions and send-time recommendations (rule-based + optional LLM) |
| **Data Access** | Entity Framework Core with SQL Server, migrations, repository pattern |
| **SQL Server** | Relational store for contacts, campaigns, segments, journeys, and engagement events |

### Data Flow

1. **Segmentation**: User defines segment rules (e.g., "industry = Tech AND last activity < 30 days") -> API evaluates rules against contacts -> returns matching contact set
2. **Campaign**: User creates campaign with content + segment -> API resolves segment -> renders personalized emails -> records send events
3. **Journey**: User defines journey (trigger + steps) -> engine activates journey for contacts matching trigger -> executes steps (wait, condition check, send email) on schedule
4. **Analytics**: Send/open/click events are recorded -> API aggregates into campaign and journey performance metrics -> frontend renders charts

---

## 3. Tech Stack

| Technology | Role | Rationale |
|-----------|------|-----------|
| **C# / .NET 8** | Backend API | Primary language in job requirements; LTS release |
| **ASP.NET Core** | Web framework | Standard .NET web API framework |
| **Entity Framework Core** | ORM | Type-safe data access with migrations for SQL Server |
| **SQL Server 2022** | Database | SQL is a required skill; SQL Server is natural with .NET |
| **TypeScript** | Frontend language | Required in job listing |
| **React 18** | Frontend framework | Modern web tech mentioned in listing; strong TypeScript support |
| **Vite** | Build tool | Fast TypeScript bundling, HMR for development |
| **xUnit + Moq** | Testing | Standard .NET testing stack |
| **Vitest** | Frontend testing | Fast TypeScript test runner compatible with Vite |
| **GitHub Actions** | CI/CD | Job lists GitHub and CI/CD as requirements |
| **Docker Compose** | Local orchestration | Single-command local setup |

### AI Integration Approach

The AI features use a **built-in rule-based engine by default** (no API keys required). Subject line suggestions use proven copywriting heuristics (length optimization, power words, personalization tokens). Send-time recommendations use engagement history patterns.

If the environment variable `OPENAI_API_KEY` is set, the subject line generator upgrades to LLM-powered suggestions. This is optional — the app is fully functional without it.

---

## 4. Features & Acceptance Criteria

### Feature 1: Contact Management

Manage a CRM contact database with company and activity tracking.

| # | Acceptance Criteria |
|---|-------------------|
| 1.1 | User can view a paginated, searchable list of contacts |
| 1.2 | User can create/edit contacts with fields: name, email, company, industry, tags |
| 1.3 | Each contact shows an activity timeline (emails sent, opens, clicks) |
| 1.4 | Contacts can be imported via CSV upload |
| 1.5 | API supports filtering contacts by any field combination |

### Feature 2: Segment Builder

Create dynamic customer segments using rule-based conditions.

| # | Acceptance Criteria |
|---|-------------------|
| 2.1 | User can create segments with AND/OR rule groups |
| 2.2 | Available rule fields: industry, company, tags, last activity date, email engagement score |
| 2.3 | Segment preview shows matching contact count in real-time as rules change |
| 2.4 | Segments are evaluated dynamically — contacts matching rules at query time are included |
| 2.5 | Segment evaluation query is generated as parameterized SQL (no raw string concatenation) |

### Feature 3: Email Campaign Manager

Create, preview, and send email campaigns to segments.

| # | Acceptance Criteria |
|---|-------------------|
| 3.1 | User can create campaigns with: subject, HTML body (rich text editor), target segment |
| 3.2 | Email body supports personalization tokens: `{{firstName}}`, `{{company}}` |
| 3.3 | Campaign preview renders with sample contact data before sending |
| 3.4 | "Send campaign" resolves the target segment and records a send event per contact |
| 3.5 | Campaign list shows status (draft / sent) with sent count and engagement rates |
| 3.6 | AI suggests 3 subject line alternatives when user enters a draft subject |

### Feature 4: Customer Journey Designer

Build automated multi-step marketing journeys with a visual editor.

| # | Acceptance Criteria |
|---|-------------------|
| 4.1 | User can create a journey with a trigger condition (e.g., "added to segment X", "tag added") |
| 4.2 | Journey steps: **Send Email**, **Wait** (N hours/days), **Condition** (if/else on engagement) |
| 4.3 | Visual journey editor displays steps as a connected flowchart |
| 4.4 | Journey can be activated/deactivated; active journeys process new contacts matching the trigger |
| 4.5 | Each contact's journey progress is tracked (current step, completed steps, timestamps) |

### Feature 5: Analytics Dashboard

Real-time campaign and journey performance metrics.

| # | Acceptance Criteria |
|---|-------------------|
| 5.1 | Dashboard shows: total contacts, active campaigns, active journeys, overall engagement rate |
| 5.2 | Campaign detail view shows: sends, opens, clicks, open rate, click-through rate over time |
| 5.3 | Journey detail view shows: contacts entered, completed, dropped off at each step |
| 5.4 | Charts render engagement trends over a selectable date range (7d, 30d, 90d) |
| 5.5 | Data refreshes on navigation (no manual refresh needed) |

### Feature 6: AI-Powered Content Assistance

Smart suggestions to improve marketing content and timing.

| # | Acceptance Criteria |
|---|-------------------|
| 6.1 | Given a draft subject line, the system suggests 3 improved alternatives |
| 6.2 | Suggestions work without any API key (rule-based defaults) |
| 6.3 | If `OPENAI_API_KEY` is set, suggestions use LLM for higher quality |
| 6.4 | Suggested send-time is computed from historical engagement patterns per segment |
| 6.5 | AI features are clearly labeled in the UI and non-blocking (campaign works without them) |

---

## 5. Data Models

### Entity Relationship Diagram

```
Contact ──────< ContactTag >────── Tag
   │
   │ 1:N
   ▼
ActivityEvent (send, open, click)
   │
   │ N:1
   ▼
Campaign ──────── Segment
                    │
                    │ 1:N
                    ▼
               SegmentRule
                    
Journey ──────< JourneyStep
   │
   │ 1:N
   ▼
JourneyEnrollment ──── Contact
   │
   │ 1:N
   ▼
JourneyStepExecution
```

### Database Schema

```sql
-- Core CRM
CREATE TABLE Contacts (
    Id              INT IDENTITY PRIMARY KEY,
    FirstName       NVARCHAR(100) NOT NULL,
    LastName        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(255) NOT NULL UNIQUE,
    Company         NVARCHAR(200),
    Industry        NVARCHAR(100),
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Tags (
    Id              INT IDENTITY PRIMARY KEY,
    Name            NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE ContactTags (
    ContactId       INT NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    TagId           INT NOT NULL REFERENCES Tags(Id) ON DELETE CASCADE,
    PRIMARY KEY (ContactId, TagId)
);

-- Segmentation
CREATE TABLE Segments (
    Id              INT IDENTITY PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(1000),
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE SegmentRules (
    Id              INT IDENTITY PRIMARY KEY,
    SegmentId       INT NOT NULL REFERENCES Segments(Id) ON DELETE CASCADE,
    GroupIndex      INT NOT NULL,           -- rules in same group are AND'd
    Field           NVARCHAR(50) NOT NULL,  -- 'industry', 'company', 'tag', 'lastActivity', 'engagementScore'
    Operator        NVARCHAR(20) NOT NULL,  -- 'equals', 'contains', 'greaterThan', 'lessThan', 'before', 'after'
    Value           NVARCHAR(200) NOT NULL,
    SortOrder       INT NOT NULL DEFAULT 0
);
-- Groups are OR'd together: (rule1 AND rule2) OR (rule3 AND rule4)

-- Campaigns
CREATE TABLE Campaigns (
    Id              INT IDENTITY PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    Subject         NVARCHAR(500) NOT NULL,
    HtmlBody        NVARCHAR(MAX) NOT NULL,
    SegmentId       INT REFERENCES Segments(Id),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- 'Draft', 'Sent'
    SentAt          DATETIME2,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Engagement Tracking
CREATE TABLE ActivityEvents (
    Id              BIGINT IDENTITY PRIMARY KEY,
    ContactId       INT NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    CampaignId      INT REFERENCES Campaigns(Id) ON DELETE SET NULL,
    EventType       NVARCHAR(20) NOT NULL,  -- 'send', 'open', 'click'
    OccurredAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Metadata        NVARCHAR(500)            -- e.g., click URL
);

CREATE INDEX IX_ActivityEvents_Contact ON ActivityEvents(ContactId, OccurredAt DESC);
CREATE INDEX IX_ActivityEvents_Campaign ON ActivityEvents(CampaignId, EventType);

-- Customer Journeys
CREATE TABLE Journeys (
    Id              INT IDENTITY PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    TriggerType     NVARCHAR(50) NOT NULL,  -- 'segment_enter', 'tag_added'
    TriggerConfig   NVARCHAR(500) NOT NULL, -- JSON: {"segmentId": 1} or {"tagId": 2}
    IsActive        BIT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE JourneySteps (
    Id              INT IDENTITY PRIMARY KEY,
    JourneyId       INT NOT NULL REFERENCES Journeys(Id) ON DELETE CASCADE,
    StepOrder       INT NOT NULL,
    StepType        NVARCHAR(20) NOT NULL,  -- 'send_email', 'wait', 'condition'
    Config          NVARCHAR(MAX) NOT NULL,  -- JSON per step type
    -- send_email: {"subject": "...", "htmlBody": "..."}
    -- wait: {"duration": 24, "unit": "hours"}
    -- condition: {"field": "lastEventType", "operator": "equals", "value": "open", "trueNextStep": 3, "falseNextStep": 4}
    TrueNextStepId  INT REFERENCES JourneySteps(Id),
    FalseNextStepId INT REFERENCES JourneySteps(Id)
);

CREATE TABLE JourneyEnrollments (
    Id              INT IDENTITY PRIMARY KEY,
    JourneyId       INT NOT NULL REFERENCES Journeys(Id) ON DELETE CASCADE,
    ContactId       INT NOT NULL REFERENCES Contacts(Id) ON DELETE CASCADE,
    CurrentStepId   INT REFERENCES JourneySteps(Id),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'Active', -- 'Active', 'Completed', 'Exited'
    EnrolledAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt     DATETIME2,
    UNIQUE (JourneyId, ContactId)
);

CREATE TABLE JourneyStepExecutions (
    Id              INT IDENTITY PRIMARY KEY,
    EnrollmentId    INT NOT NULL REFERENCES JourneyEnrollments(Id) ON DELETE CASCADE,
    StepId          INT NOT NULL REFERENCES JourneySteps(Id),
    ExecutedAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Result          NVARCHAR(20) -- 'completed', 'true_branch', 'false_branch'
);
```

---

## 6. API Design

### Base URL: `/api`

All endpoints return JSON. Errors follow RFC 7807 Problem Details format.

### Contacts

| Method | Path | Description |
|--------|------|-------------|
| GET | `/contacts` | List contacts (paginated, filterable) |
| GET | `/contacts/{id}` | Get contact with activity timeline |
| POST | `/contacts` | Create contact |
| PUT | `/contacts/{id}` | Update contact |
| DELETE | `/contacts/{id}` | Delete contact |
| POST | `/contacts/import` | Import contacts from CSV |

**GET `/contacts`** query params: `page`, `pageSize`, `search`, `industry`, `tag`, `sortBy`, `sortDir`

```json
// Response
{
  "items": [
    {
      "id": 1,
      "firstName": "Ola",
      "lastName": "Nordmann",
      "email": "ola@example.no",
      "company": "TechCo AS",
      "industry": "Technology",
      "tags": ["prospect", "enterprise"],
      "engagementScore": 72,
      "lastActivityAt": "2026-03-28T14:30:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20
}
```

### Segments

| Method | Path | Description |
|--------|------|-------------|
| GET | `/segments` | List all segments |
| GET | `/segments/{id}` | Get segment with rules |
| POST | `/segments` | Create segment with rules |
| PUT | `/segments/{id}` | Update segment and rules |
| DELETE | `/segments/{id}` | Delete segment |
| GET | `/segments/{id}/preview` | Evaluate segment, return matching contact count and sample |

```json
// POST /segments request
{
  "name": "Engaged Tech Companies",
  "description": "Active technology sector contacts",
  "rules": [
    { "groupIndex": 0, "field": "industry", "operator": "equals", "value": "Technology" },
    { "groupIndex": 0, "field": "engagementScore", "operator": "greaterThan", "value": "50" },
    { "groupIndex": 1, "field": "tag", "operator": "equals", "value": "enterprise" }
  ]
}
```

```json
// GET /segments/{id}/preview response
{
  "segmentId": 1,
  "matchingCount": 42,
  "sampleContacts": [
    { "id": 1, "firstName": "Ola", "lastName": "Nordmann", "email": "ola@example.no" }
  ]
}
```

### Campaigns

| Method | Path | Description |
|--------|------|-------------|
| GET | `/campaigns` | List campaigns with stats |
| GET | `/campaigns/{id}` | Get campaign detail with engagement metrics |
| POST | `/campaigns` | Create campaign |
| PUT | `/campaigns/{id}` | Update draft campaign |
| POST | `/campaigns/{id}/send` | Send campaign to target segment |
| GET | `/campaigns/{id}/stats` | Get detailed engagement stats (time series) |

```json
// POST /campaigns request
{
  "name": "Spring Product Update",
  "subject": "What's new in Q2, {{firstName}}",
  "htmlBody": "<h1>Hi {{firstName}}</h1><p>Here's what's new at {{company}}...</p>",
  "segmentId": 1
}
```

```json
// GET /campaigns/{id}/stats response
{
  "campaignId": 1,
  "totalSent": 42,
  "totalOpens": 28,
  "totalClicks": 12,
  "openRate": 0.667,
  "clickThroughRate": 0.286,
  "timeline": [
    { "date": "2026-03-28", "sends": 42, "opens": 15, "clicks": 5 },
    { "date": "2026-03-29", "sends": 0, "opens": 10, "clicks": 5 },
    { "date": "2026-03-30", "sends": 0, "opens": 3, "clicks": 2 }
  ]
}
```

### Journeys

| Method | Path | Description |
|--------|------|-------------|
| GET | `/journeys` | List journeys |
| GET | `/journeys/{id}` | Get journey with steps and enrollment stats |
| POST | `/journeys` | Create journey with steps |
| PUT | `/journeys/{id}` | Update journey |
| POST | `/journeys/{id}/activate` | Activate journey |
| POST | `/journeys/{id}/deactivate` | Deactivate journey |
| GET | `/journeys/{id}/stats` | Get step-by-step funnel stats |

```json
// POST /journeys request
{
  "name": "Welcome Series",
  "triggerType": "segment_enter",
  "triggerConfig": { "segmentId": 1 },
  "steps": [
    { "stepOrder": 1, "stepType": "send_email", "config": { "subject": "Welcome!", "htmlBody": "<p>Thanks for joining</p>" } },
    { "stepOrder": 2, "stepType": "wait", "config": { "duration": 48, "unit": "hours" } },
    { "stepOrder": 3, "stepType": "condition", "config": { "field": "lastEventType", "operator": "equals", "value": "open" } },
    { "stepOrder": 4, "stepType": "send_email", "config": { "subject": "Glad you're here!", "htmlBody": "<p>Since you opened...</p>" } },
    { "stepOrder": 5, "stepType": "send_email", "config": { "subject": "Don't miss out", "htmlBody": "<p>We noticed you haven't...</p>" } }
  ]
}
```

### Analytics

| Method | Path | Description |
|--------|------|-------------|
| GET | `/analytics/overview` | Dashboard summary metrics |
| GET | `/analytics/engagement` | Engagement trends over time |

```json
// GET /analytics/overview?days=30 response
{
  "totalContacts": 500,
  "activeCampaigns": 3,
  "activeJourneys": 2,
  "overallEngagementRate": 0.45,
  "recentCampaigns": [
    { "id": 1, "name": "Spring Update", "sentAt": "2026-03-28T10:00:00Z", "openRate": 0.667 }
  ]
}
```

### AI Suggestions

| Method | Path | Description |
|--------|------|-------------|
| POST | `/ai/subject-suggestions` | Get AI-powered subject line suggestions |
| GET | `/ai/send-time/{segmentId}` | Get optimal send time for a segment |

```json
// POST /ai/subject-suggestions request
{
  "draftSubject": "Check out our new features",
  "campaignContext": "Product update for tech companies"
}

// Response
{
  "suggestions": [
    { "subject": "3 new features your team will love, {{firstName}}", "reason": "Adds personalization and specificity" },
    { "subject": "Your Q2 toolkit just got an upgrade", "reason": "Creates ownership and curiosity" },
    { "subject": "New features shipping today — here's what changed", "reason": "Urgency with concrete value" }
  ]
}
```

### Authentication

No authentication for this demo — the app runs locally. The API is designed so auth middleware (e.g., JWT) could be added at the controller level without changing service logic.

---

## 7. Testing Strategy

### Backend (C# / xUnit)

**Unit Tests** — test business logic in isolation:
- Segment rule evaluation: verify AND/OR logic produces correct SQL predicates
- Campaign personalization: token replacement in email body
- Journey step engine: state transitions (wait expiry, condition branching)
- AI subject suggestion rules: verify heuristic transformations
- Input validation: reject invalid segment rules, malformed campaign data

**Integration Tests** — test API + database together:
- Use `WebApplicationFactory<Program>` with a real SQL Server test container
- Campaign send flow: create segment + campaign -> send -> verify activity events created
- Segment preview: insert test contacts -> create segment rules -> verify correct contacts matched
- Journey enrollment: activate journey -> add contact to trigger segment -> verify enrollment created
- CSV import: upload file -> verify contacts created with correct data

**Target**: Cover all service-layer methods and all API endpoints. Focus on correctness of segment evaluation SQL and journey state machine transitions — these are the most complex logic.

### Frontend (TypeScript / Vitest)

**Unit Tests**:
- Segment rule builder: adding/removing rules updates state correctly
- Campaign form validation: required fields, token syntax validation
- Journey step rendering: correct visualization of step types and connections
- Analytics chart data transformation: raw API data -> chart-ready format

**Target**: Cover all non-trivial utility functions and state management logic. UI component tests focus on interactive elements (segment builder, journey designer) rather than static displays.

### CI Pipeline

GitHub Actions workflow runs on every push and PR:
1. Backend: `dotnet test` with SQL Server service container
2. Frontend: `npm test` (Vitest)
3. `docker compose build` — verify the full stack builds
4. Lint: `dotnet format --verify-no-changes` + ESLint

---

## 8. Infrastructure & Deployment

### Local Development (Docker Compose)

The entire application runs with a single command:

```bash
docker compose up
```

**Services:**

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| `api` | Custom (.NET 8 SDK) | 5000 | ASP.NET Core Web API |
| `web` | Custom (Node 20) | 3000 | React dev server (proxies API) |
| `db` | mcr.microsoft.com/mssql/server:2022-latest | 1433 | SQL Server |

**Startup sequence:**
1. `db` starts and becomes healthy (healthcheck query)
2. `api` runs EF Core migrations automatically on startup, then seeds demo data (sample contacts, segments, a draft campaign)
3. `web` starts and proxies `/api` requests to the API container

**Demo seed data**: On first startup, the API seeds ~100 sample contacts across industries, a few tags, one example segment, and one draft campaign — so the app looks populated immediately.

### CI/CD (GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: CI
on: [push, pull_request]
jobs:
  backend:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: TestPassword123!
        ports: ["1433:1433"]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build
      - run: dotnet format --verify-no-changes

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: cd web && npm ci
      - run: cd web && npm run lint
      - run: cd web && npm test

  docker:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: docker compose build
```

---

## 9. Project Structure

```
marketflow/
├── docker-compose.yml
├── .github/
│   └── workflows/
│       └── ci.yml
├── README.md
├── docs/
│   └── PRD.md
│
├── src/
│   └── MarketFlow.Api/
│       ├── MarketFlow.Api.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Controllers/
│       │   ├── ContactsController.cs
│       │   ├── SegmentsController.cs
│       │   ├── CampaignsController.cs
│       │   ├── JourneysController.cs
│       │   ├── AnalyticsController.cs
│       │   └── AiController.cs
│       ├── Services/
│       │   ├── ContactService.cs
│       │   ├── SegmentService.cs         # Rule evaluation, SQL generation
│       │   ├── CampaignService.cs        # Send logic, personalization
│       │   ├── JourneyService.cs         # State machine, step execution
│       │   ├── AnalyticsService.cs       # Aggregation queries
│       │   └── AiSuggestionService.cs    # Rule-based + optional LLM
│       ├── Models/
│       │   ├── Contact.cs
│       │   ├── Segment.cs
│       │   ├── SegmentRule.cs
│       │   ├── Campaign.cs
│       │   ├── ActivityEvent.cs
│       │   ├── Journey.cs
│       │   ├── JourneyStep.cs
│       │   ├── JourneyEnrollment.cs
│       │   └── Tag.cs
│       ├── Data/
│       │   ├── AppDbContext.cs
│       │   ├── Migrations/
│       │   └── SeedData.cs
│       └── Dtos/
│           ├── ContactDto.cs
│           ├── SegmentDto.cs
│           ├── CampaignDto.cs
│           ├── JourneyDto.cs
│           └── AnalyticsDto.cs
│
├── tests/
│   └── MarketFlow.Tests/
│       ├── MarketFlow.Tests.csproj
│       ├── Unit/
│       │   ├── SegmentServiceTests.cs
│       │   ├── CampaignServiceTests.cs
│       │   ├── JourneyServiceTests.cs
│       │   └── AiSuggestionServiceTests.cs
│       └── Integration/
│           ├── ContactsApiTests.cs
│           ├── CampaignFlowTests.cs
│           └── SegmentEvaluationTests.cs
│
└── web/
    ├── package.json
    ├── tsconfig.json
    ├── vite.config.ts
    ├── index.html
    ├── src/
    │   ├── main.tsx
    │   ├── App.tsx
    │   ├── api/
    │   │   └── client.ts              # Typed API client
    │   ├── components/
    │   │   ├── Layout.tsx
    │   │   ├── contacts/
    │   │   │   ├── ContactList.tsx
    │   │   │   └── ContactForm.tsx
    │   │   ├── segments/
    │   │   │   ├── SegmentList.tsx
    │   │   │   └── SegmentBuilder.tsx  # Interactive rule builder
    │   │   ├── campaigns/
    │   │   │   ├── CampaignList.tsx
    │   │   │   ├── CampaignEditor.tsx  # Rich text + AI suggestions
    │   │   │   └── CampaignPreview.tsx
    │   │   ├── journeys/
    │   │   │   ├── JourneyList.tsx
    │   │   │   └── JourneyDesigner.tsx # Visual step editor
    │   │   └── analytics/
    │   │       └── Dashboard.tsx       # Charts and metrics
    │   ├── hooks/
    │   │   └── useApi.ts
    │   └── types/
    │       └── index.ts
    └── tests/
        ├── SegmentBuilder.test.tsx
        └── CampaignEditor.test.tsx
```
