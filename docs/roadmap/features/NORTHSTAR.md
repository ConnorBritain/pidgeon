Pidgeon Feature Roadmap & Requirements

north star

Realistic scenario testing without the legal/compliance nightmare of using real patient data

Focus: help teams test and troubleshoot interfaces confidently by enabling synthetic + de-identified data, workflow simulation, and fast issue resolution.

⸻

p0 — mvp (next 6–10 weeks)

core engines
	•	HL7 v2 generator + validator (strict & compatibility modes)
	•	Prebuilt packs: ADT, ORU, RDE/RXO/RXR.
	•	FHIR R4 starter set: Patient, Encounter, Observation, Medication, MedicationRequest.
	•	NCPDP skeleton: parse/echo, limited generation.

requirements:
	•	Deterministic message generation.
	•	CLI and GUI support.
	•	Validation with clear error output.

on-prem de-ident
	•	Import real messages, replace identifiers while preserving scenario integrity.
	•	Must run fully on-prem; optional lightweight local models.

requirements:
	•	Deterministic ID remapping.
	•	Date shifting.
	•	Cross-message consistency.

workflow wizard (mvp)
	•	Guided flow: base patient → scenario selection → vendor config → message generation → validation checklists.

requirements:
	•	Minimal GUI to select scenarios.
	•	Checklist output for testing.

diff + ai triage (mvp)
	•	Side-by-side message diff.
	•	AI suggestions with confidence scores.

requirements:
	•	HL7 field-level diff.
	•	Initial FHIR JSON tree diff.
	•	Output: suggested fix (e.g. field mapping issues).

vendor configuration patterns (lite)
	•	Epic/Cerner/Meditech-style baselines.
	•	Save inferred patterns from imports.

requirements:
	•	Pattern inference algorithm.
	•	Library storage and retrieval.

packaging & gtm
	•	CLI: always free.
	•	Community GUI: free with capped messages; de-identified telemetry feeds improvements.
	•	Pro: unlimited gen/validate, vendor packs, Workflow Wizard, Diff+Triage.
	•	Enterprise: on-prem, SSO, audit, private models, advanced profiles.

⸻

p1 — expand reach (months 2–4)

configuration manager (v1)
	•	Infer specs from traffic.
	•	Versioning and diff of configurations.
	•	Library of validated patterns.

vendor spec guide + trust hub (v1)
	•	Upload specs, annotate rule checks.
	•	Link validation failures to spec clauses.
	•	Vendor-certified profiles form “trust hub.”

fhir r4 expansion
	•	Add Practitioner, Organization, Location, DiagnosticReport, ServiceRequest, AllergyIntolerance, Condition, Procedure.
	•	Search test harness (simulate queries against FHIR servers).

message studio (v1)
	•	Natural-language → message generation.
	•	GUI editor with field-level hints.

standards-tuned chatbot (read-only)
	•	“Explain this message.”
	•	“Map HL7 fields to FHIR resources.”

targeted packs
	•	Mirth migration test pack.
	•	Redox/network pre-flight pack.

⸻

p2 — defensible moat (months 4–9)

cross-standard transforms (ga)
	•	HL7 ↔ FHIR ↔ NCPDP conversions.
	•	Terminology integration (LOINC, RxNorm).

team & governance
	•	Projects, roles, SSO, audit, approval workflows.

profiles at scale
	•	Full IG/profile validation matrix.
	•	Cached terminology services.

trust hub (v2) & marketplace
	•	Shareable vendor packs.
	•	Consultancy blueprints.
	•	Revenue-sharing model.

⸻

what success looks like eoy
	•	3–5 design partners active.
	•	≥2 end-to-end workflows completed in Workflow Wizard.
	•	Diff+Triage adopted for daily debugging.
	•	Free→Pro conversion via multi-step Wizard, vendor packs, unlimited runs.
	•	Vendor pattern library seeded with ≥10 validated patterns.
	•	Spec Guide used in at least one external audit.
	•	Credible FHIR story: simulate, validate, and diff HL7↔FHIR for admit, labs, Rx.