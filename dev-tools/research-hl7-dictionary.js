#!/usr/bin/env node

/**
 * INTERNAL DEVELOPMENT TOOL - HL7 Dictionary Research
 *
 * Use this BEFORE creating template files to understand the official
 * HL7 v2.3 structure from the battle-tested hl7-dictionary package.
 *
 * This helps us write accurate templates from the start.
 */

const HL7Dictionary = require('hl7-dictionary');

const v23 = HL7Dictionary.definitions['2.3'];

/**
 * Research a datatype structure
 */
function researchDatatype(datatypeName) {
  console.log(`\n=== RESEARCHING DATATYPE: ${datatypeName} ===`);

  const dictData = v23.fields[datatypeName];
  if (!dictData) {
    console.log(`❌ ${datatypeName} not found in HL7 dictionary`);
    return;
  }

  console.log(`📖 Official Name: "${dictData.desc}"`);
  console.log(`📝 Type: ${dictData.subfields && dictData.subfields.length > 0 ? 'COMPOSITE' : 'PRIMITIVE'}`);

  if (dictData.subfields && dictData.subfields.length > 0) {
    console.log(`🔧 Components (${dictData.subfields.length} total):`);
    dictData.subfields.forEach((subfield, index) => {
      const pos = index + 1;
      const required = subfield.opt === 2 ? '(REQUIRED)' : '(optional)';
      const table = subfield.table ? ` [Table ${subfield.table}]` : '';
      console.log(`  ${datatypeName}.${pos}: ${subfield.desc} (${subfield.datatype})${table} ${required}`);
    });
  } else {
    console.log(`📄 Primitive type - no components`);
  }

  console.log(`\n💡 Template Guidance:`);
  console.log(`   - category: "${dictData.subfields && dictData.subfields.length > 0 ? 'composite' : 'primitive'}"`);
  console.log(`   - name: "${dictData.desc}"`);
  if (dictData.subfields && dictData.subfields.length > 0) {
    console.log(`   - Include components section with ${dictData.subfields.length} fields`);
  }
}

/**
 * Research a segment structure
 */
function researchSegment(segmentName) {
  console.log(`\n=== RESEARCHING SEGMENT: ${segmentName} ===`);

  const dictData = v23.segments[segmentName];
  if (!dictData) {
    console.log(`❌ ${segmentName} not found in HL7 dictionary`);
    return;
  }

  console.log(`📖 Official Name: "${dictData.desc}"`);
  console.log(`🔧 Fields (${dictData.fields.length} total):`);

  dictData.fields.forEach((field, index) => {
    const pos = index + 1;
    const opt = field.opt === 1 ? 'optional' : (field.opt === 2 ? 'REQUIRED' : 'conditional');
    const length = field.len ? ` (max ${field.len})` : '';
    const rep = field.rep > 1 ? ` [repeating max ${field.rep}]` : '';
    const table = field.table ? ` [Table ${field.table}]` : '';

    console.log(`  ${segmentName}.${pos}: ${field.desc}`);
    console.log(`    └─ ${field.datatype}${length}${rep}${table} - ${opt}`);
  });

  console.log(`\n💡 Template Guidance:`);
  console.log(`   - name: "${dictData.desc}"`);
  console.log(`   - Include fields section with ${dictData.fields.length} fields`);
  console.log(`   - Pay attention to required vs optional fields`);
}

/**
 * Research a table structure
 */
function researchTable(tableNum) {
  console.log(`\n=== RESEARCHING TABLE: ${tableNum} ===`);

  // Convert table number to integer for library lookup
  const tableNumInt = parseInt(tableNum, 10);
  const dictTable = HL7Dictionary.tables[tableNumInt];

  if (!dictTable) {
    console.log(`❌ Table ${tableNum} (${tableNumInt}) not found in HL7 dictionary`);
    return;
  }

  console.log(`📖 Official Name: "${dictTable.desc}"`);
  console.log(`📊 Values (${Object.keys(dictTable.values).length} total):`);

  Object.entries(dictTable.values).forEach(([code, description]) => {
    console.log(`  ${code.padEnd(8)} - ${description}`);
  });

  console.log(`\n💡 Template Guidance:`);
  console.log(`   - name: "${dictTable.desc}"`);
  console.log(`   - Include values section with ${Object.keys(dictTable.values).length} codes`);
  console.log(`   - table: "${tableNumInt.toString().padStart(4, '0')}"`);
  console.log(`   - file: "${tableNumInt.toString().padStart(4, '0')}.json"`);
}

/**
 * Research message structure (comprehensive library support)
 */
function researchMessage(messageType) {
  console.log(`\n=== RESEARCHING MESSAGE: ${messageType} ===`);

  const dictMessage = v23.messages[messageType];
  if (!dictMessage) {
    console.log(`❌ ${messageType} not found in HL7 dictionary`);
    return;
  }

  console.log(`📖 Official Name: "${dictMessage.desc}"`);
  console.log(`🏗️ Message Structure:`);

  // Debug: Let's see what the actual structure looks like
  console.log(`🔍 Debug - Message structure:`, JSON.stringify(dictMessage, null, 2));

  if (dictMessage.segments && Array.isArray(dictMessage.segments)) {
    dictMessage.segments.forEach((segment, index) => {
    const min = segment.min || 0;
    const max = segment.max || 1;
    const cardinality = min === max ? `[${min}]` : `[${min}..${max}]`;
    const name = segment.name || segment;

    console.log(`  ${(index + 1).toString().padStart(2)}. ${name} ${cardinality} - ${segment.desc || 'Segment'}`);

    // Show nested segments if they exist
    if (segment.segments) {
      segment.segments.forEach((nested, nestedIndex) => {
        const nestedMin = nested.min || 0;
        const nestedMax = nested.max || 1;
        const nestedCard = nestedMin === nestedMax ? `[${nestedMin}]` : `[${nestedMin}..${nestedMax}]`;
        console.log(`      ${(nestedIndex + 1).toString().padStart(2)}. ${nested.name} ${nestedCard} - ${nested.desc || 'Nested segment'}`);
      });
    }
  });

    console.log(`\n💡 Template Guidance:`);
    console.log(`   - name: "${dictMessage.desc}"`);
    console.log(`   - Include segments section with ${dictMessage.segments.length} top-level segments`);
    console.log(`   - Pay attention to segment cardinality (min/max occurrences)`);
    console.log(`   - Consider nested segment structures where present`);
  } else {
    console.log(`⚠️  Message structure format different than expected`);
    console.log(`📝 Available properties:`, Object.keys(dictMessage));
    console.log(`💡 Template Guidance:`);
    console.log(`   - name: "${dictMessage.desc}"`);
    console.log(`   - Message structure needs manual investigation`);
    console.log(`   - Check library documentation for message format`);
  }
}

/**
 * Research trigger event (not supported by library)
 */
function researchTriggerEvent(eventCode) {
  console.log(`\n=== RESEARCHING TRIGGER EVENT: ${eventCode} ===`);
  console.log(`❌ Trigger events not available in hl7-dictionary library`);
  console.log(`📖 REQUIRED: Use official HL7 v2.3 documentation`);
  console.log(`🔗 Reference: https://www.hl7.eu/HL7v2x/v23/std23/hl7.htm`);
  console.log(`💡 Search for "Trigger Event ${eventCode}" in the documentation`);
}

/**
 * Compare multiple similar items
 */
function compareItems(type, items) {
  console.log(`\n=== COMPARING ${type.toUpperCase()}S: ${items.join(', ')} ===`);

  items.forEach(item => {
    const itemUpper = item.toUpperCase();
    let data = null;

    switch (type) {
      case 'datatype':
        data = v23.fields[itemUpper];
        break;
      case 'segment':
        data = v23.segments[itemUpper];
        break;
      case 'table':
        data = HL7Dictionary.tables[parseInt(item, 10)];
        break;
    }

    if (data) {
      const componentCount = type === 'datatype' && data.subfields ? data.subfields.length :
                           type === 'segment' ? data.fields.length :
                           type === 'table' ? Object.keys(data.values).length : 0;

      console.log(`${itemUpper.padEnd(8)} - ${data.desc} (${componentCount} ${type === 'datatype' ? 'components' : type === 'segment' ? 'fields' : 'values'})`);
    } else {
      console.log(`${itemUpper.padEnd(8)} - ❌ Not found`);
    }
  });
}

// CLI interface
const command = process.argv[2];
const target = process.argv[3];

switch (command) {
  case 'datatype':
    if (target) {
      researchDatatype(target.toUpperCase());
    } else {
      console.log('Usage: node research-hl7-dictionary.js datatype <DATATYPE>');
    }
    break;

  case 'segment':
    if (target) {
      researchSegment(target.toUpperCase());
    } else {
      console.log('Usage: node research-hl7-dictionary.js segment <SEGMENT>');
    }
    break;

  case 'table':
    if (target) {
      researchTable(target);
    } else {
      console.log('Usage: node research-hl7-dictionary.js table <TABLE_NUM>');
    }
    break;

  case 'message':
    if (target) {
      researchMessage(target.toUpperCase());
    } else {
      console.log('Usage: node research-hl7-dictionary.js message <MESSAGE_TYPE>');
    }
    break;

  case 'triggerevent':
    if (target) {
      researchTriggerEvent(target.toUpperCase());
    } else {
      console.log('Usage: node research-hl7-dictionary.js triggerevent <EVENT_CODE>');
    }
    break;

  case 'compare':
    const type = target;
    const items = process.argv.slice(4);
    if (type && items.length > 1) {
      compareItems(type, items);
    } else {
      console.log('Usage: node research-hl7-dictionary.js compare <datatype|segment|table> <ITEM1> <ITEM2> [ITEM3...]');
    }
    break;

  case 'list':
    const category = target;
    switch (category) {
      case 'datatypes':
        console.log('\n=== ALL DATATYPES IN HL7 v2.3 ===');
        Object.keys(v23.fields).sort().forEach(dt => {
          const data = v23.fields[dt];
          const type = data.subfields && data.subfields.length > 0 ? 'composite' : 'primitive';
          console.log(`${dt.padEnd(15)} ${type.padEnd(10)} ${data.desc}`);
        });
        break;
      case 'segments':
        console.log('\n=== ALL SEGMENTS IN HL7 v2.3 ===');
        Object.keys(v23.segments).sort().forEach(seg => {
          const data = v23.segments[seg];
          console.log(`${seg.padEnd(8)} ${data.desc}`);
        });
        break;
      case 'tables':
        console.log('\n=== ALL TABLES IN HL7 v2.3 ===');
        Object.entries(HL7Dictionary.tables).sort().forEach(([num, table]) => {
          console.log(`${num.padEnd(6)} ${table.desc}`);
        });
        break;
      case 'messages':
        console.log('\n=== ALL MESSAGES IN HL7 v2.3 ===');
        Object.keys(v23.messages).sort().forEach(msg => {
          const data = v23.messages[msg];
          console.log(`${msg.padEnd(15)} ${data.desc}`);
        });
        break;
      default:
        console.log('Usage: node research-hl7-dictionary.js list <datatypes|segments|tables|messages>');
    }
    break;

  default:
    console.log(`
HL7 Dictionary Research Tool - Use BEFORE creating template files

Commands:
  datatype <NAME>     - Research datatype structure ✅ LIBRARY SUPPORTED
  segment <NAME>      - Research segment structure ✅ LIBRARY SUPPORTED
  table <NUM>         - Research table values ✅ LIBRARY SUPPORTED
  message <TYPE>      - Research message structure ✅ LIBRARY SUPPORTED
  triggerevent <CODE> - Research trigger event ❌ LIBRARY NOT SUPPORTED
  compare <type> <items...> - Compare multiple items
  list <category>     - List all available items

Examples:
  node research-hl7-dictionary.js datatype CX
  node research-hl7-dictionary.js segment MSH
  node research-hl7-dictionary.js table 1     # Creates 0001.json
  node research-hl7-dictionary.js table 0001  # Also works
  node research-hl7-dictionary.js message ADT_A01
  node research-hl7-dictionary.js triggerevent A01
  node research-hl7-dictionary.js compare datatype ST ID NM
  node research-hl7-dictionary.js list datatypes

Use this tool to understand the official HL7 structure BEFORE writing templates.
`);
}