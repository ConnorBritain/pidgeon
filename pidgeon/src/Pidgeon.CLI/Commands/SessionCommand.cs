// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.CLI.Services;
using System.CommandLine;
using System.Text.Json;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for comprehensive session management.
/// Provides explicit control over session lifecycle, import/export, and status.
/// </summary>
public class SessionCommand : CommandBuilderBase
{
    private readonly ILockSessionService _lockSessionService;
    private readonly SessionHelper _sessionHelper;

    public SessionCommand(
        ILogger<SessionCommand> logger,
        ILockSessionService lockSessionService,
        SessionHelper sessionHelper)
        : base(logger)
    {
        _lockSessionService = lockSessionService;
        _sessionHelper = sessionHelper;
    }

    public override Command CreateCommand()
    {
        var command = new Command("session", "Manage sessions for maintaining field values across commands");

        // Add subcommands
        command.Add(CreateStatusCommand());
        command.Add(CreateSaveCommand());
        command.Add(CreateCreateCommand());
        command.Add(CreateUseCommand());
        command.Add(CreateListCommand());
        command.Add(CreateShowCommand());
        command.Add(CreateClearCommand());
        command.Add(CreateRemoveCommand());
        command.Add(CreateExportCommand());
        command.Add(CreateImportCommand());

        // Default action when no subcommand is provided
        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            return await HandleStatusCommand(cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session (default) - Show current session status
    /// </summary>
    private Command CreateStatusCommand()
    {
        var command = new Command("status", "Show current session status");

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            return await HandleStatusCommand(cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session save <name> - Save temporary session permanently
    /// </summary>
    private Command CreateSaveCommand()
    {
        var command = new Command("save", "Save temporary session with a permanent name");

        var nameArg = new Argument<string>("name")
        {
            Description = "Name for the permanent session"
        };
        command.Add(nameArg);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameArg)!;
            return await HandleSaveCommand(name, cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session create <name> - Create new named session and switch to it
    /// </summary>
    private Command CreateCreateCommand()
    {
        var command = new Command("create", "Create new named session and switch to it");

        var nameArg = new Argument<string>("name")
        {
            Description = "Name for the new session"
        };
        var descriptionOption = CreateNullableOption("--description", "Description for the session");

        command.Add(nameArg);
        command.Add(descriptionOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameArg)!;
            var description = parseResult.GetValue(descriptionOption);
            return await HandleCreateCommand(name, description, cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session use <name> - Switch to existing session
    /// </summary>
    private Command CreateUseCommand()
    {
        var command = new Command("use", "Switch to existing session");

        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the session to switch to"
        };
        command.Add(nameArg);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameArg)!;
            return await HandleUseCommand(name, cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session list - List all sessions with status
    /// </summary>
    private Command CreateListCommand()
    {
        var command = new Command("list", "List all sessions with status");

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            return await HandleListCommand(cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session show [name] - Show session details
    /// </summary>
    private Command CreateShowCommand()
    {
        var command = new Command("show", "Show session details");

        var nameArg = new Argument<string?>("name")
        {
            Description = "Name of session to show (current session if not specified)",
            Arity = ArgumentArity.ZeroOrOne
        };
        command.Add(nameArg);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameArg);
            return await HandleShowCommand(name, cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session clear - Clear current session
    /// </summary>
    private Command CreateClearCommand()
    {
        var command = new Command("clear", "Clear current session");

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            return await HandleClearCommand(cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session remove <name> - Remove specific session
    /// </summary>
    private Command CreateRemoveCommand()
    {
        var command = new Command("remove", "Remove specific session");

        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the session to remove"
        };
        command.Add(nameArg);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameArg)!;
            return await HandleRemoveCommand(name, cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session export <file> - Export session as template
    /// </summary>
    private Command CreateExportCommand()
    {
        var command = new Command("export", "Export session as template");

        var fileArg = new Argument<string>("file")
        {
            Description = "Output file path (JSON format)"
        };
        var sessionOption = CreateNullableOption("--session", "Session to export (current if not specified)");

        command.Add(fileArg);
        command.Add(sessionOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var sessionName = parseResult.GetValue(sessionOption);
            return await HandleExportCommand(file, sessionName, cancellationToken);
        });

        return command;
    }

    /// <summary>
    /// pidgeon session import <file> - Import session template
    /// </summary>
    private Command CreateImportCommand()
    {
        var command = new Command("import", "Import session template");

        var fileArg = new Argument<string>("file")
        {
            Description = "Template file path (JSON format)"
        };
        var nameOption = CreateNullableOption("--name", "Name for imported session (derived from file if not specified)");

        command.Add(fileArg);
        command.Add(nameOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var name = parseResult.GetValue(nameOption);
            return await HandleImportCommand(file, name, cancellationToken);
        });

        return command;
    }

    // Command Handlers

    private async Task<int> HandleStatusCommand(CancellationToken cancellationToken)
    {
        var currentSession = await _sessionHelper.GetCurrentSessionAsync(cancellationToken);

        if (currentSession == null)
        {
            Console.WriteLine("❌ No active session");
            Console.WriteLine("💡 Create a session by setting a value:");
            Console.WriteLine("   pidgeon set patient.mrn \"TEST123\"");
            Console.WriteLine("💡 Or create a named session:");
            Console.WriteLine("   pidgeon session create my_scenario");
            return 0;
        }

        var sessionResult = await _lockSessionService.GetSessionAsync(currentSession, cancellationToken);
        if (sessionResult.IsFailure)
        {
            Console.WriteLine($"❌ Current session '{currentSession}' not found");
            await _sessionHelper.ClearCurrentSessionAsync(cancellationToken);
            return 1;
        }

        var session = sessionResult.Value;
        var isTemporary = await _sessionHelper.IsTemporarySessionAsync(currentSession, cancellationToken);

        Console.WriteLine($"🔒 Current Session: {currentSession}");
        Console.WriteLine($"   Type: {(isTemporary ? "Temporary (expires in 24h unless saved)" : "Permanent")}");
        Console.WriteLine($"   Values: {session.LockedValues.Count} field(s) set");
        Console.WriteLine($"   Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");

        if (session.LockedValues.Any())
        {
            Console.WriteLine();
            Console.WriteLine("💡 Commands:");
            Console.WriteLine("   pidgeon set --list                 # Show all values");
            Console.WriteLine("   pidgeon generate \"ADT^A01\"          # Use in generation");
            if (isTemporary)
            {
                Console.WriteLine("   pidgeon session save <name>       # Save permanently");
            }
        }

        return 0;
    }

    private async Task<int> HandleSaveCommand(string name, CancellationToken cancellationToken)
    {
        var currentSession = await _sessionHelper.GetCurrentSessionAsync(cancellationToken);
        if (currentSession == null)
        {
            Console.WriteLine("❌ No active session to save");
            Console.WriteLine("💡 Create a session first:");
            Console.WriteLine("   pidgeon set patient.mrn \"TEST123\"");
            return 1;
        }

        var isTemporary = await _sessionHelper.IsTemporarySessionAsync(currentSession, cancellationToken);
        if (!isTemporary)
        {
            Console.WriteLine($"❌ Session '{currentSession}' is already permanent");
            Console.WriteLine("💡 Use 'pidgeon session create <name>' to create a new session");
            return 1;
        }

        var result = await _sessionHelper.SaveTemporarySessionAsync(currentSession, name, cancellationToken);
        if (result.IsFailure)
        {
            Console.WriteLine($"❌ Failed to save session: {result.Error.Message}");
            return 1;
        }

        Console.WriteLine($"✅ Saved session: {currentSession} → {name}");
        Console.WriteLine("ℹ️  Permanent sessions persist until manually deleted");
        return 0;
    }

    private async Task<int> HandleCreateCommand(string name, string? description, CancellationToken cancellationToken)
    {
        try
        {
            var sessionName = await _sessionHelper.CreateNamedSessionAsync(name, description, cancellationToken);
            Console.WriteLine($"✅ Created and switched to session: {sessionName}");
            Console.WriteLine("💡 Set values with: pidgeon set <field> <value>");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            Console.WriteLine("💡 Use a different name or remove the existing session first");
            return 1;
        }
    }

    private async Task<int> HandleUseCommand(string name, CancellationToken cancellationToken)
    {
        var sessionResult = await _lockSessionService.GetSessionAsync(name, cancellationToken);
        if (sessionResult.IsFailure)
        {
            Console.WriteLine($"❌ Session '{name}' not found");
            Console.WriteLine("💡 List available sessions: pidgeon session list");
            return 1;
        }

        await _sessionHelper.SetCurrentSessionAsync(name, isTemporary: false, cancellationToken);

        var session = sessionResult.Value;
        Console.WriteLine($"✅ Current session: {name} ({session.LockedValues.Count} fields set)");
        Console.WriteLine("💡 Use 'pidgeon set --list' to see all field values");

        return 0;
    }

    private async Task<int> HandleListCommand(CancellationToken cancellationToken)
    {
        var sessions = await _lockSessionService.ListSessionsAsync(cancellationToken);
        if (sessions.IsFailure)
        {
            Console.WriteLine($"❌ Failed to list sessions: {sessions.Error.Message}");
            return 1;
        }

        var currentSession = await _sessionHelper.GetCurrentSessionAsync(cancellationToken);

        if (!sessions.Value.Any())
        {
            Console.WriteLine("No sessions found.");
            Console.WriteLine("💡 Create a session:");
            Console.WriteLine("   pidgeon set patient.mrn \"TEST123\"     # Auto-creates temporary session");
            Console.WriteLine("   pidgeon session create my_scenario     # Creates named session");
            return 0;
        }

        Console.WriteLine("📋 Available Sessions:");
        Console.WriteLine();

        foreach (var session in sessions.Value.OrderBy(s => s.Name))
        {
            var isTemporary = await _sessionHelper.IsTemporarySessionAsync(session.Name, cancellationToken);
            var isCurrent = session.Name == currentSession;
            var prefix = isCurrent ? "→ " : "  ";
            var suffix = isCurrent ? " [current]" : "";
            var type = isTemporary ? "temporary" : "permanent";

            Console.WriteLine($"{prefix}{session.Name} ({session.LockedValues.Count} fields, {type}){suffix}");
            if (!string.IsNullOrEmpty(session.Description))
            {
                Console.WriteLine($"    {session.Description}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("💡 Commands:");
        Console.WriteLine("   pidgeon session use <name>        # Switch to session");
        Console.WriteLine("   pidgeon session show <name>       # Show session details");

        return 0;
    }

    private async Task<int> HandleShowCommand(string? name, CancellationToken cancellationToken)
    {
        string sessionName;
        if (string.IsNullOrEmpty(name))
        {
            var current = await _sessionHelper.GetCurrentSessionAsync(cancellationToken);
            if (current == null)
            {
                Console.WriteLine("❌ No current session to show");
                Console.WriteLine("💡 Specify a session name or create one first");
                return 1;
            }
            sessionName = current;
        }
        else
        {
            sessionName = name;
        }

        var sessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
        if (sessionResult.IsFailure)
        {
            Console.WriteLine($"❌ Session '{sessionName}' not found");
            return 1;
        }

        var session = sessionResult.Value;
        var isTemporary = await _sessionHelper.IsTemporarySessionAsync(sessionName, cancellationToken);

        Console.WriteLine($"🔒 Session: {sessionName}");
        Console.WriteLine($"   Type: {(isTemporary ? "Temporary (expires in 24h unless saved)" : "Permanent")}");
        Console.WriteLine($"   Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        if (!string.IsNullOrEmpty(session.Description))
        {
            Console.WriteLine($"   Description: {session.Description}");
        }
        Console.WriteLine();

        if (!session.LockedValues.Any())
        {
            Console.WriteLine("No values set yet.");
            Console.WriteLine("💡 Set values: pidgeon set <field> <value>");
            return 0;
        }

        foreach (var value in session.LockedValues.OrderBy(v => v.FieldPath))
        {
            Console.WriteLine($"📝 {value.FieldPath} = \"{value.Value}\"");
            if (!string.IsNullOrEmpty(value.Reason))
            {
                Console.WriteLine($"   Reason: {value.Reason}");
            }
            Console.WriteLine($"   Set: {value.LockedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
        }

        Console.WriteLine($"📊 Total: {session.LockedValues.Count} value(s) set");

        return 0;
    }

    private async Task<int> HandleClearCommand(CancellationToken cancellationToken)
    {
        var currentSession = await _sessionHelper.GetCurrentSessionAsync(cancellationToken);
        if (currentSession == null)
        {
            Console.WriteLine("❌ No active session to clear");
            return 0;
        }

        await _sessionHelper.ClearCurrentSessionAsync(cancellationToken);
        Console.WriteLine("✅ Cleared session context");
        Console.WriteLine("💡 Next 'set' command will create a new session");

        return 0;
    }

    private async Task<int> HandleRemoveCommand(string name, CancellationToken cancellationToken)
    {
        var sessionResult = await _lockSessionService.GetSessionAsync(name, cancellationToken);
        if (sessionResult.IsFailure)
        {
            Console.WriteLine($"❌ Session '{name}' not found");
            return 1;
        }

        // Check if it's the current session
        var currentSession = await _sessionHelper.GetCurrentSessionAsync(cancellationToken);
        if (currentSession == name)
        {
            await _sessionHelper.ClearCurrentSessionAsync(cancellationToken);
        }

        var removeResult = await _lockSessionService.RemoveSessionAsync(name, cancellationToken);
        if (removeResult.IsFailure)
        {
            Console.WriteLine($"❌ Failed to remove session: {removeResult.Error.Message}");
            return 1;
        }

        Console.WriteLine($"✅ Removed session: {name}");
        if (currentSession == name)
        {
            Console.WriteLine("💡 Session was current - cleared session context");
        }

        return 0;
    }

    private async Task<int> HandleExportCommand(string file, string? sessionName, CancellationToken cancellationToken)
    {
        // Determine which session to export
        string targetSession;
        if (string.IsNullOrEmpty(sessionName))
        {
            var current = await _sessionHelper.GetCurrentSessionAsync(cancellationToken);
            if (current == null)
            {
                Console.WriteLine("❌ No current session to export");
                Console.WriteLine("💡 Specify session name: --session <name>");
                return 1;
            }
            targetSession = current;
        }
        else
        {
            targetSession = sessionName;
        }

        var sessionResult = await _lockSessionService.GetSessionAsync(targetSession, cancellationToken);
        if (sessionResult.IsFailure)
        {
            Console.WriteLine($"❌ Session '{targetSession}' not found");
            return 1;
        }

        var session = sessionResult.Value;

        // Create template structure for export
        var template = new
        {
            name = targetSession,
            description = session.Description ?? $"Exported session from {targetSession}",
            created = session.CreatedAt,
            exported = DateTime.UtcNow,
            values = session.LockedValues.Select(v => new
            {
                field = v.FieldPath,
                value = v.Value,
                reason = v.Reason
            }).ToArray()
        };

        try
        {
            var json = JsonSerializer.Serialize(template, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(file, json, cancellationToken);
            Console.WriteLine($"✅ Exported session: {targetSession} → {file}");
            Console.WriteLine($"📊 Exported {session.LockedValues.Count} field value(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to export session: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private async Task<int> HandleImportCommand(string file, string? name, CancellationToken cancellationToken)
    {
        if (!File.Exists(file))
        {
            Console.WriteLine($"❌ File not found: {file}");
            return 1;
        }

        try
        {
            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var template = JsonSerializer.Deserialize<JsonElement>(json);

            // Determine session name
            var sessionName = name;
            if (string.IsNullOrEmpty(sessionName))
            {
                if (template.TryGetProperty("name", out var nameProperty))
                {
                    sessionName = nameProperty.GetString();
                }
                else
                {
                    sessionName = Path.GetFileNameWithoutExtension(file);
                }
            }

            if (string.IsNullOrEmpty(sessionName))
            {
                Console.WriteLine("❌ Unable to determine session name");
                Console.WriteLine("💡 Specify name: --name <session_name>");
                return 1;
            }

            // Extract description
            var description = template.TryGetProperty("description", out var descProperty) ?
                             descProperty.GetString() :
                             "Imported session template";

            // Create the session
            var newSessionName = await _sessionHelper.CreateNamedSessionAsync(sessionName, description, cancellationToken);

            // Import values
            if (template.TryGetProperty("values", out var valuesProperty) && valuesProperty.ValueKind == JsonValueKind.Array)
            {
                var importedCount = 0;
                foreach (var valueElement in valuesProperty.EnumerateArray())
                {
                    if (valueElement.TryGetProperty("field", out var fieldProperty) &&
                        valueElement.TryGetProperty("value", out var valueProperty))
                    {
                        var fieldPath = fieldProperty.GetString();
                        var value = valueProperty.GetString();
                        var reason = valueElement.TryGetProperty("reason", out var reasonProperty) ?
                                   reasonProperty.GetString() :
                                   "Imported from template";

                        if (!string.IsNullOrEmpty(fieldPath) && !string.IsNullOrEmpty(value))
                        {
                            var setResult = await _lockSessionService.SetValueAsync(
                                newSessionName, fieldPath, value, reason, cancellationToken);

                            if (setResult.IsSuccess)
                            {
                                importedCount++;
                            }
                        }
                    }
                }

                Console.WriteLine($"✅ Imported session: {newSessionName}");
                Console.WriteLine($"📊 Imported {importedCount} field value(s) from {file}");
                Console.WriteLine($"🔒 Current session: {newSessionName}");
            }
            else
            {
                Console.WriteLine($"✅ Created session: {newSessionName} (no values in template)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to import session: {ex.Message}");
            return 1;
        }

        return 0;
    }
}