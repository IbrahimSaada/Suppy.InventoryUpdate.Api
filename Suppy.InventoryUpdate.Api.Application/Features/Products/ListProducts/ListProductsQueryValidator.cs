using FluentValidation;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.ListProducts;

internal sealed class ListProductsQueryValidator : AbstractValidator<ListProductsQuery>
{
    public const int MaxPageSize = 200;
    public const int MaxPage = 100_000;

    public ListProductsQueryValidator()
    {
        RuleFor(query => query.TenantId)
            .Must(value => TenantId.TryCreate(value, out _))
            .WithMessage("Tenant id is invalid.");

        RuleFor(query => query.Page)
            .InclusiveBetween(1, MaxPage);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MaxPageSize);
    }
}
