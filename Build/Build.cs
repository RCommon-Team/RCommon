using System;
using System.Linq;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(Compile) },
    OnPushIncludePaths = new[] {
        "Src/**",
        "Build/**"
    },
    OnPushBranches = new[] { "main" },
    AutoGenerate = false)]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter] readonly bool CI = false;

    [Solution] readonly Solution Solution;

    [GitVersion] readonly GitVersion GitVersion;

    [GitRepository] readonly GitRepository GitRepository;

    GitHubActions GitHubActions => GitHubActions.Instance;

    AbsolutePath Directory_Src => RootDirectory / "Src";

    AbsolutePath Directory_Build => RootDirectory / "Build";

    AbsolutePath Directory_NuGet => RootDirectory / "NuGet";

    [Parameter] string NuGetApiUrl = "https://api.nuget.org/v3/index.json";
    [Parameter] string NuGetApiKey;

    string Copyright = $"Copyright © Jason Webb {DateTime.Now.Year}";

    protected override void OnBuildInitialized()
    {
        Log.Information("🚀 Build process started");

        base.OnBuildInitialized();
    }

    Target Print => _ => _
    .Executes(() =>
    {
        Log.Information("Branch = {Branch}", GitHubActions.Ref);
        Log.Information("Commit = {Commit}", GitHubActions.Sha);
    });

    Target Print_Net_SDK => _ => _
        .DependsOn(Print)
        .Executes(() =>
        {
            DotNetTasks.DotNet("--list-sdks");
        });


    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            Log.Information("Cleaning solution");
            Directory_Src.GlobDirectories("**/bin", "**/obj")
                .ForEach(DeleteDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            Log.Information("Restoring solution");
            DotNetTasks
                .DotNetRestore(_ => _
                    .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .After(Pack)
        .Executes(() =>
        {
            Log.Information("Compiling solution");
            DotNetTasks
               .DotNetBuild(_ => _
               .SetProjectFile(Solution)
                   .SetConfiguration(Configuration)
                   .EnableNoRestore()
                   .SetCopyright(Copyright));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Log.Information("Generating NuGet packages for projects in solution");
            int commitNum = 0;
            string NuGetVersionCustom = GitVersion.NuGetVersionV2;

            //if it's not a tagged release - append the commit number to the package version
            //tagged commits on main have versions
            // - v0.3.0-beta
            //other commits have
            // - v0.3.0-beta1

            if (Int32.TryParse(GitVersion.CommitsSinceVersionSource, out commitNum))
                NuGetVersionCustom = commitNum > 0 ? NuGetVersionCustom + $"{commitNum}" : NuGetVersionCustom;
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Emailing")
                    .SetProject(Solution.GetProject("RCommon.Emailing"))
                    .SetPackageTags("RCommon, emailing, email abstractions, smtp")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.SendGrid")
                    .SetProject(Solution.GetProject("RCommon.SendGrid"))
                    .SetPackageTags("RCommon, email, emailing, sendgrid")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.MassTransit")
                    .SetProject(Solution.GetProject("RCommon.MassTransit"))
                    .SetPackageTags("RCommon, masstransit, message bus, event bus, messaging, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Wolverine")
                    .SetProject(Solution.GetProject("RCommon.Wolverine"))
                    .SetPackageTags("RCommon, Wolverine, messaging, message bus, event bus, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Mediator")
                    .SetProject(Solution.GetProject("RCommon.Mediator"))
                    .SetPackageTags("RCommon, mediator abstraction, pub/sub, mediator pattern")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.MediatR")
                    .SetProject(Solution.GetProject("RCommon.MediatR"))
                    .SetPackageTags("RCommon, MediatR, mediator implementation, pub/sub, mediator pattern")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Dapper")
                    .SetProject(Solution.GetProject("RCommon.Dapper"))
                    .SetPackageTags("RCommon, dapper, dapper repository, repository pattern, crud, dapper extensions, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.EFCore")
                    .SetProject(Solution.GetProject("RCommon.EFCore"))
                    .SetPackageTags("RCommon, entity framework, efcore, repository, crud, repository pattern, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Linq2Db")
                    .SetProject(Solution.GetProject("RCommon.Linq2Db"))
                    .SetPackageTags("RCommon, linq2db, linqtosql, linqtodb, repository pattern, linq2db repository, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Persistence")
                    .SetProject(Solution.GetProject("RCommon.Persistence"))
                    .SetPackageTags("RCommon, persistence abstractions, repository pattern, crud, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.ApplicationServices")
                    .SetProject(Solution.GetProject("RCommon.ApplicationServices"))
                    .SetPackageTags("RCommon, application services, CQRS, auto web api, commands, command handlers, queries, query handlers, command bus, query bus, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Authorization.Web")
                    .SetProject(Solution.GetProject("RCommon.Authorization.Web"))
                    .SetPackageTags("RCommon, web authorization, web security, web identity, bearer tokens, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Core")
                    .SetProject(Solution.GetProject("RCommon.Core"))
                    .SetPackageTags("RCommon, infrastructure code, design patterns, design pattern abstractions, cloud pattern abstractions, persistence, event handling, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Entities")
                    .SetProject(Solution.GetProject("RCommon.Entities"))
                    .SetPackageTags("RCommon, business entities, domain objects, domain model, ddd, domain events, event aware entities, entity helpers, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Models")
                    .SetProject(Solution.GetProject("RCommon.Models"))
                    .SetPackageTags("RCommon, model helpers, dto, dto conversion, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Security")
                    .SetProject(Solution.GetProject("RCommon.Security"))
                    .SetPackageTags("RCommon, security extensions, claims, identity, authorization, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Web")
                    .SetProject(Solution.GetProject("RCommon.Web"))
                    .SetPackageTags("RCommon, web extensions, asp.net core, c#, .NET")
                    .SetDescription("A cohesive set of infrastructure libraries for .NET 6, .NET 7, and .NET 8 that utilizes abstractions for event handling, persistence, unit of work, mediator, distributed messaging, event bus, CQRS, email, and more.")
                    .SetConfiguration(Configuration)
                    .SetCopyright(Copyright)
                    .SetAuthors("Jason Webb")
                    .SetPackageIconUrl("https://avatars.githubusercontent.com/u/96881178?s=200&v=4")
                    .SetRepositoryUrl("https://github.com/RCommon-Team/RCommon")
                    .SetPackageProjectUrl("https://rcommon.com")
                    .SetPackageLicenseUrl("https://licenses.nuget.org/Apache-2.0")
                    .SetVersion(NuGetVersionCustom)
                    .SetNoDependencies(true)
                    .SetOutputDirectory(Directory_NuGet)
                    .EnableNoBuild()
                    .EnableNoRestore());
        });


    //Target Push => _ => _
    //   //.DependsOn(Pack)
    //   .Requires(() => NuGetApiUrl)
    //   .Requires(() => NuGetApiKey)
    //   .Requires(() => Configuration.Equals(Configuration.Release))
    //   .Executes(() =>
    //   {
    //       Glob.Files(Directory_NuGet, "*.nupkg")
    //           .NotEmpty()
    //           .Where(x => !x.EndsWith("symbols.nupkg"))
    //           .ForEach(x =>
    //           {
    //               DotNetTasks
    //               .DotNetNuGetPush(s => s
    //                   .SetTargetPath(x)
    //                   .SetSource(NuGetApiUrl)
    //                   .SetApiKey(NuGetApiKey)
    //               );
    //           });
    //   });
}
