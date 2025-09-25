"use client";

import { Button, Card, CardContent, CardHeader, CardTitle } from "@/components/ui";
import { QuickTemplates } from "@/components/workbench/QuickTemplates";
import { Play, Upload } from "lucide-react";

export interface EmptyStateProps {
  onQuickGenerate: () => void;
  onImportDeidentify: () => void;
  onPickTemplate?: (standard: string, messageType: string) => void;
  primaryLabel?: string;
}

export function EmptyState({ onQuickGenerate, onImportDeidentify, onPickTemplate, primaryLabel }: EmptyStateProps) {
  return (
    <div className="w-full flex items-start justify-center">
      <Card className="w-full max-w-xl">
        <CardHeader>
          <CardTitle className="text-ph-white">Healthcare Message Workbench</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-desktop-lg space-y-desktop-md">
            <div className="ph-text-desktop-base text-ph-gray-300">
              Get started by generating a message or importing one (de-identified).
            </div>
            <div className="flex items-center justify-center gap-desktop-sm">
              <Button variant="primary" onClick={onQuickGenerate}>
                <Play size={16} className="mr-2" /> {primaryLabel ?? 'Generate'}
              </Button>
              <Button variant="secondary" onClick={onImportDeidentify}>
                <Upload size={16} className="mr-2" /> Import (De-identify)
              </Button>
            </div>
          </div>

          {/* Quick templates to tastefully fill whitespace */}
          {onPickTemplate && (
            <div className="pt-desktop-md text-left">
              <div className="ph-text-desktop-sm text-ph-gray-400 mb-desktop-sm">Quick templates</div>
              <QuickTemplates onPick={onPickTemplate} />
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
