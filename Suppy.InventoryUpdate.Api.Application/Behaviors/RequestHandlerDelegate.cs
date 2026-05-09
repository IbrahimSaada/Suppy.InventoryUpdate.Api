using Suppy.InventoryUpdate.Api.Abstractions.Results;

namespace Suppy.InventoryUpdate.Api.Application.Behaviors;

public delegate Task<Result<TResponse>> RequestHandlerDelegate<TResponse>();
