namespace Suppy.InventoryUpdate.Api.Application.Contracts.Requests;

public interface ICommand<TResponse> : IRequest<TResponse>
{
}

public interface ICommand : ICommand<Unit>
{
}
