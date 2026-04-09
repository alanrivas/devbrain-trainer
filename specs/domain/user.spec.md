# Spec: User (Usuario)

**Tipo**: Entidad de dominio  
**Ubicación**: `DevBrain.Domain`  
**Versión**: 1.0  

---

## Qué es

Un `User` representa a un usuario registrado en la app. Su identidad viene de Supabase Auth — DevBrain no gestiona contraseñas ni sesiones, solo almacena los datos de perfil necesarios para gamificación y estadísticas.

---

## Propiedades

| Propiedad     | Tipo             | Reglas                                                       |
|---------------|------------------|--------------------------------------------------------------|
| `Id`          | `string`         | Es el SupabaseId (UUID como string), requerido, no vacío, inmutable |
| `Email`       | `string`         | Requerido, debe contener `@`, inmutable                      |
| `DisplayName` | `string`         | Requerido, entre 2 y 50 caracteres                           |
| `CreatedAt`   | `DateTimeOffset` | Asignado al crear, inmutable                                 |

---

## Comportamientos

### Creación

- Se crea con `User.Create(supabaseId, email, displayName)`
- Valida todas las propiedades al crearse — si alguna es inválida, lanza `DomainException`
- `CreatedAt` es asignado internamente con `DateTimeOffset.UtcNow`

### Actualización de DisplayName

- `User.UpdateDisplayName(newDisplayName)` — permite cambiar el nombre de display
- Aplica las mismas reglas de validación (entre 2 y 50 caracteres, no vacío)
- `Email` e `Id` no pueden cambiar después de la creación

---

## Invariantes (reglas que nunca se rompen)

1. `Id` (SupabaseId) no puede ser vacío o solo espacios
2. `Email` debe contener `@` y no puede estar vacío
3. `DisplayName` debe tener entre 2 y 50 caracteres
4. `Id`, `Email` y `CreatedAt` no cambian después de la creación

---

## Qué NO es esta entidad

- No gestiona contraseñas, sesiones ni tokens (eso es Supabase Auth)
- No acumula estadísticas directamente (eso es responsabilidad del servicio)
- No conoce su ELO ni su streak (esos son calculados por los servicios de gamificación)

---

## Escenarios de test esperados

| Escenario | Resultado |
|-----------|-----------|
| Crear con todos los campos válidos | OK — objeto creado |
| Crear con `Id` vacío | `DomainException` |
| Crear con `Email` sin `@` | `DomainException` |
| Crear con `Email` vacío | `DomainException` |
| Crear con `DisplayName` de 1 carácter | `DomainException` |
| Crear con `DisplayName` de 51 caracteres | `DomainException` |
| Crear con `DisplayName` vacío | `DomainException` |
| `CreatedAt` está asignado al crear | OK |
| `UpdateDisplayName` con nombre válido | OK — nombre actualizado |
| `UpdateDisplayName` con nombre vacío | `DomainException` |
| `UpdateDisplayName` con nombre de 1 carácter | `DomainException` |
