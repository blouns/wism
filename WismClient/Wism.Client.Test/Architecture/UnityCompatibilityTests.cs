using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

#nullable enable

namespace Wism.Client.Test.Architecture;

[TestFixture]
public class UnityCompatibilityTests
{
    private const string RequiredUnityTargetFramework = "netstandard2.1";
    private const string UnityPublishTargetsSuffix = "Build/Wism.PublishToUnity.targets";

    [Test]
    public void WismUnity_ProjectProfile_IsUnity6NetStandard21()
    {
        var repoRoot = FindRepoRoot();
        var projectVersion = File.ReadAllText(Path.Combine(repoRoot, "WismUnity", "ProjectSettings", "ProjectVersion.txt"));
        var projectSettings = File.ReadAllText(Path.Combine(repoRoot, "WismUnity", "ProjectSettings", "ProjectSettings.asset"));
        var unityGameProject = File.ReadAllText(Path.Combine(repoRoot, "WismUnity", "UnityGame.csproj"));

        Assert.That(projectVersion, Does.Contain("m_EditorVersion: 6000.0.34f1"));
        Assert.That(projectSettings, Does.Contain("apiCompatibilityLevel: 6"));
        Assert.That(
            unityGameProject,
            Does.Contain(@"NetStandard\ref\2.1.0\netstandard.dll"),
            "The generated Unity project should reference Unity's .NET Standard 2.1 profile.");
    }

    [Test]
    public void UnityPublishedProjects_TargetUnityNetStandardProfile()
    {
        var repoRoot = FindRepoRoot();
        var projects = LoadProjects(repoRoot);
        var unityProjects = projects.Values
            .Where(project => project.ImportsUnityPublisher)
            .OrderBy(project => project.RelativePath)
            .ToList();

        Assert.That(unityProjects, Is.Not.Empty);

        foreach (var project in unityProjects)
        {
            Assert.That(
                project.TargetFramework,
                Is.EqualTo(RequiredUnityTargetFramework),
                $"{project.RelativePath} is copied into WismUnity/Assets/Plugins/WismClient and must match the current Unity 6 .NET Standard 2.1 profile.");
        }
    }

    [Test]
    public void UnityPublishedProjects_OnlyReferenceUnityCompatibleProjects()
    {
        var repoRoot = FindRepoRoot();
        var projects = LoadProjects(repoRoot);
        var unityProjects = projects.Values
            .Where(project => project.ImportsUnityPublisher)
            .OrderBy(project => project.RelativePath);

        foreach (var project in unityProjects)
        {
            foreach (var referencePath in project.ProjectReferences)
            {
                Assert.That(
                    projects.TryGetValue(referencePath, out var referencedProject),
                    Is.True,
                    $"{project.RelativePath} references a project outside WismClient: {referencePath}");

                Assert.That(
                    referencedProject!.TargetFramework,
                    Is.EqualTo(RequiredUnityTargetFramework),
                    $"{project.RelativePath} references {referencedProject.RelativePath}, so that dependency must also match the Unity-loaded framework profile.");
            }
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "WismClient")) &&
                Directory.Exists(Path.Combine(current.FullName, "WismUnity")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate WISM repo root from test output directory.");
    }

    private static Dictionary<string, ProjectInfo> LoadProjects(string repoRoot)
    {
        var wismClientRoot = Path.Combine(repoRoot, "WismClient");
        return Directory.EnumerateFiles(wismClientRoot, "*.csproj", SearchOption.AllDirectories)
            .Select(path => LoadProject(repoRoot, path))
            .ToDictionary(project => project.FullPath, StringComparer.OrdinalIgnoreCase);
    }

    private static ProjectInfo LoadProject(string repoRoot, string projectPath)
    {
        var document = XDocument.Load(projectPath);
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var relativePath = Path.GetRelativePath(repoRoot, projectPath);
        var importsUnityPublisher = document.Descendants()
            .Where(element => element.Name.LocalName == "Import")
            .Select(element => (string?)element.Attribute("Project"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeSeparators(value!))
            .Any(value => value.EndsWith(UnityPublishTargetsSuffix, StringComparison.OrdinalIgnoreCase));

        var projectReferences = document.Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(projectDirectory, value!)))
            .ToArray();

        var targetFramework = document.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "TargetFramework")
            ?.Value
            ?.Trim();

        return new ProjectInfo(
            Path.GetFullPath(projectPath),
            relativePath,
            targetFramework ?? string.Empty,
            importsUnityPublisher,
            projectReferences);
    }

    private static string NormalizeSeparators(string value)
    {
        return value.Replace('\\', '/');
    }

    private sealed record ProjectInfo(
        string FullPath,
        string RelativePath,
        string TargetFramework,
        bool ImportsUnityPublisher,
        IReadOnlyList<string> ProjectReferences);
}
