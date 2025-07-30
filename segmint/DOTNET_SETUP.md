# .NET Path Setup for dev.sh

The dev.sh script has been modified to support configurable .NET paths.

## Quick Setup Options:

### Option 1: Set Environment Variable (Recommended)
```bash
# Windows (WSL):
export DOTNET_PATH="/mnt/c/Program Files/dotnet/dotnet.exe"

# Linux:
export DOTNET_PATH="/usr/share/dotnet/dotnet"

# Then run:
./dev.sh build
```

### Option 2: Modify dev.sh Directly
Edit line 41 in dev.sh to set your dotnet path:
```bash
DOTNET_PATH="/mnt/c/Program Files/dotnet/dotnet.exe"  # Your path here
```

### Option 3: One-time Run
```bash
DOTNET_PATH="/mnt/c/Program Files/dotnet/dotnet.exe" ./dev.sh build
```

## Common .NET Paths:
- **Windows**: `C:\Program Files\dotnet\dotnet.exe`
- **WSL**: `/mnt/c/Program Files/dotnet/dotnet.exe`
- **Linux**: `/usr/share/dotnet/dotnet` or `/usr/bin/dotnet`
- **macOS**: `/usr/local/share/dotnet/dotnet`

## Restore Original:
```bash
cp dev.sh.backup dev.sh
```