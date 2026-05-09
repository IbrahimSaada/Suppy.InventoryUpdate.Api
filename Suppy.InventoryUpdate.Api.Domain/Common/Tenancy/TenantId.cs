using System.Text.RegularExpressions;
using Suppy.InventoryUpdate.Api.Domain.ValueObjects;

namespace Suppy.InventoryUpdate.Api.Domain.Tenancy;

public sealed class TenantId : ValueObject
{
    public const int MaxLength = 100;

    private static readonly Regex AllowedPattern = new(
        "^[a-z0-9][a-z0-9._-]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private TenantId()
    {
    }

    private TenantId(string value)
    {
        Value = value;
    }

    public string Value { get; private set; } = string.Empty;

    public static TenantId From(string value)
    {
        var normalized = Normalize(value);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Tenant id is required.", nameof(value));
        }

        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException($"Tenant id cannot exceed {MaxLength} characters.", nameof(value));
        }

        if (!AllowedPattern.IsMatch(normalized))
        {
            throw new ArgumentException(
                "Tenant id can contain lowercase letters, numbers, dot, dash, and underscore only. It must start with a letter or number.",
                nameof(value));
        }

        return new TenantId(normalized);
    }

    public static bool TryCreate(string? value, out TenantId? tenantId)
    {
        tenantId = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            tenantId = From(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public override string ToString()
    {
        return Value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
