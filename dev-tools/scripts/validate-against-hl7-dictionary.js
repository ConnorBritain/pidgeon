#!/usr/bin/env node

/**
 * INTERNAL DEVELOPMENT TOOL - HL7 Dictionary Validation
 *
 * Use this AFTER creating template files to validate our work against
 * the battle-tested hl7-dictionary package to catch any mistakes.
 *
 * This helps us catch hallucinations and ensure accuracy.
 */

const HL7Dictionary = require('hl7-dictionary');
const fs = require('fs');
const path = require('path');

const v23 = HL7Dictionary.definitions['2.3'];

/**
 * Validate a datatype against hl7-dictionary
 */
function validateDatatype(datatypeName) {
  console.log(`\n=== VALIDATING ${datatypeName} ===`);

  // Load our template-based version
  const ourFile = path.join(__dirname, '../pidgeon/data/standards/hl7v23/datatypes', `${datatypeName.toLowerCase()}.json`);

  if (!fs.existsSync(ourFile)) {
    console.log(`❌ Our file not found: ${ourFile}`);
    return false;
  }

  const ourData = JSON.parse(fs.readFileSync(ourFile, 'utf8'));
  const dictData = v23.fields[datatypeName];

  if (!dictData) {
    console.log(`❌ ${datatypeName} not found in hl7-dictionary`);
    return false;
  }

  console.log(`✅ Found in both sources`);
  console.log(`📖 Dictionary desc: "${dictData.desc}"`);
  console.log(`📋 Our desc: "${ourData.description}"`);

  // Validate components for composite types
  if (dictData.subfields && dictData.subfields.length > 0) {
    console.log(`🔧 Composite type with ${dictData.subfields.length} components`);

    dictData.subfields.forEach((subfield, index) => {
      const componentKey = `${datatypeName}.${index + 1}`;
      console.log(`  ${componentKey}: ${subfield.desc} (${subfield.datatype})`);

      // Check if our data has this component
      if (ourData.components && ourData.components[componentKey]) {
        const ourComponent = ourData.components[componentKey];
        if (ourComponent.dataType === subfield.datatype) {
          console.log(`    ✅ Matches our component`);
        } else {
          console.log(`    ❌ Datatype mismatch: ours=${ourComponent.dataType}, dict=${subfield.datatype}`);
        }
      } else {
        console.log(`    ⚠️  Missing in our data`);
      }
    });
  } else {
    console.log(`📄 Primitive type - no components`);
  }

  return true;
}

/**
 * Validate a table against hl7-dictionary
 */
function validateTable(tableNum) {
  console.log(`\n=== VALIDATING TABLE ${tableNum} ===`);

  // Convert table number to integer for library lookup
  const tableNumInt = parseInt(tableNum, 10);
  const dictTable = HL7Dictionary.tables[tableNumInt];

  if (!dictTable) {
    console.log(`❌ Table ${tableNum} (${tableNumInt}) not found in hl7-dictionary`);
    return false;
  }

  // Load our template-based version using the padded format
  const paddedTableNum = tableNum.toString().padStart(4, '0');
  const ourFile = path.join(__dirname, '../pidgeon/data/standards/hl7v23/tables', `${paddedTableNum}.json`);

  if (!fs.existsSync(ourFile)) {
    console.log(`✅ Found in dictionary but our file not found: ${ourFile}`);
    console.log(`📖 Dictionary desc: "${dictTable.desc}"`);
    console.log(`📊 Dictionary values (${Object.keys(dictTable.values).length} total):`);
    Object.entries(dictTable.values).forEach(([code, description]) => {
      console.log(`  ${code}: ${description}`);
    });
    return false;
  }

  const ourData = JSON.parse(fs.readFileSync(ourFile, 'utf8'));

  console.log(`✅ Found in both sources`);
  console.log(`📖 Dictionary desc: "${dictTable.desc}"`);
  console.log(`📋 Our desc: "${ourData.description}"`);

  console.log(`📊 Validating ${Object.keys(dictTable.values).length} values`);

  let issues = 0;
  Object.entries(dictTable.values).forEach(([code, description]) => {
    console.log(`  ${code}: ${description}`);

    if (ourData.values && ourData.values.find(v => v.code === code)) {
      const ourValue = ourData.values.find(v => v.code === code);
      if (ourValue.description === description) {
        console.log(`    ✅ Matches our value`);
      } else {
        console.log(`    ❌ Description mismatch: ours="${ourValue.description}", dict="${description}"`);
        issues++;
      }
    } else {
      console.log(`    ⚠️  Missing in our data`);
      issues++;
    }
  });

  if (issues === 0) {
    console.log(`\n🎉 All values validated successfully!`);
  } else {
    console.log(`\n⚠️  Found ${issues} issues to fix`);
  }

  return issues === 0;
}

/**
 * Validate a message against hl7-dictionary
 */
function validateMessage(messageType) {
  console.log(`\n=== VALIDATING MESSAGE ${messageType} ===`);

  // Load our template-based version
  const ourFile = path.join(__dirname, '../pidgeon/data/standards/hl7v23/messages', `${messageType.toLowerCase()}.json`);

  if (!fs.existsSync(ourFile)) {
    console.log(`❌ Our file not found: ${ourFile}`);
    return false;
  }

  const ourData = JSON.parse(fs.readFileSync(ourFile, 'utf8'));
  const dictData = v23.messages[messageType];

  if (!dictData) {
    console.log(`❌ ${messageType} not found in hl7-dictionary`);
    return false;
  }

  console.log(`✅ Found in both sources`);
  console.log(`📖 Dictionary desc: "${dictData.desc}"`);
  console.log(`📋 Our desc: "${ourData.description}"`);

  // Debug: Let's see what the actual structure looks like
  console.log(`🔍 Debug - Message structure:`, JSON.stringify(dictData, null, 2));

  if (dictData.segments && Array.isArray(dictData.segments)) {
    console.log(`🏗️ Validating ${dictData.segments.length} segments`);

    let issues = 0;
    dictData.segments.forEach((segment, index) => {
      const segmentName = segment.name || segment;
      console.log(`  ${(index + 1).toString().padStart(2)}. ${segmentName} - ${segment.desc || 'Segment'}`);

      if (ourData.segments && ourData.segments.find(s => s.name === segmentName)) {
        console.log(`    ✅ Matches our message structure`);
      } else {
        console.log(`    ⚠️  Missing in our data`);
        issues++;
      }
    });

    if (issues === 0) {
      console.log(`\n🎉 All segments validated successfully!`);
    } else {
      console.log(`\n⚠️  Found ${issues} issues to fix`);
    }

    return issues === 0;
  } else {
    console.log(`⚠️  Message structure format different than expected`);
    console.log(`📝 Available properties:`, Object.keys(dictData));
    console.log(`💡 Cannot validate - message structure format unknown`);
    return false;
  }
}

/**
 * Validate a segment against hl7-dictionary
 */
function validateSegment(segmentName) {
  console.log(`\n=== VALIDATING SEGMENT ${segmentName} ===`);

  // Load our template-based version
  const ourFile = path.join(__dirname, '../pidgeon/data/standards/hl7v23/segments', `${segmentName.toLowerCase()}.json`);

  if (!fs.existsSync(ourFile)) {
    console.log(`❌ Our file not found: ${ourFile}`);
    return false;
  }

  const ourData = JSON.parse(fs.readFileSync(ourFile, 'utf8'));
  const dictData = v23.segments[segmentName];

  if (!dictData) {
    console.log(`❌ ${segmentName} not found in hl7-dictionary`);
    return false;
  }

  console.log(`✅ Found in both sources`);
  console.log(`📖 Dictionary desc: "${dictData.desc}"`);
  console.log(`📋 Our desc: "${ourData.description}"`);

  console.log(`🔧 Validating ${dictData.fields.length} fields`);

  let issues = 0;
  dictData.fields.forEach((field, index) => {
    const fieldKey = `${segmentName}.${index + 1}`;
    console.log(`  ${fieldKey}: ${field.desc} (${field.datatype})`);

    if (ourData.fields && ourData.fields[fieldKey]) {
      const ourField = ourData.fields[fieldKey];
      if (ourField.dataType === field.datatype) {
        console.log(`    ✅ Matches our field`);
      } else {
        console.log(`    ❌ Datatype mismatch: ours=${ourField.dataType}, dict=${field.datatype}`);
        issues++;
      }
    } else {
      console.log(`    ⚠️  Missing in our data`);
      issues++;
    }
  });

  if (issues === 0) {
    console.log(`\n🎉 All fields validated successfully!`);
  } else {
    console.log(`\n⚠️  Found ${issues} issues to fix`);
  }

  return issues === 0;
}

// CLI interface
const command = process.argv[2];
const target = process.argv[3];

switch (command) {
  case 'datatype':
    if (target) {
      validateDatatype(target.toUpperCase());
    } else {
      console.log('Usage: node validate-against-hl7-dictionary.js datatype <DATATYPE>');
    }
    break;

  case 'segment':
    if (target) {
      validateSegment(target.toUpperCase());
    } else {
      console.log('Usage: node validate-against-hl7-dictionary.js segment <SEGMENT>');
    }
    break;

  case 'table':
    if (target) {
      validateTable(target);
    } else {
      console.log('Usage: node validate-against-hl7-dictionary.js table <TABLE_NUM>');
    }
    break;

  case 'message':
    if (target) {
      validateMessage(target.toUpperCase());
    } else {
      console.log('Usage: node validate-against-hl7-dictionary.js message <MESSAGE_TYPE>');
    }
    break;

  case 'triggerevent':
    console.log(`\n⚠️ TRIGGER EVENT VALIDATION NOT SUPPORTED`);
    console.log(`❌ hl7-dictionary does not include trigger event definitions`);
    console.log(`📖 RECOMMENDATION: Use official HL7 v2.3 documentation for validation`);
    console.log(`🔗 Reference: https://www.hl7.eu/HL7v2x/v23/std23/hl7.htm`);
    break;

  default:
    console.log(`
HL7 Dictionary Validation Tool - Use AFTER creating template files

Commands:
  datatype <NAME>     - Validate our datatype against hl7-dictionary ✅ SUPPORTED
  segment <NAME>      - Validate our segment against hl7-dictionary ✅ SUPPORTED
  table <NUM>         - Validate our table against hl7-dictionary ✅ SUPPORTED
  message <TYPE>      - Validate our message against hl7-dictionary ✅ SUPPORTED
  triggerevent <CODE> - Trigger event validation info ❌ NOT SUPPORTED

Examples:
  node validate-against-hl7-dictionary.js datatype CX
  node validate-against-hl7-dictionary.js segment MSH
  node validate-against-hl7-dictionary.js table 1    # Validates against 0001.json
  node validate-against-hl7-dictionary.js table 0001 # Also works
  node validate-against-hl7-dictionary.js message ADT_A01

Use this tool to catch mistakes AFTER writing templates.
`);
}