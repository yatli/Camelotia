using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

// ReSharper disable ArrangeTypeMemberModifiers

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
internal class Build : NukeBuild
{
    const string InteractiveProjectName = "Camelotia.Presentation.Avalonia";
    const string CoverageFileName = "coverage.cobertura.xml";

    public static int Main() => Execute<Build>(x => x.RunInteractive);
    
    [Parameter] readonly string Configuration = IsLocalBuild ? "Debug" : "Release";
    [Parameter] readonly bool Interactive;
    [Parameter] readonly bool Full;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(RunUnitTests)
        .Executes(() => SourceDirectory
            .GlobDirectories("**/bin", "**/obj", "**/AppPackages", "**/BundleArtifacts")
            .Concat(RootDirectory.GlobDirectories("**/artifacts"))
            .ForEach(DeleteDirectory));
    
    Target RunUnitTests => _ => _
        .DependsOn(Clean)
        .Executes(() => SourceDirectory
            .GlobFiles("**/*.Tests.csproj")
            .ForEach(path =>
                DotNetTest(settings => settings
                    .SetProjectFile(path)
                    .SetConfiguration(Configuration)
                    .SetLogger($"trx;LogFileName={ArtifactsDirectory / "report.trx"}")
                    .AddProperty("CollectCoverage", true)
                    .AddProperty("CoverletOutputFormat", "cobertura")
                    .AddProperty("Exclude", "[xunit.*]*")
                    .AddProperty("CoverletOutput", ArtifactsDirectory / CoverageFileName))));

    Target CompileAvaloniaApp => _ => _
        .DependsOn(RunUnitTests)
        .Executes(() => SourceDirectory
            .GlobFiles("**/*.Avalonia.csproj")
            .ForEach(path =>
                DotNetBuild(settings => settings
                    .SetProjectFile(path)
                    .SetConfiguration(Configuration))));

    Target RunInteractive => _ => _
        .DependsOn(CompileAvaloniaApp)
        .DependsOn(CompileUniversalWindowsApp)
        .DependsOn(CompileXamarinAndroidApp)
        .DependsOn(CompileWindowsPresentationApp)
        .Executes(() => SourceDirectory
            .GlobFiles($"**/{InteractiveProjectName}.csproj")
            .Where(x => Interactive)
            .ForEach(path => 
                DotNetRun(settings => settings
                    .SetProjectFile(path)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoBuild())));
}
