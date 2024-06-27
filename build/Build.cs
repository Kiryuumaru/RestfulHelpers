using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using NukeBuildHelpers;
using NukeBuildHelpers.Common.Attributes;
using NukeBuildHelpers.Common.Enums;
using NukeBuildHelpers.Entry;
using NukeBuildHelpers.Entry.Extensions;
using NukeBuildHelpers.RunContext.Extensions;
using NukeBuildHelpers.Runner.Abstraction;

public class Build : BaseNukeBuildHelpers
{
    public static int Main () => Execute<Build>(x => x.Version);

    public override string[] EnvironmentBranches { get; } = ["prerelease", "master"];

    public override string MainEnvironmentBranch { get; } = "master";

    [SecretVariable("NUGET_AUTH_TOKEN")]
    readonly string NuGetAuthToken;

    [SecretVariable("GITHUB_TOKEN")]
    readonly string GithubToken;

    TestEntry RestfulHelpersTest => _ => _
        .AppId("restful_helpers")
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Execute(context =>
        {
            var projectPath = RootDirectory / "RestfulHelpers.UnitTest" / "RestfulHelpers.UnitTest.csproj";

            DotNetTasks.DotNetClean(_ => _
                .SetProject(projectPath));
            DotNetTasks.DotNetTest(_ => _
                .SetProjectFile(projectPath));
        });

    BuildEntry RestfulHelpersBuild => _ => _
        .AppId("restful_helpers")
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Condition(true)
        .Execute(context =>
        {
            var projectPath = RootDirectory / "RestfulHelpers" / "RestfulHelpers.csproj";
            var version = "0.0.0";
            var releaseNotes = "";
            if (context.TryGetBumpContext(out var bumpContext))
            {
                version = bumpContext.AppVersion.Version.ToString();
                releaseNotes = bumpContext.AppVersion.ReleaseNotes;
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
        });

    PublishEntry RestfulHelpersPublish => _ => _
        .AppId("restful_helpers")
        .RunnerOS(RunnerOS.Ubuntu2204)
        .Execute(context =>
        {
            if (context.RunType == RunType.Bump)
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
        });
}
