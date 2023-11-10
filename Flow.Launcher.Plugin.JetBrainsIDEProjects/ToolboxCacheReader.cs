﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects;

internal static class ToolboxCacheReader
{
    private static readonly string ToolboxDirectoryPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JetBrains", "Toolbox");

    private static readonly string IntelliJProjectsPath =
        Path.Combine(ToolboxDirectoryPath, "cache", "intellij_projects.json");

    private static readonly string StatePath =
        Path.Combine(ToolboxDirectoryPath, "state.json");

    private static readonly string[] BlacklistedToolIds = {"Space"};

    public static List<ApplicationInfo> GetApplications()
    {
        if (!File.Exists(StatePath))
        {
            throw new FileNotFoundException($"File not found: {StatePath}. Is toolbox V2 installed?");
        }

        using var stateStream = File.OpenRead(StatePath);
        var state = JsonSerializer.Deserialize<State>(stateStream);

        var applications = new List<ApplicationInfo>();
        foreach (var tool in state.Tools)
        {
            if (BlacklistedToolIds.Contains(tool.ToolId) || tool.LaunchCommand == "")
            {
                continue;
            }
            var path = Path.GetFullPath(Path.Combine(tool.InstallLocation, tool.LaunchCommand));
            var icoFile =
                Directory.GetParent(path)
                    ?.GetFiles("*.ico")
                    .FirstOrDefault()
                    ?.FullName
                        ?? throw new FileNotFoundException("Failed to determine application icon file.");

            applications.Add(new ApplicationInfo
            {
                Path = Path.Combine(tool.InstallLocation, tool.LaunchCommand),
                ApplicationId = tool.ToolId,
                ChannelId = tool.ChannelId,
                BuildNumber = tool.BuildNumber,
                IcoFile = icoFile
            });
        }

        return applications;
    }

    public static List<ProjectInfo> GetProjects(List<ApplicationInfo> applications)
    {
        if (!File.Exists(IntelliJProjectsPath))
        {
            throw new FileNotFoundException($"File not found: {IntelliJProjectsPath}. Is toolbox installed?");
        }

        using var projectsStream = File.OpenRead(IntelliJProjectsPath);
        var intellijProjects = JsonSerializer.Deserialize<List<Project>>(projectsStream);

        var projects = new List<ProjectInfo>();

        foreach (var project in intellijProjects)
        {
            if (project.DefaultNewOpenItem is null)
                continue;

            var openItem = project.NewOpenItems.Find(openItem =>
                openItem.ChannelId == project.DefaultNewOpenItem
            );

            if (openItem is null)
                continue;

            var application = applications.Find(app =>
                app.ApplicationId == openItem.ToolId &&
                app.ChannelId == openItem.ChannelId
            );

            projects.Add(new ProjectInfo
            {
                Path = project.Path,
                Name = project.Name,
                Application = application
            });
        }

        return projects;
    }
}

public class ApplicationInfo
{
    public string Path { get; init; }
    public string BuildNumber { get; init; }
    public string ChannelId { get; init; }
    public string ApplicationId { get; init; }
    public string IcoFile { get; init; }
}

public class ProjectInfo
{
    public string Name { get; init; }
    public string Path { get; init; }
    public ApplicationInfo Application { get; init; }
}

/// <summary>
///
/// </summary>
public class Project
{
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; }

    /// <summary>
    /// null if application is not installed
    /// This is equal to channelId
    /// </summary>
    [JsonPropertyName("defaultNewOpenItem")]
    public string DefaultNewOpenItem { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("newOpenItems")]
    public List<NewOpenItem> NewOpenItems { get; set; }
}


/// <summary>
///
/// </summary>
public class NewOpenItem
{
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("toolId")]
    public string ToolId { get; set; }

    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; }
}

internal class State
{
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("tools")]
    public List<Tool> Tools { get; set; }
}

internal class Tool
{
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("channelId")]
    public string ChannelId { get; set; }
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("toolId")]
    public string ToolId { get; set; }
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("buildNumber")]
    public string BuildNumber { get; set; }
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("installLocation")]
    public string InstallLocation { get; set; }
    /// <summary>
    ///
    /// </summary>
    [JsonPropertyName("launchCommand")]
    public string LaunchCommand { get; set; }
}
