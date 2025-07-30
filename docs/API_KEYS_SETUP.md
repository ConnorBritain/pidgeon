# API Keys Setup Guide

This guide explains how to configure API keys for AI-powered synthetic data generation in Segmint HL7.

## Overview

Segmint HL7 supports multiple AI providers for generating realistic synthetic healthcare data. API keys are **optional** - the application works without them using high-quality fallback data generation.

### Supported AI Providers

| Provider | Models | Use Case | Cost |
|----------|---------|----------|------|
| **OpenAI** | GPT-3.5, GPT-4 | High-quality synthetic data | Pay-per-use |
| **Anthropic** | Claude | Healthcare-appropriate content | Pay-per-use |
| **Azure OpenAI** | GPT models via Azure | Enterprise deployments | Azure pricing |
| **Hugging Face** | Open-source models | Custom/local models | Free tier available |

## Configuration Methods

### Method 1: Environment Variables (Recommended)

Set environment variables that Segmint automatically detects:

#### Windows
```cmd
# Command Prompt
set OPENAI_API_KEY=your_api_key_here

# PowerShell
$env:OPENAI_API_KEY="your_api_key_here"

# Permanent (add to System Environment Variables)
# Windows Key + R → "sysdm.cpl" → Environment Variables
```

#### macOS/Linux
```bash
# Temporary (current session)
export OPENAI_API_KEY="your_api_key_here"

# Permanent (add to ~/.bashrc or ~/.zshrc)
echo 'export OPENAI_API_KEY="your_api_key_here"' >> ~/.bashrc
source ~/.bashrc
```

#### Supported Environment Variables
- **OpenAI**: `OPENAI_API_KEY` or `OPENAI_KEY`
- **Anthropic**: `ANTHROPIC_API_KEY` or `ANTHROPIC_KEY`
- **Azure OpenAI**: `AZURE_OPENAI_API_KEY` or `AZURE_OPENAI_KEY`
- **Hugging Face**: `HUGGINGFACE_API_KEY` or `HF_TOKEN`

### Method 2: CLI Configuration

Use the command-line interface to manage API keys:

```bash
# Set API key for OpenAI
segmint apikey set openai your_api_key_here

# List configured providers
segmint apikey list

# Test API key
segmint apikey test openai

# Remove API key
segmint apikey remove openai
```

#### CLI Options
```bash
# Set with additional options
segmint apikey set openai your_key \
    --organization org-123456 \
    --model gpt-3.5-turbo

# Set Azure OpenAI with custom endpoint
segmint apikey set azure_openai your_key \
    --api-base https://your-resource.openai.azure.com/
```

### Method 3: GUI Configuration

Use the graphical interface for secure API key management:

1. **Launch Segmint GUI**
   ```bash
   python segmint.py
   ```

2. **Open Settings Panel**
   - Click "⚙️ Settings" in the top navigation
   - Or click "⚙️ Settings" in the sidebar

3. **Configure API Keys**
   - Navigate to "🔑 API Keys" tab
   - Enter your API key for the desired provider
   - Click "💾 Save" to store securely
   - Click "🧪 Test" to verify the key

### Method 4: Configuration File

API keys are stored in a secure configuration file:

#### Location
- **Windows**: `%LOCALAPPDATA%\Segmint\api_keys.json`
- **macOS**: `~/.config/segmint/api_keys.json`
- **Linux**: `~/.config/segmint/api_keys.json`

#### Format
```json
{
  "openai": {
    "api_key": "sk-...",
    "organization": "org-123456",
    "model": "gpt-3.5-turbo"
  },
  "anthropic": {
    "api_key": "sk-ant-...",
    "model": "claude-3-haiku-20240307"
  }
}
```

⚠️ **Security Note**: The configuration file has restricted permissions (600 on Unix systems) to protect your API keys.

## Getting API Keys

### OpenAI
1. Visit [OpenAI Platform](https://platform.openai.com/signup)
2. Create an account or sign in
3. Navigate to [API Keys](https://platform.openai.com/api-keys)
4. Click "Create new secret key"
5. Copy the key (starts with `sk-`)

**Cost**: Pay-per-use, approximately $0.002 per 1K tokens

### Anthropic
1. Visit [Anthropic Console](https://console.anthropic.com/)
2. Create an account or sign in
3. Navigate to API Keys section
4. Generate a new API key
5. Copy the key (starts with `sk-ant-`)

**Cost**: Pay-per-use, pricing varies by model

### Azure OpenAI
1. Sign up for [Azure OpenAI Service](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
2. Create an OpenAI resource in Azure Portal
3. Get your API key from the resource's "Keys and Endpoint" section
4. Note your endpoint URL

**Cost**: Azure pricing model, includes enterprise features

### Hugging Face
1. Visit [Hugging Face](https://huggingface.co/join)
2. Create a free account
3. Navigate to [Settings → Access Tokens](https://huggingface.co/settings/tokens)
4. Create a new token with "Read" permissions
5. Copy the token

**Cost**: Free tier available, paid plans for higher usage

## Usage Examples

### CLI with API Key
```bash
# Set API key first
export OPENAI_API_KEY="your_key_here"

# Generate message with AI
segmint generate --type RDE --facility DEMO_HOSPITAL --count 5

# Generate workflow with AI-powered data
segmint workflow --type new_prescription --output ./messages/
```

### GUI with API Key
1. Configure API key in Settings
2. Navigate to Message Generator
3. Enable "Use AI for synthetic data" (automatically enabled if key is configured)
4. Generate messages with realistic, AI-powered synthetic data

### Python API with API Key
```python
from app.synthetic.data_generator import SyntheticDataGenerator

# Using environment variable (recommended)
generator = SyntheticDataGenerator(use_ai=True)

# Using explicit key
generator = SyntheticDataGenerator(
    openai_api_key="your_key_here",
    use_ai=True
)

# Generate realistic patient
patient = generator.generate_patient()
print(f"Generated: {patient.full_name}")
```

## Features Enabled by API Keys

### Without API Keys (Fallback Mode)
- ✅ **High-quality synthetic data** from curated datasets
- ✅ **All message types** (RDE, ADT, ORM, ACK, etc.)
- ✅ **Realistic demographics** and clinical data
- ✅ **HIPAA-compliant** synthetic data
- ✅ **Full application functionality**

### With API Keys (AI Mode)
- 🚀 **Enhanced realism** with AI-generated content
- 🚀 **Context-aware generation** (age-appropriate medications, etc.)
- 🚀 **Dynamic narrative generation** for clinical notes
- 🚀 **Specialized patient types** (pediatric, geriatric, etc.)
- 🚀 **Advanced medication interactions**
- 🚀 **Realistic diagnostic correlations**

## Security Best Practices

### DO ✅
- **Use environment variables** for production deployments
- **Store keys securely** using the GUI or CLI
- **Use least-privilege keys** (read-only when possible)
- **Rotate keys regularly** (every 90 days)
- **Monitor usage** through your provider's dashboard

### DON'T ❌
- **Never commit API keys** to version control
- **Don't share keys** via email or chat
- **Don't use production keys** for development
- **Don't hardcode keys** in scripts
- **Don't use overprivileged keys**

### Key Rotation
```bash
# Update key using CLI
segmint apikey set openai new_api_key_here

# Test new key
segmint apikey test openai

# Remove old key (if using different storage)
segmint apikey remove openai
```

## Troubleshooting

### Common Issues

#### 1. "No API key configured"
```bash
# Check if key is set
segmint apikey list

# Set key if missing
segmint apikey set openai your_key_here
```

#### 2. "API key test failed"
```bash
# Test the key
segmint apikey test openai

# Common causes:
# - Invalid key format
# - Expired key
# - Insufficient credits
# - Network/firewall issues
```

#### 3. "Using fallback data generation"
This is normal! The application works without API keys. To enable AI features:
```bash
# Verify key is configured
echo $OPENAI_API_KEY

# Or check in GUI Settings
```

#### 4. Corporate Network Issues
```bash
# If behind proxy, configure your provider's settings
# For OpenAI, you may need to whitelist:
# - api.openai.com
# - Platform-specific endpoints
```

### Verification Commands
```bash
# Check environment
echo $OPENAI_API_KEY

# Test CLI
segmint --openai-key your_key_here generate --type RDE --facility TEST

# List all configured providers
segmint apikey list

# Test specific provider
segmint apikey test openai
```

## Cost Management

### Monitoring Usage
- **OpenAI**: [Usage Dashboard](https://platform.openai.com/usage)
- **Anthropic**: Check your console for usage statistics
- **Azure**: Azure Cost Management + Billing

### Reducing Costs
1. **Use smaller models** (GPT-3.5 vs GPT-4)
2. **Cache generated data** for repeated use
3. **Use fallback mode** for development/testing
4. **Set usage limits** in your provider dashboard
5. **Generate in batches** to reduce API calls

### Typical Costs
For generating 100 HL7 messages with patient data:
- **OpenAI GPT-3.5**: ~$0.10-0.50
- **OpenAI GPT-4**: ~$1.00-3.00
- **Anthropic Claude**: ~$0.25-1.00
- **Azure OpenAI**: Similar to OpenAI with Azure markup

## Support

If you encounter issues with API key configuration:

1. **Check the logs** for specific error messages
2. **Verify key format** (should start with provider prefix)
3. **Test network connectivity** to provider endpoints
4. **Review provider documentation** for any recent changes
5. **Use fallback mode** as a workaround

Remember: **Segmint HL7 works perfectly without API keys** using high-quality synthetic data generation. API keys simply enhance the realism and variety of generated content.

---

**Next Steps**: After configuring API keys, see the [Usage Examples](USAGE_EXAMPLES.md) guide for practical examples of generating realistic HL7 messages.