# API Endpoints Reference — DevBrain Trainer

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

### Header-Based (Development)

```bash
curl -H "X-User-Id: 550e8400-e29b-41d4-a716-446655440000" \
  http://localhost:5000/api/v1/users/me/stats
```

### JWT Token (Production)

```bash
curl -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  https://devbrain-trainer.azurewebsites.net/api/v1/users/me/stats
```

---

## Endpoints

### Authentication

#### POST /auth/register

Register a new user.

**Request**:
```bash
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "alan@example.com",
    "password": "SecureP@ss123",
    "displayName": "Alan"
  }'
```

**Request Body**:
```json
{
  "email": "alan@example.com",
  "password": "SecureP@ss123",
  "displayName": "Alan Rivas"
}
```

**Response (201 Created)**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "alan@example.com",
  "displayName": "Alan Rivas",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-04-11T12:00:00Z"
}
```

**Error Responses**:
- `400 Bad Request` — Missing/invalid fields
- `409 Conflict` — Email already exists

---

#### POST /auth/login

Authenticate user and get JWT token.

**Request**:
```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "alan@example.com",
    "password": "SecureP@ss123"
  }'
```

**Request Body**:
```json
{
  "email": "alan@example.com",
  "password": "SecureP@ss123"
}
```

**Response (200 OK)**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "alan@example.com",
  "displayName": "Alan Rivas",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-04-11T12:00:00Z"
}
```

**Error Responses**:
- `404 Not Found` — User not found
- `401 Unauthorized` — Invalid password

---

### Challenges

#### GET /challenges

List all challenges with optional filtering and pagination.

**Request**:
```bash
# Get all challenges
curl http://localhost:5000/api/v1/challenges

# With pagination
curl "http://localhost:5000/api/v1/challenges?page=1&pageSize=10"

# With filtering
curl "http://localhost:5000/api/v1/challenges?category=Sql&difficulty=Easy"
```

**Query Parameters**:
| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Current page number |
| `pageSize` | int | 10 | Items per page (max 100) |
| `category` | string | null | Filter by category (Sql, Logic, Architecture, Docker, Memory) |
| `difficulty` | string | null | Filter by difficulty (Easy, Medium, Hard) |

**Response (200 OK)**:
```json
{
  "items": [
    {
      "id": "abf1fb18-27b8-4051-ba46-467d723dee50",
      "title": "SQL Query Optimization",
      "description": "Optimize this slow query...",
      "category": "Sql",
      "difficulty": "Medium",
      "timeLimitSecs": 300
    }
  ],
  "totalCount": 27,
  "page": 1,
  "pageSize": 10
}
```

**Error Responses**:
- `400 Bad Request` — Invalid query parameters

---

#### GET /challenges/{id}

Get a single challenge by ID.

**Request**:
```bash
curl http://localhost:5000/api/v1/challenges/abf1fb18-27b8-4051-ba46-467d723dee50
```

**Response (200 OK)**:
```json
{
  "id": "abf1fb18-27b8-4051-ba46-467d723dee50",
  "title": "SQL Query Optimization",
  "description": "Optimize this slow query to run in <150ms...",
  "category": "Sql",
  "difficulty": "Medium",
  "timeLimitSecs": 300,
  "createdAt": "2026-04-10T00:00:00Z"
}
```

**Error Responses**:
- `404 Not Found` — Challenge not found

---

### Attempts

#### POST /challenges/{id}/attempt

Submit an attempt to a challenge.

**Request**:
```bash
curl -X POST http://localhost:5000/api/v1/challenges/abf1fb18-27b8-4051-ba46-467d723dee50/attempt \
  -H "X-User-Id: 550e8400-e29b-41d4-a716-446655440000" \
  -H "Content-Type: application/json" \
  -d '{
    "userAnswer": "SELECT * FROM users LIMIT 10",
    "elapsedSeconds": 45
  }'
```

**Request Body**:
```json
{
  "userAnswer": "SELECT * FROM users LIMIT 10",
  "elapsedSeconds": 45
}
```

**Response (201 Created)**:
```json
{
  "attemptId": "e7f4c5b0-8d9e-4f2a-b1c3-5d6e7f8a9b0c",
  "challengeId": "abf1fb18-27b8-4051-ba46-467d723dee50",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "userAnswer": "SELECT * FROM users LIMIT 10",
  "isCorrect": false,
  "correctAnswer": "SELECT id, name FROM users WHERE active = true",
  "elapsedSeconds": 45,
  "eloChange": -10,
  "badgeAwarded": null,
  "occurredAt": "2026-04-10T12:34:56Z"
}
```

**Error Responses**:
- `401 Unauthorized` — Missing X-User-Id header
- `404 Not Found` — Challenge or user not found
- `400 Bad Request` — Invalid request body

---

### User Stats & Badges

#### GET /users/me/stats

Get authenticated user's statistics.

**Request**:
```bash
curl -H "X-User-Id: 550e8400-e29b-41d4-a716-446655440000" \
  http://localhost:5000/api/v1/users/me/stats
```

**Response (200 OK)**:
```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "displayName": "Alan Rivas",
  "totalAttempts": 25,
  "correctAttempts": 18,
  "accuracyRate": 72.0,
  "currentStreak": 5,
  "eloRating": 1150,
  "lastAttemptAt": "2026-04-10T12:34:56Z"
}
```

**Error Responses**:
- `401 Unauthorized` — Missing X-User-Id header
- `404 Not Found` — User not found

---

#### GET /users/me/badges

Get all badges earned by the user.

**Request**:
```bash
curl -H "X-User-Id: 550e8400-e29b-41d4-a716-446655440000" \
  http://localhost:5000/api/v1/users/me/badges
```

**Response (200 OK)**:
```json
[
  {
    "id": "badge-1",
    "type": "FirstAttempt",
    "earnedAt": "2026-04-09T10:15:00Z"
  },
  {
    "id": "badge-2",
    "type": "Streak5",
    "earnedAt": "2026-04-10T09:30:00Z"
  }
]
```

**Error Responses**:
- `401 Unauthorized` — Missing X-User-Id header

---

## Error Response Format

All errors follow this format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Email is required",
  "instance": "/api/v1/auth/register"
}
```

**Status Codes**:
- `200 OK` — Success
- `201 Created` — Resource created
- `400 Bad Request` — Client error (invalid input)
- `401 Unauthorized` — Authentication required
- `404 Not Found` — Resource not found
- `409 Conflict` — Duplicate resource (e.g., email exists)
- `500 Internal Server Error` — Server error

---

## Rate Limiting (Future)

Currently **not implemented**, but planned:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1660828800
```

---

## API Documentation (Interactive)

**OpenAPI/Swagger UI** available at:
- Development: `http://localhost:5000/swagger`
- Production: `https://devbrain-trainer.azurewebsites.net/swagger`

**Scalar Documentation** available at:
- Development: `http://localhost:5000/scalar`
- Production: `https://devbrain-trainer.azurewebsites.net/scalar`

---

## Examples by Language

### JavaScript/Node.js

```javascript
// Register
const response = await fetch('http://localhost:5000/api/v1/auth/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'alan@example.com',
    password: 'SecureP@ss123',
    displayName: 'Alan'
  })
});

const { token } = await response.json();

// Submit attempt
const attemptResponse = await fetch(
  `http://localhost:5000/api/v1/challenges/${challengeId}/attempt`,
  {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-User-Id': userId
    },
    body: JSON.stringify({
      userAnswer: 'SELECT * FROM users',
      elapsedSeconds: 45
    })
  }
);
```

### Python

```python
import requests

# Register
response = requests.post(
    'http://localhost:5000/api/v1/auth/register',
    json={
        'email': 'alan@example.com',
        'password': 'SecureP@ss123',
        'displayName': 'Alan'
    }
)
token = response.json()['token']

# Submit attempt
response = requests.post(
    f'http://localhost:5000/api/v1/challenges/{challenge_id}/attempt',
    headers={
        'X-User-Id': user_id,
        'Content-Type': 'application/json'
    },
    json={
        'userAnswer': 'SELECT * FROM users',
        'elapsedSeconds': 45
    }
)
```

### cURL

```bash
# Register
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alan@example.com","password":"SecureP@ss123","displayName":"Alan"}'

# Get token
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"alan@example.com","password":"SecureP@ss123"}' \
  | jq -r '.token')

# Submit attempt with token
curl -X POST http://localhost:5000/api/v1/challenges/{id}/attempt \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"userAnswer":"SELECT *","elapsedSeconds":45}'
```

---

## Postman Collection

Import from `postman/devbrain-trainer.postman_collection.json`:

1. Open Postman
2. File → Import
3. Select the collection file
4. Set environment: `postman/devbrain-trainer.localhost.postman_environment.json`
5. Run requests in sequence

**Available Requests**:
- Register User
- Login
- List Challenges
- Get Challenge by ID
- Submit Attempt
- Get User Stats
- Get User Badges
