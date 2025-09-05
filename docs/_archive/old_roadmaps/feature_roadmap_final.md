pidgeon feature roadmap

north star

Deliver the universal healthcare interoperability toolkit that solves real-world interface pain:
	•	Safe de-identified test data
	•	Fast synthetic message/workflow generation
	•	Accurate validation & diffing
	•	Smart config inference and vendor patterns
	•	An elegant CLI → GUI growth path

⸻

p0 — mvp (next 6–10 weeks)

goal: prove value to design partners; deliver essential daily-use workflows for consultants + IT teams.
	•	CLI core engine
	•	HL7v2 generator + validator (strict + compatibility)
	•	FHIR R4 starter (Patient, Encounter, Observation, Medication, MedicationRequest)
	•	NCPDP skeleton (basic parse/echo)
	•	Sensible defaults, profiles, short flags
	•	on-prem de-identification
	•	Deterministic ID remap + date shifting
	•	Preserve cross-message relationships
	•	workflow wizard (mvp)
	•	Guided flow (admit → lab → Rx)
	•	Step prompts or scenario file input
	•	Output validation checklists
	•	diff + triage (mvp)
	•	HL7 field-level diff + basic FHIR JSON tree diff
	•	AI-assisted “why it broke” with confidence hints
	•	vendor config patterns (lite)
	•	Baseline profiles (Epic, Cerner, Meditech)
	•	Infer & save from sample messages
	•	packaging
	•	Free CLI + capped Community GUI
	•	Pro unlocks unlimited gen/validate, vendor packs, Wizard, Diff+Triage
	•	Enterprise adds SSO, on-prem, private models

⸻

p1 — expand (months 2–4)

goal: lock in early logos, expand FHIR reach, build stickiness.
	•	configuration manager (v1)
	•	Infer specs from traffic
	•	Version/diff configs
	•	Library of validated patterns
	•	vendor spec guide + trust hub (v1)
	•	Upload specs, annotate checks
	•	Link validation errors to spec clauses
	•	Shareable attested profiles
	•	FHIR expansion
	•	Add Practitioner, Organization, Location, DiagnosticReport, ServiceRequest, AllergyIntolerance, Condition, Procedure
	•	Search harness for FHIR servers
	•	message studio (v1)
	•	NL → message generation
	•	Structured editor with hints
	•	standards-tuned chatbot (read-only)
	•	Explain messages, map HL7↔FHIR
	•	targeted packs
	•	Mirth migration regression kit
	•	Redox/network pre-flight

⸻

p2 — defensible moat (months 4–9)

goal: become indispensable enterprise quality layer.
	•	cross-standard transforms (GA)
	•	HL7 ↔ FHIR ↔ NCPDP with terminology integration (LOINC, RxNorm)
	•	team & governance
	•	Projects, roles, SSO, audit, approval workflows
	•	profiles at scale
	•	Full IG/profile validation matrix
	•	Cached terminology services
	•	trust hub (v2) & marketplace
	•	Share vendor packs, consultancy blueprints
	•	Revenue share ecosystem

⸻

success metrics (eoy)
	•	3–5 design partners actively using Wizard + Diff
	•	≥2 end-to-end workflows completed
	•	Free→Pro conversion via multi-step Wizard, vendor packs
	•	Vendor pattern library with ≥10 validated profiles
	•	Spec Guide used in an external audit
	•	FHIR credibility: generate/validate/diff HL7↔FHIR for admit, labs, Rx

⸻

✅ This roadmap ensures Pidgeon is coherent and complementary:
	•	P0: solves core pain (safe test data, realistic message gen, fast debugging).
	•	P1: adds config intelligence and FHIR breadth, boosting stickiness.
	•	P2: builds moat via transforms, profiles, governance, ecosystem.