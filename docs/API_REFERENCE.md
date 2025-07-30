# Segmint HL7 API Reference

Complete API documentation for the Segmint HL7 platform.

## Table of Contents

- [Core Classes](#core-classes)
- [Message Generation](#message-generation)
- [Configuration Management](#configuration-management)
- [Validation Framework](#validation-framework)
- [Synthetic Data Generation](#synthetic-data-generation)
- [Analysis Engine](#analysis-engine)
- [CLI Interface](#cli-interface)

## Core Classes

### HL7Message

Base class for all HL7 message types.

```python
class HL7Message:
    """Base class for HL7 messages."""
    
    def __init__(self, message_type: str = None):
        """Initialize HL7 message."""
        
    def add_segment(self, segment: HL7Segment) -> None:
        """Add a segment to the message."""
        
    def get_segment(self, segment_type: str) -> HL7Segment:
        """Get first segment of specified type."""
        
    def get_segments(self, segment_type: str) -> List[HL7Segment]:
        """Get all segments of specified type."""
        
    def encode(self) -> str:
        """Encode message to HL7 format."""
        
    @classmethod
    def decode(cls, hl7_text: str) -> 'HL7Message':
        """Decode HL7 text to message object."""
```

#### Usage Example
```python
from app.core.messages import RDEMessage

# Create new prescription message
message = RDEMessage()
message.create_standard_segments(
    facility_id="MAIN_HOSPITAL",
    patient_id="PAT123456"
)

# Encode to HL7 format
hl7_text = message.encode()
```

### HL7Segment

Base class for HL7 segments.

```python
class HL7Segment:
    """Base class for HL7 segments."""
    
    def __init__(self, segment_type: str):
        """Initialize segment with type."""
        
    def set_field(self, position: int, value: Any) -> None:
        """Set field value at position."""
        
    def get_field(self, position: int) -> Any:
        """Get field value at position."""
        
    def encode(self) -> str:
        """Encode segment to HL7 format."""
        
    @classmethod
    def decode(cls, segment_text: str) -> 'HL7Segment':
        """Decode HL7 segment text."""
```

#### Usage Example
```python
from app.core.segments import PIDSegment

# Create patient identification segment
pid = PIDSegment()
pid.set_patient_id("PAT123456")
pid.set_patient_name("Smith^John^A")
pid.set_date_of_birth("19800315")
```

### HL7Field

Base class for HL7 field types.

```python
class HL7Field:
    """Base class for HL7 fields."""
    
    def __init__(self, value: Any = None):
        """Initialize field with value."""
        
    def validate(self) -> ValidationResult:
        """Validate field value."""
        
    def encode(self) -> str:
        """Encode field to HL7 format."""
        
    @classmethod
    def decode(cls, field_text: str) -> 'HL7Field':
        """Decode HL7 field text."""
```

## Message Generation

### RDEMessage

Prescription/pharmacy order message (RDE^O11).

```python
class RDEMessage(HL7Message):
    """RDE^O11 Pharmacy/Treatment Encoded Order message."""
    
    def create_standard_segments(
        self,
        facility_id: str,
        patient_id: str = None,
        patient: SyntheticPatient = None
    ) -> None:
        """Create standard RDE segments."""
        
    def add_prescription(
        self,
        medication: SyntheticMedication,
        prescriber_id: str = "DEFAULT_PRESCRIBER"
    ) -> None:
        """Add prescription details to message."""
        
    def add_custom_segment(self, segment: HL7Segment) -> None:
        """Add custom Z-segment."""
```

#### Usage Example
```python
from app.core.messages import RDEMessage
from app.synthetic.data_generator import SyntheticDataGenerator

generator = SyntheticDataGenerator()
patient = generator.generate_patient()
medication = generator.generate_medication()

message = RDEMessage()
message.create_standard_segments("HOSPITAL_A", patient=patient)
message.add_prescription(medication)
```

### ADTMessage

Admission/discharge/transfer message (ADT^A01, ADT^A03, etc.).

```python
class ADTMessage(HL7Message):
    """ADT Admission/Discharge/Transfer message."""
    
    def create_admission_message(
        self,
        facility_id: str,
        patient: SyntheticPatient,
        admission_datetime: str = None
    ) -> None:
        """Create admission message (A01)."""
        
    def create_discharge_message(
        self,
        facility_id: str,
        patient: SyntheticPatient,
        discharge_datetime: str = None
    ) -> None:
        """Create discharge message (A03)."""
```

### ORMMessage

Order message (ORM^O01).

```python
class ORMMessage(HL7Message):
    """ORM^O01 Order message."""
    
    def add_order(
        self,
        order_type: str,
        order_details: Dict[str, Any]
    ) -> None:
        """Add order to message."""
```

### ACKMessage

Acknowledgment message (ACK).

```python
class ACKMessage(HL7Message):
    """ACK Acknowledgment message."""
    
    def create_acknowledgment(
        self,
        original_message: HL7Message,
        ack_code: str = "AA",
        error_message: str = None
    ) -> None:
        """Create acknowledgment for original message."""
```

## Configuration Management

### ConfigurationManager

Manages HL7 interface configurations with versioning and locking.

```python
class ConfigurationManager:
    """Manages HL7 interface configurations."""
    
    def __init__(self, library_path: str):
        """Initialize configuration manager."""
        
    def create_configuration(
        self,
        name: str,
        config_data: Dict[str, Any],
        user: str,
        description: str = None,
        tags: List[str] = None
    ) -> ConfigMetadata:
        """Create new configuration."""
        
    def get_configuration(
        self,
        name: str,
        version: str = None
    ) -> Tuple[Dict[str, Any], ConfigMetadata]:
        """Get configuration data and metadata."""
        
    def update_configuration(
        self,
        name: str,
        config_data: Dict[str, Any],
        user: str,
        change_note: str = None
    ) -> ConfigMetadata:
        """Update existing configuration."""
        
    def lock_configuration(
        self,
        name: str,
        user: str,
        reason: str = None
    ) -> ConfigMetadata:
        """Lock configuration to prevent changes."""
        
    def unlock_configuration(
        self,
        name: str,
        user: str,
        reason: str = None
    ) -> ConfigMetadata:
        """Unlock configuration to allow changes."""
        
    def list_configurations(
        self,
        status: ConfigStatus = None,
        tags: List[str] = None
    ) -> List[ConfigMetadata]:
        """List configurations with optional filtering."""
        
    def get_configuration_history(
        self,
        name: str
    ) -> List[ConfigChange]:
        """Get change history for configuration."""
```

#### Usage Example
```python
from app.config_library.config_manager import ConfigurationManager

manager = ConfigurationManager("./config_library")

# Create new configuration
metadata = manager.create_configuration(
    name="epic_interface",
    config_data=config_dict,
    user="admin@hospital.com",
    description="Epic EMR interface",
    tags=["epic", "production"]
)

# Lock for production
manager.lock_configuration(
    "epic_interface",
    user="admin@hospital.com",
    reason="Production deployment"
)
```

### ConfigurationComparator

Compares configurations to detect changes and assess impact.

```python
class ConfigurationComparator:
    """Compares HL7 configurations for changes."""
    
    def compare_configurations(
        self,
        old_config: Dict[str, Any],
        new_config: Dict[str, Any],
        old_name: str = "Old Configuration",
        new_name: str = "New Configuration"
    ) -> ComparisonReport:
        """Compare two configurations."""
        
    def analyze_vendor_changes(
        self,
        old_messages: List[str],
        new_messages: List[str],
        vendor_name: str
    ) -> VendorChangeReport:
        """Analyze vendor message changes."""
```

#### Usage Example
```python
from app.config_library.config_comparator import ConfigurationComparator

comparator = ConfigurationComparator()

report = comparator.compare_configurations(
    old_config=old_config_dict,
    new_config=new_config_dict,
    old_name="Epic v1.0",
    new_name="Epic v2.0"
)

# Review critical changes
for diff in report.differences:
    if diff.impact == ChangeImpact.CRITICAL:
        print(f"CRITICAL: {diff.description}")
```

## Validation Framework

### ValidationEngine

Coordinates multi-level validation of HL7 messages.

```python
class ValidationEngine:
    """Coordinates multi-level HL7 message validation."""
    
    def __init__(self):
        """Initialize validation engine."""
        
    def validate_message(
        self,
        message: Union[str, HL7Message],
        levels: List[ValidationLevel] = None,
        config: Dict[str, Any] = None
    ) -> List[ValidationResult]:
        """Validate message at specified levels."""
        
    def validate_batch(
        self,
        messages: List[Union[str, HL7Message]],
        levels: List[ValidationLevel] = None
    ) -> Dict[str, List[ValidationResult]]:
        """Validate multiple messages."""
```

#### Usage Example
```python
from app.validation.message_validators import ValidationEngine
from app.validation.base_validator import ValidationLevel

engine = ValidationEngine()

results = engine.validate_message(
    message_text,
    levels=[
        ValidationLevel.SYNTAX,
        ValidationLevel.SEMANTIC,
        ValidationLevel.CLINICAL
    ]
)

for result in results:
    print(f"{result.severity}: {result.message}")
```

### BaseValidator

Abstract base class for all validators.

```python
class BaseValidator(ABC):
    """Base class for HL7 message validators."""
    
    @abstractmethod
    def validate(self, message: HL7Message) -> List[ValidationResult]:
        """Validate message and return results."""
        
    def get_validation_level(self) -> ValidationLevel:
        """Get validation level for this validator."""
```

### ValidationResult

Container for validation results.

```python
class ValidationResult:
    """Container for validation result."""
    
    def __init__(
        self,
        severity: str,
        message: str,
        field: str = None,
        segment: str = None,
        code: str = None
    ):
        """Initialize validation result."""
        
    @property
    def is_error(self) -> bool:
        """Check if result is an error."""
        
    @property
    def is_warning(self) -> bool:
        """Check if result is a warning."""
```

## Synthetic Data Generation

### SyntheticDataGenerator

Generates realistic synthetic healthcare data using AI.

```python
class SyntheticDataGenerator:
    """Generates synthetic healthcare data for HL7 messages."""
    
    def __init__(self, use_ai: bool = True, seed: int = None):
        """Initialize data generator."""
        
    def generate_patient(
        self,
        patient_type: PatientType = PatientType.ADULT,
        gender: str = None
    ) -> SyntheticPatient:
        """Generate synthetic patient data."""
        
    def generate_medication(
        self,
        medication_type: MedicationType = MedicationType.ORAL,
        patient_age: int = None
    ) -> SyntheticMedication:
        """Generate synthetic medication data."""
        
    def generate_address(self) -> Dict[str, str]:
        """Generate synthetic address."""
        
    def generate_phone_number(self) -> str:
        """Generate synthetic phone number."""
```

#### Usage Example
```python
from app.synthetic.data_generator import SyntheticDataGenerator, PatientType

generator = SyntheticDataGenerator(seed=12345)

# Generate pediatric patient
patient = generator.generate_patient(PatientType.PEDIATRIC)

# Generate appropriate medication
medication = generator.generate_medication(patient_age=patient.age)
```

### SyntheticPatient

Container for synthetic patient data.

```python
class SyntheticPatient:
    """Synthetic patient data container."""
    
    def __init__(
        self,
        patient_id: str,
        first_name: str,
        last_name: str,
        date_of_birth: str,
        gender: str,
        address: Dict[str, str] = None,
        phone: str = None,
        medical_record_number: str = None
    ):
        """Initialize synthetic patient."""
        
    @property
    def age(self) -> int:
        """Calculate patient age."""
        
    @property
    def full_name(self) -> str:
        """Get formatted full name."""
```

### MessageTemplateLibrary

Pre-built message templates for common workflows.

```python
class MessageTemplateLibrary:
    """Library of HL7 message templates for common workflows."""
    
    def generate_workflow(
        self,
        workflow_type: WorkflowType,
        patient_count: int = 1,
        facility_id: str = "DEFAULT_FACILITY"
    ) -> List[HL7Message]:
        """Generate complete workflow messages."""
        
    def get_template(
        self,
        template_name: str
    ) -> Dict[str, Any]:
        """Get specific message template."""
```

## Analysis Engine

### MessageAnalyzer

Analyzes HL7 messages to infer interface specifications.

```python
class MessageAnalyzer:
    """Analyzes HL7 messages to understand interface patterns."""
    
    def analyze_message(self, message_text: str) -> Dict[str, Any]:
        """Analyze single HL7 message."""
        
    def analyze_multiple_messages(
        self,
        messages: List[str]
    ) -> Dict[str, Any]:
        """Analyze multiple messages for patterns."""
        
    def extract_vendor_patterns(
        self,
        messages: List[str]
    ) -> Dict[str, Any]:
        """Extract vendor-specific patterns."""
```

### SchemaInferencer

Infers HL7 schema from message analysis.

```python
class SchemaInferencer:
    """Infers HL7 schema from analyzed messages."""
    
    def infer_schema(
        self,
        analysis_results: Dict[str, Any]
    ) -> Dict[str, Any]:
        """Infer schema from analysis results."""
        
    def infer_field_types(
        self,
        field_samples: List[str]
    ) -> str:
        """Infer field type from samples."""
```

### ConfigurationGenerator

Generates interface configurations from analysis.

```python
class ConfigurationGenerator:
    """Generates interface configurations from message analysis."""
    
    def generate_from_analysis(
        self,
        analysis_results: Dict[str, Any],
        interface_name: str,
        version: str = "1.0.0"
    ) -> Dict[str, Any]:
        """Generate configuration from analysis."""
        
    def generate_validation_rules(
        self,
        schema: Dict[str, Any]
    ) -> Dict[str, Any]:
        """Generate validation rules from schema."""
```

## CLI Interface

### Main Commands

The Segmint CLI provides streamlined commands for all platform capabilities.

#### Generate Command
```bash
segmint generate [OPTIONS]
```

Options:
- `--type`: Message type (RDE, ADT, ORM, ACK)
- `--facility`: Facility identifier
- `--count`: Number of messages to generate
- `--output`: Output directory
- `--config`: Configuration file path

#### Workflow Command
```bash
segmint workflow [OPTIONS]
```

Options:
- `--type`: Workflow type (new_prescription, patient_admission, etc.)
- `--patient-count`: Number of patients
- `--output`: Output directory

#### Analyze Command
```bash
segmint analyze [OPTIONS] MESSAGES_FILE
```

Options:
- `--name`: Interface name
- `--generate-config`: Generate configuration file
- `--output`: Output directory

#### Validate Command
```bash
segmint validate [OPTIONS] MESSAGE_FILE
```

Options:
- `--levels`: Validation levels (syntax, semantic, interface, clinical)
- `--config`: Configuration file
- `--report`: Output report file

#### Config Command
```bash
segmint config [SUBCOMMAND] [OPTIONS]
```

Subcommands:
- `list`: List configurations
- `create`: Create new configuration
- `update`: Update configuration
- `lock/unlock`: Lock/unlock configuration
- `compare`: Compare configurations
- `history`: Show change history

## Error Handling

All API functions follow consistent error handling patterns:

```python
try:
    result = api_function()
except ValidationError as e:
    print(f"Validation failed: {e}")
except ConfigurationError as e:
    print(f"Configuration error: {e}")
except HL7ParseError as e:
    print(f"HL7 parsing failed: {e}")
```

## Type Hints

The API uses comprehensive type hints for better IDE support:

```python
from typing import List, Dict, Any, Optional, Union, Tuple
from app.core.types import HL7Message, ValidationResult
```

## Async Support

Some operations support async execution for better performance:

```python
import asyncio
from app.validation.async_validators import AsyncValidationEngine

async def validate_large_batch():
    engine = AsyncValidationEngine()
    results = await engine.validate_batch_async(large_message_list)
    return results
```

This API reference provides comprehensive documentation for integrating with the Segmint HL7 platform programmatically.