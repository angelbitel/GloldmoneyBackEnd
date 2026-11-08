using MediatR;

namespace GoldmoneyBackend.Application.Clientes.Commands.ActualizarComentarioCliente;

/// <summary>Nota del cliente (CLIENTES.comentario), compartida con el VB6. Vacía la borra.</summary>
public sealed record ActualizarComentarioClienteCommand(string IdCliente, string? Comentario) : IRequest;
