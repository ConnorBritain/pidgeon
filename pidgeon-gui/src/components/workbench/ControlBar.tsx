"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui";
import { Bot, Play, CheckCircle2, MoreHorizontal, Upload, Beaker } from "lucide-react";

export type UserMode = "basic" | "guided" | "expert";

export interface ControlBarProps {
  mode: UserMode;
  onModeChange: (m: UserMode) => void;

  standard: string;
  onStandardChange: (s: string) => void;

  messageType: string;
  onMessageTypeChange: (t: string) => void;

  hasMessage: boolean;
  onGenerate: () => void;
  onValidate: () => void;
  onImport: (deidentify: boolean) => void;

  aiOpen: boolean;
  onToggleAI: () => void;

  vendorProfile?: string | null;
  onAnalyzeVendor: () => void;
  onUseVendor: () => void;

  buildGenerateCli: () => string;
  buildValidateCli: () => string;
}

export function ControlBar(props: ControlBarProps) {
  const {
    mode,
    onModeChange,
    standard,
    onStandardChange,
    messageType,
    onMessageTypeChange,
    hasMessage,
    onGenerate,
    onValidate,
    onImport,
    aiOpen,
    onToggleAI,
    vendorProfile,
    onAnalyzeVendor,
    onUseVendor,
    buildGenerateCli,
    buildValidateCli,
  } = props;

  const [menuOpen, setMenuOpen] = useState(false);

  const hl7Families = ["ADT", "ORU", "ORM", "RDE"] as const;
  const hl7EventsByFamily: Record<string, string[]> = {
    ADT: ["A01", "A03", "A04", "A08"],
    ORU: ["R01"],
    ORM: ["O01"],
    RDE: ["O11"],
  };

  const fhirResources = ["Patient", "Observation", "MedicationRequest", "Bundle"] as const;
  const ncpdpTransactions = ["NewRx", "Refill", "CancelRx"] as const;

  // Derive HL7 family/event from messageType when standard is HL7
  const currentFamily = useMemo(() => {
    if (!standard.toLowerCase().includes("hl7")) return "";
    const [fam] = (messageType || "").split("^");
    return fam || "ADT";
  }, [standard, messageType]);

  const currentEvent = useMemo(() => {
    if (!standard.toLowerCase().includes("hl7")) return "";
    const [, evt] = (messageType || "").split("^");
    const def = hl7EventsByFamily[currentFamily]?.[0] ?? "A01";
    return evt || def;
  }, [standard, messageType, currentFamily]);

  const PrimaryButton = () => (
    !hasMessage ? (
      <Button variant="primary" onClick={onGenerate} title={buildGenerateCli()}>
        <Play size={14} className="mr-2" /> Generate
      </Button>
    ) : null
  );

  return (
    <div className="flex items-center gap-desktop-sm px-desktop-md py-desktop-sm border-b border-ph-gray-800/60 bg-[color:var(--ph-desktop-panel-bg)] rounded-md">
      {/* Mode */}
      <div className="flex items-center gap-2">
        <select
          className="ph-input"
          value={mode}
          onChange={(e) => onModeChange(e.target.value as UserMode)}
        >
          <option value="basic">Basic</option>
          <option value="guided">Guided</option>
          <option value="expert">Expert</option>
        </select>
      </div>

      {/* Standard */}
      <div className="flex items-center gap-2">
        <select
          className="ph-input"
          value={standard}
          onChange={(e) => {
            const std = e.target.value;
            onStandardChange(std);
            // Reset message type appropriately
            if (std.toLowerCase().includes('hl7')) {
              onMessageTypeChange('ADT^A01');
            } else if (std.toLowerCase().includes('fhir')) {
              onMessageTypeChange('Patient');
            } else {
              onMessageTypeChange('NewRx');
            }
          }}
        >
          <option value="HL7 v2.x">HL7 v2.x</option>
          <option value="FHIR R4">FHIR R4</option>
          <option value="NCPDP">NCPDP</option>
        </select>
      </div>

      {/* Dynamic type selectors based on standard */}
      {standard.toLowerCase().includes('hl7') && (
        <>
          <div>
            <select
              className="ph-input"
              value={currentFamily}
              onChange={(e) => {
                const fam = e.target.value;
                const evt = hl7EventsByFamily[fam]?.[0] ?? 'A01';
                onMessageTypeChange(`${fam}^${evt}`);
              }}
            >
              {hl7Families.map((f) => (
                <option key={f} value={f}>{f}</option>
              ))}
            </select>
          </div>
          <div>
            <select
              className="ph-input"
              value={currentEvent}
              onChange={(e) => {
                const evt = e.target.value;
                onMessageTypeChange(`${currentFamily}^${evt}`);
              }}
            >
              {(hl7EventsByFamily[currentFamily] ?? ['A01']).map((ev) => (
                <option key={ev} value={ev}>{ev}</option>
              ))}
            </select>
          </div>
        </>
      )}

      {standard.toLowerCase().includes('fhir') && (
        <div>
          <select
            className="ph-input"
            value={messageType}
            onChange={(e) => onMessageTypeChange(e.target.value)}
          >
            {fhirResources.map((r) => (
              <option key={r} value={r}>{r}</option>
            ))}
          </select>
        </div>
      )}

      {standard.toLowerCase().includes('ncpdp') && (
        <div>
          <select
            className="ph-input"
            value={messageType}
            onChange={(e) => onMessageTypeChange(e.target.value)}
          >
            {ncpdpTransactions.map((t) => (
              <option key={t} value={t}>{t}</option>
            ))}
          </select>
        </div>
      )}

      {/* Spacer */}
      <div className="flex-1" />

      {/* Vendor badge (only when active) */}
      {vendorProfile ? (
        <span className="px-2 py-1 rounded bg-ph-graphite-700 text-ph-white ph-text-desktop-xs" title="Active vendor profile">
          {vendorProfile}
        </span>
      ) : null}

      {/* Overflow menu */}
      <div className="relative">
        <button
          type="button"
          aria-label="More"
          className="ph-text-desktop-sm text-ph-gray-500 hover:text-ph-gray-300 inline-flex items-center"
          onClick={() => setMenuOpen((prev: boolean) => !prev)}
        >
          <MoreHorizontal size={18} />
        </button>
        {menuOpen && (
          <div className="absolute right-0 mt-2 w-56 rounded-md border border-ph-gray-800 bg-[color:var(--ph-desktop-panel-bg)] shadow-ph-md z-10">
            <button className="w-full text-left px-3 py-2 hover:bg-ph-gray-800 ph-text-desktop-sm" onClick={() => { setMenuOpen(false); onImport(true); }}>
              <Upload size={14} className="inline mr-2" /> Import (De-identify)
            </button>
            <button className="w-full text-left px-3 py-2 hover:bg-ph-gray-800 ph-text-desktop-sm" onClick={() => { setMenuOpen(false); onAnalyzeVendor(); }}>
              <Beaker size={14} className="inline mr-2" /> Analyze Vendor…
            </button>
            <button className="w-full text-left px-3 py-2 hover:bg-ph-gray-800 ph-text-desktop-sm" onClick={() => { setMenuOpen(false); onUseVendor(); }}>
              Use Vendor Profile
            </button>
            <div className="h-px bg-ph-gray-800 my-1" />
            <button className="w-full text-left px-3 py-2 hover:bg-ph-gray-800 ph-text-desktop-sm" title={hasMessage ? buildValidateCli() : buildGenerateCli()} onClick={() => setMenuOpen(false)}>
              Copy CLI Command
            </button>
          </div>
        )}
      </div>

      {/* Primary Action */}
      <div className="flex items-center gap-2">
        <PrimaryButton />
      </div>

      {/* AI toggle */}
      <div>
        <button
          type="button"
          aria-label="AI Panel"
          className={`inline-flex items-center justify-center w-9 h-9 rounded-md border ${aiOpen ? 'border-ph-blue-400 text-ph-blue-400' : 'border-ph-gray-800 text-ph-gray-500 hover:text-ph-gray-300'}`}
          onClick={onToggleAI}
          title={aiOpen ? 'Hide AI Panel' : 'Show AI Panel'}
        >
          <Bot size={16} />
        </button>
      </div>
    </div>
  );
}
