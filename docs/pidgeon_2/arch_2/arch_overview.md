1. architecture overview (the big map)

[ Interface Engines: Mirth, Rhapsody, Cloverleaf ]
   │
   └──> Edge Agent (Go/Rust binary + engine plugin)
         - De-ID PHI, normalize events
         - Local buffer (BadgerDB/SQLite)
         - Send to Gateway via gRPC/HTTPS
   │
[ Ingest Gateway ] (ASP.NET Core or Go API)
   - Auth (OIDC, mTLS)
   - Writes to Stream + Metadata DB
   │
[ Stream Bus ] (Redpanda, Kafka API compatible)
   - Topic per org/engine/interface
   │
[ Stream Processors ]
   - Metrics aggregator → ClickHouse
   - Message indexer → OpenSearch
   - Lake writer → S3/MinIO (Parquet)
   │
[ Storage ]
   - Postgres (auth, config, alerts, vendor profiles)
   - ClickHouse (time-series metrics, rollups)
   - OpenSearch (search logs/messages)
   - S3/MinIO + DuckDB/Trino (long-term analytics)
   │
[ Services ]
   - Validation service (HL7 engine in C#, HAPI FHIR in JVM sidecar)
   - Alert service (thresholds, anomaly detection, notifications)
   - Report service (compliance/BI dashboards)
   │
[ UI + CLI ]
   - Next.js (React, Tailwind, shadcn/ui) web console
   - Tauri desktop (later if demand)
   - CLI (Go/C#), open-core


⸻

2. tech choices & why
	•	Edge agent in Go/Rust: tiny, cross-compiled binaries; easy to deploy in hospital environments; built-in concurrency + resilience.
	•	Redpanda instead of Kafka: Kafka-compatible but easier ops, no Zookeeper, free tier works for 100% MVP.
	•	ClickHouse: blazing-fast analytics DB, open-source, excels at metrics rollups and time-series dashboards.
	•	Postgres: relational ground truth for orgs/users/config/vendor profiles.
	•	OpenSearch: open-source Elasticsearch fork for log/message search.
	•	MinIO: S3-compatible object storage for de-ID messages, report exports.
	•	ASP.NET Core / Go: pragmatic, productive backend; stick to one for MVP (probably .NET since you’re C#-native).
	•	Next.js: battle-tested, enterprise-friendly frontend.
	•	Free all the way: all components are open-source; can scale with enterprise support later (Confluent, Elastic, etc.).

⸻

3. feature → tech mapping

Feature / Value	Tech Layer(s)
Synthetic message generation & validation	C# HL7 engine, HAPI FHIR validator sidecar (Java)
On-prem de-ID	Go agent (SHA/HMAC IDs, date shifting, SQLite buffer)
Observability (errors, latency, drift)	Agent → Gateway → Redpanda → ClickHouse + OpenSearch
Proactive alerts	Stream processor + Alert service (rules in Postgres)
Diff/triage	Validator + OpenSearch + AI (Python job, optional)
Dashboards (ops & clinical)	ClickHouse → Grafana/Next.js UI charts
Compliance/audit reports	MinIO (data), Report service (HTML/PDF), Postgres logs
Multi-engine support	Adapter plugins (Java for Rhapsody, Tcl/Python for Cloverleaf), same agent protocol
AI anomaly detection	Python/Rust job consuming Redpanda topics; outputs to Postgres/OpenSearch


⸻

4. sequence plan (90 days → 2 years)

embryonic (days 0–30): mvp plumbing
	•	Goal: working Mirth-only pipeline, single org.
	•	Deliverables:
	•	Go agent + Mirth plugin: collect channel stats, error counts; de-ID; post to gateway.
	•	Gateway API (ASP.NET Core): receive, auth (JWT, OIDC stub), write to Redpanda + Postgres.
	•	Stream processor v0: aggregate → ClickHouse (metrics).
	•	Simple Next.js UI: list channels, show metrics sparkline.
	•	User value: “I can see my Mirth interfaces on a dashboard, with errors/latency trending.”

mvp+ (days 31–90): first pilot-ready
	•	Add OpenSearch indexing of error messages (searchable by channel, type).
	•	Add alert service (threshold on error count, send email/Slack).
	•	Add vendor profile drift detection (compare message structure vs baseline).
	•	Add de-ID message storage in MinIO (for later analytics).
	•	Expand UI: alerts view, search logs, simple reports.
	•	Package: Docker Compose bundle (Postgres, Redpanda, ClickHouse, OpenSearch, MinIO, Gateway, UI).
	•	User value: “Pidgeon catches problems early, I can search logs safely, I get alerts.”

phase 2 (3–6 months): analytics & ai
	•	Build reporting service: pre-built Grafana dashboards (ClickHouse backend).
	•	Add anomaly detection (simple z-scores on metrics).
	•	Add AI triage (Python service: given error snippet, explain cause).
	•	Add validation integration: failed messages auto-fed to validation service.
	•	Package: Helm charts for k8s (cloud/hybrid), Compose remains for on-prem.
	•	User value: “I see anomalies, I get intelligent alerts, I can auto-validate failures.”

phase 3 (6–12 months): multi-engine + BI
	•	Add Rhapsody plugin adapter (Java).
	•	Add Cloverleaf SMAT reader adapter.
	•	Extend agent protocol to support multiple engine adapters.
	•	Add BI dashboards: admissions/day, labs volume, etc. (ClickHouse queries + Next.js charts).
	•	Add role-based auth (Keycloak).
	•	Add report exports (PDF/CSV).
	•	User value: “I see cross-engine metrics, clinical/ops dashboards, exportable compliance reports.”

phase 4 (12–24 months): enterprise scale
	•	Add FHIR API observability (HTTP logs → agent).
	•	Add distributed tracing across engines (correlation IDs).
	•	Add team features: SSO, RBAC, audit logs, usage quotas.
	•	Add marketplace integration (vendor packs, configs).
	•	Optimize scale: partition Redpanda by org, shard ClickHouse, scale OpenSearch.
	•	User value: “One pane of glass across engines + APIs, enterprise governance, plug-in ecosystem.”

⸻

5. packaging strategy
	•	MVP pilots: Docker Compose stack (1 VM). Single-node, all OSS.
	•	Multi-pilot: Helm chart deploy on k8s (AKS/EKS). Still all free OSS.
	•	Enterprise: Offer managed Pidgeon Cloud (multi-tenant), or on-prem k8s support. Optional enterprise licenses (Elastic, Confluent) if customers demand SLAs.

⸻

6. hinge points / boundaries
	•	Agent ↔ Gateway: stable gRPC contract (protobuf schema). This is your most critical boundary: once stable, you can add adapters without touching core.
	•	Stream Bus ↔ Processors: topics are append-only; add consumers freely. Decouple ingestion from processing logic.
	•	Processors ↔ Storage: each processor writes to one store (metrics → ClickHouse, logs → OpenSearch, messages → MinIO). Clear separation of concerns.
	•	UI ↔ API: GraphQL/REST served by Gateway; UI consumes only stable endpoints. Decouple UI from backends.
	•	Validation services: keep as sidecars (C# HL7, JVM FHIR). Treat as black boxes with REST/gRPC APIs.

⸻

7. founder nuances to know
	•	Streams vs DBs: Streams (Redpanda) handle bursty workloads + fan-out. DBs (ClickHouse/Postgres) hold state and serve queries. Don’t overload one for the other.
	•	Columnar vs row: ClickHouse (columnar) is blazing fast for aggregates, but not good for per-record updates. Postgres is for configs, relationships, auth.
	•	Search vs analytics: OpenSearch is for free-text/fielded queries (“find all errors from X”). ClickHouse is for trends/metrics (“errors per hour”).
	•	De-ID edge importance: Always de-identify before sending off host. This wins trust and saves you HIPAA headaches.
	•	Agents: Make them resilient (local buffer, retry, offline mode). Hospital IT loves things that “just keep running.”
	•	Packaging: Hospitals prefer “one binary” or “one container” agent. Keep ops as simple as possible.
	•	OSS licensing: Choose permissive (Apache 2.0) for open-core parts. Reserve enterprise features under commercial license.

⸻

8. foolproof 90-day path (hands-on founder)
	1.	Week 1–2: Scaffold Go agent + .NET gateway + Redpanda + Postgres. Hard-code Mirth poller. Print metrics to console.
	2.	Week 3–4: Write metrics to ClickHouse. UI: Next.js dashboard. Package Compose.
	3.	Week 5–6: Add OpenSearch + error logs. Add Slack/email alert worker.
	4.	Week 7–8: Add MinIO + Parquet writer. Add vendor profile drift detection.
	5.	Week 9–10: Polish UI (alerts, logs). Add user/org auth stub.
	6.	Week 11–12: Run in test Mirth environment. Iterate with design partner.
	7.	Day 90 Deliverable: A hospital can install agent + Compose stack, see live metrics, get alerts, search errors. MVP in hand.

⸻

✅ Bottom line: You can build the whole pipeline with free tech. Sequence matters: get edge agent → gateway → metrics → UI working first. Then add logs/search, then alerts, then BI/AI. By 90 days, you’ll have a credible MVP for pilots. By 12–24 months, you’ll have a multi-engine, enterprise-ready platform.