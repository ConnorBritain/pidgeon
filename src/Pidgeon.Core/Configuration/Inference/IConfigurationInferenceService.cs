namespace Pidgeon.Core.Configuration.Inference;

using Pidgeon.Core.Common;

public interface IConfigurationInferenceService
{
    Result<InferredConfiguration> AnalyzeMessages(IEnumerable<string> messages);
    Result<VendorSignature> DetectVendor(string message);
    Result<FieldPatterns> AnalyzeFieldUsage(IEnumerable<string> messages);
}