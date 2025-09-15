export function buildGenerateCli(standard: string, messageType: string, count = 1, vendor?: string | null) {
  // Normalize to CLI-friendly inputs
  const std = standard.toLowerCase().includes("hl7")
    ? ""
    : standard.toLowerCase().includes("fhir")
    ? ""
    : "";
  // Use smart inference by default (standard omitted unless needed)
  const parts: string[] = ["pidgeon", "generate", JSON.stringify(messageType)];
  if (count && count !== 1) {
    parts.push("--count", String(count));
  }
  if (vendor) {
    parts.push("--vendor", JSON.stringify(vendor));
  }
  return parts.join(" ");
}

export function buildValidateCli(filePath = "current.hl7", mode: "strict" | "compatibility" = "strict") {
  return ["pidgeon", "validate", "--file", JSON.stringify(filePath), "--mode", mode].join(" ");
}
