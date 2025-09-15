"use client";

export interface RecipeSelectProps {
  standard: string;
  messageType: string;
  onChange: (standard: string, messageType: string) => void;
}

const HL7_TYPES = ["ADT^A01", "ORU^R01", "ORM^O01", "RDE^O11"] as const;
const FHIR_TYPES = ["Patient", "Observation", "MedicationRequest", "Bundle"] as const;
const NCPDP_TYPES = ["NewRx", "Refill", "CancelRx"] as const;

export function RecipeSelect({ standard, messageType, onChange }: RecipeSelectProps) {
  const value = `${standard}::${messageType}`;

  const options: Array<{ label: string; std: string; types: readonly string[] }> = [
    { label: "HL7 v2.x", std: "HL7 v2.x", types: HL7_TYPES },
    { label: "FHIR R4", std: "FHIR R4", types: FHIR_TYPES },
    { label: "NCPDP", std: "NCPDP", types: NCPDP_TYPES },
  ];

  const handle = (v: string) => {
    const [std, type] = v.split("::");
    onChange(std, type);
  };

  return (
    <select className="ph-input min-w-[220px]" value={value} onChange={(e) => handle(e.target.value)}>
      {options.map((group) => (
        <optgroup key={group.std} label={group.label}>
          {group.types.map((t) => (
            <option key={t} value={`${group.std}::${t}`}>{`${group.label} • ${t}`}</option>
          ))}
        </optgroup>
      ))}
    </select>
  );
}
