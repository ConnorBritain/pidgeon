# Contributing to Segmint HL7

Thank you for your interest in contributing to Segmint HL7! This document outlines the contribution guidelines for our dual-licensed project.

## 🏗️ Project Structure & Licensing

Segmint HL7 operates under a **dual licensing model**:

### Open Source (MPL 2.0)
These components welcome community contributions:
- Core HL7 engine (`app/core/`, `app/field_types/`, `app/segments/`, `app/messages/`)
- CLI interface (`app/cli/`)
- Validation system (`app/validation/`)
- Configuration tools (`app/config_analyzer/`, `app/config_library/`)
- Installation utilities (`install.py`, `environments/`)

### Proprietary (Closed Source)
These components are not open for external contributions:
- GUI application (`app/gui/`, `segmint.py`)
- Cloud features and credit system
- Premium templates and workflows

## 🤝 How to Contribute

### 1. Before You Start

- **Check the scope**: Ensure your contribution targets open-source components only
- **Review existing issues**: Look for related discussions or feature requests
- **Discuss major changes**: Open an issue first for significant modifications

### 2. Setting Up Development Environment

```bash
# Clone the repository
git clone <repository-url>
cd hl7generator

# Install in development mode
python install.py

# Run tests
python -m pytest tests/

# Test CLI functionality
python segmint_launcher.py --help
```

### 3. Code Standards

#### General Guidelines
- **Follow PEP 8** for Python code style
- **Write comprehensive tests** for new functionality
- **Document public APIs** with clear docstrings
- **Maintain healthcare compliance** awareness in all changes

#### HL7-Specific Standards
- **Preserve message structure** integrity per HL7 v2.3 specification
- **Validate field types** according to HL7 data type definitions
- **Maintain HIPAA compliance** for synthetic data generation
- **Test with realistic data** while ensuring no PHI exposure

### 4. Testing Requirements

#### Required Tests
- **Unit tests** for new classes and functions
- **Integration tests** for HL7 message generation/validation
- **Cross-platform tests** for Windows, macOS, Linux compatibility
- **Healthcare compliance tests** for synthetic data safety

#### Test Coverage
- Minimum 80% code coverage for new code
- All public APIs must have tests
- Critical healthcare validation logic requires 100% coverage

### 5. Pull Request Process

#### Before Submitting
1. **Run the test suite**: `python -m pytest tests/`
2. **Check code style**: Use `flake8` or `black` for formatting
3. **Update documentation**: Reflect API changes in docstrings/docs
4. **Test installation**: Verify `python install.py` works correctly

#### PR Requirements
- **Clear description** of changes and motivation
- **Reference related issues** using `Fixes #123` syntax
- **Include tests** for new functionality
- **Update relevant documentation**
- **Maintain backward compatibility** where possible

#### Review Process
- All PRs require review from maintainers
- Healthcare-related changes need additional domain expert review
- Breaking changes require community discussion

## 📝 License Agreement

### Contributor License Agreement
By contributing to Segmint HL7, you agree that:

1. **Your contributions** will be licensed under **Mozilla Public License 2.0**
2. **You have the right** to submit the work under this license
3. **Your work is original** or you have permission to contribute it
4. **You understand** the dual licensing model and contribution scope

### MPL 2.0 Requirements
- **Source code availability**: All modifications must remain open source
- **File-level copyleft**: Changes to MPL files must remain MPL
- **Compatible mixing**: Can combine with proprietary code in larger works
- **Patent protection**: Contributors grant patent licenses for their contributions

## 🏥 Healthcare Domain Guidelines

### Compliance Considerations
- **No PHI handling**: Never process real patient health information
- **Synthetic data only**: All test data must be artificially generated
- **HIPAA awareness**: Understand healthcare privacy requirements
- **Vendor neutrality**: Avoid implementation bias toward specific vendors

### Clinical Accuracy
- **Age-appropriate medications**: Ensure realistic prescribing patterns
- **Valid medical codes**: Use proper terminology and coding systems
- **Realistic demographics**: Generate plausible but synthetic patient data
- **Safety checks**: Validate dosing and contraindication logic

## 🚀 Development Workflows

### Adding New HL7 Message Types
1. **Research the specification** in HL7 v2.3 standard
2. **Create segment classes** in `app/segments/`
3. **Implement message class** in `app/messages/`
4. **Add validation rules** in `app/validation/`
5. **Create test cases** with synthetic examples
6. **Update CLI commands** for new message type

### Extending Field Types
1. **Review HL7 data type specification**
2. **Implement in** `app/field_types/`
3. **Add validation logic**
4. **Create comprehensive tests**
5. **Document usage patterns**

### Improving Configuration Management
1. **Understand existing inference logic**
2. **Enhance analysis capabilities**
3. **Maintain backward compatibility**
4. **Test with diverse HL7 samples**

## 🐛 Bug Reports

### Security Issues
**Do not** report security vulnerabilities in public issues. Contact maintainers directly for:
- PHI exposure risks
- Authentication bypasses
- Code injection vulnerabilities

### Standard Bug Reports
Include:
- **Environment details** (OS, Python version, installation method)
- **Steps to reproduce** the issue
- **Expected vs actual behavior**
- **Sample HL7 messages** (synthetic only)
- **Error logs** with sensitive information removed

## 💡 Feature Requests

### Evaluation Criteria
- **Alignment** with healthcare interface testing goals
- **Community benefit** vs implementation complexity
- **Compatibility** with existing workflows
- **Maintenance burden** for long-term support

### Proposal Format
- **Use case description** with healthcare context
- **Proposed API/interface** design
- **Implementation approach** overview
- **Testing strategy** including edge cases
- **Documentation requirements**

## 📚 Resources

### HL7 Standards
- [HL7 v2.3 Specification](https://www.hl7.org/implement/standards/product_brief.cfm?product_id=140)
- [HL7 Data Types Reference](https://www.hl7.org/fhir/datatypes.html)

### Healthcare Compliance
- [HIPAA Privacy Rule](https://www.hhs.gov/hipaa/for-professionals/privacy/index.html)
- [Healthcare Testing Best Practices](https://www.healthit.gov/test-method)

### Development
- [Mozilla Public License 2.0](https://mozilla.org/MPL/2.0/)
- [Python Healthcare Libraries](https://github.com/python-hl7)

## 🤔 Questions?

- **General questions**: Open a GitHub Discussion
- **Bug reports**: Create a GitHub Issue
- **Security concerns**: Contact maintainers directly
- **Commercial licensing**: See business contact information

---

**Thank you for contributing to better healthcare technology!** 🏥

Your contributions help healthcare developers worldwide build more reliable, compliant, and effective HL7 interfaces.