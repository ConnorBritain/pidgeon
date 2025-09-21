@echo off
REM Development script for Pidgeon CLI with Pro tier bypass
REM Sets PIDGEON_DEV_MODE=true to enable all Pro/Enterprise features during development

REM Set development mode environment variable
set PIDGEON_DEV_MODE=true

REM Build the project
echo 🔨 Building Pidgeon CLI...
dotnet build src/Pidgeon.CLI/Pidgeon.CLI.csproj --configuration Debug --verbosity minimal

REM Run the CLI with all arguments passed through
echo 🚀 Running Pidgeon CLI in development mode (Pro/Enterprise features unlocked)...
echo    Environment: PIDGEON_DEV_MODE=true
echo    Command: dotnet run --project src/Pidgeon.CLI -- %*
echo.

REM Execute the command
dotnet run --project src/Pidgeon.CLI -- %*