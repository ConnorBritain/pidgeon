// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Adapters.Interfaces;
using Pidgeon.Core.Infrastructure.Standards.Abstractions;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;
using Pidgeon.Core.Domain.Messaging;
using System.Text.RegularExpressions;

namespace Pidgeon.Core.Standards.Common.HL7.Configuration;

/// <summary>
/// Universal HL7 field analysis plugin using adapter architecture.
/// Parses raw HL7 messages and delegates analysis to messaging-to-configuration adapter.
/// Works across all HL7v2 versions using standard segment parsing.
/// </summary>
internal class HL7FieldAnalysisPlugin : IStandardFieldAnalysisPlugin
{
    private readonly ILogger<HL7FieldAnalysisPlugin> _logger;
    private readonly IMessagingToConfigurationAdapter _adapter;

    private static readonly Regex SegmentPattern = new(@"^([A-Z0-9]{3})\|", RegexOptions.Compiled);

    public HL7FieldAnalysisPlugin(
        ILogger<HL7FieldAnalysisPlugin> logger,
        IMessagingToConfigurationAdapter adapter)
    {
        _logger = logger;
        _adapter = adapter;
    }

    public string StandardName => "HL7v2";

    public bool CanHandle(string standard)
    {
        return standard?.Equals("HL7", StringComparison.OrdinalIgnoreCase) == true ||
               standard?.StartsWith("HL7v2", StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<Result<FieldPatterns>> AnalyzeFieldPatternsAsync(
        IEnumerable<string> messages,
        string messageType)
    {
        try
        {
            var messageList = messages.ToList();
            _logger.LogInformation("Analyzing HL7 field patterns for {MessageCount} {MessageType} messages",
                messageList.Count, messageType);

            var hl7Messages = new List<HealthcareMessage>();
            foreach (var rawMessage in messageList)
            {
                if (string.IsNullOrWhiteSpace(rawMessage))
                    continue;

                var hl7Message = await ParseRawMessageAsync(rawMessage, messageType);
                if (hl7Message != null)
                {
                    hl7Messages.Add(hl7Message);
                }
            }

            if (!hl7Messages.Any())
            {
                return Result<FieldPatterns>.Failure(Error.Create("NoValidMessages", "No valid HL7 messages found to analyze"));
            }

            var fieldPatterns = await _adapter.AnalyzePatternsAsync(hl7Messages);
            
            var totalFieldCount = fieldPatterns.SegmentPatterns.Values.Sum(sp => sp.Fields.Count);
            _logger.LogInformation("Successfully analyzed {MessageCount} HL7 messages, found {FieldCount} field patterns",
                hl7Messages.Count, totalFieldCount);

            return Result<FieldPatterns>.Success(fieldPatterns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing HL7 field patterns for message type {MessageType}", messageType);
            return Result<FieldPatterns>.Failure(Error.Create("AnalysisError", ex.Message));
        }
    }

    private async Task<HealthcareMessage?> ParseRawMessageAsync(string rawMessage, string messageType)
    {
        try
        {
            var segments = rawMessage.Split('\r', '\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (!segments.Any())
                return null;

            var hl7Message = new GenericHL7Message
            {
                MessageType = new HL7MessageType { MessageCode = messageType, TriggerEvent = string.Empty },
                Standard = "HL7v2",
                Version = ExtractVersionId(segments) ?? "2.3",  // Default to 2.3 if not specified
                MessageControlId = ExtractMessageControlId(segments),
                SendingSystem = ExtractSendingSystem(segments),
                ReceivingSystem = ExtractReceivingSystem(segments),
                Timestamp = DateTime.UtcNow,
                Segments = ParseSegments(segments).Values.ToList()
            };

            await Task.CompletedTask;
            return hl7Message;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse raw HL7 message");
            return null;
        }
    }

    private static string? ExtractMessageControlId(List<string> segments)
    {
        var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
        if (mshSegment == null) return null;

        var fields = mshSegment.Split('|');
        return fields.Length > 9 ? fields[9] : null;
    }

    private static string? ExtractSendingSystem(List<string> segments)
    {
        var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
        if (mshSegment == null) return null;

        var fields = mshSegment.Split('|');
        return fields.Length > 2 ? fields[2] : null;
    }

    private static string? ExtractReceivingSystem(List<string> segments)
    {
        var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
        if (mshSegment == null) return null;

        var fields = mshSegment.Split('|');
        return fields.Length > 4 ? fields[4] : null;
    }

    private static string? ExtractVersionId(List<string> segments)
    {
        var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
        if (mshSegment == null) return null;

        var fields = mshSegment.Split('|');
        // MSH.12 is the version field (field 11 in zero-based array after splitting)
        return fields.Length > 11 && !string.IsNullOrWhiteSpace(fields[11]) ? fields[11] : null;
    }

    private static Dictionary<string, Domain.Messaging.HL7v2.Messages.HL7Segment> ParseSegments(List<string> segments)
    {
        var segmentDict = new Dictionary<string, Domain.Messaging.HL7v2.Messages.HL7Segment>();
        var segmentCounts = new Dictionary<string, int>();
        
        foreach (var segment in segments)
        {
            var match = SegmentPattern.Match(segment);
            if (match.Success)
            {
                var segmentId = match.Groups[1].Value;
                var fields = segment.Split('|');
                
                // Handle repeating segments by adding sequence numbers
                if (!segmentCounts.ContainsKey(segmentId))
                    segmentCounts[segmentId] = 0;
                
                segmentCounts[segmentId]++;
                var key = segmentCounts[segmentId] == 1 ? segmentId : $"{segmentId}{segmentCounts[segmentId]}";
                
                segmentDict[key] = new GenericHL7Segment(segmentId, fields);
            }
        }

        return segmentDict;
    }
}