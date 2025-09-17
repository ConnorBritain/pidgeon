# HL7 v2.3 Template Migration Backup Manifest

**Purpose**: Track progress of template-based cleanup migration
**Date Created**: 2025-01-16
**Strategy**: Bulk backup → systematic cleanup → delete backups as completed

## Migration Status Overview

### COMPLETED
- [x] **ADT_A01.json** - Messages (completed: template migration done)

### IN PROGRESS
- [ ] None currently

### BACKUP COMPLETE - READY FOR MIGRATION

#### **Tables** (Priority: High - widely referenced)
- [x] **500 files backed up** to _backup/tables/
- [ ] Status: Ready for template migration

#### **Segments** (Priority: High - core lookup functionality)
- [x] **140 files backed up** to _backup/segments/
- [ ] Status: Ready for template migration

#### **Data Types** (Priority: Medium - foundational but fewer files)
- [x] **47 files backed up** to _backup/datatypes/
- [ ] Status: Ready for template migration

#### **Trigger Events** (Priority: Low - simple structures)
- [x] **337 files backed up** to _backup/triggerevents/
- [ ] Status: Ready for template migration

### BACKUP COMPLETE SUMMARY
- **Total files backed up**: 1,025 files (+ 1 message already migrated)
- **Disk space**: Temporary duplication for safety during migration
- **Next step**: Begin systematic template migration starting with high-priority segments

## High-Priority Cleanup Order
*Per CLEANUP.md strategic remediation*

### **Phase 1: Core Segments**
- [ ] MSH (Message Header) - most critical
- [ ] PID (Patient Identification) - core patient data
- [ ] PV1 (Patient Visit) - encounter information
- [ ] OBR (Observation Request) - lab/radiology orders
- [ ] OBX (Observation/Result) - test results

### **Phase 2: Essential Tables**
- [ ] Table 0001 (Administrative Sex) - widely referenced
- [ ] Table 0002 (Marital Status) - patient demographics
- [ ] Table 0076 (Message Type) - message structure
- [ ] Table 0080 (Nature of Abnormal Testing) - clinical results

### **Phase 3: Foundation Data Types**
- [ ] ST (String) - most common primitive
- [ ] TS (Timestamp) - critical for sequencing
- [ ] CE (Coded Element) - coded values
- [ ] XPN (Extended Person Name) - patient names
- [ ] CX (Extended Composite ID) - patient identifiers

## Backup Structure

```
_backup/
├── segments/           # 140 segment definition backups
├── tables/            # 500 table definition backups
├── datatypes/         # 47 data type definition backups
├── triggerevents/     # 337 trigger event definition backups
├── messages/          # 1 message backup (ADT_A01 already migrated)
└── BACKUP_MANIFEST.md # This progress tracking file
```

## Success Metrics

### **Quality Improvements Expected**
- **File size reduction**: 40-60% smaller JSON files
- **YAGNI elimination**: No generation artifacts or vendor bloat
- **Template compliance**: 100% adherence to new standards
- **Lookup optimization**: Cleaner search results and faster parsing

### **Progress Tracking**
- **Backup files deleted** = migration completed for that file
- **Empty _backup folders** = category completion
- **Final cleanup**: Delete entire _backup folder when migration 100% complete

---

**Remember**: Each completed file should have its backup deleted immediately to maintain clear progress visibility. The shrinking _backup folder is our progress bar!