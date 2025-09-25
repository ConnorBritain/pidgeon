"use client";

import { useState } from "react";
import { Button } from "@/components/ui";
import { CheckCircle2, MoreHorizontal, Download, Code2, Eye } from "lucide-react";

export interface EditorHeaderProps {
  label: string; // e.g., "HL7 v2.x • ADT^A01" or "FHIR R4 • Patient"
  onValidate: () => void;
  buildValidateCli: () => string;
  onExport?: () => void;
  inspectorOpen?: boolean;
  onToggleInspector?: () => void;
}

export function EditorHeader({ label, onValidate, buildValidateCli, onExport, inspectorOpen, onToggleInspector }: EditorHeaderProps) {
  const [open, setOpen] = useState(false);

  return (
    <div className="flex items-center justify-between px-desktop-md py-desktop-sm border-b border-ph-gray-800/60 bg-[color:var(--ph-desktop-panel-bg)] rounded-md sticky top-0 z-10">
      <div className="ph-text-desktop-sm text-ph-gray-300">
        {label}
      </div>

      <div className="flex items-center gap-desktop-sm">
        {onToggleInspector && (
          <button
            type="button"
            className={`inline-flex items-center justify-center w-9 h-9 rounded-md border ${inspectorOpen ? 'border-ph-blue-400 text-ph-blue-400' : 'border-ph-gray-800 text-ph-gray-500 hover:text-ph-gray-300'}`}
            onClick={onToggleInspector}
            title={inspectorOpen ? 'Hide Inspector' : 'Show Inspector'}
          >
            <Eye size={16} />
          </button>
        )}
        <Button variant="primary" onClick={onValidate} title={buildValidateCli()}>
          <CheckCircle2 size={14} className="mr-2" /> Validate
        </Button>

        <div className="relative">
          <button
            type="button"
            aria-label="More"
            className="inline-flex items-center ph-text-desktop-sm text-ph-gray-500 hover:text-ph-gray-300"
            onClick={() => setOpen((p) => !p)}
          >
            <MoreHorizontal size={18} />
          </button>
          {open && (
            <div className="absolute right-0 mt-2 w-52 rounded-md border border-ph-gray-800 bg-[color:var(--ph-desktop-panel-bg)] shadow-ph-md z-10">
              <button
                className="w-full text-left px-3 py-2 hover:bg-ph-gray-800 ph-text-desktop-sm"
                onClick={() => { setOpen(false); onExport?.(); }}
              >
                <Download size={14} className="inline mr-2" /> Export
              </button>
              <button
                className="w-full text-left px-3 py-2 hover:bg-ph-gray-800 ph-text-desktop-sm"
                title={buildValidateCli()}
                onClick={() => setOpen(false)}
              >
                <Code2 size={14} className="inline mr-2" /> Copy CLI Command
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
