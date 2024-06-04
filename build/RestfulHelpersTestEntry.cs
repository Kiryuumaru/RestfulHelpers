using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using NukeBuildHelpers;
using NukeBuildHelpers.Attributes;
using NukeBuildHelpers.Enums;
using NukeBuildHelpers.Models;
using NukeBuildHelpers.Models.RunContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _build;

public class RestfulHelpersTestEntry : AppTestEntry<Build>
{
    public override RunsOnType RunsOn => RunsOnType.Ubuntu2204;

    public override Type[] AppEntryTargets => [typeof(RestfulHelpersEntry)];

    public override void Run(AppTestRunContext appTestContext)
    {
        var projectPath = RootDirectory / "RestfulHelpers.UnitTest" / "RestfulHelpers.UnitTest.csproj";

        DotNetTasks.DotNetClean(_ => _
            .SetProject(projectPath));
        DotNetTasks.DotNetTest(_ => _
            .SetProjectFile(projectPath));
    }
}
    