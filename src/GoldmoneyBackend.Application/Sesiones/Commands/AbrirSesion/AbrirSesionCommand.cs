using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;

public sealed record AbrirSesionCommand(
    string CodigoEmpresa,
    DateTime FechaApertura,
    string UsuarioResponsable) : IRequest<SesionAbiertaDto>;
