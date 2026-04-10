# Spec: E2E Integration Tests — Happy Path Flow

**Tipo**: Integration Testing — End-to-End user flow  
**Ubicación**: `DevBrain.Integration.Tests`  
**Versión**: 1.0  
**Depende de**: Real PostgreSQL (TestContainers), Real Redis (TestContainers)

---

## Qué es

Suite de tests E2E que valida el flujo completo de un usuario desde registro hasta obtención de badges, usando **bases de datos y caché reales** (no mocks ni in-memory). Objetivo: garantizar que todo funciona en conjunto sin sorpresas entre componentes.

**Diferencia con tests unitarios**: 
- Unit tests = componentes aislados (en-memoria)
- E2E tests = flujo completo con DB + Redis reales

---

## Escenarios / Flujos

### Happy Path — Flujo completo exitoso

```
1. POST /auth/register
   Email: "e2e-user@example.com"
   Password: "E2ETest123!"
   DisplayName: "E2E Tester"
   
   ✅ Resultado: 201 Created, User persiste en PostgreSQL
   
2. POST /auth/login
   Email: "e2e-user@example.com"
   Password: "E2ETest123!"
   
   ✅ Resultado: 200 OK, JWT token generado (24h expiration)
   
3. GET /api/v1/challenges (con token)
   Query params: None (obtener primeros 10)
   
   ✅ Resultado: 200 OK, array con 10 challenges seeded
   
4. GET /api/v1/challenges/{id} (con token)
   Id: primer challenge ID
   
   ✅ Resultado: 200 OK, challenge details (sin createdAt, sin correctAnswer)
   
5. POST /api/v1/challenges/{id}/attempt (con token)
   ChallengeId: primer challenge
   UserAnswer: respuesta correcta (validada contra DB)
   ElapsedSeconds: 30
   
   ✅ Resultado: 201 Created
   - Attempt persiste en PostgreSQL
   - ELO actualizado en PostgreSQL
   - Streak grabado en Redis (TTL 48h)
   - Badges evaluados y persisten en PostgreSQL
   
6. GET /api/v1/users/me/stats (con token)
   
   ✅ Resultado: 200 OK
   - totalAttempts: 1
   - correctAttempts: 1
   - accuracyRate: 100%
   - currentStreak: 1 (desde Redis)
   - eloRating: >1000 (con bonus por tiempo)
   
7. GET /api/v1/users/me/badges (con token)
   
   ✅ Resultado: 200 OK
   - Array contiene "FirstBlood" (primer intento exitoso)
   - Otros badges: [] (solo primeiro cumplido)
```

---

## Invariantes / Garantías

1. **Persistencia**: Datos registrados en step 1 existen en PostgreSQL al leer en step 6
2. **Autenticación**: Sin token válido, todos los endpoints requieren 401
3. **JWT Expiration**: Token es válido por 24h exactamente
4. **ELO Determinístico**: Mismo intento correcto → siempre mismo nuevo ELO
5. **Streak en Redis**: Dato existe en Redis con TTL 48h
6. **Badge Uniqueness**: Mismo usuario en el mismo challenge → no duplica badges
7. **No Cross-Contamination**: Usuario A no ve estadísticas de Usuario B

---

## Configuración / Setup

### TestContainers
- **PostgreSQL**: v17 (mismo que production)
- **Redis**: v7 (mismo que production)
- Puerto dinámico asignado por TestContainers (no hardcodeado)
- Databases creadas y migradas automaticamente en cada test run
- Cleanup automático después de cada test (contenedores destruidos)

### Proyecto
- Nombre: `DevBrain.Integration.Tests`
- Framework: xUnit
- NuGet: 
  - `Testcontainers.PostgreSql` 
  - `Testcontainers.Redis`
  - `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory)

### IAsyncLifetime
- `InitializeAsync`: Spinner TestContainers, crear clients, registrar usuario
- `DisposeAsync`: Detener contenedores, limpiar conexiones

---

## Escenarios de error (validación)

### Negative cases para agregar luego
- Intento sin autenticación → 401
- Challenge no existe → 404
- Password incorrecta → 401
- Email duplicado en registro → 400

*(Para Fase 3.1, solo validamos happy path. Errores = Fase 3.3)*

---

## Validaciones esperadas

| Punto en el flujo | Qué validar | Dónde |
|---|---|---|
| 1. POST /register | Email único en DB | PostgreSQL query |
| 2. POST /login | JWT format correcto | Token structure |
| 3. GET /challenges | 10 challenges seed data | PostgreSQL query |
| 4. GET /challenges/{id} | Sin createdAt ni correctAnswer | Respuesta JSON |
| 5. POST /attempt | Attempt row + ELO actualizado + Streak en Redis + Badge creado | DB + Redis + DB |
| 6. GET /users/me/stats | Suma de attempts coincide | PostgreSQL aggregate |
| 7. GET /users/me/badges | Array contiene "FirstBlood" | Respuesta JSON |

---

## Test Class Structure

```csharp
public class E2EHappyPathTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer;
    private RedisContainer _redisContainer;
    private HttpClient _httpClient;
    private string _jwtToken;
    private Guid _userId;
    private Guid _challengeId;
    
    // InitializeAsync: spin up containers, run migrations
    // DisposeAsync: spin down containers
    
    [Fact]
    public async Task E2E_Register_Login_Challenges_Attempt_Stats_Badges_HappyPath()
    {
        // 7 steps as described above, with assertions at each step
    }
}
```

---

## Orden de ejecución del test

1. **Setup**: PostgreSQL + Redis containers up
2. **Register**: Usuario creado
3. **Login**: JWT obtenido
4. **GetChallenges**: Validar seed data
5. **GetChallenge**: Validar details (sin respuesta)
6. **PostAttempt**: Respuesta enviada, validar response DTO
7. **GetStats**: Validar totales
8. **GetBadges**: Validar badge FirstBlood
9. **Teardown**: Contenedores down, datos limpios

---

## Dependencias de otras specs

- ✅ Todos los endpoints de API ya existen (Fases 1-2)
- ✅ PostgreSQL schema ya migrado
- ✅ Redis configurado
- ✅ JWT middleware implementado
- ✅ Badges + ELO + Streak implementados

**Esta spec NO introduce código nuevo en la API — solo tests que usan componentes existentes.**

---

## Success Criteria

- ✅ Test ejecuta sin errores
- ✅ Todas las 7 assertions pasan
- ✅ PostgreSQL y Redis usados (no mocks)
- ✅ Build + test suite verde
- ✅ Proyecto `DevBrain.Integration.Tests` agregado a `.slnx`
