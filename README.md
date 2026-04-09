# DevBrain Trainer

App de entrenamiento cognitivo gamificada para desarrolladores. Mejora tu lógica, memoria y razonamiento con problemas del mundo tech real.

## Categorías de desafíos

- **SQL / Bases de datos** — queries, optimización, detección de errores
- **Lógica de código** — ¿qué imprime este código?, encontrar bugs, completar métodos
- **Arquitectura / Diseño** — elegir la solución correcta, detectar anti-patterns
- **Docker / DevOps** — Dockerfiles con errores, docker-compose, secuencias de comandos
- **Memoria de trabajo** — tracing de variables, aplicar reglas de negocio

## Gamificación

- Streak diario (racha de días consecutivos)
- Rating ELO por categoría
- Tiempo límite por problema (presión real de trabajo)
- Explicación post-respuesta
- Modo "sprint": 5 problemas en 3 minutos
- Logros / badges

## Stack

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 10 |
| Frontend | Next.js + Tailwind |
| DB | PostgreSQL + Redis |
| Deploy | Railway + GitHub Pages |

## Metodología

TDD + Spec-Driven Development. Cada feature comienza con specs/tests.
