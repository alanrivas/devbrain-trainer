# 🧑‍🔬 Test Users & Credentials

## Usuarios preconfigurados

Durante la inicialización de la BD (via EF Core migrations), se crean automáticamente los siguientes usuarios de prueba:

### Administrator

| Campo | Valor |
|-------|-------|
| **Email** | `admin@devbrain.local` |
| **Password** | `Admin123!` |
| **Display Name** | Admin User |
| **Initial ELO** | 1200 |
| **Status** | ✅ Ready to use |

### Test User 1

| Campo | Valor |
|-------|-------|
| **Email** | `testuser@devbrain.local` |
| **Password** | `TestUser123!` |
| **Display Name** | Test User |
| **Initial ELO** | 1200 |
| **Status** | ✅ Ready to use |

### Test User 2 (Hardcore Mode)

| Campo | Valor |
|-------|-------|
| **Email** | `pro@devbrain.local` |
| **Password** | `Pro123!` |
| **Display Name** | Hardcore Tester |
| **Initial ELO** | 1300 (pre-trained) |
| **Status** | ✅ Ready to use |

---

## 🎯 Quick Login

### En la app (http://localhost:3000):

1. Click "Sign In"
2. Selecciona uno de los usuarios de arriba:
   ```
   Email: admin@devbrain.local
   Password: Admin123!
   ```
3. Click "Sign In"
4. ✅ Serás redirigido a `/challenges`

### Via API (cURL):

```bash
curl -X POST http://localhost:5118/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@devbrain.local",
    "password": "Admin123!"
  }' | jq .

# Response:
{
  "token": "eyJhbGc...",
  "user": {
    "id": "...",
    "email": "admin@devbrain.local",
    "displayName": "Admin User"
  }
}
```

### En Postman:

1. Importa `postman/devbrain-trainer.localhost.postman_collection.json`
2. Usa la request "Auth Login" con credenciales de arriba
3. El token se autocarga en variables globales para las siguientes requests

---

## 🎲 Challenges (Seed Data)

Por defecto, durante database initialization (migraciones EF), se crean **10 challenges** de ejemplo:

### Categorías

- **SQL Injection** — 3 challenges (Easy, Medium, Hard)
- **C# Async** — 3 challenges (Easy, Medium, Hard)
- **Docker** — 2 challenges (Medium, Hard)
- **Architecture** — 2 challenges (Medium, Hard)

### Estructura de un Challenge

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Identify the SQL Injection vulnerability",
  "description": "Analyze this code and identify the SQL injection vulnerability...",
  "category": "SQL Injection",
  "difficulty": "Easy",
  "content": "SELECT * FROM users WHERE name = '" + userInput + "'",
  "correctAnswer": "YES",
  "points": 10
}
```

### Accediendo a challenges

```bash
# Listar todos (requiere JWT)
curl -X GET "http://localhost:5118/api/v1/challenges?page=1&pageSize=10" \
  -H "Authorization: Bearer <JWT_TOKEN>"

# Obtener uno específico
curl -X GET "http://localhost:5118/api/v1/challenges/{ID}" \
  -H "Authorization: Bearer <JWT_TOKEN}"
```

---

## 🚀 Test Scenario: Happy Path

### 1. Register a new user

```bash
curl -X POST http://localhost:5118/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "password": "NewUser123!",
    "displayName": "New Tester"
  }'

# ✅ Status: 201 Created
# Response:
{
  "token": "eyJhbGc...",
  "user": {
    "id": "71e3b4f0-...",
    "email": "newuser@example.com",
    "displayName": "New Tester"
  }
}
```

### 2. Get user stats

```bash
JWT="eyJhbGc..."

curl -X GET http://localhost:5118/api/v1/users/me/stats \
  -H "Authorization: Bearer $JWT"

# ✅ Status: 200 OK
# Response:
{
  "totalAttempts": 0,
  "correctAttempts": 0,
  "accuracy": 0.0,
  "currentElo": 1200,
  "currentStreak": 0,
  "bestStreak": 0
}
```

### 3. Get challenges

```bash
curl -X GET "http://localhost:5118/api/v1/challenges?page=1&pageSize=5" \
  -H "Authorization: Bearer $JWT"

# ✅ Status: 200 OK
# Response:
{
  "items": [
    {
      "id": "550e8400-...",
      "title": "Identify the SQL Injection vulnerability",
      "description": "...",
      "category": "SQL Injection",
      "difficulty": "Easy"
    },
    ...
  ],
  "page": 1,
  "pageSize": 5,
  "total": 10
}
```

### 4. Submit an attempt

```bash
CHALLENGE_ID="550e8400-..." # Del paso anterior

curl -X POST http://localhost:5118/api/v1/challenges/$CHALLENGE_ID/attempt \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "userAnswer": "YES",
    "elapsedSeconds": 20
  }'

# ✅ Status: 200 OK
# Response:
{
  "attemptId": "...",
  "isCorrect": true,
  "newEloRating": 1210,
  "eloDelta": +10,
  "badgeAwarded": null
}
```

### 5. Check updated stats

```bash
curl -X GET http://localhost:5118/api/v1/users/me/stats \
  -H "Authorization: Bearer $JWT"

# ✅ Now shows:
{
  "totalAttempts": 1,
  "correctAttempts": 1,
  "accuracy": 1.0,
  "currentElo": 1210,
  "currentStreak": 1,
  "bestStreak": 1
}
```

### 6. Get user badges (if any)

```bash
curl -X GET http://localhost:5118/api/v1/users/me/badges \
  -H "Authorization: Bearer $JWT"

# ✅ Status: 200 OK
# Response:
{
  "items": [
    {
      "id": "...",
      "name": "First Answer",
      "description": "Submit your first correct answer",
      "awardedAt": "2026-04-13T..."
    }
  ],
  "page": 1,
  "pageSize": 10,
  "total": 1
}
```

---

## 📊 Database Schema Reference

### Main Tables

```sql
-- Users
CREATE TABLE "Users" (
  "Id" uuid PRIMARY KEY,
  "Email" varchar(255) UNIQUE NOT NULL,
  "DisplayName" varchar(100) NOT NULL,
  "PasswordHash" varchar(255) NOT NULL,
  "EloRating" int DEFAULT 1200,
  "CreatedAt" timestamp DEFAULT NOW(),
  "UpdatedAt" timestamp DEFAULT NOW()
);

-- Challenges
CREATE TABLE "Challenges" (
  "Id" uuid PRIMARY KEY,
  "Title" varchar(255) NOT NULL,
  "Description" text NOT NULL,
  "Category" varchar(50) NOT NULL,
  "Difficulty" varchar(20) NOT NULL,
  "Content" text NOT NULL,
  "CorrectAnswer" varchar(50) NOT NULL,
  "Points" int DEFAULT 10,
  "CreatedAt" timestamp DEFAULT NOW()
);

-- Attempts
CREATE TABLE "Attempts" (
  "Id" uuid PRIMARY KEY,
  "UserId" uuid NOT NULL REFERENCES "Users"("Id"),
  "ChallengeId" uuid NOT NULL REFERENCES "Challenges"("Id"),
  "UserAnswer" varchar(50) NOT NULL,
  "IsCorrect" boolean NOT NULL,
  "ElapsedSeconds" int,
  "CreatedAt" timestamp DEFAULT NOW()
);

-- Badges
CREATE TABLE "Badges" (
  "Id" uuid PRIMARY KEY,
  "Name" varchar(100) NOT NULL,
  "Description" text,
  "Icon" varchar(255)
);
```

---

## 🔐 Password Requirements

Todos los usuarios deben cumplir:
- **Mínimo 6 caracteres**
- **Recomendado**: Incluir mayúsculas, minúsculas, números

Ejemplo válidos: `Admin123!`, `TestUser123!`, `MyPass2024`

---

## 🧪 Postman Integration

La colección Postman incluye:
1. **Auth**
   - Register
   - Login
   - Get current user

2. **Challenges**
   - List challenges (paginated)
   - Get challenge detail
   - Submit attempt

3. **User**
   - Get stats
   - Get badges
   - Get history

4. **Health**
   - Health check
   - API documentation link

Todos con preconfigured variables:
- `{{api_url}}` = `http://localhost:5118/api/v1`
- `{{jwt_token}}` = Automáticamente seteado tras login
- `{{user_id}}` = ID del usuario autenticado

---

## 💾 Crear más datos de prueba

### Via SQL (directo en PostgreSQL):

```sql
-- Conectar a la BD
psql -h localhost -p 5433 -U devbrain -d devbrain

-- Ver usuarios
SELECT "Email", "DisplayName", "EloRating" FROM "Users";

-- Ver challenges
SELECT "Title", "Category", "Difficulty" FROM "Challenges" LIMIT 5;

-- Ver intentos de un usuario
SELECT a."IsCorrect", a."ElapsedSeconds", c."Title"
  FROM "Attempts" a
  JOIN "Challenges" c ON a."ChallengeId" = c."Id"
  WHERE a."UserId" = (SELECT "Id" FROM "Users" WHERE "Email" = 'admin@devbrain.local')
  ORDER BY a."CreatedAt" DESC;
```

### Via API (crear nuevo usuario):

```bash
curl -X POST http://localhost:5118/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "yourname@example.com",
    "password": "YourPass123!",
    "displayName": "Your Name"
  }' | jq .
```

---

**Happy testing! 🎉**

Need help? Check [`docs/TESTING.md`](./TESTING.md)
