// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Completions;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for generating shell completion setup scripts.
/// Users run this to manually configure tab completion in their shell environment.
/// </summary>
public class CompletionsCommand : CommandBuilderBase
{
    public CompletionsCommand(ILogger<CompletionsCommand> logger)
        : base(logger)
    {
    }

    public override Command CreateCommand()
    {
        var command = new Command("completions", "Generate shell completion setup scripts");

        // Shell argument - which shell to generate completion for
        var shellArgument = new Argument<string>("shell")
        {
            Description = "Shell to generate completion for: bash|zsh|powershell|fish"
        };

        // Add completions for the shell argument itself
        shellArgument.CompletionSources.Add("bash", "zsh", "powershell", "fish");

        command.Add(shellArgument);

        SetInfoCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var shell = parseResult.GetValue(shellArgument);

            if (string.IsNullOrEmpty(shell))
            {
                ShowCompletionHelp();
                return 1;
            }

            // Get the root command to generate completion for
            var rootCommand = GetRootCommand(parseResult);
            if (rootCommand == null)
            {
                Logger.LogError("Unable to access root command for completion generation");
                return 1;
            }

            return await GenerateCompletionAsync(rootCommand, shell, cancellationToken);
        });

        return command;
    }

    private async Task<int> GenerateCompletionAsync(RootCommand rootCommand, string shell, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Ensure async behavior

        try
        {
            // TODO: GetCompletionScript method not available in our System.CommandLine version
            // For now, provide instructions for manual setup using dotnet-suggest
            Console.WriteLine($"# Completion setup for {shell}");
            Console.WriteLine("# Install dotnet-suggest tool first:");
            Console.WriteLine("# dotnet tool install --global dotnet-suggest");
            Console.WriteLine();

            switch (shell.ToLowerInvariant())
            {
                case "bash":
                    Console.WriteLine("# Add to ~/.bashrc:");
                    Console.WriteLine("# Register your app: dotnet-suggest register --command-path $(which pidgeon)");
                    Console.WriteLine("# Add completion: eval \"$(dotnet-suggest script bash)\"");
                    break;
                case "zsh":
                    Console.WriteLine("# Add to ~/.zshrc:");
                    Console.WriteLine("# Register your app: dotnet-suggest register --command-path $(which pidgeon)");
                    Console.WriteLine("# Add completion: eval \"$(dotnet-suggest script zsh)\"");
                    break;
                case "powershell":
                case "pwsh":
                    Console.WriteLine("# Add to PowerShell profile:");
                    Console.WriteLine("# Register your app: dotnet-suggest register --command-path (Get-Command pidgeon).Source");
                    Console.WriteLine("# Add completion: Invoke-Expression (dotnet-suggest script pwsh | Out-String)");
                    break;
                case "fish":
                    Console.WriteLine("# Add to ~/.config/fish/config.fish:");
                    Console.WriteLine("# Register your app: dotnet-suggest register --command-path (which pidgeon)");
                    Console.WriteLine("# Add completion: dotnet-suggest script fish | source");
                    break;
                default:
                    Console.WriteLine($"Error: Unsupported shell '{shell}'");
                    Console.WriteLine("Supported shells: bash, zsh, powershell, fish");
                    return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate completion instructions for {Shell}", shell);
            return 1;
        }
    }

    private static RootCommand? GetRootCommand(ParseResult parseResult)
    {
        // Try to get root command directly from ParseResult
        return parseResult.RootCommandResult?.Command as RootCommand;
    }

    private static void ShowCompletionHelp()
    {
        Console.WriteLine("Pidgeon CLI Shell Completion Setup");
        Console.WriteLine("═══════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("USAGE");
        Console.WriteLine("  pidgeon completions <shell>");
        Console.WriteLine();
        Console.WriteLine("SUPPORTED SHELLS");
        Console.WriteLine("  bash       Generate bash completion script");
        Console.WriteLine("  zsh        Generate zsh completion script");
        Console.WriteLine("  powershell Generate PowerShell completion script");
        Console.WriteLine("  fish       Generate fish completion script");
        Console.WriteLine();
        Console.WriteLine("QUICK SETUP");
        Console.WriteLine();
        Console.WriteLine("Bash:");
        Console.WriteLine("  echo 'source <(pidgeon completions bash)' >> ~/.bashrc");
        Console.WriteLine("  source ~/.bashrc");
        Console.WriteLine();
        Console.WriteLine("Zsh:");
        Console.WriteLine("  pidgeon completions zsh > ~/.zsh/completions/_pidgeon");
        Console.WriteLine();
        Console.WriteLine("PowerShell:");
        Console.WriteLine("  Add-Content $PROFILE \"`npidgeon completions powershell | Out-String | Invoke-Expression\"");
        Console.WriteLine();
        Console.WriteLine("Fish:");
        Console.WriteLine("  pidgeon completions fish > ~/.config/fish/completions/pidgeon.fish");
    }
}