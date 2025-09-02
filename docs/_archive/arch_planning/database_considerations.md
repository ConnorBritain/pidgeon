# Database Architecture Considerations

**Document Version**: 1.0  
**Date**: August 26, 2025  
**Status**: Architectural Planning - Pre-Implementation  
**Scope**: Database strategy for Pidgeon configuration intelligence and platform features

---

## 🎯 **Database Requirements by Feature Category**

### **1. Configuration Intelligence (Core Differentiator)**

#### **Configuration Storage & Management**
```sql
-- Core configuration tables
Configurations (id, vendor, standard, message_type, config_json, version, created_at, updated_at)
ConfigurationHistory (config_id, version, changes_json, change_type, timestamp, user_id)
VendorSignatures (id, vendor, standard, signature_patterns, confidence, sample_count)
DeviationPatterns (id, config_id, deviation_type, location, frequency, severity)

-- Queries needed:
-- Find configurations by vendor/standard/message_type hierarchy
-- Track configuration evolution over time  
-- Detect configuration drift and changes
-- Cross-configuration pattern analysis
```

**Storage Implications:**
- **JSON columns**: Configuration data is semi-structured, varies by standard
- **Time-series data**: Configuration changes tracked over time
- **Full-text search**: Need to search within configuration patterns
- **Analytical queries**: Cross-vendor comparison, pattern recognition

#### **Sample Message Analysis**
```sql  
-- Message analysis workflow
AnalysisJobs (id, status, vendor, standard, message_type, sample_count, started_at, completed_at)
MessageSamples (id, job_id, message_content, parsed_segments, analysis_results)
InferredPatterns (id, job_id, pattern_type, pattern_data, confidence, frequency)

-- Queries needed:
-- Bulk insert thousands of sample messages
-- Pattern extraction and statistical analysis
-- Incremental analysis as new samples added
```

**Storage Implications:**
- **Large text fields**: Full HL7/FHIR/NCPDP message content
- **Bulk operations**: Efficient batch inserts for sample analysis
- **Temporary storage**: Analysis jobs may be ephemeral
- **Computational queries**: Statistical analysis within database

### **2. Generation Services (AI + Dataset Management)**

#### **Dataset Management**
```sql
-- Live dataset serving for subscriptions
HealthcareMedications (id, ndc_code, brand_name, generic_name, dosage_form, strength, status, updated_at)
DemographicNames (id, first_name, last_name, gender, ethnicity, frequency_weight, region)
AddressPatterns (id, street_patterns, city, state, zip_code, region, healthcare_density)
ProviderDirectory (id, npi, name, specialty, dea_number, practice_locations, credentials)

-- Queries needed:
-- Weighted random selection for realistic generation
-- Regional filtering for geographic accuracy
-- Real-time updates as healthcare data changes
-- Usage tracking per customer
```

**Storage Implications:**
- **Reference data management**: Regular updates from healthcare databases
- **Weighted randomization**: Complex selection algorithms
- **Multi-tenancy**: Customer-specific dataset access controls
- **Usage metering**: Track API calls per customer

#### **AI Operations Tracking**
```sql
-- AI usage monitoring and cost management
AIOperations (id, customer_id, operation_type, token_count, cost, provider, timestamp)
CustomerTokenLimits (customer_id, tier, monthly_limit, current_usage, reset_date)
AIProviderConfigs (id, customer_id, provider, api_key_encrypted, model_preferences)

-- Queries needed:
-- Real-time usage tracking against limits
-- Cost allocation and billing calculations
-- Customer-specific AI configuration management
```

**Storage Implications:**
- **High-volume transactional**: Every AI operation logged
- **Encryption**: Customer API keys stored securely
- **Real-time queries**: Usage limits checked on every request
- **Billing aggregation**: Monthly rollups for invoicing

### **3. Team Collaboration & Enterprise Features**

#### **Multi-Tenancy & Access Control**
```sql
-- Team workspace management
Organizations (id, name, subscription_tier, billing_info, created_at)
Users (id, email, name, organization_id, role, permissions, last_active)
Workspaces (id, organization_id, name, description, configuration_access_rules)
UserSessions (id, user_id, session_token, expires_at, last_activity)

-- Queries needed:
-- User authentication and authorization
-- Workspace-scoped data access
-- Permission-based feature access
-- Session management
```

**Storage Implications:**
- **Row-level security**: Configurations scoped to organizations/workspaces
- **Session management**: Secure token-based authentication
- **Audit trails**: Track all user actions for compliance
- **Hierarchical permissions**: Organization → Workspace → Resource access

#### **Configuration Collaboration**
```sql
-- Team configuration workflows
ConfigurationApprovals (id, config_id, submitted_by, approved_by, status, comments, timestamp)
ConfigurationShares (id, config_id, shared_by, workspace_id, permissions, shared_at)
ChangeRequests (id, config_id, requested_by, changes_proposed, status, reviewed_by)
ConfigurationComments (id, config_id, user_id, comment_text, thread_id, created_at)

-- Queries needed:  
-- Approval workflow management
-- Configuration sharing across teams
-- Change tracking with review process
-- Discussion threads on configurations
```

**Storage Implications:**
- **Workflow state management**: Track approval processes
- **Versioning with branching**: Configuration changes may need merging
- **Real-time collaboration**: Comments and changes need live updates
- **Notification system**: Alert users to relevant changes

### **4. Analytics & Reporting (Enterprise)**

#### **Usage Analytics**
```sql
-- Platform usage monitoring
FeatureUsage (user_id, feature, usage_count, date, organization_id)
ConfigurationMetrics (config_id, usage_frequency, accuracy_score, user_feedback)
PlatformHealth (timestamp, active_users, configurations_processed, response_times)
CustomerAnalytics (organization_id, feature_adoption, retention_score, upgrade_signals)

-- Queries needed:
-- User behavior analysis for product development
-- Configuration effectiveness measurement
-- Platform performance monitoring  
-- Customer success tracking
```

**Storage Implications:**
- **Time-series analytics**: Aggregate usage over time periods
- **Business intelligence**: Complex analytical queries across all data
- **Data warehousing**: May need separate analytical database
- **Real-time dashboards**: Live metrics for operations team

---

## 🏗️ **Database Architecture Options**

### **Option 1: Hybrid Multi-Database Architecture (RECOMMENDED)**

#### **Primary Transactional Database (PostgreSQL)**
```yaml
Purpose: Core application data, user management, configuration storage
Tables: Users, Organizations, Configurations, ConfigurationHistory, Workspaces
Characteristics:
  - ACID compliance for critical data integrity
  - JSON support for flexible configuration storage
  - Row-level security for multi-tenancy
  - Full-text search capabilities
  - Excellent performance for complex queries
```

#### **Time-Series Database (InfluxDB or TimescaleDB)**
```yaml
Purpose: Analytics, usage tracking, performance monitoring
Tables: FeatureUsage, AIOperations, PlatformHealth, ConfigurationMetrics
Characteristics:
  - Optimized for time-based data insertion and queries
  - Automatic data retention and downsampling
  - Efficient aggregation queries
  - Real-time analytics capabilities
```

#### **Document Store (MongoDB or PostgreSQL JSON)**
```yaml
Purpose: Message samples, analysis results, large configuration data
Tables: MessageSamples, InferredPatterns, AnalysisJobs
Characteristics:
  - Flexible schema for varying message formats
  - Efficient storage of large JSON documents
  - Full-text search within document content
  - Easy horizontal scaling
```

#### **Cache Layer (Redis)**
```yaml
Purpose: Session management, API rate limiting, real-time features
Data: UserSessions, TokenLimits, ConfigurationCache, LiveNotifications
Characteristics:
  - High-performance in-memory operations
  - Pub/Sub for real-time collaboration
  - Rate limiting and session storage
  - Cache frequently accessed configurations
```

### **Option 2: PostgreSQL-Only Architecture (SIMPLER)**

#### **Single Database with Specialized Extensions**
```yaml
Primary Database: PostgreSQL with extensions
Extensions:
  - TimescaleDB: Time-series tables for analytics
  - pg_vector: Vector similarity for configuration matching
  - pg_cron: Scheduled jobs for data maintenance
  - pgcrypto: Encryption for sensitive data

Advantages:
  - Simpler operational complexity
  - Single source of truth
  - ACID consistency across all data
  - Proven reliability and tooling

Disadvantages:
  - May not scale as efficiently for time-series data
  - Less specialized for analytics workloads
  - Higher resource requirements on single database
```

---

## 💽 **Storage Requirements by Tier**

### **🆓 FREE TIER: No Persistent Storage**
- **Client-side only**: All data stored in local files
- **No database backend**: Zero hosting costs
- **Stateless operations**: Each CLI invocation independent
- **Export capabilities**: Generate configs to JSON files

### **💼 PROFESSIONAL TIER: SQLite Local Database**
```sql
-- Single-user local database (~100-500MB typical)
Configuration Storage: ~50MB (hundreds of configs)
Usage History: ~10MB (personal usage tracking)  
Cache Data: ~25MB (frequently used configurations)
AI Operation Log: ~15MB (personal AI usage history)

Total: ~100MB typical, ~500MB heavy usage
```

**Characteristics:**
- **File-based**: Single SQLite file in user directory
- **No networking**: All operations local
- **Backup/Sync**: User manages file backup/sharing
- **Migration path**: Export/import to Team tier

### **👥 TEAM TIER: Cloud PostgreSQL**
```sql
-- Multi-user shared database (~1-10GB typical)
Organizations: ~1MB (team metadata)
Users: ~5MB (team member data) 
Configurations: ~500MB (shared team configurations)
Configuration History: ~2GB (change tracking over time)
Collaboration Data: ~100MB (approvals, comments, shares)
Usage Analytics: ~1GB (team usage patterns)

Total per team: ~3.5GB typical, ~10GB heavy usage
```

**Characteristics:**
- **Managed PostgreSQL**: AWS RDS, Google Cloud SQL, or Azure Database
- **Multi-tenancy**: Row-level security for team isolation  
- **Automatic backups**: Point-in-time recovery
- **High availability**: Master/replica setup for uptime

### **🏢 ENTERPRISE TIER: Full Multi-Database Architecture**
```sql
-- Enterprise-scale with multiple databases (~10GB-1TB+)

Transactional Database (PostgreSQL):
  - Organizations: ~100MB (enterprise org data)
  - Users: ~500MB (large organization users)
  - Configurations: ~10GB (extensive configuration library)
  - Collaboration: ~5GB (enterprise workflow data)

Analytics Database (TimescaleDB):
  - Usage Metrics: ~100GB (detailed usage over years)
  - AI Operations: ~50GB (comprehensive AI usage tracking)
  - Performance Data: ~25GB (platform monitoring)

Document Store (MongoDB):
  - Message Samples: ~500GB (extensive message analysis)
  - Analysis Results: ~100GB (pattern analysis data)

Cache Layer (Redis):
  - Session Data: ~1GB (concurrent user sessions)
  - Real-time Data: ~500MB (live collaboration state)

Total: ~791GB+ depending on usage patterns
```

**Characteristics:**
- **Multi-database architecture**: Specialized databases for different workloads
- **Custom deployment**: On-premise or dedicated cloud instances
- **Advanced monitoring**: Database performance optimization
- **Disaster recovery**: Cross-region replication and backup

---

## 📊 **Data Access Patterns by Feature**

### **Configuration Intelligence Queries**

#### **Read-Heavy Patterns (90% of operations)**
```sql
-- Get configuration by address (microsecond response required)
SELECT config_json FROM configurations 
WHERE vendor = ? AND standard = ? AND message_type = ?;

-- Find similar configurations (sub-second response required)  
SELECT vendor, standard, message_type, similarity_score
FROM configurations 
WHERE vector_similarity(config_vector, ?) > 0.8;

-- Historical analysis (second-range acceptable)
SELECT version, changes_json, timestamp 
FROM configuration_history 
WHERE config_id = ? ORDER BY timestamp DESC;
```

#### **Write-Heavy Patterns (10% of operations)**
```sql
-- Batch configuration updates (during analysis jobs)
INSERT INTO configurations (vendor, standard, message_type, config_json)
VALUES (?, ?, ?, ?), (?, ?, ?, ?), ... -- Bulk inserts

-- Real-time collaboration updates (low latency required)
INSERT INTO configuration_comments (config_id, user_id, comment_text)
VALUES (?, ?, ?);
```

### **Generation Service Queries**

#### **High-Frequency Dataset Access**
```sql
-- Weighted random medication selection (sub-millisecond required)
SELECT ndc_code, brand_name, generic_name 
FROM healthcare_medications 
WHERE specialty = ? AND status = 'active'
ORDER BY RANDOM() * frequency_weight DESC 
LIMIT 1;

-- Regional demographic data (millisecond range)
SELECT first_name, last_name FROM demographic_names
WHERE region = ? AND gender = ?
ORDER BY RANDOM() * frequency_weight DESC
LIMIT ?;
```

#### **AI Usage Tracking**
```sql
-- Token limit checking (microsecond response critical)
SELECT monthly_limit - current_usage as remaining
FROM customer_token_limits 
WHERE customer_id = ? AND reset_date > NOW();

-- Usage logging (eventual consistency acceptable)
INSERT INTO ai_operations (customer_id, token_count, cost, timestamp)
VALUES (?, ?, ?, NOW());
```

---

## 🔧 **Database Technology Selection Criteria**

### **PostgreSQL Primary Database - RECOMMENDED**

**Advantages:**
- **JSON/JSONB support**: Perfect for flexible configuration storage
- **Full-text search**: Built-in search capabilities for configurations
- **Row-level security**: Native multi-tenancy support
- **ACID compliance**: Critical for financial/usage data
- **Mature ecosystem**: Extensive tooling and expertise
- **Horizontal scaling**: Read replicas and partitioning options

**Use Cases:**
- User management and authentication
- Configuration storage and versioning
- Team collaboration and permissions
- Financial data (billing, usage limits)

### **TimescaleDB for Analytics - RECOMMENDED**

**Advantages:**
- **PostgreSQL compatibility**: Same interface as primary database
- **Time-series optimization**: Efficient storage and queries for usage data
- **Automatic partitioning**: Handles time-based data lifecycle
- **Compression**: Reduces storage costs for historical data

**Use Cases:**
- Usage analytics and reporting
- AI operation tracking
- Performance monitoring
- Customer behavior analysis

### **Redis for Caching - RECOMMENDED**

**Advantages:**
- **In-memory performance**: Sub-millisecond response times
- **Pub/Sub messaging**: Real-time collaboration features
- **Rate limiting**: Built-in token bucket algorithms
- **Session storage**: Secure, fast session management

**Use Cases:**
- User session management
- API rate limiting and quota enforcement
- Real-time collaboration state
- Frequently accessed configuration caching

---

## 🚀 **Implementation Strategy**

### **Phase 1: File-Based Foundation (Months 1-2)**
- **No database required**: JSON file storage for CLI
- **Repository pattern**: Abstract storage behind interfaces
- **Validation**: Prove configuration intelligence value
- **Foundation**: Establish data models and access patterns

### **Phase 2: SQLite Professional (Months 3-4)**
- **Local database**: Add SQLite for Professional tier
- **Migration tools**: Import/export between file and database formats
- **Persistence features**: Configuration history and evolution
- **Testing**: Validate multi-repository architecture

### **Phase 3: PostgreSQL Team Features (Months 5-6)**
- **Cloud database**: Add PostgreSQL for Team tier
- **Multi-tenancy**: Row-level security and workspace isolation
- **Collaboration**: Real-time features with Redis integration
- **Monitoring**: Database performance and optimization

### **Phase 4: Enterprise Analytics (Months 7-12)**
- **TimescaleDB**: Add time-series database for analytics
- **Advanced features**: Cross-configuration analysis, usage reporting
- **Custom deployment**: On-premise and hybrid cloud options
- **Scale optimization**: Performance tuning for large enterprises

---

## ⚠️ **Risk Mitigation & Considerations**

### **Data Security & Compliance**
- **Encryption at rest**: All customer data encrypted in database
- **Encryption in transit**: TLS for all database connections
- **Key management**: Separate key management system for customer API keys
- **Audit logging**: Complete audit trail for all configuration changes
- **HIPAA consideration**: PHI handling for healthcare message samples

### **Scalability Challenges**
- **Configuration storage**: JSON documents may grow large for complex configs
- **Message samples**: Raw HL7/FHIR messages can consume significant storage
- **Analytics queries**: Cross-customer analytics may impact performance
- **Multi-tenancy**: Ensure tenant isolation doesn't impact performance

### **Operational Complexity**
- **Database maintenance**: Multiple databases require specialized expertise
- **Backup strategy**: Coordinated backups across multiple systems
- **Monitoring**: Comprehensive monitoring across all database systems
- **Cost management**: Multi-database architecture increases hosting costs

### **Business Model Alignment**
- **Free tier costs**: Ensure file-based approach has zero hosting costs
- **Professional value**: SQLite features must justify subscription price
- **Team collaboration**: Database features must provide clear team value
- **Enterprise ROI**: Advanced features must justify premium pricing

---

## 🏥 **Healthcare Compliance & Deployment Considerations**

### **Healthcare IT Reality Check**

**Critical Constraints:**
- **PHI Concerns**: HL7 messages often contain Protected Health Information
- **Compliance Requirements**: HIPAA, HITECH, state privacy laws
- **Risk Aversion**: Healthcare organizations extremely cautious about cloud data
- **Windows Dominance**: ~85% of healthcare IT environments are Windows-based
- **On-Premise Preference**: Many organizations require local data control
- **BAA Requirements**: Cloud services need Business Associate Agreements

**Implications for Database Strategy:**
- Must support **on-premise deployment** for all paid tiers
- Database choices must work well on **Windows Server environments**
- **Hybrid architectures** needed (local data, cloud collaboration)
- **Enterprise-grade security** required for any cloud offering

### **Deployment Architecture Options**

#### **Option A: On-Premise First (RECOMMENDED for Healthcare)**
```yaml
Local Installation (All Tiers):
  Database: PostgreSQL or SQL Server on customer premises
  Application: Windows Service or Docker containers
  Data Storage: Customer-controlled, encrypted at rest
  Network: Air-gapped or VPN-only external access

Cloud Integration (Optional):
  Metadata Only: Team membership, permissions, non-PHI data
  Encrypted Sync: Configuration patterns (no message content)
  Secure Channels: Always customer-initiated, TLS 1.3+
```

#### **Option B: Hybrid Cloud Architecture**
```yaml
Local Components:
  - Message Processing: All PHI stays on-premise
  - Configuration Storage: Local database with customer control
  - Analysis Engine: Runs locally on customer hardware

Cloud Components (Non-PHI):
  - Team Management: User accounts, permissions, workspaces
  - Template Library: Vendor configuration patterns (anonymized)
  - Collaboration: Comments, approvals, sharing (no message content)
  - Analytics: Aggregated usage patterns (no PHI)
```

#### **Option C: Secure Cloud (Requires Significant Investment)**
```yaml
HIPAA-Compliant Cloud Infrastructure:
  - SOC2 Type II certified data centers
  - HIPAA BAA coverage with all cloud providers
  - Customer-specific encryption keys
  - Dedicated tenant isolation
  - Complete audit logging
  - Data residency controls
```

### **Database Technology Re-Evaluation for Healthcare**

#### **SQL Server: Strong Healthcare Fit**
**Advantages:**
- **Windows Integration**: Native Windows Server deployment
- **Healthcare Adoption**: Widely used in hospital environments
- **Security Features**: Always Encrypted, Row Level Security, Advanced Auditing
- **Compliance Support**: HIPAA compliance guidance from Microsoft
- **Enterprise Features**: AlwaysOn availability groups, backup encryption

**Considerations:**
- **Licensing Costs**: Significant per-core licensing fees
- **Operational Complexity**: Requires SQL Server expertise
- **Linux Limitations**: Less mature on non-Windows platforms

#### **PostgreSQL: Healthcare-Friendly Alternative**
**Advantages:**
- **Cost Effective**: Open source with enterprise features
- **Cross-Platform**: Excellent Windows support, flexible deployment
- **Security Features**: Row-level security, transparent encryption
- **JSON Support**: Perfect for configuration storage
- **Compliance**: Can be configured for HIPAA compliance

**Considerations:**
- **Healthcare Adoption**: Less common in healthcare than SQL Server
- **Enterprise Support**: May need commercial support for critical deployments

#### **SQLite: Ideal for Local/Offline Scenarios**
**Perfect for Healthcare Because:**
- **No Network Dependencies**: Database file stays on local machine
- **No Server Process**: Eliminates attack surface
- **Encryption Support**: SQLite with SEE (SQLite Encryption Extension)
- **Portable**: Single file, easy backup and archival
- **Zero Configuration**: No database server administration

### **Infrastructure Requirements**

#### **Customer On-Premise Requirements**

**Minimum Professional Tier:**
```yaml
Hardware:
  - CPU: 4 cores minimum (Intel/AMD x64)
  - RAM: 8GB minimum, 16GB recommended
  - Storage: 100GB available (SSD recommended)
  - Network: Standard corporate network

Software:
  - Windows Server 2019+ or Windows 10/11 Pro
  - .NET 8 Runtime
  - Database: PostgreSQL 14+ or SQL Server 2019+
  - Antivirus: Exclusions for database and application files
```

**Enterprise Tier:**
```yaml
Hardware:
  - CPU: 8+ cores, enterprise-grade
  - RAM: 32GB minimum, 64GB+ recommended
  - Storage: 1TB+ with RAID configuration
  - Network: Dedicated network segment, firewall rules

Software:
  - Windows Server 2019+ Standard/Datacenter
  - Active Directory integration
  - Enterprise database (SQL Server Enterprise, PostgreSQL with HA)
  - Backup software with encryption
  - Monitoring and alerting system
```

#### **Pidgeon Hosting Infrastructure (If Cloud Option Chosen)**

**Security-First Cloud Architecture:**
```yaml
Infrastructure:
  - HIPAA-eligible cloud provider (AWS/Azure/GCP)
  - Dedicated Virtual Private Cloud (VPC)
  - Customer-specific encryption keys (Customer Managed Keys)
  - Geographic data residency controls
  - Network isolation and micro-segmentation

Compliance Requirements:
  - SOC2 Type II certification
  - HIPAA Business Associate Agreement
  - Annual security audits and penetration testing
  - 24/7 security monitoring (SIEM)
  - Incident response plan and procedures

Data Protection:
  - Encryption at rest (AES-256)
  - Encryption in transit (TLS 1.3)
  - Database-level encryption (column-level for PHI)
  - Secure key management (HSM)
  - Regular encrypted backups with retention policies
```

### **Recommended Database Strategy for Healthcare**

#### **Phase 1: SQLite + PostgreSQL On-Premise**
```yaml
Professional Tier: SQLite (local file, encrypted)
Team Tier: PostgreSQL (customer-hosted, on-premise)
Enterprise Tier: SQL Server or PostgreSQL (customer choice)

Benefits:
  - Complete customer data control
  - No cloud compliance concerns
  - Windows-friendly deployment
  - Familiar technologies for healthcare IT
```

#### **Phase 2: Hybrid Architecture (Optional)**
```yaml
Local Processing: All message analysis on-premise
Cloud Metadata: Team collaboration, non-PHI data only
Sync Mechanism: Customer-controlled, encrypted, one-way push

Benefits:
  - Team collaboration without PHI exposure
  - Vendor template sharing (anonymized patterns)
  - Analytics without compliance concerns
```

#### **Phase 3: Secure Cloud Option (Long-term)**
```yaml
Full HIPAA Compliance: BAA, SOC2, dedicated infrastructure
Customer Control: Encryption keys, data residency, access controls
Premium Pricing: Reflects compliance and security investment

Benefits:
  - Fully managed service for customers who prefer cloud
  - Enterprise-grade security and compliance
  - Simplified customer infrastructure requirements
```

### **Database Selection Matrix for Healthcare**

| Database | On-Premise Fit | Windows Support | Healthcare Adoption | Compliance Features | Cost |
|----------|----------------|-----------------|-------------------|-------------------|------|
| **SQL Server** | ✅ Excellent | ✅ Native | ✅ Very High | ✅ Advanced | 💰 High |
| **PostgreSQL** | ✅ Good | ✅ Good | ⚠️ Moderate | ✅ Good | 💰 Low |
| **SQLite** | ✅ Perfect | ✅ Excellent | ✅ High (local) | ✅ Basic | 💰 None |
| **MySQL** | ⚠️ Moderate | ⚠️ Fair | ⚠️ Low | ⚠️ Limited | 💰 Low |

**Recommendation**: 
- **Start with SQLite + PostgreSQL** to maximize healthcare adoption
- **Add SQL Server support** for Enterprise tier customers who prefer it
- **Avoid MySQL** due to limited healthcare adoption and compliance features

---

## 📋 **Decision Framework**

### **Key Questions to Resolve:**

1. **Start Simple vs Future-Proof**: 
   - Begin with PostgreSQL-only architecture for simplicity?
   - Or implement multi-database architecture from the start?

2. **Storage Cost Management**:
   - How much message sample data should we retain?
   - What data retention policies align with subscription tiers?

3. **Multi-Tenancy Approach**:
   - Single database with row-level security?
   - Or separate databases per large enterprise customer?

4. **Analytics Requirements**:
   - Real-time analytics necessary, or batch processing acceptable?
   - What level of cross-customer analytics do we need?

### **Recommended Decision Path:**

1. **Phase 1**: Start with file-based storage (no database) to validate configuration intelligence
2. **Phase 2**: Add PostgreSQL-only architecture for Professional/Team tiers
3. **Phase 3**: Evaluate adding specialized databases (TimescaleDB, Redis) based on actual usage patterns
4. **Phase 4**: Scale to multi-database architecture only when performance/features demand it

This approach balances architectural soundness with practical business constraints while maintaining flexibility for future scaling.

---

**Next Steps**: Review database strategy, then proceed with file-based configuration intelligence implementation to validate the data models before committing to specific database technologies.