using Suppy.InventoryUpdate.Api.Abstractions.Messaging;
using Suppy.InventoryUpdate.Api.Persistence.Sql;
using Microsoft.EntityFrameworkCore;

namespace Suppy.InventoryUpdate.Api.Persistence.Outbox;

internal sealed class EfOutboxStore : IOutboxStore
{
    private readonly AppDbContext _dbContext;

    public EfOutboxStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task EnqueueAsync(OutboxMessageToEnqueue message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _dbContext.Set<OutboxMessageEntity>().Add(MapForEnqueue(message));
        return Task.CompletedTask;
    }

    public Task EnqueueManyAsync(IEnumerable<OutboxMessageToEnqueue> messages, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        var entities = messages.Select(MapForEnqueue).ToArray();
        if (entities.Length == 0)
        {
            return Task.CompletedTask;
        }

        _dbContext.Set<OutboxMessageEntity>().AddRange(entities);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<OutboxPendingMessage>> ClaimBatchAsync(
        int batchSize,
        string lockId,
        DateTime utcNow,
        TimeSpan lockTimeout,
        CancellationToken ct = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentException("Batch size must be greater than zero.", nameof(batchSize));
        }

        if (string.IsNullOrWhiteSpace(lockId))
        {
            throw new ArgumentException("Lock id is required.", nameof(lockId));
        }

        if (lockTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Lock timeout must be greater than zero.", nameof(lockTimeout));
        }

        var staleLockCutoff = utcNow - lockTimeout;

        var candidates = await _dbContext.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(x =>
                (x.Status == OutboxMessageStatus.Pending || x.Status == OutboxMessageStatus.Failed) &&
                x.AvailableAtUtc <= utcNow &&
                (x.LockId == null || x.LockedAtUtc == null || x.LockedAtUtc < staleLockCutoff))
            .OrderBy(x => x.AvailableAtUtc)
            .ThenBy(x => x.OccurredAtUtc)
            .Take(batchSize)
            .ToArrayAsync(ct);

        if (candidates.Length == 0)
        {
            return Array.Empty<OutboxPendingMessage>();
        }

        var claimedMessages = new List<OutboxPendingMessage>(candidates.Length);

        foreach (var candidate in candidates)
        {
            var claimed = await _dbContext.Set<OutboxMessageEntity>()
                .Where(x =>
                    x.Id == candidate.Id &&
                    (x.Status == OutboxMessageStatus.Pending || x.Status == OutboxMessageStatus.Failed) &&
                    x.AvailableAtUtc <= utcNow &&
                    (x.LockId == null || x.LockedAtUtc == null || x.LockedAtUtc < staleLockCutoff))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, OutboxMessageStatus.Processing)
                        .SetProperty(x => x.LockId, lockId)
                        .SetProperty(x => x.LockedAtUtc, utcNow),
                    ct);

            if (claimed == 1)
            {
                claimedMessages.Add(new OutboxPendingMessage(
                    MessageId: candidate.Id,
                    MessageType: candidate.MessageType,
                    Payload: candidate.Payload,
                    Headers: candidate.Headers,
                    OccurredAtUtc: candidate.OccurredAtUtc,
                    RetryCount: candidate.RetryCount,
                    CorrelationId: candidate.CorrelationId,
                    CausationId: candidate.CausationId,
                    IdempotencyKey: candidate.IdempotencyKey,
                    Version: candidate.Version));
            }
        }

        return claimedMessages;
    }

    public Task MarkSucceededAsync(
        Guid messageId,
        string lockId,
        DateTime processedAtUtc,
        CancellationToken ct = default)
    {
        return _dbContext.Set<OutboxMessageEntity>()
            .Where(x =>
                x.Id == messageId &&
                x.LockId == lockId &&
                x.Status == OutboxMessageStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, OutboxMessageStatus.Succeeded)
                    .SetProperty(x => x.ProcessedAtUtc, processedAtUtc)
                    .SetProperty(x => x.LastError, (string?)null)
                    .SetProperty(x => x.LockId, (string?)null)
                    .SetProperty(x => x.LockedAtUtc, (DateTime?)null),
                ct);
    }

    public Task MarkFailedAsync(
        Guid messageId,
        string lockId,
        string error,
        DateTime nextAvailableAtUtc,
        bool moveToPoison,
        CancellationToken ct = default)
    {
        var safeError = string.IsNullOrWhiteSpace(error)
            ? "Outbox dispatch failed."
            : error[..Math.Min(error.Length, 2048)];

        var targetStatus = moveToPoison
            ? OutboxMessageStatus.Poison
            : OutboxMessageStatus.Failed;

        return _dbContext.Set<OutboxMessageEntity>()
            .Where(x =>
                x.Id == messageId &&
                x.LockId == lockId &&
                x.Status == OutboxMessageStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, targetStatus)
                    .SetProperty(x => x.RetryCount, x => x.RetryCount + 1)
                    .SetProperty(x => x.AvailableAtUtc, nextAvailableAtUtc)
                    .SetProperty(x => x.LastError, safeError)
                    .SetProperty(x => x.LockId, (string?)null)
                    .SetProperty(x => x.LockedAtUtc, (DateTime?)null),
                ct);
    }

    private static OutboxMessageEntity MapForEnqueue(OutboxMessageToEnqueue message)
    {
        return new OutboxMessageEntity
        {
            Id = message.MessageId,
            MessageType = message.MessageType,
            Payload = message.Payload,
            Headers = message.Headers,
            OccurredAtUtc = message.OccurredAtUtc,
            AvailableAtUtc = message.AvailableAtUtc,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0,
            LastError = null,
            ProcessedAtUtc = null,
            LockId = null,
            LockedAtUtc = null,
            CorrelationId = message.CorrelationId,
            CausationId = message.CausationId,
            IdempotencyKey = message.IdempotencyKey,
            Version = message.Version
        };
    }
}
