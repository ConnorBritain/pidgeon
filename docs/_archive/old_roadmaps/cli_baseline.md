here’s what I’d expect a polished pidgeon --help to look like for the MVP we scoped (P0), with clear verbs, sane defaults, pro/enterprise gating markers, and discoverable examples.

pidgeon — universal healthcare interface CLI
Generate, de-identify, validate, diff, and orchestrate HL7 v2 / FHIR R4 (and basic NCPDP) test flows.

USAGE
  pidgeon [command] [subcommand] [options]

CORE COMMANDS
  generate         Generate synthetic messages or bundles (HL7 v2, FHIR R4, basic NCPDP)
  validate         Validate messages/resources against base specs (Strict) or real-world (Compatibility)
  deident          De-identify real messages/resources (on-device), preserving referential integrity
  diff             Compare two messages/resources or folders; highlight field-level changes; suggest fixes [Pro]
  workflow         Create/run multi-step scenarios (e.g., admit→lab→rx) with checklists & artifacts [Pro]
  config           Infer, view, and manage vendor patterns & configurations (Epic/Cerner/Meditech baselines)
  tools            Utilities (faker datasets, sample generators, converters)

PLATFORM & ACCOUNT
  login            Authenticate to unlock Pro/Enterprise features (local models, cloud AI, SSO, telemetry controls)
  account          Show plan, usage limits, and feature entitlements
  open             Open the GUI (if installed) or the web console for the current project [Pro/Ent]
  completion       Shell completion for bash|zsh|fish|pwsh
  version          Print version and environment info
  help             Show help for any command (e.g., `pidgeon help generate`)

GLOBAL OPTIONS
  -p, --project <path>     Project/workspace directory (default: current dir)
  -s, --standard <name>    Target standard: hl7|fhir|ncpdp (default: hl7)
  -o, --output <path>      Write result(s) to file or folder (default: stdout when safe)
  -f, --format <fmt>       Output format: auto|hl7|json|ndjson (default: auto)
  -v, --verbose            Verbose logs
      --quiet              Minimal output (errors only)
      --color <when>       auto|always|never (default: auto)
      --no-telemetry       Disable anonymous usage telemetry for this run
      --config <path>      Override user config (default: ~/.pidgeon/config.json)
      --profile <name>     Use named profile from config (e.g., “dev”, “customer-A”)
      --help               Show this help and exit

ENV VARS
  PIDGEON_PROFILE=<name>         Default profile
  PIDGEON_STD=<hl7|fhir|ncpdp>   Default standard
  PIDGEON_API_KEY=<token>        Pro/Enterprise API access
  PIDGEON_NO_TELEMETRY=1         Disable telemetry

EXAMPLES
  # Quick start: generate one HL7 ADT and print to console
  pidgeon generate message --type ADT^A01

  # Generate 10 lab results (HL7 ORU) to a file
  pidgeon generate message --type ORU^R01 --count 10 --output labs.hl7

  # Generate a FHIR R4 Observation bundle (ndjson for bulk)
  pidgeon generate bundle --standard fhir --resource Observation --count 100 --format ndjson -o obs.ndjson

  # Validate with strict rules vs real-world compatibility
  pidgeon validate --file labs.hl7 --mode strict
  pidgeon validate --file labs.hl7 --mode compatibility

  # De-identify a folder of HL7 files, preserve links & shift dates
  pidgeon deident --in ./samples --out ./samples_synthetic --date-shift 30d

  # Diff two runs and get remediation hints (Pro)
  pidgeon diff --left ./envA --right ./envB --report diff.html

  # Run a guided workflow (admit→lab→rx) and emit a checklist (Pro)
  pidgeon workflow wizard

  # Infer a vendor pattern from sample traffic and save it
  pidgeon config analyze --samples ./inbox --save epic_er.json


⸻

pidgeon generate --help

Generate synthetic messages or resource bundles.

USAGE
  pidgeon generate <message|bundle> [options]

SUBCOMMANDS
  message   Generate a single message/resource type repeatedly
  bundle    Generate a multi-resource FHIR Bundle or multi-message HL7 batch

COMMON OPTIONS
  -t, --type <name>            HL7: ADT^A01|ORU^R01|RDE^O11 ...   FHIR: Patient|Observation|MedicationRequest ...
  -n, --count <int>            Number to generate (default: 1)
      --seed <int>             Deterministic seed for reproducible data
      --vendor <name>          Apply vendor pattern (e.g., epic|cerner|meditech or custom file)
      --profile <path>         Use a saved config/profile JSON for shaping fields
      --fields <path>          Field template (JSONPath overrides)
      --format <fmt>           auto|hl7|json|ndjson (default: auto)
  -o,  --output <path>         File/folder for output (default: stdout for single, ./out for multiple)

AI MODES (gated)
      --mode procedural        Deterministic generator (default; Free)
      --mode local-ai          Use local model for realistic variations [Pro]
      --mode api-ai            Use cloud completion endpoint [Pro/Ent]
      --model <name>           Model selector (e.g., llama-local, gpt-4o-mini) [Pro/Ent]

EXAMPLES
  pidgeon generate message --type ADT^A01
  pidgeon generate message --standard fhir --type Observation --count 50 -o obs.ndjson --format ndjson
  pidgeon generate bundle --standard fhir --kind message --header ADT --entries Patient,Encounter

pidgeon validate --help

Validate messages/resources against base specs or real-world rules.

USAGE
  pidgeon validate [options]

OPTIONS
  -f, --file <path>            File to validate (HL7|JSON|NDJSON)
      --folder <path>          Validate all files in folder (recursively)
      --mode <name>            strict|compatibility (default: strict)
      --ig <path|url>          FHIR Implementation Guide/Profile to enforce (US-Core, etc.) [Pro/Ent]
      --report <path>          Write HTML/JSON report with failures & guidance

EXAMPLES
  pidgeon validate --file msg.hl7 --mode compatibility
  pidgeon validate --file patient.json --ig uscore-4.0.0 --report val.html

pidgeon deident --help

De-identify HL7/FHIR/NCPDP content on-device. Preserves referential integrity across messages.

USAGE
  pidgeon deident [options]

OPTIONS
      --in <path>              File or folder of source data
      --out <path>             Output file/folder (created if missing)
      --date-shift <offset>    Shift dates by +/-N days (e.g., 30d, -14d)
      --keep-ids <keys>        Comma-list of identifiers to keep unhashed (e.g., visitId)
      --salt <string>          Salt for deterministic hashing
      --preview                Show sample before/after rows without writing files

EXAMPLES
  pidgeon deident --in ./samples --out ./synthetic --date-shift 21d --salt "team-seed"

pidgeon diff --help  [Pro]

Compare two artifacts (files or folders). Field-aware for HL7; JSON-tree for FHIR. Emits hints.

USAGE
  pidgeon diff --left <path> --right <path> [options]

OPTIONS
      --left <path>            Baseline file/folder
      --right <path>           Candidate file/folder
      --ignore <paths>         Comma-list of fields/segments to ignore (e.g., MSH-7, PID.3[*].assigningAuthority)
      --report <path>          HTML/JSON diff report with triage hints
      --severity <min>         hint|warn|error (default: hint)

EXAMPLES
  pidgeon diff --left ./envA --right ./envB --ignore MSH-7,Pv1.44 --report diff.html

pidgeon workflow --help  [Pro]

Create and execute multi-step workflows (e.g., Admit → Lab → Rx) with artifacts & checklists.

USAGE
  pidgeon workflow <wizard|run|list|show> [options]

SUBCOMMANDS
  wizard                    Interactive guided setup (recommended)
  run     --file <path>     Execute a saved scenario file (YAML/JSON)
  list                      List saved workflows in the project
  show    --name <id>       Show a workflow’s steps and outputs

COMMON OPTIONS
      --vendor <name>       Apply vendor pattern for all steps
      --count <int>         Repeat scenario N times (default: 1)
      --emit <what>         messages|bundles|checklist|all (default: all)
  -o,  --output <path>      Output folder (default: ./run_<timestamp>)

EXAMPLES
  pidgeon workflow wizard
  pidgeon workflow run --file ./scenarios/admit_lab_rx.yml -o ./runs/sep04

pidgeon config --help

Infer, inspect, and manage vendor configuration patterns.

USAGE
  pidgeon config <analyze|list|show|use|export|diff> [options]

SUBCOMMANDS
  analyze  --samples <path> [--save <file>]   Infer pattern from sample traffic (files/folder)
  list                                        List saved patterns in the project
  show     --name <file|id>                   Show details of a pattern (rules, examples)
  use      --name <file|id>                   Set active pattern for this project/profile
  export   --name <id> --out <file>           Export pattern as JSON
  diff     --left <id> --right <id>           Compare two patterns (rules and impacts)

EXAMPLES
  pidgeon config analyze --samples ./inbox --save epic_er.json
  pidgeon config use --name epic_er.json

pidgeon tools --help

Handy utilities for test data and conversions.

USAGE
  pidgeon tools <faker|samples|convert> [options]

SUBCOMMANDS
  faker      Print fake demographics/IDs for scripting (supports --seed)
  samples    Emit canonical samples for a type (good for smoke tests)
  convert    hl7↔json (line-safe HL7 to JSON representation and back)

EXAMPLES
  pidgeon tools faker --seed 42
  pidgeon tools samples --type ORU^R01 --count 3
  pidgeon tools convert --in msg.hl7 --out msg.json


⸻

Notes on UX & discoverability (baked into the help)
	•	Defaults everywhere: --standard hl7, --mode procedural/strict, single message → stdout; multiple → ./out.
	•	Profiles & config: ~/.pidgeon/config.json + --profile keep daily commands short; pidgeon login stores auth & plan.
	•	Interactive paths: pidgeon workflow wizard and selective prompts (when safe) avoid “flag hell.”
	•	Pro/Ent markers: clearly shown in help so users know what upgrades unlock.
	•	Output formats: --format ndjson for FHIR bulk; HTML reports for validate/diff.
	•	Shell completion: pidgeon completion zsh etc.

----

