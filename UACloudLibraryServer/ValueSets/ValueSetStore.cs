using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.ValueSets
{
    /// <summary>
    /// EF Core backed <see cref="IValueSetStore"/>. Entries are one row per node, so reading a set is
    /// an indexed range scan on the composite primary key and editing a node is a single upsert.
    /// </summary>
    public sealed class ValueSetStore : IValueSetStore
    {
        private readonly AppDbContext _dbContext;

        public ValueSetStore(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<NodesetValueSet>> ListAsync(string nodesetIdentifier, string userId, CancellationToken ct = default)
        {
            RequireUser(userId);

            return await _dbContext.NodesetValueSets
                .AsNoTracking()
                .Where(vs => vs.NodesetIdentifier == nodesetIdentifier && vs.UserId == userId)
                .OrderBy(vs => vs.Name)
                .ToListAsync(ct).ConfigureAwait(false);
        }

        public async Task<NodesetValueSet> GetAsync(Guid id, string userId, CancellationToken ct = default)
        {
            RequireUser(userId);

            return await _dbContext.NodesetValueSets
                .AsNoTracking()
                .FirstOrDefaultAsync(vs => vs.Id == id && vs.UserId == userId, ct).ConfigureAwait(false);
        }

        public async Task<NodesetValueSet> CreateAsync(string nodesetIdentifier, string userId, string name, string description, CancellationToken ct = default)
        {
            RequireUser(userId);
            name = NormalizeName(name);

            if (string.IsNullOrWhiteSpace(nodesetIdentifier))
            {
                throw new ArgumentException("Nodeset identifier is required.", nameof(nodesetIdentifier));
            }

            await EnsureNameAvailableAsync(nodesetIdentifier, userId, name, null, ct).ConfigureAwait(false);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            var set = new NodesetValueSet {
                Id = Guid.NewGuid(),
                NodesetIdentifier = nodesetIdentifier,
                UserId = userId,
                Name = name,
                Description = description,
                CreatedAt = now,
                LastModified = now,
            };

            _dbContext.NodesetValueSets.Add(set);
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return set;
        }

        public async Task<NodesetValueSet> UpdateAsync(Guid id, string userId, string name, string description, CancellationToken ct = default)
        {
            RequireUser(userId);
            name = NormalizeName(name);

            NodesetValueSet set = await TrackedAsync(id, userId, ct).ConfigureAwait(false);
            if (set == null)
            {
                return null;
            }

            await EnsureNameAvailableAsync(set.NodesetIdentifier, userId, name, id, ct).ConfigureAwait(false);

            set.Name = name;
            set.Description = description;
            set.LastModified = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return set;
        }

        public async Task<bool> DeleteAsync(Guid id, string userId, CancellationToken ct = default)
        {
            RequireUser(userId);

            NodesetValueSet set = await TrackedAsync(id, userId, ct).ConfigureAwait(false);
            if (set == null)
            {
                return false;
            }

            _dbContext.NodesetValueSets.Remove(set);
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }

        public async Task<NodesetValueSet> CloneAsync(Guid sourceId, string userId, string newName, CancellationToken ct = default)
        {
            RequireUser(userId);
            newName = NormalizeName(newName);

            NodesetValueSet source = await GetAsync(sourceId, userId, ct).ConfigureAwait(false);
            if (source == null)
            {
                return null;
            }

            await EnsureNameAvailableAsync(source.NodesetIdentifier, userId, newName, null, ct).ConfigureAwait(false);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            var clone = new NodesetValueSet {
                Id = Guid.NewGuid(),
                NodesetIdentifier = source.NodesetIdentifier,
                UserId = userId,
                Name = newName,
                Description = source.Description,
                CreatedAt = now,
                LastModified = now,
            };

            List<NodesetValueSetEntry> entries = await _dbContext.NodesetValueSetEntries
                .AsNoTracking()
                .Where(e => e.ValueSetId == sourceId)
                .ToListAsync(ct).ConfigureAwait(false);

            _dbContext.NodesetValueSets.Add(clone);
            _dbContext.NodesetValueSetEntries.AddRange(entries.Select(e => new NodesetValueSetEntry {
                ValueSetId = clone.Id,
                NodeId = e.NodeId,
                Value = e.Value,
                LastModified = now,
            }));

            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return clone;
        }

        public async Task<IReadOnlyDictionary<string, string>> GetValuesAsync(Guid id, string userId, CancellationToken ct = default)
        {
            RequireUser(userId);

            if (await GetAsync(id, userId, ct).ConfigureAwait(false) == null)
            {
                return null;
            }

            return await _dbContext.NodesetValueSetEntries
                .AsNoTracking()
                .Where(e => e.ValueSetId == id)
                .ToDictionaryAsync(e => e.NodeId, e => e.Value, ct).ConfigureAwait(false);
        }

        public Task SetValueAsync(Guid id, string userId, string nodeId, string value, CancellationToken ct = default)
        {
            return SetValuesAsync(id, userId, new Dictionary<string, string> { [nodeId] = value }, ct);
        }

        public async Task SetValuesAsync(Guid id, string userId, IReadOnlyDictionary<string, string> values, CancellationToken ct = default)
        {
            RequireUser(userId);
            ArgumentNullException.ThrowIfNull(values);

            NodesetValueSet set = await TrackedAsync(id, userId, ct).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Value set {id} not found.");

            List<string> nodeIds = values.Keys.ToList();
            Dictionary<string, NodesetValueSetEntry> existing = await _dbContext.NodesetValueSetEntries
                .Where(e => e.ValueSetId == id && nodeIds.Contains(e.NodeId))
                .ToDictionaryAsync(e => e.NodeId, ct).ConfigureAwait(false);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            foreach (KeyValuePair<string, string> kv in values)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                {
                    throw new ArgumentException("NodeId must not be empty.", nameof(values));
                }

                if (existing.TryGetValue(kv.Key, out NodesetValueSetEntry entry))
                {
                    entry.Value = kv.Value ?? string.Empty;
                    entry.LastModified = now;
                }
                else
                {
                    _dbContext.NodesetValueSetEntries.Add(new NodesetValueSetEntry {
                        ValueSetId = id,
                        NodeId = kv.Key,
                        Value = kv.Value ?? string.Empty,
                        LastModified = now,
                    });
                }
            }

            set.LastModified = now;
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        public async Task<bool> RemoveValueAsync(Guid id, string userId, string nodeId, CancellationToken ct = default)
        {
            RequireUser(userId);

            NodesetValueSet set = await TrackedAsync(id, userId, ct).ConfigureAwait(false);
            if (set == null)
            {
                return false;
            }

            NodesetValueSetEntry entry = await _dbContext.NodesetValueSetEntries
                .FirstOrDefaultAsync(e => e.ValueSetId == id && e.NodeId == nodeId, ct).ConfigureAwait(false);
            if (entry == null)
            {
                return false;
            }

            _dbContext.NodesetValueSetEntries.Remove(entry);
            set.LastModified = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }

        private Task<NodesetValueSet> TrackedAsync(Guid id, string userId, CancellationToken ct)
        {
            return _dbContext.NodesetValueSets.FirstOrDefaultAsync(vs => vs.Id == id && vs.UserId == userId, ct);
        }

        private async Task EnsureNameAvailableAsync(string nodesetIdentifier, string userId, string name, Guid? excludeId, CancellationToken ct)
        {
            bool taken = await _dbContext.NodesetValueSets
                .AnyAsync(vs => vs.NodesetIdentifier == nodesetIdentifier && vs.UserId == userId && vs.Name == name && (excludeId == null || vs.Id != excludeId), ct)
                .ConfigureAwait(false);

            if (taken)
            {
                throw new InvalidOperationException($"A value set named '{name}' already exists for this nodeset.");
            }
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value set name is required.", nameof(name));
            }

            name = name.Trim();
            if (name.Length > 200)
            {
                throw new ArgumentException("Value set name must be 200 characters or fewer.", nameof(name));
            }

            return name;
        }

        private static void RequireUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("A user is required to access value sets.", nameof(userId));
            }
        }
    }
}
