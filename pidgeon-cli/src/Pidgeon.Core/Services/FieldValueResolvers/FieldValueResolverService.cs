// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Orchestrates field value resolution using priority-based resolver chain.
/// Follows chain of responsibility pattern for clean separation of concerns.
/// </summary>
public class FieldValueResolverService : IFieldValueResolverService
{
    private readonly ILogger<FieldValueResolverService> _logger;
    private readonly IEnumerable<IFieldValueResolver> _resolvers;

    public FieldValueResolverService(
        ILogger<FieldValueResolverService> logger,
        IEnumerable<IFieldValueResolver> resolvers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
    }

    /// <summary>
    /// Resolve field value using registered resolvers in priority order.
    /// Tries each resolver until one returns a non-null value.
    /// </summary>
    public async Task<string> ResolveFieldValueAsync(FieldResolutionContext context)
    {
        try
        {
            _logger.LogDebug("Resolving field value for {SegmentCode}.{FieldPosition} ({FieldName})",
                context.SegmentCode, context.FieldPosition, context.Field.Name);

            // Try resolvers in priority order (highest priority first)
            var sortedResolvers = _resolvers.OrderByDescending(r => r.Priority);

            foreach (var resolver in sortedResolvers)
            {
                try
                {
                    var result = await resolver.ResolveAsync(context);
                    if (result != null)
                    {
                        _logger.LogDebug("Field {SegmentCode}.{FieldPosition} resolved by {ResolverType}: '{Value}'",
                            context.SegmentCode, context.FieldPosition, resolver.GetType().Name, result);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Resolver {ResolverType} failed for field {SegmentCode}.{FieldPosition}",
                        resolver.GetType().Name, context.SegmentCode, context.FieldPosition);
                    // Continue to next resolver
                }
            }

            // No resolver succeeded, return empty string as safe fallback
            _logger.LogDebug("No resolver found value for field {SegmentCode}.{FieldPosition}, using empty string",
                context.SegmentCode, context.FieldPosition);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving field value for {SegmentCode}.{FieldPosition}",
                context.SegmentCode, context.FieldPosition);
            return string.Empty;
        }
    }
}