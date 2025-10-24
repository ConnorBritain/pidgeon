// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json;

namespace Pidgeon.CLI.Services;

/// <summary>
/// Manages configuration file storage with organized directory structure.
/// Separates Pidgeon-standard configurations from user-generated configurations.
/// </summary>
public class ConfigurationStorage
{
    private readonly string _rootDirectory;
    private readonly string _standardDirectory;
    private readonly string _localDirectory;
    
    public ConfigurationStorage()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _rootDirectory = Path.Combine(homeDir, ".pidgeon", "configs");
        _standardDirectory = Path.Combine(_rootDirectory, "standard");
        _localDirectory = Path.Combine(_rootDirectory, "local");
        
        // Ensure directory structure exists
        Directory.CreateDirectory(_standardDirectory);
        Directory.CreateDirectory(_localDirectory);
    }
    
    /// <summary>
    /// Gets the root configuration directory path.
    /// </summary>
    public string ConfigDirectory => _rootDirectory;
    
    /// <summary>
    /// Gets the standard (Pidgeon-provided) configuration directory.
    /// </summary>
    public string StandardDirectory => _standardDirectory;
    
    /// <summary>
    /// Gets the local (user-generated) configuration directory.
    /// </summary>
    public string LocalDirectory => _localDirectory;
    
    /// <summary>
    /// Gets the full path for a configuration file.
    /// </summary>
    /// <param name="fileName">Name of the configuration file</param>
    /// <param name="isStandard">Whether this is a standard Pidgeon configuration</param>
    /// <returns>Full path to the configuration file</returns>
    public string GetConfigPath(string fileName, bool isStandard = false)
    {
        // Ensure .json extension
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".json";
        }
        
        var baseDir = isStandard ? _standardDirectory : _localDirectory;
        return Path.Combine(baseDir, fileName);
    }
    
    /// <summary>
    /// Gets or creates a vendor-specific subdirectory in the local configs.
    /// </summary>
    /// <param name="vendorName">Name of the vendor</param>
    /// <returns>Path to the vendor-specific directory</returns>
    public string GetVendorDirectory(string vendorName)
    {
        var vendorDir = Path.Combine(_localDirectory, vendorName.ToLower());
        Directory.CreateDirectory(vendorDir);
        return vendorDir;
    }
    
    /// <summary>
    /// Lists all configuration files in the storage directory.
    /// </summary>
    /// <param name="includeStandard">Include standard configurations</param>
    /// <param name="includeLocal">Include local configurations</param>
    /// <returns>Array of configuration file paths</returns>
    public string[] ListConfigFiles(bool includeStandard = true, bool includeLocal = true)
    {
        var files = new List<string>();
        
        if (includeStandard && Directory.Exists(_standardDirectory))
        {
            files.AddRange(Directory.GetFiles(_standardDirectory, "*.json", SearchOption.AllDirectories));
        }
        
        if (includeLocal && Directory.Exists(_localDirectory))
        {
            files.AddRange(Directory.GetFiles(_localDirectory, "*.json", SearchOption.AllDirectories));
        }
        
        return files.ToArray();
    }
    
    /// <summary>
    /// Saves a configuration to the storage directory.
    /// </summary>
    /// <param name="config">Configuration to save</param>
    /// <param name="fileName">Name for the configuration file</param>
    /// <param name="vendorName">Optional vendor name for organization</param>
    /// <param name="isStandard">Whether this is a standard configuration</param>
    /// <returns>Full path to the saved file</returns>
    public async Task<string> SaveConfigurationAsync<T>(T config, string fileName, string? vendorName = null, bool isStandard = false)
    {
        string filePath;
        
        if (isStandard)
        {
            // Standard configs go in the standard directory
            filePath = GetConfigPath(fileName, true);
        }
        else if (!string.IsNullOrEmpty(vendorName))
        {
            // Vendor-specific configs go in vendor subdirectory with timestamp
            var vendorDir = GetVendorDirectory(vendorName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var timestampedFileName = $"{baseFileName}_{timestamp}.json";
            filePath = Path.Combine(vendorDir, timestampedFileName);
        }
        else
        {
            // General local configs with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var baseFileName = Path.GetFileNameWithoutExtension(fileName);
            var timestampedFileName = $"{baseFileName}_{timestamp}.json";
            filePath = Path.Combine(_localDirectory, timestampedFileName);
        }
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(config, options);
        await File.WriteAllTextAsync(filePath, json);
        
        return filePath;
    }
    
    /// <summary>
    /// Loads a configuration from the storage directory.
    /// </summary>
    /// <typeparam name="T">Type of configuration to load</typeparam>
    /// <param name="fileName">Name of the configuration file</param>
    /// <returns>Loaded configuration or null if not found</returns>
    public async Task<T?> LoadConfigurationAsync<T>(string fileName)
    {
        // Try multiple locations in order of preference
        var searchPaths = new List<string>
        {
            // Check standard directory
            GetConfigPath(fileName, true),
            // Check local directory root
            GetConfigPath(fileName, false),
            // Check as absolute path
            fileName
        };
        
        // Also search vendor subdirectories
        if (Directory.Exists(_localDirectory))
        {
            var vendorDirs = Directory.GetDirectories(_localDirectory);
            foreach (var vendorDir in vendorDirs)
            {
                searchPaths.Add(Path.Combine(vendorDir, fileName));
                // Also search for files with timestamps
                var baseFileName = Path.GetFileNameWithoutExtension(fileName);
                var pattern = $"{baseFileName}_*.json";
                var timestampedFiles = Directory.GetFiles(vendorDir, pattern);
                if (timestampedFiles.Length > 0)
                {
                    // Use the most recent timestamped file
                    searchPaths.Add(timestampedFiles.OrderByDescending(f => f).First());
                }
            }
        }
        
        string? filePath = null;
        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                filePath = path;
                break;
            }
        }
        
        if (filePath == null)
        {
            return default;
        }
        
        var json = await File.ReadAllTextAsync(filePath);
        
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        return JsonSerializer.Deserialize<T>(json, options);
    }
    
    /// <summary>
    /// Checks if a configuration file exists.
    /// </summary>
    /// <param name="fileName">Name of the configuration file</param>
    /// <returns>True if the file exists</returns>
    public bool ConfigExists(string fileName)
    {
        // Check standard directory
        if (File.Exists(GetConfigPath(fileName, true)))
            return true;
            
        // Check local directory
        if (File.Exists(GetConfigPath(fileName, false)))
            return true;
            
        // Check vendor subdirectories
        if (Directory.Exists(_localDirectory))
        {
            var vendorDirs = Directory.GetDirectories(_localDirectory);
            foreach (var vendorDir in vendorDirs)
            {
                var vendorPath = Path.Combine(vendorDir, fileName);
                if (File.Exists(vendorPath))
                    return true;
                    
                // Check for timestamped versions
                var baseFileName = Path.GetFileNameWithoutExtension(fileName);
                var pattern = $"{baseFileName}_*.json";
                if (Directory.GetFiles(vendorDir, pattern).Length > 0)
                    return true;
            }
        }
        
        // Check as absolute path
        return File.Exists(fileName);
    }
    
    /// <summary>
    /// Gets information about the configuration storage.
    /// </summary>
    /// <returns>Storage information string</returns>
    public string GetStorageInfo()
    {
        var standardCount = ListConfigFiles(true, false).Length;
        var localCount = ListConfigFiles(false, true).Length;
        
        var info = new System.Text.StringBuilder();
        info.AppendLine($"Configuration storage: {_rootDirectory}");
        info.AppendLine($"Standard configurations: {standardCount}");
        info.AppendLine($"Local configurations: {localCount}");
        
        // List vendor subdirectories
        if (Directory.Exists(_localDirectory))
        {
            var vendorDirs = Directory.GetDirectories(_localDirectory);
            if (vendorDirs.Length > 0)
            {
                info.AppendLine("Vendor directories:");
                foreach (var dir in vendorDirs)
                {
                    var dirName = Path.GetFileName(dir);
                    var fileCount = Directory.GetFiles(dir, "*.json").Length;
                    info.AppendLine($"  - {dirName}: {fileCount} configs");
                }
            }
        }
        
        return info.ToString();
    }
}