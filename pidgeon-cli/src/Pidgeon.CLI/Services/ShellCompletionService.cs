// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Pidgeon.Core;

namespace Pidgeon.CLI.Services;

/// <summary>
/// Service for automatically setting up shell completion during first-time setup.
/// Detects user's shell and configures tab completion without manual intervention.
/// </summary>
public class ShellCompletionService
{
    private readonly ILogger<ShellCompletionService> _logger;
    private readonly string _configPath;

    public ShellCompletionService(ILogger<ShellCompletionService> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".pidgeon");
    }

    /// <summary>
    /// Auto-setup shell completion during first-time user initialization.
    /// Detects shell and configures completion automatically.
    /// </summary>
    public async Task<Result<bool>> SetupCompletionAsync(RootCommand rootCommand)
    {
        try
        {
            _logger.LogInformation("Starting shell completion auto-setup");

            // Detect user's shell
            var shellInfo = await DetectShellAsync();
            if (shellInfo.IsFailure)
            {
                _logger.LogWarning("Could not detect shell: {Error}", shellInfo.Error);
                ShowManualSetupInstructions();
                return Result<bool>.Failure(shellInfo.Error);
            }

            var shell = shellInfo.Value;
            _logger.LogInformation("Detected shell: {Shell}", shell.Name);

            // Generate completion script
            var script = GenerateCompletionScript(rootCommand, shell.Name);
            if (string.IsNullOrEmpty(script))
            {
                return Result<bool>.Failure($"Could not generate completion script for {shell.Name}");
            }

            // Auto-install completion based on shell
            var setupResult = await AutoInstallCompletionAsync(shell, script);
            if (setupResult.IsSuccess)
            {
                _logger.LogInformation("Shell completion enabled for {Shell}", shell.Name);
                return Result<bool>.Success(true);
            }

            // Fallback - log but don't show user instructions during auto-setup
            _logger.LogInformation("Auto-install failed for {Shell}, completion not configured", shell.Name);
            return Result<bool>.Success(false); // Not an error, just couldn't auto-configure
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shell completion setup");
            return Result<bool>.Success(false); // Silent failure during auto-setup
        }
    }

    /// <summary>
    /// Generate completion scripts for manual setup (used by CLI command).
    /// </summary>
    public string GenerateCompletionScript(RootCommand rootCommand, string shell)
    {
        try
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate completion script for {Shell}", shell);
            return string.Empty;
        }
    }

    private async Task<Result<ShellInfo>> DetectShellAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await DetectWindowsShellAsync();
            }
            else
            {
                return await DetectUnixShellAsync();
            }
        }
        catch (Exception ex)
        {
            return Result<ShellInfo>.Failure($"Shell detection failed: {ex.Message}");
        }
    }

    private async Task<Result<ShellInfo>> DetectWindowsShellAsync()
    {
        await Task.Yield();

        // Check if running in PowerShell
        var psVersion = Environment.GetEnvironmentVariable("PSVersionTable");
        if (!string.IsNullOrEmpty(psVersion))
        {
            return Result<ShellInfo>.Success(new ShellInfo
            {
                Name = "powershell",
                ConfigFile = Environment.GetEnvironmentVariable("PROFILE") ??
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "PowerShell", "Microsoft.PowerShell_profile.ps1"),
                IsSupported = true
            });
        }

        // Default to PowerShell on Windows
        return Result<ShellInfo>.Success(new ShellInfo
        {
            Name = "powershell",
            ConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PowerShell", "Microsoft.PowerShell_profile.ps1"),
            IsSupported = true
        });
    }

    private async Task<Result<ShellInfo>> DetectUnixShellAsync()
    {
        try
        {
            // Check $SHELL environment variable
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (string.IsNullOrEmpty(shell))
            {
                return Result<ShellInfo>.Failure("SHELL environment variable not set");
            }

            var shellName = Path.GetFileName(shell).ToLowerInvariant();
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var shellInfo = shellName switch
            {
                "bash" => new ShellInfo
                {
                    Name = "bash",
                    ConfigFile = Path.Combine(homeDir, ".bashrc"),
                    IsSupported = true
                },
                "zsh" => new ShellInfo
                {
                    Name = "zsh",
                    ConfigFile = Path.Combine(homeDir, ".zshrc"),
                    CompletionDir = Path.Combine(homeDir, ".zsh", "completions"),
                    IsSupported = true
                },
                "fish" => new ShellInfo
                {
                    Name = "fish",
                    ConfigFile = Path.Combine(homeDir, ".config", "fish", "config.fish"),
                    CompletionDir = Path.Combine(homeDir, ".config", "fish", "completions"),
                    IsSupported = true
                },
                _ => new ShellInfo
                {
                    Name = shellName,
                    ConfigFile = "",
                    IsSupported = false
                }
            };

            await Task.Yield();
            return Result<ShellInfo>.Success(shellInfo);
        }
        catch (Exception ex)
        {
            return Result<ShellInfo>.Failure($"Unix shell detection failed: {ex.Message}");
        }
    }

    private async Task<Result<bool>> AutoInstallCompletionAsync(ShellInfo shell, string script)
    {
        try
        {
            switch (shell.Name.ToLowerInvariant())
            {
                case "bash":
                    return await SetupBashCompletionAsync(shell, script);

                case "zsh":
                    return await SetupZshCompletionAsync(shell, script);

                case "fish":
                    return await SetupFishCompletionAsync(shell, script);

                case "powershell":
                    return await SetupPowerShellCompletionAsync(shell, script);

                default:
                    return Result<bool>.Failure($"Auto-setup not supported for {shell.Name}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-install failed for {Shell}", shell.Name);
            return Result<bool>.Failure($"Auto-install failed: {ex.Message}");
        }
    }

    private async Task<Result<bool>> SetupBashCompletionAsync(ShellInfo shell, string script)
    {
        try
        {
            var bashrcPath = shell.ConfigFile;
            var completionLine = "source <(pidgeon completions bash)";

            // Check if already configured
            if (File.Exists(bashrcPath))
            {
                var content = await File.ReadAllTextAsync(bashrcPath);
                if (content.Contains("pidgeon completions bash"))
                {
                    _logger.LogInformation("Bash completion already configured");
                    return Result<bool>.Success(true);
                }
            }

            // Add completion line to .bashrc
            var newContent = $"\n# Pidgeon CLI completion\n{completionLine}\n";
            await File.AppendAllTextAsync(bashrcPath, newContent);

            _logger.LogInformation("Added completion to {BashrcPath}", bashrcPath);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Bash setup failed: {ex.Message}");
        }
    }

    private async Task<Result<bool>> SetupZshCompletionAsync(ShellInfo shell, string script)
    {
        try
        {
            // Create completion directory
            var completionDir = shell.CompletionDir ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".zsh", "completions");

            Directory.CreateDirectory(completionDir);

            // Write completion file
            var completionFile = Path.Combine(completionDir, "_pidgeon");
            await File.WriteAllTextAsync(completionFile, script);

            // Add to .zshrc if needed
            var zshrcPath = shell.ConfigFile;
            if (File.Exists(zshrcPath))
            {
                var content = await File.ReadAllTextAsync(zshrcPath);
                if (!content.Contains("fpath=(~/.zsh/completions"))
                {
                    var fpathLine = "\n# Pidgeon CLI completion\nfpath=(~/.zsh/completions $fpath)\nautoload -U compinit && compinit\n";
                    await File.AppendAllTextAsync(zshrcPath, fpathLine);
                }
            }

            _logger.LogInformation("Created zsh completion file at {CompletionFile}", completionFile);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Zsh setup failed: {ex.Message}");
        }
    }

    private async Task<Result<bool>> SetupFishCompletionAsync(ShellInfo shell, string script)
    {
        try
        {
            // Create completion directory
            var completionDir = shell.CompletionDir ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "fish", "completions");

            Directory.CreateDirectory(completionDir);

            // Write completion file
            var completionFile = Path.Combine(completionDir, "pidgeon.fish");
            await File.WriteAllTextAsync(completionFile, script);

            _logger.LogInformation("Created fish completion file at {CompletionFile}", completionFile);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Fish setup failed: {ex.Message}");
        }
    }

    private async Task<Result<bool>> SetupPowerShellCompletionAsync(ShellInfo shell, string script)
    {
        try
        {
            var profilePath = shell.ConfigFile;
            var completionLine = "pidgeon completions powershell | Out-String | Invoke-Expression";

            // Ensure profile directory exists
            var profileDir = Path.GetDirectoryName(profilePath);
            if (!string.IsNullOrEmpty(profileDir))
            {
                Directory.CreateDirectory(profileDir);
            }

            // Check if already configured
            if (File.Exists(profilePath))
            {
                var content = await File.ReadAllTextAsync(profilePath);
                if (content.Contains("pidgeon completions powershell"))
                {
                    _logger.LogInformation("PowerShell completion already configured");
                    return Result<bool>.Success(true);
                }
            }

            // Add completion line to profile
            var newContent = $"\n# Pidgeon CLI completion\n{completionLine}\n";
            await File.AppendAllTextAsync(profilePath, newContent);

            _logger.LogInformation("Added completion to {ProfilePath}", profilePath);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"PowerShell setup failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Show manual setup instructions (only used by explicit completions command).
    /// </summary>
    private void ShowManualSetupInstructions(ShellInfo? shell = null)
    {
        Console.WriteLine();
        Console.WriteLine("⚠️  Shell completion auto-setup unavailable");
        Console.WriteLine();

        if (shell != null)
        {
            Console.WriteLine($"To enable tab completion for {shell.Name}, run:");
            Console.WriteLine();

            switch (shell.Name.ToLowerInvariant())
            {
                case "bash":
                    Console.WriteLine("  echo 'source <(pidgeon completions bash)' >> ~/.bashrc");
                    Console.WriteLine("  source ~/.bashrc");
                    break;

                case "zsh":
                    Console.WriteLine("  pidgeon completions zsh > ~/.zsh/completions/_pidgeon");
                    Console.WriteLine("  # Add to ~/.zshrc: fpath=(~/.zsh/completions $fpath)");
                    Console.WriteLine("  autoload -U compinit && compinit");
                    break;

                case "fish":
                    Console.WriteLine("  pidgeon completions fish > ~/.config/fish/completions/pidgeon.fish");
                    break;

                case "powershell":
                    Console.WriteLine("  # Add to $PROFILE:");
                    Console.WriteLine("  Add-Content $PROFILE \"`npidgeon completions powershell | Out-String | Invoke-Expression\"");
                    break;
            }
        }
        else
        {
            Console.WriteLine("Run: pidgeon completions <shell> for setup instructions");
            Console.WriteLine("Supported shells: bash, zsh, powershell, fish");
        }

        Console.WriteLine();
    }

    private class ShellInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ConfigFile { get; set; } = string.Empty;
        public string? CompletionDir { get; set; }
        public bool IsSupported { get; set; }
    }
}