# API Reference — DevBrain Trainer

Complete documentation of all available endpoints.

---

## Base URL

**Development**:
```
http://localhost:5000/api/v1
```

**Production (Azure)**:
```
https://devbrain-trainer.azurewebsites.net/api/v1
```

---

## Authentication

**Current (dev/testing)**: Use `X-User-Id` header

```bash
curl -H "X-User-Id: test_user_123" http://localhost:5000/api/v1/challenges
```

**Future (production)**: Supabase JWT in `Authorization` header

```bash
curl -H "Authorization: Bearer {supabase_jwt_token}" http://localhost:5000/api/v1/challenges
```

---

## Endpoints

### GET /challenges — List Challenges

List all challenges with optional filtering and pagination.

**Request**
```http
GET /api/v1/challenges?category=Sql&difficulty=Easy&page=1&pageSize=10
```

**Query Parameters**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| `category` | string | No | Filter by category: `Sql`, `CodeLogic`, `Architecture`, `DevOps`, `WorkingMemory` |
| `difficulty` | string | No | Filter by difficulty: `Easy`, `Medium`, `Hard` |
| `page` | integer | No | Page number (1-based, default: 1) |
| `pageSize` | integer | No | Items per page (default: 10, max: 100) |

**Response** — `200 OK`
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "title": "SQL: Select Top N Records",
      "description": "Write a SQL query that returns the top 5 users by attempts",
      "category": "Sql",
      "difficulty": "Easy",
      "timeLimitSecs": 60
    },
    {
      "id": "6ba7b810-9dad-11d1-80b4-00c04fd430c8",
      "title": "Memory: Loop Counting",
      "description": "Count how many times this loop executes",
      "category": "WorkingMemory",
      "difficulty": "Easy",
      "timeLimitSecs": 60
    }
  ],
  "totalCount": 27,
  "page": 1,
  "pageSize": 10
}
```

**Error Responses**
| Status | Reason |
|--------|--------|
| `400` | Invalid query parameters (e.g., invalid category) |

**Example Requests**
```bash
# All challenges, first 10
curl http://localhost:5000/api/v1/challenges

# SQL challenges only
curl "http://localhost:5000/api/v1/challenges?category=Sql"

# Easy SQL challenges
curl "http://localhost:5000/api/v1/challenges?category=Sql&difficulty=Easy"

# Page 3, 20 per page
curl "http://localhost:5000/api/v1/challenges?page=3&pageSize=20"
```

---

### POST /challenges/{id}/attempt — Submit Attempt

Submit your answer to a challenge and receive immediate feedback.

**Request**
```http
POST /api/v1/challenges/{id}/attempt
X-User-Id: user_uuid_or_id
Content-Type: application/json

{
  "userAnswer": "7",
  "elapsedSeconds": 45
}
```

**Path Parameters**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| `id` | UUID | Yes | Challenge ID (from GET /challenges) |

**Headers**
| Name | Required | Description |
|------|----------|-------------|
| `X-User-Id` | Yes | User identifier (currently test header, future JWT) |
| `Content-Type` | Yes | Must be `application/json` |

**Request Body**
| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `userAnswer` | string | Yes | Non-empty, trimmed | Your answer to the challenge |
| `elapsedSeconds` | integer | Yes | 0-3600 | Seconds spent on challenge |

**Response** — `201 Created`
```json
{
  "attemptId": "550e8400-e29b-41d4-a716-446655440001",
  "challengeId": "550e8400-e29b-41d4-a716-446655440000",
  "userId": "user_123",
  "userAnswer": "7",
  "isCorrect": true,
  "correctAnswer": "7",
  "elapsedSeconds": 45,
  "challengeTitle": "Memory: Loop Counting",
  "occurredAt": "2026-04-09T12:15:30Z"
}
```

**Error Responses**

| Status | Scenario | Example |
|--------|----------|---------|
| `400` | Empty answer | `{ "error": "User answer cannot be empty" }` |
| `400` | Answer only whitespace | `{ "error": "User answer cannot be empty" }` |
| `400` | Invalid elapsed time | `{ "error": "Elapsed time must be between 0 and 3600 seconds" }` |
| `404` | Challenge not found | `{ "error": "Challenge not found" }` |

**Notes**
- Answer comparison is **case-insensitive** and **trims whitespace**
  - `"7"`, `" 7"`, `"7 "`, `"  7  "` are all equivalent
  - For SQL: `"SELECT * FROM users"` matches `"select * from users"`
- Attempts are always recorded, even if `elapsedSeconds > timeLimitSecs`
  - Frontend shows warning if time limit exceeded
  - Backend logs but allows submission
- `isCorrect` is computed using domain logic — never trust client for correctness

**Example Requests**

```bash
# Correct answer
curl -X POST http://localhost:5000/api/v1/challenges/550e8400-e29b-41d4-a716-446655440000/attempt \
  -H "X-User-Id: user_123" \
  -H "Content-Type: application/json" \
  -d '{"userAnswer":"7","elapsedSeconds":45}'

# Wrong answer
curl -X POST http://localhost:5000/api/v1/challenges/550e8400-e29b-41d4-a716-446655440000/attempt \
  -H "X-User-Id: user_123" \
  -H "Content-Type: application/json" \
  -d '{"userAnswer":"wrong","elapsedSeconds":120}'

# With whitespace (gets trimmed server-side)
curl -X POST http://localhost:5000/api/v1/challenges/550e8400-e29b-41d4-a716-446655440000/attempt \
  -H "X-User-Id: user_123" \
  -H "Content-Type: application/json" \
  -d '{"userAnswer":"  7  ","elapsedSeconds":45}'
```

---

## Common Response Patterns

### Success (2xx)

**200 OK** — GET successful
```json
{
  "data": "..."
}
```

**201 Created** — POST successful, resource created
```json
{
  "id": "uuid",
  "data": "..."
}
```

### Client Errors (4xx)

**400 Bad Request** — Validation failed
```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "User answer cannot be empty"
}
```

**404 Not Found** — Resource doesn't exist
```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "Challenge not found"
}
```

---

## Status Codes Reference

| Code | Meaning | When |
|------|---------|------|
| `200` | OK | GET successful |
| `201` | Created | POST successful, resource created |
| `400` | Bad Request | Validation error (invalid input) |
| `401` | Unauthorized | Missing/invalid auth (future) |
| `404` | Not Found | Resource doesn't exist |
| `409` | Conflict | Duplicate email, etc. (future) |
| `500` | Server Error | Unexpected error |

---

## Rate Limiting

**Current**: None (not yet implemented)

**Future**: 
- 100 requests per minute per user
- 1000 requests per minute globally

---

## Pagination

All list endpoints support pagination:

```
GET /challenges?page=2&pageSize=20
```

Response includes:
```json
{
  "items": [...],
  "totalCount": 100,
  "page": 2,
  "pageSize": 20
}
```

**Rules**
- `page` starts at 1 (not 0)
- `pageSize` default is 10, max is 100
- Returns empty `items` array if page exceeds total

---

## Filtering

### By Category

Challenge categories:
- `Sql` — SQL queries and database operations
- `CodeLogic` — Code execution tracing, logic puzzles
- `Architecture` — System design, patterns, principles
- `DevOps` — Docker, Kubernetes, CI/CD
- `WorkingMemory` — Memory tests, variable tracing

```bash
curl "http://localhost:5000/api/v1/challenges?category=Sql"
```

### By Difficulty

- `Easy` — 5-10 minute problems, basic concepts
- `Medium` — 10-20 minute problems, intermediate concepts
- `Hard` — 20+ minute problems, advanced topics

```bash
curl "http://localhost:5000/api/v1/challenges?difficulty=Medium"
```

### Combine Filters

```bash
curl "http://localhost:5000/api/v1/challenges?category=Sql&difficulty=Easy"
```

---

## Testing with Postman

Import the collection:
```
postman/devbrain-trainer.postman_collection.json
```

**Setup Variables**
1. Open collection → Variables tab
2. Set `baseUrl` = `http://localhost:5000`
3. Set `bearerToken` = empty (for now, tests use `X-User-Id`)

**Run Requests**
All requests are pre-configured with headers and example bodies.

---

## Future Endpoints (Roadmap)

| Endpoint | Status | Notes |
|----------|--------|-------|
| `POST /auth/register` | 🔄 | User registration (Supabase) |
| `POST /auth/login` | 🔄 | User login (Supabase JWT) |
| `GET /user/profile` | 🔄 | Current user stats |
| `GET /attempts` | 🔄 | User's attempt history |
| `GET /leaderboard` | 🔄 | Global rankings by category |
| `PATCH /user/profile` | 🔄 | Update displayName, settings |

---

## Support

For issues or questions:
1. Check [`DEVELOPMENT.md`](./DEVELOPMENT.md) for implementation patterns
2. Review specs in `specs/api/` for endpoint contract details
3. Check test examples in `tests/DevBrain.Api.Tests/`
