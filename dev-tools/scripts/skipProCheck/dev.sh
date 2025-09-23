#!/bin/bash

# Development script for Pidgeon CLI with Pro tier bypass
# Sets PIDGEON_DEV_MODE=true to enable all Pro/Enterprise features during development

set -e

# Set development mode environment variable
export PIDGEON_DEV_MODE=true

# Build the project
echo "🔨 Building Pidgeon CLI..."
dotnet build src/Pidgeon.CLI/Pidgeon.CLI.csproj --configuration Debug --verbosity minimal

# Run the CLI with all arguments passed through
echo "🚀 Running Pidgeon CLI in development mode (Pro/Enterprise features unlocked)..."
echo "   Environment: PIDGEON_DEV_MODE=true"
echo "   Command: dotnet run --project src/Pidgeon.CLI -- $@"
echo ""

# Execute the command
dotnet run --project src/Pidgeon.CLI -- "$@"