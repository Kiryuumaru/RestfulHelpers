using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Attributes;
using NukeBuildHelpers.Enums;
using NukeBuildHelpers.Models.RunContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _build;

public class RestfulHelpersEntry : AppEntry<Build>
{
    public override RunsOnType BuildRunsOn => RunsOnType.Ubuntu2204;

    public override RunsOnType PublishRunsOn => RunsOnType.Ubuntu2204;

    public override RunType RunBuildOn => RunType.All;

    [SecretVariable("NUGET_AUTH_TOKEN")]
    readonly string NuGetAuthToken;

    [SecretVariable("GITHUB_TOKEN")]
    readonly string GithubToken;

    public override bool MainRelease => true;

    public override void Build(AppRunContext appRunContext)
    {
        var projectPath = RootDirectory / "RestfulHelpers" / "RestfulHelpers.csproj";
        var version = "0.0.0";
        var releaseNotes = "";
        if (appRunContext is AppBumpRunContext appBumpRunContext)
        {
            version = appBumpRunContext.AppVersion.Version.ToString();
            releaseNotes = appBumpRunContext.AppVersion.ReleaseNotes;
        }

        DotNetTasks.DotNetClean(_ => _
            .SetProject(projectPath));
        DotNetTasks.DotNetBuild(_ => _
            .SetProjectFile(projectPath)
            .SetConfiguration("Release"));
        DotNetTasks.DotNetPack(_ => _
            .SetProject(projectPath)
            .SetConfiguration("Release")
            .SetNoRestore(true)
            .SetNoBuild(true)
            .SetIncludeSymbols(true)
            .SetSymbolPackageFormat("snupkg")
            .SetVersion(version)
            .SetPackageReleaseNotes(releaseNotes)
            .SetOutputDirectory(OutputDirectory));
    }

    public override void Publish(AppRunContext appRunContext)
    {
        if (appRunContext is AppBumpRunContext)
        {
            DotNetTasks.DotNetNuGetPush(_ => _
                .SetSource("https://nuget.pkg.github.com/kiryuumaru/index.json")
                .SetApiKey(GithubToken)
                .SetTargetPath(OutputDirectory / "**"));
            DotNetTasks.DotNetNuGetPush(_ => _
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetApiKey(NuGetAuthToken)
                .SetTargetPath(OutputDirectory / "**"));
        }
    }
}
