using GoldmoneyBackend.Application.Common.Interfaces;
using MediatR;

namespace GoldmoneyBackend.Application.Pagos.Queries.GetCobro;

public sealed record GetCobroQuery(Guid Id) : IRequest<CobroDto>;
