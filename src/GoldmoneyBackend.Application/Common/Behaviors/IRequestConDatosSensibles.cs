namespace GoldmoneyBackend.Application.Common.Behaviors;

/// <summary>
/// Marca un request que lleva contraseñas o tokens: <see cref="LoggingBehavior{TRequest, TResponse}"/>
/// registra solo su nombre, nunca su contenido.
/// </summary>
public interface IRequestConDatosSensibles;
