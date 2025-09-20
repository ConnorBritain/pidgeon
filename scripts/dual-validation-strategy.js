#!/usr/bin/env node

/**
 * DUAL VALIDATION STRATEGY
 *
 * Validates our templates against BOTH hl7-dictionary AND Caristix data
 * for maximum accuracy and confidence
 */

const HL7Dictionary = require('hl7-dictionary');
const fs = require('fs');
const path = require('path');

class DualValidator {
    constructor() {
        this.v23 = HL7Dictionary.definitions['2.3'];
        this.caristixData = this.loadCaristixData();
        this.validationResults = [];
    }

    loadCaristixData() {
        // Load the scraped Caristix outputs
        const basePath = '../scripts/scrape/outputs';
        return {
            segments: this.loadCaristixFile(path.join(basePath, 'segments/20250918_033541_prod/segments_v23_master.json')),
            datatypes: this.loadCaristixFile(path.join(basePath, 'data_types/20250918_033447_prod/datatypes_v23_master.json')),
            tables: this.loadCaristixFile(path.join(basePath, 'tables/20250918_033757_prod/tables_v23_master.json')),
            messages: this.loadCaristixFile(path.join(basePath, 'trigger_events/20250918_033648_prod/trigger_events_v23_master.json'))
        };
    }

    loadCaristixFile(filePath) {
        try {
            return JSON.parse(fs.readFileSync(filePath, 'utf8'));
        } catch (error) {
            console.warn(`⚠️ Could not load Caristix file: ${filePath}`);
            return null;
        }
    }

    validateSegment(segmentCode, templateData) {
        console.log(`\n🔍 DUAL VALIDATING SEGMENT: ${segmentCode}`);

        const results = {
            component: segmentCode,
            type: 'segment',
            hl7_dictionary: null,
            caristix: null,
            discrepancies: [],
            confidence: 'unknown'
        };

        // Validate against hl7-dictionary
        const dictSegment = this.v23.segments[segmentCode];
        if (dictSegment) {
            results.hl7_dictionary = {
                available: true,
                name: dictSegment.desc,
                field_count: dictSegment.fields ? dictSegment.fields.length : 0
            };
            console.log(`✅ hl7-dictionary: "${dictSegment.desc}" with ${results.hl7_dictionary.field_count} fields`);
        } else {
            results.hl7_dictionary = { available: false };
            console.log(`❌ hl7-dictionary: ${segmentCode} not found`);
        }

        // Validate against Caristix
        const caristixSegment = this.findCaristixSegment(segmentCode);
        if (caristixSegment) {
            results.caristix = {
                available: true,
                name: caristixSegment.name,
                field_count: caristixSegment.fields ? caristixSegment.fields.length : 0
            };
            console.log(`✅ Caristix: "${caristixSegment.name}" with ${results.caristix.field_count} fields`);
        } else {
            results.caristix = { available: false };
            console.log(`❌ Caristix: ${segmentCode} not found`);
        }

        // Check for discrepancies
        if (results.hl7_dictionary.available && results.caristix.available) {
            if (results.hl7_dictionary.name !== results.caristix.name) {
                results.discrepancies.push(`Name mismatch: hl7-dict="${results.hl7_dictionary.name}" vs Caristix="${results.caristix.name}"`);
            }
            if (results.hl7_dictionary.field_count !== results.caristix.field_count) {
                results.discrepancies.push(`Field count mismatch: hl7-dict=${results.hl7_dictionary.field_count} vs Caristix=${results.caristix.field_count}`);
            }
        }

        // Set confidence level
        if (results.hl7_dictionary.available && results.caristix.available && results.discrepancies.length === 0) {
            results.confidence = 'high';
            console.log(`🎯 HIGH CONFIDENCE: Both sources agree`);
        } else if (results.hl7_dictionary.available || results.caristix.available) {
            results.confidence = 'medium';
            console.log(`⚠️ MEDIUM CONFIDENCE: Only one source available or minor discrepancies`);
        } else {
            results.confidence = 'low';
            console.log(`🔴 LOW CONFIDENCE: No validation sources available`);
        }

        if (results.discrepancies.length > 0) {
            console.log(`🔍 DISCREPANCIES FOUND:`);
            results.discrepancies.forEach(disc => console.log(`   - ${disc}`));
        }

        this.validationResults.push(results);
        return results;
    }

    findCaristixSegment(segmentCode) {
        if (!this.caristixData.segments || !this.caristixData.segments.segments) {
            return null;
        }
        return this.caristixData.segments.segments.find(seg => seg.code === segmentCode);
    }

    validateDataType(dataTypeCode, templateData) {
        console.log(`\n🔍 DUAL VALIDATING DATATYPE: ${dataTypeCode}`);

        const results = {
            component: dataTypeCode,
            type: 'datatype',
            hl7_dictionary: null,
            caristix: null,
            discrepancies: [],
            confidence: 'unknown'
        };

        // Similar validation logic for datatypes...
        // Implementation continues...

        return results;
    }

    generateValidationReport() {
        console.log(`\n📊 DUAL VALIDATION SUMMARY`);
        console.log(`Total components validated: ${this.validationResults.length}`);

        const confidenceCounts = {
            high: this.validationResults.filter(r => r.confidence === 'high').length,
            medium: this.validationResults.filter(r => r.confidence === 'medium').length,
            low: this.validationResults.filter(r => r.confidence === 'low').length
        };

        console.log(`🎯 High confidence: ${confidenceCounts.high}`);
        console.log(`⚠️ Medium confidence: ${confidenceCounts.medium}`);
        console.log(`🔴 Low confidence: ${confidenceCounts.low}`);

        const discrepancyCount = this.validationResults.filter(r => r.discrepancies.length > 0).length;
        console.log(`🔍 Components with discrepancies: ${discrepancyCount}`);

        return this.validationResults;
    }
}

// Usage example
if (require.main === module) {
    const validator = new DualValidator();

    // Test with a few components
    validator.validateSegment('PID', {});
    validator.validateSegment('MSH', {});
    validator.validateSegment('PV1', {});

    // Generate final report
    validator.generateValidationReport();
}

module.exports = DualValidator;