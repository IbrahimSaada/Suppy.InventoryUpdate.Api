using FluentValidation;
using Suppy.InventoryUpdate.Api.Domain.Products.ValueObjects;
using Suppy.InventoryUpdate.Api.Domain.Tenancy;

namespace Suppy.InventoryUpdate.Api.Application.Features.Products.SubmitBatchUpdate;

internal sealed class SubmitProductBatchUpdateCommandValidator : AbstractValidator<SubmitProductBatchUpdateCommand>
{
    public const int MaxBatchItems = 10_000;

    public SubmitProductBatchUpdateCommandValidator()
    {
        RuleFor(command => command.TenantId)
            .Must(value => TenantId.TryCreate(value, out _))
            .WithMessage("Tenant id is invalid.");

        RuleFor(command => command.Items)
            .NotNull()
            .WithMessage("Items are required.")
            .Must(items => items.Count > 0)
            .WithMessage("Batch must contain at least one item.")
            .Must(items => items.Count <= MaxBatchItems)
            .WithMessage($"Batch cannot contain more than {MaxBatchItems} items.");

        RuleFor(command => command.IdempotencyKey)
            .MaximumLength(BatchIdempotencyKey.MaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.IdempotencyKey));

        RuleForEach(command => command.Items)
            .SetValidator(new SubmitProductBatchUpdateItemValidator());

        RuleFor(command => command.Items)
            .Must(NotContainDuplicateItemIds)
            .WithMessage("Batch contains duplicate item ids.");
    }

    private static bool NotContainDuplicateItemIds(IReadOnlyCollection<SubmitProductBatchUpdateItem> items)
    {
        if (items.Count == 0)
        {
            return true;
        }

        return items
            .Select(item => item.ItemId.Trim())
            .Distinct(StringComparer.Ordinal)
            .Count() == items.Count;
    }
}

internal sealed class SubmitProductBatchUpdateItemValidator : AbstractValidator<SubmitProductBatchUpdateItem>
{
    public SubmitProductBatchUpdateItemValidator()
    {
        RuleFor(item => item.ItemId)
            .NotEmpty()
            .MaximumLength(ItemId.MaxLength);

        RuleFor(item => item.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(item => item.Stock)
            .GreaterThanOrEqualTo(0);

        RuleFor(item => item.MetadataJson)
            .MaximumLength(ProductUpdateBatchItemDraft.MaxMetadataJsonLength)
            .When(item => !string.IsNullOrWhiteSpace(item.MetadataJson));
    }
}
