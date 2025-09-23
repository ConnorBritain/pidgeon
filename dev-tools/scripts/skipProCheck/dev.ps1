# Development script for Pidgeon CLI with Pro tier bypass
# Sets PIDGEON_DEV_MODE=true to enable all Pro/Enterprise features during development

# Set development mode environment variable
$env:PIDGEON_DEV_MODE = "true"

# Build the project
Write-Host "🔨 Building Pidgeon CLI..." -ForegroundColor Green
dotnet build src/Pidgeon.CLI/Pidgeon.CLI.csproj --configuration Debug --verbosity minimal

# Run the CLI with all arguments passed through
Write-Host "🚀 Running Pidgeon CLI in development mode (Pro/Enterprise features unlocked)..." -ForegroundColor Green
Write-Host "   Environment: PIDGEON_DEV_MODE=true" -ForegroundColor Gray
Write-Host "   Command: dotnet run --project src/Pidgeon.CLI -- $args" -ForegroundColor Gray
Write-Host ""

# Execute the command
dotnet run --project src/Pidgeon.CLI -- @args