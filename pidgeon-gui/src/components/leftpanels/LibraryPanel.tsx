"use client";

import { useMemo, useState } from "react";

type Tab = "hl7" | "fhir" | "ncpdp";

const HL7_SEGMENTS = [
  { id: "MSH", label: "MSH — Message Header" },
  { id: "PID", label: "PID — Patient Identification" },
  { id: "PV1", label: "PV1 — Patient Visit" },
  { id: "ORC", label: "ORC — Common Order" },
  { id: "OBR", label: "OBR — Observation Request" },
  { id: "OBX", label: "OBX — Observation Result" },
];

const FHIR_RESOURCES = [
  { id: "Patient", label: "Patient" },
  { id: "Observation", label: "Observation" },
  { id: "Encounter", label: "Encounter" },
  { id: "MedicationRequest", label: "MedicationRequest" },
  { id: "Bundle", label: "Bundle" },
];

const NCPDP_TRANSACTIONS = [
  { id: "NewRx", label: "NewRx" },
  { id: "Refill", label: "Refill" },
  { id: "CancelRx", label: "CancelRx" },
];

export function LibraryPanel() {
  const [tab, setTab] = useState<Tab>("hl7");
  const [query, setQuery] = useState("");

  const filteredAll = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return [] as Array<{ group: string; id: string; label: string }>;
    const pick = (group: string, arr: { id: string; label: string }[]) =>
      arr
        .filter((x) => x.id.toLowerCase().includes(q) || x.label.toLowerCase().includes(q))
        .map((x) => ({ group, ...x }));
    return [
      ...pick("HL7 v2.x", HL7_SEGMENTS),
      ...pick("FHIR R4", FHIR_RESOURCES),
      ...pick("NCPDP", NCPDP_TRANSACTIONS),
    ];
  }, [query]);

  const groupData = tab === "hl7" ? HL7_SEGMENTS : tab === "fhir" ? FHIR_RESOURCES : NCPDP_TRANSACTIONS;

  const onClickItem = (group: string, itemId: string) => {
    // Placeholder: copy the identifier to clipboard for now
    if (typeof navigator !== "undefined" && navigator.clipboard) {
      navigator.clipboard.writeText(`${group}:${itemId}`).catch(() => {});
    }
  };

  return (
    <div className="p-desktop-sm space-y-desktop-sm">
      <div className="ph-text-desktop-sm text-ph-gray-300">Standards Library</div>
      <input
        className="ph-input w-full"
        placeholder="Search across HL7, FHIR, NCPDP (e.g., PID-5, Patient.name)"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
      />

      {/* Tabs */}
      <div className="flex gap-1">
        {([
          { id: "hl7", label: "HL7 v2.x" },
          { id: "fhir", label: "FHIR R4" },
          { id: "ncpdp", label: "NCPDP" },
        ] as Array<{ id: Tab; label: string }>).map((t) => (
          <button
            key={t.id}
            className={`ph-btn ph-btn-secondary ${tab === t.id ? 'ring-1 ring-ph-blue-400' : ''}`}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Search results across groups */}
      {query.trim() && (
        <div className="space-y-1">
          <div className="ph-text-desktop-xs text-ph-gray-400">Search results</div>
          <div className="max-h-64 overflow-auto">
            {filteredAll.length === 0 && (
              <div className="ph-text-desktop-xs text-ph-gray-500">No matches</div>
            )}
            {filteredAll.map((r, idx) => (
              <button
                key={`${r.group}-${r.id}-${idx}`}
                className="w-full text-left px-2 py-1 rounded hover:bg-ph-gray-100"
                onClick={() => onClickItem(r.group, r.id)}
                title="Click to copy identifier"
              >
                <span className="ph-text-desktop-xs text-ph-gray-500 mr-2">{r.group}</span>
                <span className="ph-text-desktop-sm text-ph-gray-300">{r.id}</span>
                {r.label && <span className="ph-text-desktop-xs text-ph-gray-500 ml-2">{r.label}</span>}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Grouped browser */}
      {!query.trim() && (
        <div className="space-y-1">
          <div className="ph-text-desktop-xs text-ph-gray-400">{tab === 'hl7' ? 'Segments' : tab === 'fhir' ? 'Resources' : 'Transactions'}</div>
          <div className="max-h-64 overflow-auto">
            {groupData.map((x) => (
              <button
                key={x.id}
                className="w-full text-left px-2 py-1 rounded hover:bg-ph-gray-100"
                onClick={() => onClickItem(tab === 'hl7' ? 'HL7 v2.x' : tab === 'fhir' ? 'FHIR R4' : 'NCPDP', x.id)}
                title="Click to copy identifier"
              >
                <span className="ph-text-desktop-sm text-ph-gray-300">{x.id}</span>
                {x.label && <span className="ph-text-desktop-xs text-ph-gray-500 ml-2">{x.label}</span>}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
