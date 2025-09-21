// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for managing lock sessions that maintain consistent values across message generations.
/// Enables workflow automation and incremental testing scenarios.
/// </summary>
public class LockCommand : CommandBuilderBase
{
    private readonly ILockSessionService _lockSessionService;

    public LockCommand(
        ILogger<LockCommand> logger,
        ILockSessionService lockSessionService)
        : base(logger)
    {
        _lockSessionService = lockSessionService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("lock", "Manage lock sessions for consistent message generation workflows");

        // Subcommands
        command.Add(CreateCreateCommand());
        command.Add(CreateListCommand());
        command.Add(CreateShowCommand());
        command.Add(CreateRemoveCommand());
        command.Add(CreateCleanupCommand());

        return command;
    }

    private Command CreateCreateCommand()
    {
        var command = new Command("create", "Create a new lock session");

        var nameArg = new Argument<string>("name")
        {
            Description = "Name for the lock session"
        };

        var scopeOption = CreateOptionalOption("--scope", "Lock scope: patient|encounter|provider|facility|temporal|custom|global", "patient");
        var descriptionOption = CreateNullableOption("--description", "Optional description of the session's purpose");

        command.Add(nameArg);
        command.Add(scopeOption);
        command.Add(descriptionOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var name = parseResult.GetValue(nameArg)!;
                var scopeStr = parseResult.GetValue(scopeOption)!;
                var description = parseResult.GetValue(descriptionOption);

                if (!Enum.TryParse<LockScope>(scopeStr, true, out var scope))
                {
                    Console.WriteLine($"❌ Invalid scope: {scopeStr}");
                    Console.WriteLine("Valid scopes: patient, encounter, provider, facility, temporal, custom, global");
                    return 1;
                }

                var result = await _lockSessionService.CreateSessionAsync(name, scope, description, cancellationToken);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Failed to create lock session: {result.Error.Message}");
                    return 1;
                }

                var session = result.Value;
                Console.WriteLine($"✅ Created lock session: {session.Name}");
                Console.WriteLine($"   Session ID: {session.SessionId}");
                Console.WriteLine($"   Scope: {session.Scope}");
                if (!string.IsNullOrEmpty(session.Description))
                {
                    Console.WriteLine($"   Description: {session.Description}");
                }
                Console.WriteLine($"   Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"   Expires: {session.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine();
                Console.WriteLine("💡 Next steps:");
                Console.WriteLine($"   pidgeon set {session.Name} patient.mrn \"MR123456\"");
                Console.WriteLine($"   pidgeon generate \"ADT^A01\" --use-lock {session.Name}");

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error creating lock session");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "List all active lock sessions");

        var formatOption = CreateOptionalOption("--format", "Output format: text|json|table", "text");

        command.Add(formatOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var format = parseResult.GetValue(formatOption)!;

                var result = await _lockSessionService.ListSessionsAsync(cancellationToken);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Failed to list lock sessions: {result.Error.Message}");
                    return 1;
                }

                var sessions = result.Value;

                if (!sessions.Any())
                {
                    Console.WriteLine("No active lock sessions found.");
                    Console.WriteLine();
                    Console.WriteLine("💡 Create a new session:");
                    Console.WriteLine("   pidgeon lock create patient_workflow --scope patient");
                    return 0;
                }

                Console.WriteLine($"📋 Active Lock Sessions ({sessions.Count})");
                Console.WriteLine();

                foreach (var session in sessions.OrderBy(s => s.CreatedAt))
                {
                    Console.WriteLine($"🔒 {session.Name}");
                    Console.WriteLine($"   Scope: {session.Scope}");
                    Console.WriteLine($"   Values: {session.LockedValues.Count}");
                    Console.WriteLine($"   Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"   Last Used: {session.LastAccessedAt:yyyy-MM-dd HH:mm:ss}");
                    if (!string.IsNullOrEmpty(session.Description))
                    {
                        Console.WriteLine($"   Description: {session.Description}");
                    }
                    Console.WriteLine();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error listing lock sessions");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateShowCommand()
    {
        var command = new Command("show", "Show details of a specific lock session");

        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the lock session to show"
        };

        var formatOption = CreateOptionalOption("--format", "Output format: text|json", "text");

        command.Add(nameArg);
        command.Add(formatOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var name = parseResult.GetValue(nameArg)!;
                var format = parseResult.GetValue(formatOption)!;

                var result = await _lockSessionService.GetSessionAsync(name, cancellationToken);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ {result.Error.Message}");
                    return 1;
                }

                var session = result.Value;

                Console.WriteLine($"🔒 Lock Session: {session.Name}");
                Console.WriteLine($"   Session ID: {session.SessionId}");
                Console.WriteLine($"   Scope: {session.Scope}");
                Console.WriteLine($"   Status: {(session.IsActive ? "Active" : "Inactive")}");
                if (!string.IsNullOrEmpty(session.Description))
                {
                    Console.WriteLine($"   Description: {session.Description}");
                }
                Console.WriteLine($"   Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"   Last Used: {session.LastAccessedAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"   Expires: {session.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine();

                if (session.LockedValues.Any())
                {
                    Console.WriteLine($"📝 Locked Values ({session.LockedValues.Count}):");
                    foreach (var value in session.LockedValues.OrderBy(v => v.FieldPath))
                    {
                        Console.WriteLine($"   {value.FieldPath} = \"{value.Value}\"");
                        if (!string.IsNullOrEmpty(value.Reason))
                        {
                            Console.WriteLine($"     Reason: {value.Reason}");
                        }
                        Console.WriteLine($"     Locked: {value.LockedAt:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("📝 No locked values yet.");
                    Console.WriteLine("💡 Set values with: pidgeon set {session.Name} <field> <value>");
                    Console.WriteLine();
                }

                if (session.Metadata.AuditTrail.Any())
                {
                    Console.WriteLine($"📋 Recent Activity:");
                    foreach (var entry in session.Metadata.AuditTrail.TakeLast(5))
                    {
                        Console.WriteLine($"   {entry.Timestamp:HH:mm:ss} - {entry.Action}");
                        if (!string.IsNullOrEmpty(entry.FieldPath))
                        {
                            Console.WriteLine($"     Field: {entry.FieldPath}");
                        }
                        if (!string.IsNullOrEmpty(entry.Context))
                        {
                            Console.WriteLine($"     Context: {entry.Context}");
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error showing lock session");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateRemoveCommand()
    {
        var command = new Command("remove", "Remove a lock session");

        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the lock session to remove"
        };

        var forceOption = CreateBooleanOption("--force", "Skip confirmation prompt");

        command.Add(nameArg);
        command.Add(forceOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var name = parseResult.GetValue(nameArg)!;
                var force = parseResult.GetValue(forceOption);

                // Check if session exists first
                var sessionResult = await _lockSessionService.GetSessionAsync(name, cancellationToken);
                if (sessionResult.IsFailure)
                {
                    Console.WriteLine($"❌ {sessionResult.Error.Message}");
                    return 1;
                }

                if (!force)
                {
                    var session = sessionResult.Value;
                    Console.WriteLine($"⚠️  About to remove lock session: {session.Name}");
                    Console.WriteLine($"   Locked values: {session.LockedValues.Count}");
                    Console.WriteLine($"   Created: {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine();
                    Console.Write("Are you sure? (y/N): ");

                    var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (response != "y" && response != "yes")
                    {
                        Console.WriteLine("Operation cancelled.");
                        return 0;
                    }
                }

                var result = await _lockSessionService.RemoveSessionAsync(name, cancellationToken);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Failed to remove lock session: {result.Error.Message}");
                    return 1;
                }

                Console.WriteLine($"✅ Removed lock session: {name}");
                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error removing lock session");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateCleanupCommand()
    {
        var command = new Command("cleanup", "Clean up expired lock sessions");

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                Console.WriteLine("🧹 Cleaning up expired lock sessions...");

                var result = await _lockSessionService.CleanupExpiredSessionsAsync(cancellationToken);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Failed to cleanup sessions: {result.Error.Message}");
                    return 1;
                }

                var count = result.Value;
                if (count > 0)
                {
                    Console.WriteLine($"✅ Cleaned up {count} expired session(s)");
                }
                else
                {
                    Console.WriteLine("✅ No expired sessions found");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during cleanup");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }
}