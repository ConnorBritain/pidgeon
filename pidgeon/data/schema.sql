-- Pidgeon HL7 Database Schema
-- Core HL7 v2.3 Structure Data + Semantic Path Integration

PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;

-- ============================================================================
-- PHASE 1-2: FOUNDATION TABLES (Standards and Core Structure)
-- ============================================================================

-- Standards (HL7 v2.3, v2.5, future FHIR, NCPDP)
CREATE TABLE standards (
    id INTEGER PRIMARY KEY,
    version TEXT NOT NULL UNIQUE,              -- 'HL7v2.3', 'HL7v2.5', 'FHIRv4'
    release_date DATE,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Segments definitions (MSH, PID, OBR, etc.)
CREATE TABLE segments (
    id INTEGER PRIMARY KEY,
    standard_id INTEGER NOT NULL,
    code TEXT NOT NULL,                         -- 'MSH', 'PID', 'OBR'
    name TEXT NOT NULL,                         -- 'Message Header', 'Patient Identification'
    chapter TEXT,                               -- 'Patient Administration', 'Orders'
    description TEXT,
    purpose TEXT,
    usage_notes TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (standard_id) REFERENCES standards(id),
    UNIQUE(standard_id, code)
);

-- Data types (ST, NM, CX, XPN, etc.)
CREATE TABLE data_types (
    id INTEGER PRIMARY KEY,
    standard_id INTEGER NOT NULL,
    code TEXT NOT NULL,                         -- 'ST', 'NM', 'CX', 'XPN'
    name TEXT NOT NULL,                         -- 'String Data', 'Extended Composite ID'
    description TEXT,
    category TEXT DEFAULT 'primitive',          -- 'primitive', 'composite'
    max_length INTEGER,
    example TEXT,                               -- Example usage
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (standard_id) REFERENCES standards(id),
    UNIQUE(standard_id, code)
);

-- Fields within segments (PID.1, PID.2, PID.3, etc.)
CREATE TABLE fields (
    id INTEGER PRIMARY KEY,
    segment_id INTEGER NOT NULL,
    position INTEGER NOT NULL,                  -- 1, 2, 3 (for PID.1, PID.2, etc.)
    field_name TEXT NOT NULL,                   -- 'PID.1', 'PID.2'
    field_description TEXT NOT NULL,            -- 'Set ID - Patient ID'
    data_type_id INTEGER NOT NULL,
    length INTEGER,                             -- Maximum field length
    optionality TEXT CHECK(optionality IN ('R', 'O', 'C', 'B')), -- Required, Optional, Conditional, Backward Compatible
    repeatability TEXT,                         -- '-', '∞', specific number
    table_id INTEGER,                           -- Reference to code table
    usage_notes TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (segment_id) REFERENCES segments(id) ON DELETE CASCADE,
    FOREIGN KEY (data_type_id) REFERENCES data_types(id),
    FOREIGN KEY (table_id) REFERENCES code_tables(id),
    UNIQUE(segment_id, position)
);

-- Components for composite data types (CX.1, CX.2, etc.)
CREATE TABLE data_type_components (
    id INTEGER PRIMARY KEY,
    data_type_id INTEGER NOT NULL,
    position INTEGER NOT NULL,                  -- 1, 2, 3 (for CX.1, CX.2, etc.)
    field_name TEXT NOT NULL,                   -- 'CX.1', 'CX.2'
    field_description TEXT NOT NULL,            -- 'ID', 'Check Digit'
    component_data_type_id INTEGER,             -- References another data_type
    length INTEGER,
    optionality TEXT CHECK(optionality IN ('R', 'O', 'C', 'B')),
    repeatability TEXT,
    table_id INTEGER,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (data_type_id) REFERENCES data_types(id) ON DELETE CASCADE,
    FOREIGN KEY (component_data_type_id) REFERENCES data_types(id),
    FOREIGN KEY (table_id) REFERENCES code_tables(id),
    UNIQUE(data_type_id, position)
);

-- ============================================================================
-- PHASE 3: CODE TABLES AND VALUES
-- ============================================================================

-- Code tables (Table 0001, 0002, etc.)
CREATE TABLE code_tables (
    id INTEGER PRIMARY KEY,
    standard_id INTEGER NOT NULL,
    table_number TEXT NOT NULL,                 -- '0001', '0002', '0003'
    name TEXT NOT NULL,                         -- 'Sex', 'Marital Status'
    chapter TEXT,                               -- 'Patient Administration'
    description TEXT,
    type TEXT DEFAULT 'User',                   -- 'User', 'HL7', 'External'
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (standard_id) REFERENCES standards(id),
    UNIQUE(standard_id, table_number)
);

-- Code values within tables (M/F/U for gender, etc.)
CREATE TABLE code_values (
    id INTEGER PRIMARY KEY,
    table_id INTEGER NOT NULL,
    code TEXT NOT NULL,                         -- 'M', 'F', 'U'
    description TEXT NOT NULL,                  -- 'Male', 'Female', 'Unknown'
    comment TEXT,
    sort_order INTEGER DEFAULT 0,
    is_deprecated BOOLEAN DEFAULT FALSE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (table_id) REFERENCES code_tables(id) ON DELETE CASCADE,
    UNIQUE(table_id, code)
);

-- ============================================================================
-- PHASE 4: TRIGGER EVENTS AND MESSAGE STRUCTURE
-- ============================================================================

-- Trigger Events (ADT_A01, ORM_O01, etc.)
CREATE TABLE trigger_events (
    id INTEGER PRIMARY KEY,
    standard_id INTEGER NOT NULL,
    code TEXT NOT NULL,                         -- 'ADT_A01', 'ORM_O01'
    title TEXT,                                 -- 'Admit Patient'
    chapter TEXT,                               -- 'Patient Administration'
    description TEXT,
    message_structure_id INTEGER,               -- Link to message structure
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (standard_id) REFERENCES standards(id),
    UNIQUE(standard_id, code)
);

-- Hierarchical event-segment relationships (message structure)
CREATE TABLE event_segments (
    id INTEGER PRIMARY KEY,
    event_id INTEGER NOT NULL,
    segment_id INTEGER,                         -- NULL for groups
    parent_id INTEGER,                          -- Self-referencing for hierarchy
    position INTEGER NOT NULL,
    level INTEGER NOT NULL DEFAULT 0,
    optionality TEXT CHECK(optionality IN ('R', 'O', 'C', 'B')),
    repeatability TEXT,                         -- '1', '*', 'inf', specific number
    is_group BOOLEAN DEFAULT FALSE,
    group_name TEXT,                            -- For groups like 'PATIENT', 'PROCEDURE'
    group_path TEXT,                            -- JSON array for nested groups
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (event_id) REFERENCES trigger_events(id) ON DELETE CASCADE,
    FOREIGN KEY (segment_id) REFERENCES segments(id),
    FOREIGN KEY (parent_id) REFERENCES event_segments(id)
);

-- ============================================================================
-- PHASE 7: SEMANTIC PATH INTEGRATION (Existing - Enhanced)
-- ============================================================================

-- Semantic Paths: High-level user-friendly paths for healthcare concepts
CREATE TABLE semantic_paths (
    id INTEGER PRIMARY KEY,
    semantic_path TEXT NOT NULL UNIQUE,           -- 'patient.mrn', 'patient.name.family'
    tier TEXT NOT NULL CHECK(tier IN ('essential', 'advanced')),
    category TEXT NOT NULL,                       -- 'patient', 'encounter', 'order'
    description TEXT NOT NULL,
    usage_priority INTEGER DEFAULT 100,           -- Lower = higher priority
    hl7_field TEXT,                               -- 'PID-3', 'PID-5.1'
    fhir_path TEXT,                               -- 'Patient.identifier[2]'
    source_file TEXT,                             -- Original CSV file reference
    data_type_hint TEXT,                          -- 'identifier', 'name', 'date'
    validation_pattern TEXT,                      -- Regex patterns
    example_value TEXT,                           -- Sample values
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Links semantic paths to actual HL7 structure
CREATE TABLE semantic_path_mappings (
    id INTEGER PRIMARY KEY,
    semantic_path_id INTEGER NOT NULL,
    field_id INTEGER,                             -- Links to fields table
    component_id INTEGER,                         -- Links to components table
    mapping_type TEXT NOT NULL DEFAULT 'direct', -- 'direct', 'computed', 'conditional'
    transformation_rule TEXT,                     -- For complex mappings
    confidence_score REAL DEFAULT 1.0,           -- 0.0-1.0 mapping confidence
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (semantic_path_id) REFERENCES semantic_paths(id) ON DELETE CASCADE,
    FOREIGN KEY (field_id) REFERENCES fields(id) ON DELETE SET NULL,
    FOREIGN KEY (component_id) REFERENCES data_type_components(id) ON DELETE SET NULL,
    CHECK (field_id IS NOT NULL OR component_id IS NOT NULL)
);

-- ============================================================================
-- INTEROPERABILITY TABLES (HL7-FHIR Mappings)
-- ============================================================================

-- Segment Mappings: Official HL7 International segment-to-FHIR mappings
CREATE TABLE segment_mappings (
    id INTEGER PRIMARY KEY,
    source_file TEXT NOT NULL,
    sort_order INTEGER,
    hl7_identifier TEXT,                         -- PID-3, MSH-10
    hl7_name TEXT,                              -- Patient Identifier List
    hl7_data_type TEXT,                         -- CX, HD
    hl7_cardinality_min INTEGER,
    hl7_cardinality_max TEXT,
    condition_rule TEXT,
    fhir_attribute TEXT,                        -- identifier[2], name[1]
    fhir_data_type TEXT,                        -- Identifier, HumanName
    fhir_cardinality_min INTEGER,
    fhir_cardinality_max TEXT,
    data_type_mapping TEXT,                     -- CX[Identifier]
    vocabulary_mapping TEXT,
    assignment TEXT,
    comments TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- DataType Mappings: Official HL7 datatype-to-FHIR mappings
CREATE TABLE datatype_mappings (
    id INTEGER PRIMARY KEY,
    source_file TEXT NOT NULL,
    sort_order INTEGER,
    hl7_identifier TEXT,                        -- CX.1, XPN.2
    hl7_name TEXT,                             -- ID Number, Family Name
    hl7_data_type TEXT,                        -- ST, FN
    hl7_cardinality_min INTEGER,
    hl7_cardinality_max TEXT,
    condition_rule TEXT,
    fhir_attribute TEXT,                       -- value, family
    fhir_data_type TEXT,                       -- string, string
    fhir_cardinality_min INTEGER,
    fhir_cardinality_max TEXT,
    data_type_mapping TEXT,                    -- ST[String]
    vocabulary_mapping TEXT,
    assignment TEXT,
    comments TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- CodeSystem Mappings: Official HL7 code system mappings to FHIR
CREATE TABLE codesystem_mappings (
    id INTEGER PRIMARY KEY,
    source_file TEXT NOT NULL,
    hl7_code TEXT,                             -- M, F, U
    hl7_display TEXT,                          -- Male, Female, Unknown
    fhir_code TEXT,                            -- male, female, unknown
    fhir_display TEXT,                         -- Male, Female, Unknown
    fhir_system TEXT,                          -- http://hl7.org/fhir/administrative-gender
    comments TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- PERFORMANCE OPTIMIZATION: INDEXES
-- ============================================================================

-- Core structure indexes
CREATE INDEX idx_segments_code ON segments(code);
CREATE INDEX idx_segments_standard ON segments(standard_id);
CREATE INDEX idx_fields_segment_position ON fields(segment_id, position);
CREATE INDEX idx_fields_data_type ON fields(data_type_id);
CREATE INDEX idx_data_types_code ON data_types(code);
CREATE INDEX idx_data_type_components_type_pos ON data_type_components(data_type_id, position);

-- Code table indexes
CREATE INDEX idx_code_tables_number ON code_tables(table_number);
CREATE INDEX idx_code_tables_standard ON code_tables(standard_id);
CREATE INDEX idx_code_values_table_code ON code_values(table_id, code);

-- Trigger event indexes
CREATE INDEX idx_trigger_events_code ON trigger_events(code);
CREATE INDEX idx_trigger_events_standard ON trigger_events(standard_id);
CREATE INDEX idx_event_segments_hierarchy ON event_segments(event_id, parent_id, position);

-- Semantic path indexes
CREATE INDEX idx_semantic_paths_category ON semantic_paths(category);
CREATE INDEX idx_semantic_paths_tier ON semantic_paths(tier);
CREATE INDEX idx_semantic_paths_priority ON semantic_paths(usage_priority);
CREATE INDEX idx_semantic_path_mappings_path ON semantic_path_mappings(semantic_path_id);

-- Mapping table indexes
CREATE INDEX idx_segment_mappings_hl7_id ON segment_mappings(hl7_identifier);
CREATE INDEX idx_segment_mappings_source ON segment_mappings(source_file);
CREATE INDEX idx_datatype_mappings_hl7_id ON datatype_mappings(hl7_identifier);
CREATE INDEX idx_codesystem_mappings_hl7_code ON codesystem_mappings(hl7_code);

-- ============================================================================
-- PERFORMANCE VIEWS: Common Query Patterns
-- ============================================================================

-- Complete segment structure with fields and data types
CREATE VIEW segment_structure AS
SELECT
    s.code as segment_code,
    s.name as segment_name,
    s.description as segment_description,
    f.position as field_position,
    f.field_name,
    f.field_description,
    dt.code as data_type,
    dt.name as data_type_name,
    f.optionality,
    f.repeatability,
    f.length,
    ct.table_number,
    ct.name as table_name
FROM segments s
JOIN fields f ON s.id = f.segment_id
JOIN data_types dt ON f.data_type_id = dt.id
LEFT JOIN code_tables ct ON f.table_id = ct.id
ORDER BY s.code, f.position;

-- Complete data type structure with components
CREATE VIEW datatype_structure AS
SELECT
    dt.code as datatype_code,
    dt.name as datatype_name,
    dt.category,
    dtc.position as component_position,
    dtc.field_name as component_name,
    dtc.field_description as component_description,
    cdt.code as component_data_type,
    dtc.optionality,
    dtc.repeatability,
    dtc.length
FROM data_types dt
LEFT JOIN data_type_components dtc ON dt.id = dtc.data_type_id
LEFT JOIN data_types cdt ON dtc.component_data_type_id = cdt.id
ORDER BY dt.code, dtc.position;

-- Essential semantic paths view (for UI progressive disclosure)
CREATE VIEW essential_paths AS
SELECT
    semantic_path,
    category,
    description,
    hl7_field,
    fhir_path,
    data_type_hint,
    example_value
FROM semantic_paths
WHERE tier = 'essential'
ORDER BY usage_priority ASC;

-- Complete HL7 structure with semantic path integration
CREATE VIEW hl7_structure_with_paths AS
SELECT
    s.code as segment_code,
    s.name as segment_name,
    f.position as field_position,
    f.field_name,
    f.field_description,
    dt.code as data_type,
    f.optionality,
    f.repeatability,
    sp.semantic_path,
    sp.category as semantic_category,
    sp.tier as semantic_tier
FROM segments s
JOIN fields f ON s.id = f.segment_id
JOIN data_types dt ON f.data_type_id = dt.id
LEFT JOIN semantic_path_mappings spm ON f.id = spm.field_id
LEFT JOIN semantic_paths sp ON spm.semantic_path_id = sp.id
ORDER BY s.code, f.position;

-- ============================================================================
-- DATA INTEGRITY AND MAINTENANCE
-- ============================================================================

-- Update timestamps triggers
CREATE TRIGGER update_segments_timestamp
    AFTER UPDATE ON segments
    FOR EACH ROW
BEGIN
    UPDATE segments SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

CREATE TRIGGER update_fields_timestamp
    AFTER UPDATE ON fields
    FOR EACH ROW
BEGIN
    UPDATE fields SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

CREATE TRIGGER update_semantic_paths_timestamp
    AFTER UPDATE ON semantic_paths
    FOR EACH ROW
BEGIN
    UPDATE semantic_paths SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
END;

-- ============================================================================
-- INITIAL DATA: Bootstrap with HL7 v2.3
-- ============================================================================

-- Insert HL7 v2.3 standard
INSERT INTO standards (version, description, is_active) VALUES
('HL7v2.3', 'Health Level Seven Version 2.3', TRUE);

-- Essential semantic paths (from Phase 7 implementation)
INSERT INTO semantic_paths (semantic_path, tier, category, description, usage_priority, hl7_field, fhir_path, data_type_hint, example_value) VALUES
-- Patient Identity (Priority 1-10)
('patient.mrn', 'essential', 'patient', 'Patient Medical Record Number', 1, 'PID-3', 'Patient.identifier[2].value', 'identifier', '12345'),
('patient.name.family', 'essential', 'patient', 'Patient Last Name', 2, 'PID-5.1', 'Patient.name[1].family', 'name', 'Smith'),
('patient.name.given', 'essential', 'patient', 'Patient First Name', 3, 'PID-5.2', 'Patient.name[1].given[1]', 'name', 'John'),
('patient.dob', 'essential', 'patient', 'Patient Date of Birth', 4, 'PID-7', 'Patient.birthDate', 'date', '1980-01-15'),
('patient.gender', 'essential', 'patient', 'Patient Gender', 5, 'PID-8', 'Patient.gender', 'code', 'M'),
('patient.ssn', 'essential', 'patient', 'Patient Social Security Number', 6, 'PID-19', 'Patient.identifier[1].value', 'identifier', '123-45-6789'),

-- Encounter Basics (Priority 11-20)
('encounter.id', 'essential', 'encounter', 'Encounter ID', 11, 'PV1-19', 'Encounter.identifier[1].value', 'identifier', 'E123456'),
('encounter.class', 'essential', 'encounter', 'Encounter Class (Inpatient/Outpatient)', 12, 'PV1-2', 'Encounter.class.code', 'code', 'IMP'),
('encounter.admit_date', 'essential', 'encounter', 'Admission Date/Time', 13, 'PV1-44', 'Encounter.period.start', 'datetime', '202401151030'),
('encounter.discharge_date', 'essential', 'encounter', 'Discharge Date/Time', 14, 'PV1-45', 'Encounter.period.end', 'datetime', '202401171530'),
('encounter.location', 'essential', 'encounter', 'Patient Location', 15, 'PV1-3', 'Encounter.location[1].location.display', 'location', 'ICU^ICU1^A'),

-- Provider Information (Priority 21-25)
('provider.attending.id', 'essential', 'provider', 'Attending Physician ID', 21, 'PV1-7.1', 'Encounter.participant.individual.identifier.value', 'identifier', 'DOC123'),
('provider.attending.name', 'essential', 'provider', 'Attending Physician Name', 22, 'PV1-7.2', 'Encounter.participant.individual.name.family', 'name', 'Johnson'),

-- Message Control (Priority 26-30)
('message.control_id', 'essential', 'message', 'Message Control ID', 26, 'MSH-10', 'MessageHeader.id', 'identifier', 'MSG123456'),
('message.timestamp', 'essential', 'message', 'Message Timestamp', 27, 'MSH-7', 'MessageHeader.timestamp', 'datetime', '202401151030'),
('message.type', 'essential', 'message', 'Message Type', 28, 'MSH-9.1', 'MessageHeader.eventCoding.code', 'code', 'ADT'),
('message.event', 'essential', 'message', 'Message Event', 29, 'MSH-9.2', 'MessageHeader.eventCoding.display', 'code', 'A01'),
('message.version', 'essential', 'message', 'HL7 Version', 30, 'MSH-12', 'MessageHeader.definition', 'version', '2.3');

-- Schema version for migration tracking
CREATE TABLE schema_version (
    version TEXT PRIMARY KEY,
    applied_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    description TEXT
);

INSERT INTO schema_version (version, description) VALUES
('2.0.0', 'Complete schema with HL7 v2.3 structure + semantic path integration');