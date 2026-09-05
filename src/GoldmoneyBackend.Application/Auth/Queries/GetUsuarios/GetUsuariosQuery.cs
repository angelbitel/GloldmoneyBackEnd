using GoldmoneyBackend.Application.Auth.DTOs;
using MediatR;

namespace GoldmoneyBackend.Application.Auth.Queries.GetUsuarios;

public sealed record GetUsuariosQuery : IRequest<IReadOnlyList<UsuarioDto>>;
