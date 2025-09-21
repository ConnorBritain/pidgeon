"use client";

import { Card, CardHeader, CardTitle, CardContent, Badge } from "@/components/ui";
import { Code2 } from "lucide-react";

export interface ValidationIssue {
  ruleId: string;
  message: string;
  severity: "Error" | "Warning" | "Info";
  location?: string;
}

export interface ValidationResultView {
  isValid: boolean;
  issues: ValidationIssue[];
}

export interface ValidationPanelProps {
  result: ValidationResultView | null;
  buildValidateCli: () => string;
}

export function ValidationPanel({ result, buildValidateCli }: ValidationPanelProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-ph-white">
          <div className="flex items-center gap-2">
            <span>Validation Results</span>
            <span
              className={`px-2 py-0.5 rounded text-black ph-text-desktop-xs ${
                result?.isValid ? "bg-ph-success" : "bg-ph-danger"
              }`}
            >
              {result ? (result.isValid ? "Valid" : "Has Issues") : "Not run"}
            </span>
            <button
              className="ml-auto ph-text-desktop-sm text-ph-blue-400 underline inline-flex items-center"
              title={buildValidateCli()}
              type="button"
            >
              <Code2 size={14} className="mr-1" /> Copy CLI
            </button>
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent>
        {!result && (
          <div className="text-ph-gray-300 ph-text-desktop-sm">Run Validate to see results.</div>
        )}
        {result && result.issues.length === 0 && (
          <div className="text-ph-gray-300 ph-text-desktop-sm">No issues found.</div>
        )}
        {result && result.issues.length > 0 && (
          <div className="space-y-2">
            {result.issues.map((i, idx) => (
              <div key={idx} className="flex items-start gap-2">
                <Badge>{i.severity}</Badge>
                <div className="ph-text-desktop-sm text-ph-white">
                  <div>
                    {i.ruleId}: {i.message}
                  </div>
                  {i.location && (
                    <div className="text-ph-gray-400">Location: {i.location}</div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
