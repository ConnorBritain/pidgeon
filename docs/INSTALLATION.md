# Segmint HL7 Installation Guide

Complete installation instructions for Segmint HL7 Generator on all supported platforms.

## System Requirements

### Minimum Requirements
- **Python**: 3.8 or higher
- **Memory**: 512 MB RAM
- **Storage**: 100 MB disk space
- **Operating System**: Windows 10+, macOS 10.14+, Ubuntu 18.04+

### Recommended Requirements
- **Python**: 3.10 or higher
- **Memory**: 2 GB RAM
- **Storage**: 500 MB disk space
- **Display**: 1280x720 resolution for GUI

## Installation Methods

### Method 1: Development Installation (Recommended)

This method is ideal for users who want the latest features and the ability to modify the code.

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd hl7generator
   ```

2. **Create Virtual Environment (Recommended)**
   ```bash
   # Create virtual environment
   python -m venv segmint-env
   
   # Activate virtual environment
   # Windows
   segmint-env\Scripts\activate
   
   # macOS/Linux
   source segmint-env/bin/activate
   ```

3. **Install Dependencies**
   ```bash
   pip install -r requirements.txt
   ```

4. **Install in Development Mode**
   ```bash
   pip install -e .
   ```

5. **Verify Installation**
   ```bash
   # Test CLI
   segmint --help
   
   # Test GUI (if tkinter is available)
   python segmint.py
   ```

### Method 2: Package Installation

This method installs Segmint as a Python package.

1. **Install from Local Source**
   ```bash
   pip install /path/to/hl7generator
   ```

2. **Or Install from Git (if available)**
   ```bash
   pip install git+https://github.com/ConnorBritain/segmint-hl7.git
   ```

3. **Verify Installation**
   ```bash
   segmint --version
   segmint-gui  # If GUI is available
   ```

### Method 3: Direct Script Execution

This method runs Segmint without installation.

1. **Clone Repository**
   ```bash
   git clone <repository-url>
   cd hl7generator
   ```

2. **Install Dependencies**
   ```bash
   pip install -r requirements.txt
   ```

3. **Run Directly**
   ```bash
   # CLI
   python -m app.cli.main --help
   
   # GUI
   python segmint.py
   ```

## Platform-Specific Setup

### Windows

#### Prerequisites
1. **Install Python**
   - Download from [python.org](https://python.org)
   - Ensure "Add Python to PATH" is checked
   - Verify: `python --version`

2. **Install Git** (for development installation)
   - Download from [git-scm.com](https://git-scm.com)
   - Verify: `git --version`

#### GUI Requirements
Windows includes tkinter by default with Python installations.

#### PowerShell Installation (Recommended)
```powershell
# Clone repository
git clone <repository-url>
cd hl7generator

# Use the Windows installer script (handles common issues)
.\windows\install_windows.ps1

# OR manual installation:
# Create virtual environment
python -m venv segmint-env
segmint-env\Scripts\Activate.ps1

# Install dependencies step by step (avoids compilation issues)
pip install click>=8.0.0 python-dateutil>=2.8.0 typing-extensions>=4.0.0
pip install PyYAML>=6.0 deepdiff>=6.0.0
pip install "pydantic>=1.10.0,<2.0.0"  # Use v1 to avoid Rust compilation
pip install -e .

# Test installation
segmint --help
python segmint.py
```

#### Command Prompt Installation
```cmd
# Clone repository
git clone <repository-url>
cd hl7generator

# Use the Windows installer script (handles common issues)
windows\install_windows.bat

# OR manual installation:
# Create virtual environment
python -m venv segmint-env
segmint-env\Scripts\activate.bat

# Install core dependencies (avoids compilation issues)
pip install click>=8.0.0 python-dateutil>=2.8.0 typing-extensions>=4.0.0
pip install PyYAML>=6.0 deepdiff>=6.0.0
pip install "pydantic>=1.10.0,<2.0.0"
pip install -e .

# Test installation
segmint --help
python segmint.py
```

#### Windows-Specific Issues
If you encounter installation problems (especially with pydantic compilation), see the detailed [Windows Installation Guide](../windows/WINDOWS_INSTALL_GUIDE.md) for step-by-step troubleshooting.

### macOS

#### Prerequisites
1. **Install Homebrew** (recommended)
   ```bash
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
   ```

2. **Install Python**
   ```bash
   # Using Homebrew
   brew install python
   
   # Or download from python.org
   # Verify: python3 --version
   ```

3. **Install Git**
   ```bash
   brew install git
   ```

#### GUI Requirements
Install tkinter if not included:
```bash
brew install python-tk
```

#### Installation Steps
```bash
# Clone repository
git clone <repository-url>
cd hl7generator

# Create virtual environment
python3 -m venv segmint-env
source segmint-env/bin/activate

# Install dependencies
pip install -r requirements.txt
pip install -e .

# Test installation
segmint --help
python segmint.py
```

### Linux (Ubuntu/Debian)

#### Prerequisites
```bash
# Update package list
sudo apt update

# Install Python and pip
sudo apt install python3 python3-pip python3-venv

# Install tkinter for GUI
sudo apt install python3-tkinter

# Install Git
sudo apt install git

# Install development tools (optional)
sudo apt install build-essential
```

#### Installation Steps
```bash
# Clone repository
git clone <repository-url>
cd hl7generator

# Create virtual environment
python3 -m venv segmint-env
source segmint-env/bin/activate

# Upgrade pip
pip install --upgrade pip

# Install dependencies
pip install -r requirements.txt
pip install -e .

# Test installation
segmint --help
python segmint.py
```

### Linux (CentOS/RHEL/Fedora)

#### Prerequisites
```bash
# CentOS/RHEL
sudo yum install python3 python3-pip python3-tkinter git

# Fedora
sudo dnf install python3 python3-pip python3-tkinter git
```

#### Installation Steps
```bash
# Same as Ubuntu/Debian steps above
```

## Configuration

### Environment Variables

You can configure Segmint using environment variables:

```bash
# AI Provider API Keys (for enhanced synthetic data)
export OPENAI_API_KEY="your_openai_key_here"
export ANTHROPIC_API_KEY="your_anthropic_key_here"

# Application Settings
export SEGMINT_FACILITY_ID="MAIN_HOSPITAL"
export SEGMINT_CONFIG_DIR="/path/to/configs"
export SEGMINT_DEBUG=true
export SEGMINT_THEME="dark"
```

### API Keys for AI Features

Segmint includes AI-powered synthetic data generation using multiple providers. Configure your API keys to enable enhanced realistic data generation.

**Quick Setup:**
```bash
# Method 1: Environment variable
export OPENAI_API_KEY="sk-your_key_here"

# Method 2: CLI configuration
segmint apikey set openai sk-your_key_here

# Method 3: GUI Settings panel
python segmint.py  # Navigate to Settings → API Keys
```

**Supported Providers:**
- OpenAI (GPT models)
- Anthropic (Claude models)  
- Azure OpenAI (Enterprise)
- Hugging Face (Open source)

See **[API Keys Setup Guide](API_KEYS_SETUP.md)** for complete configuration instructions.

### Configuration Files

Create a configuration file for persistent settings:

```bash
# Create config directory
mkdir -p ~/.segmint

# Create configuration file
cat > ~/.segmint/config.json << EOF
{
  "default_facility": "MAIN_HOSPITAL",
  "gui_theme": "light",
  "validation_levels": ["syntax", "semantic"],
  "auto_save": true
}
EOF
```

## Verification and Testing

### Basic Verification
```bash
# Check CLI installation
segmint --version
segmint --help

# Check Python imports
python -c "from app.messages import RDEMessage; print('✓ Core imports working')"
python -c "from app.gui.main_window import main; print('✓ GUI imports working')"

# Generate test message
segmint generate --type RDE --facility TEST
```

### GUI Verification
```bash
# Launch GUI
python segmint.py

# Alternative launch methods
segmint-gui  # If package was installed
python -m app.gui.main_window
```

### Run Example Scripts
```bash
# Run validation demo
python app/examples/validation_demo.py

# Run message template demo
python app/examples/message_templates.py

# Run synthetic data demo
python app/examples/synthetic_data_demo.py
```

## Troubleshooting

### Common Installation Issues

#### 1. Python Not Found
```bash
# Check Python installation
python --version
python3 --version

# Add Python to PATH (Windows)
# Add C:\Python3X\ and C:\Python3X\Scripts\ to system PATH
```

#### 2. Permission Errors
```bash
# Use virtual environment
python -m venv segmint-env
source segmint-env/bin/activate  # Linux/macOS
segmint-env\Scripts\activate     # Windows

# Or install with --user flag
pip install --user -r requirements.txt
```

#### 3. tkinter Not Available
```bash
# Ubuntu/Debian
sudo apt install python3-tkinter

# CentOS/RHEL/Fedora
sudo yum install python3-tkinter

# macOS with Homebrew
brew install python-tk

# Test tkinter
python -c "import tkinter; print('tkinter available')"
```

#### 4. Import Errors
```bash
# Ensure proper installation
pip install -e .

# Check Python path
python -c "import sys; print(sys.path)"

# Run from project root
cd /path/to/hl7generator
python segmint.py
```

#### 6. Windows: Pydantic Compilation Errors
If you see Rust compilation errors on Windows:

```bash
# Use pydantic v1 instead of v2
pip install "pydantic>=1.10.0,<2.0.0"

# Or use the Windows installer script
.\windows\install_windows.ps1

# See detailed guide
# Review windows/WINDOWS_INSTALL_GUIDE.md for complete troubleshooting
```

#### 7. Windows: Numpy Segmentation Faults
If you get numpy MINGW-W64 experimental build errors:

```bash
# Remove problematic numpy
pip uninstall numpy

# Install stable version
pip install "numpy>=1.21.0,<1.26.0" --only-binary=numpy

# Then install LangChain
pip install "langchain>=0.2.0,<0.3.0" "langchain-openai>=0.1.0,<0.2.0"

# Or use Windows-specific requirements
pip install -r windows/requirements-windows.txt
```

#### 8. Memory Issues
```bash
# Increase available memory
# Close other applications
# Use smaller batch sizes in GUI
# Use CLI for large operations
```

### Getting Help

If you encounter issues:

1. **Check System Requirements**: Verify Python version and dependencies
2. **Review Error Messages**: Look for specific error details
3. **Check Documentation**: Review relevant documentation sections
4. **Search Issues**: Check GitHub issues for similar problems
5. **Create Issue**: Report new bugs with system details

### System Information Script

Create a diagnostic script to gather system information:

```bash
cat > check_system.py << 'EOF'
#!/usr/bin/env python3
import sys
import platform
import subprocess

print("=== Segmint HL7 System Information ===")
print(f"Python Version: {sys.version}")
print(f"Platform: {platform.platform()}")
print(f"Architecture: {platform.architecture()}")

# Check tkinter
try:
    import tkinter
    print("✓ tkinter available")
except ImportError:
    print("✗ tkinter not available")

# Check key dependencies
dependencies = ['click', 'langchain', 'pydantic', 'dateutil']
for dep in dependencies:
    try:
        __import__(dep)
        print(f"✓ {dep} available")
    except ImportError:
        print(f"✗ {dep} not available")

# Check Segmint installation
try:
    from app.messages import RDEMessage
    print("✓ Segmint core modules available")
except ImportError as e:
    print(f"✗ Segmint import error: {e}")
EOF

python check_system.py
```

## Next Steps

After successful installation:

1. **Read the Documentation**
   - [Usage Examples](USAGE_EXAMPLES.md)
   - [API Reference](API_REFERENCE.md)
   - [Architecture Guide](ARCHITECTURE.md)

2. **Try the Examples**
   - Generate your first message
   - Explore the GUI interface
   - Run validation demos

3. **Configure for Your Environment**
   - Set up facility IDs
   - Configure validation rules
   - Customize GUI themes

4. **Integrate with Your Workflow**
   - Set up automation scripts
   - Configure CI/CD integration
   - Create custom configurations

---

**Welcome to Segmint HL7! 🏥**

You're now ready to generate, validate, and manage HL7 messages for your healthcare interface testing needs.