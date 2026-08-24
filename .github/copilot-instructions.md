# Guía de Arquitectura y Estándares del Proyecto

## Principios de Diseño

- Seguimos estrictamente Clean Architecture y Domain-Driven Design (DDD).
- Los controladores (`.Api`) deben ser delgados (Slim Controllers). No deben contener lógica de negocio, mapeos complejos ni llamadas directas a acceso de datos.
- Usamos el patrón CQRS con MediatR para procesar las solicitudes desde los controladores.
- Mantenemos los proyectos separados por responsabilidades: `.Api`, `.Application`, `.Domain`, e `.Infrastructure`.

## Reglas de Persistencia y Capas

- La persistencia (DbContext, Repositorios) pertenece únicamente al proyecto `GoldmoneyBackend.Infrastructure`.
- Las interfaces de repositorios y servicios de aplicación se definen en `GoldmoneyBackend.Application`.
- La lógica de negocio y las reglas de dominio van puramente en `GoldmoneyBackend.Domain`.

## Estándares de Código C# / .NET

- Los endpoints HTTP POST que crean recursos deben retornar `CreatedAtAction` o `CreatedAtRoute` con el recurso generado.
- Usa DTOs y Commands inmutables (`records`).
- Aplica CancellationToken en todos los métodos asíncronos.
