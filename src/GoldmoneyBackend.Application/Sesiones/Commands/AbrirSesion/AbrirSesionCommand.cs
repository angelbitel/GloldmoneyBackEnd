using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Sesiones.Commands.AbrirSesion;

/// <summary>
/// Abre la caja de la empresa para el día de hoy, como <c>sp_Gravar_Sesiones_Empresa</c> del VB6.
/// Una empresa solo puede tener una caja abierta.
/// </summary>
public sealed record AbrirSesionCommand(
    string CodigoEmpresa,
    string UsuarioResponsable) : IRequest<SesionAbiertaDto>;
