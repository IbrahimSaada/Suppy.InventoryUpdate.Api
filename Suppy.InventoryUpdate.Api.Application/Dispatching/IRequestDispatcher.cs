using Suppy.InventoryUpdate.Api.Abstractions.Results;
using Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

namespace Suppy.InventoryUpdate.Api.Application.Dispatching;

public interface IRequestDispatcher
{
    Task<Result<TResponse>> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken ct = default);
}
