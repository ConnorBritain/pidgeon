"use client";

import { useState } from "react";

export interface QuickTemplatesProps {
  onPick: (standard: string, messageType: string) => void;
}

const HL7_COMMON = [
  { label: "ADT^A01 — Admit", type: "ADT^A01" },
  { label: "ADT^A03 — Discharge", type: "ADT^A03" },
  { label: "ORU^R01 — Result", type: "ORU^R01" },
  { label: "ORM^O01 — Order", type: "ORM^O01" },
];

const FHIR_COMMON = [
  { label: "Patient", type: "Patient" },
  { label: "Observation", type: "Observation" },
  { label: "MedicationRequest", type: "MedicationRequest" },
  { label: "Bundle", type: "Bundle" },
];

const NCPDP_COMMON = [
  { label: "NewRx", type: "NewRx" },
  { label: "Refill", type: "Refill" },
  { label: "CancelRx", type: "CancelRx" },
];

function Box({ title, children, open, onToggle }: { title: string; children: React.ReactNode; open: boolean; onToggle: () => void }) {
  return (
    <div className="ph-card">
      <button className="w-full text-left p-desktop-md ph-text-desktop-sm font-medium" onClick={onToggle}>
        {title}
      </button>
      {open && <div className="px-desktop-md pb-desktop-md space-y-1">{children}</div>}
    </div>
  );
}

export function QuickTemplates({ onPick }: QuickTemplatesProps) {
  const [open, setOpen] = useState<{ hl7: boolean; fhir: boolean; ncpdp: boolean }>({ hl7: true, fhir: false, ncpdp: false });

  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-desktop-sm">
      <Box title="HL7 v2.x" open={open.hl7} onToggle={() => setOpen({ ...open, hl7: !open.hl7 })}>
        {HL7_COMMON.map((t) => (
          <button key={t.type} className="ph-btn ph-btn-secondary w-full" onClick={() => onPick("HL7 v2.x", t.type)}>
            {t.label}
          </button>
        ))}
      </Box>

      <Box title="FHIR R4" open={open.fhir} onToggle={() => setOpen({ ...open, fhir: !open.fhir })}>
        {FHIR_COMMON.map((t) => (
          <button key={t.type} className="ph-btn ph-btn-secondary w-full" onClick={() => onPick("FHIR R4", t.type)}>
            {t.label}
          </button>
        ))}
      </Box>

      <Box title="NCPDP" open={open.ncpdp} onToggle={() => setOpen({ ...open, ncpdp: !open.ncpdp })}>
        {NCPDP_COMMON.map((t) => (
          <button key={t.type} className="ph-btn ph-btn-secondary w-full" onClick={() => onPick("NCPDP", t.type)}>
            {t.label}
          </button>
        ))}
      </Box>
    </div>
  );
}
