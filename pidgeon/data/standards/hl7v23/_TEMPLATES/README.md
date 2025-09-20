# HL7 v2.3 JSON Templates

**Purpose**: Pristine, lean templates for consistent HL7 v2.3 standard definition files.

## Design Principles

### ✅ **Include (Essential for Lookup)**
- **Identity**: segment/dataType/table/messageType/triggerEvent + name
- **Core attributes**: description, usage, cardinality, dataType
- **Relationships**: usedIn, components, structure
- **Standard metadata**: standard, version
- **User value**: examples, notes (when critical)

### ❌ **Exclude (YAGNI)**
- Verbose generation hints
- Complex vendor variations
- Over-detailed generationRules
- Extensive fieldGuidance objects
- Nested scenario objects
- Heavy messageTypes arrays

## Template Usage

### **Segments** (`segment_template.json`)
**Core Pattern**: segment → fields → components
- Focus on field definitions with clear datatypes
- Include table references for coded fields
- Keep examples minimal but representative
- Notes only for critical constraints

### **Data Types** (`datatype_template.json`)
**Core Pattern**: dataType → components → usage
- Clear component structure for composite types
- Length constraints for validation
- Common usage examples
- Encoding rules for parsing

### **Tables** (`table_template.json`)
**Core Pattern**: table → values → usedIn
- Complete code/description pairs
- Clear indication of where used
- Type classification (hl7_defined vs user_defined)
- Minimal notes for context

### **Messages** (`message_template.json`)
**Core Pattern**: messageType/triggerEvent → structure → useCases
- Clean segment sequence with cardinality
- Required vs optional distinction
- Key constraints in notes
- Primary use cases for context

### **Trigger Events** (`triggerevent_template.json`)
**Core Pattern**: triggerEvent → purpose → segments
- Clear business purpose
- Required/optional segment lists
- Response message identification
- Timing context

## Field Patterns

### **Cardinality**
```json
"cardinality": {"min": 0, "max": 1}        // Standard
"cardinality": {"min": 0, "max": "unbounded"}  // Repeating
```

### **Usage Codes**
- `R` - Required
- `O` - Optional
- `C` - Conditional (include condition in notes)

### **Standard Metadata**
Always include:
```json
"standard": "hl7v23",
"version": "2.3"
```

## Lookup Optimization

Templates are optimized for:
1. **Pattern Detection**: Clear field paths (PID.3.5)
2. **Smart Search**: Searchable descriptions and names
3. **Cross-Reference**: usedIn, components, structure relationships
4. **Human Display**: Clean names, descriptions, examples
5. **Machine Processing**: Consistent schema, predictable nesting

## Quality Guidelines

### **Descriptions**
- Start with purpose/function
- Avoid redundant "This field contains..."
- Focus on business meaning
- Keep under 200 characters

### **Examples**
- Real-world representative values
- Include format demonstration
- 2-3 examples maximum
- Show variation, not repetition

### **Notes**
- Only critical constraints or relationships
- Avoid obvious information
- Focus on implementation gotchas
- Keep array under 5 items

---

**Result**: Lean, consistent, lookup-optimized JSON definitions that eliminate bloat while maximizing utility for the `pidgeon lookup` command.