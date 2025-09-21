Pidgeon CLI Documentation

Overview

The pidgeon CLI is the universal healthcare interoperability toolkit for developers and IT teams. It provides powerful but elegant commands for generating, de-identifying, validating, diffing, and orchestrating HL7 v2, FHIR R4, and NCPDP test flows.

⸻

Quickstart Cheat Sheet

# Generate a single HL7 ADT message
pidgeon generate message --type ADT^A01

# Generate 10 ORU lab results to a file
pidgeon generate message --type ORU^R01 --count 10 -o labs.hl7

# Generate 50 FHIR Observations in NDJSON
pidgeon generate message --standard fhir --type Observation --count 50 --format ndjson -o obs.ndjson

# Validate a file in strict vs compatibility modes
pidgeon validate --file labs.hl7 --mode strict
pidgeon validate --file labs.hl7 --mode compatibility

# De-identify HL7 messages (shift dates by 30 days)
pidgeon deident --in ./samples --out ./synthetic --date-shift 30d

# Diff two folders and generate an HTML report (Pro)
pidgeon diff --left ./envA --right ./envB --report diff.html

# Run a guided workflow wizard (Pro)
pidgeon workflow wizard

# Infer vendor patterns from traffic
pidgeon config analyze --samples ./inbox --save epic_config.json


⸻

Global Help

pidgeon — universal healthcare interface CLI

USAGE
  pidgeon [command] [subcommand] [options]

CORE COMMANDS
  generate     Generate synthetic messages or bundles
  validate     Validate messages/resources (strict or compatibility)
  deident      De-identify data on-device, preserving links
  diff         Compare two messages/resources; triage differences [Pro]
  workflow     Create/run multi-step scenarios with checklists [Pro]
  config       Infer, manage, and apply vendor patterns
  tools        Utilities (faker, sample emitters, converters)

PLATFORM
  login        Authenticate to unlock Pro/Enterprise features
  account      Show plan and feature entitlements
  open         Launch GUI/web console [Pro/Ent]
  completion   Shell completions for bash/zsh/fish/pwsh
  version      Show version and environment info
  help         Show help for a command (e.g. `pidgeon help generate`)


⸻

Command Reference

pidgeon generate

Generate synthetic HL7, FHIR, or NCPDP messages.

Usage:

pidgeon generate <message|bundle> [options]

Options:
	•	-t, --type <name>: Message/resource type (e.g. ADT^A01, Observation)
	•	-n, --count <int>: Number to generate (default: 1)
	•	--vendor <name>: Apply vendor pattern
	•	--mode <proc|local-ai|api-ai>: Generation mode (Free/Pro/Ent)
	•	-o, --output <path>: File/folder to write (stdout if single)
	•	--format <auto|hl7|json|ndjson>: Output format

⸻

pidgeon validate

Validate messages/resources.

Usage:

pidgeon validate [options]

Options:
	•	-f, --file <path>: Input file
	•	--folder <path>: Validate all files in folder
	•	--mode <strict|compatibility>: Validation mode (default: strict)
	•	--ig <path|url>: FHIR Implementation Guide (Pro/Ent)
	•	--report <path>: Write HTML/JSON report

⸻

pidgeon deident

De-identify HL7/FHIR/NCPDP content.

Usage:

pidgeon deident [options]

Options:
	•	--in <path>: Input file/folder
	•	--out <path>: Output file/folder
	•	--date-shift <offset>: Shift dates by ±N days
	•	--keep-ids <keys>: Keep specific identifiers unchanged
	•	--salt <string>: Salt for deterministic hashing

⸻

pidgeon diff [Pro]

Compare two artifacts and suggest fixes.

Usage:

pidgeon diff --left <path> --right <path> [options]

Options:
	•	--ignore <fields>: Fields/segments to ignore
	•	--report <path>: Generate HTML/JSON diff report

⸻

pidgeon workflow [Pro]

Create and run multi-step workflows.

Usage:

pidgeon workflow <wizard|run|list|show>

Subcommands:
	•	wizard: Interactive guided scenario setup
	•	run --file <path>: Execute a saved scenario
	•	list: List saved workflows
	•	show --name <id>: Show workflow details

⸻

pidgeon config

Manage vendor patterns.

Usage:

pidgeon config <analyze|list|show|use|export|diff>

Subcommands:
	•	analyze --samples <path>: Infer pattern from sample traffic
	•	list: List saved patterns
	•	show --name <id>: Show details
	•	use --name <id>: Set active pattern
	•	export --name <id>: Export pattern to file
	•	diff --left <id> --right <id>: Compare patterns

⸻

pidgeon tools

Utilities for test data.

Subcommands:
	•	faker: Print fake demographics/IDs
	•	samples: Emit canonical message samples
	•	convert: Convert HL7 ↔ JSON

⸻

Global Options
	•	-p, --project <path>: Project directory (default: current dir)
	•	-s, --standard <name>: hl7|fhir|ncpdp (default: hl7)
	•	-o, --output <path>: Write to file/folder
	•	-f, --format <fmt>: auto|hl7|json|ndjson
	•	-v, --verbose: Verbose logs
	•	--quiet: Minimal output
	•	--no-telemetry: Disable telemetry
	•	--profile <name>: Use named profile

⸻

Environment Variables
	•	PIDGEON_PROFILE=<name>: Default profile
	•	PIDGEON_STD=<hl7|fhir|ncpdp>: Default standard
	•	PIDGEON_API_KEY=<token>: API key for Pro/Ent
	•	PIDGEON_NO_TELEMETRY=1: Disable telemetry