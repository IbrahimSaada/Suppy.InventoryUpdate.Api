using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

namespace Suppy.InventoryUpdate.Api.Application.Contracts.Handlers;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
