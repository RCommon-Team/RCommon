using System;
using System.IO;
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
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Octokit;
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

    public static int Main () => Execute<Build>(x => x.Compile, x => x.Pack);

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
    [Parameter][Secret] string NuGetApiKey;

    string Copyright = $"Copyright © Jason Webb {DateTime.Now.Year}";

    protected override void OnBuildInitialized()
    {
        Log.Information("🚀 Build process started");

        base.OnBuildInitialized();
    }

    Target Print => _ => _
    .Executes(() =>
    {
        if (GitHubActions != null)
        { 
            Log.Information("Branch = {Branch}", GitHubActions.Ref);
            Log.Information("Commit = {Commit}", GitHubActions.Sha);
        }
        var projects = Solution.GetAllProjects("RCommon*");
        foreach (var project in projects)
        {
            Log.Information("Project: {0}", project.Path.ToString());
        }
    });

    Target Print_Net_SDK => _ => _
        .DependsOn(Print)
        .Executes(() =>
        {
            DotNetTasks.DotNet("--list-sdks");
        });


    Target Clean => _ => _
        .DependsOn(Print_Net_SDK)
        .Executes(() =>
        {
            Log.Information("Cleaning solution");
            Directory_Src.GlobDirectories("**/bin", "**/obj")
                .ForEach(DeleteDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            Log.Information("Restoring solution");
            DotNetTasks
                .DotNetRestore(_ => _
                    .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
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
            string NuGetVersionCustom = "2.0.0.8";


            //if it's not a tagged release - append the commit number to the package version
            //tagged commits on main have versions
            // - v0.3.0-beta
            //other commits have
            // - v0.3.0-beta1

            if (GitVersion != null && Int32.TryParse(GitVersion.CommitsSinceVersionSource, out commitNum))
            {
                Log.Information("Setting version.....");
                NuGetVersionCustom = commitNum > 0 ? NuGetVersionCustom + $"{commitNum}" : NuGetVersionCustom;
                Log.Information("Version #: {0}", NuGetVersionCustom);
            }

            Log.Information("Configuration: {0}", Configuration);
            Log.Information("Solution: {0}", Solution.FileName);
            var projects = Solution.GetAllProjects("RCommon*");
            foreach (var project in projects)
            {
                Log.Information("Project: {0}", project.Name);
            }
            //Log.Information("Project: {0}", Solution.GetProject("RCommon.Emailing"));
            //var path = projects.FirstOrDefault(x => x.Name == "RCommon.Emailing").Path.ToString();
            DotNetTasks
                .DotNetPack(_ => _
                    .SetPackageId("RCommon.Emailing")
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Emailing").Path.ToString())
                    .SetPackageTags("RCommon emailing email abstractions smtp")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.SendGrid").Path.ToString())
                    .SetPackageTags("RCommon email emailing sendgrid")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.MassTransit").Path.ToString())
                    .SetPackageTags("RCommon masstransit message bus event bus messaging")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Wolverine").Path.ToString())
                    .SetPackageTags("RCommon Wolverine messaging message bus event bus")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Mediator").Path.ToString())
                    .SetPackageTags("RCommon mediator abstraction pubsub mediator pattern")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.MediatR").Path.ToString())
                    .SetPackageTags("RCommon MediatR mediator implementation pubsub mediator pattern")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Dapper").Path.ToString())
                    .SetPackageTags("RCommon dapper dapper repository repository pattern crud dapper extensions")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.EFCore").Path.ToString())
                    .SetPackageTags("RCommon entity framework efcore repository crud repository pattern")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Linq2Db").Path.ToString())
                    .SetPackageTags("RCommon linq2db linqtosql linqtodb repository pattern linq2db repository")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Persistence").Path.ToString())
                    .SetPackageTags("RCommon persistence abstractions repository pattern crud")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.ApplicationServices").Path.ToString())
                    .SetPackageTags("RCommon application services CQRS auto web api commands command handlers queries query handlers command bus query bus")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Authorization.Web").Path.ToString())
                    .SetPackageTags("RCommon web authorization web security web identity bearer tokens")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Core").Path.ToString())
                    .SetPackageTags("RCommon infrastructure code design patterns design pattern abstractions cloud pattern abstractions persistence event handling")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Entities").Path.ToString())
                    .SetPackageTags("RCommon business entities domain objects domain model ddd domain events event aware entities entity helpers")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Models").Path.ToString())
                    .SetPackageTags("RCommon model helpers dto dto conversion")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Security").Path.ToString())
                    .SetPackageTags("RCommon security extensions claims identity authorization")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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
                    .SetProject(projects.FirstOrDefault(x => x.Name == "RCommon.Web").Path.ToString())
                    .SetPackageTags("RCommon web extensions aspnet core")
                    .SetDescription("A cohesive set of infrastructure libraries for dotnet that utilizes abstractions for event handling persistence unit of work mediator distributed messaging event bus CQRS email and more")
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

    //Target CreateRelease => _ => _
    //   .DependsOn(Pack)
    //   .Executes(() =>
    //   {
    //       Logger.Info("Started creating the release");
    //       GitHubTasks.GitHubClient = new GitHubClient(new ProductHeaderValue(nameof(NukeBuild)))
    //       {
    //           Credentials = new Credentials(GithubRepositoryAuthToken)
    //       };

    //       var newRelease = new NewRelease(GitVersion.NuGetVersionV2)
    //       {
    //           TargetCommitish = GitVersion.Sha,
    //           Draft = true,
    //           Name = $"Release version {GitVersion.SemVer}",
    //           Prerelease = false,
    //           Body =
    //           @$"See release notes in [docs](https://[YourSite]/{GitVersion.Major}.{GitVersion.Minor}/)"
    //       };

    //       var createdRelease = GitHubTasks.GitHubClient.Repository.Release.Create(GitRepository.GetGitHubOwner(), GitRepository.GetGitHubName(), newRelease).Result;
    //       if (ArtifactsDirectory.GlobDirectories("*").Count > 0)
    //       {
    //           Logger.Warn(
    //             $"Only files on the root of {ArtifactsDirectory} directory will be uploaded as release assets.");
    //       }

    //       ArtifactsDirectory.GlobFiles("*").ForEach(p => UploadReleaseAssetToGithub(createdRelease, p));
    //       var _ = GitHubTasks.GitHubClient.Repository.Release
    //         .Edit(GitRepository.GetGitHubOwner(), GitRepository.GetGitHubName(), createdRelease.Id, new ReleaseUpdate { Draft = false })
    //         .Result;
    //   });

    //private void UploadReleaseAssetToGithub(Release release, AbsolutePath asset)
    //{
    //    if (!FileExists(asset))
    //    {
    //        return;
    //    }

    //    Logger.Info($"Started Uploading {Path.GetFileName(asset)} to the release.");
    //    var releaseAssetUpload = new ReleaseAssetUpload
    //    {
    //        ContentType = "application/x-binary",
    //        FileName = Path.GetFileName(asset),
    //        RawData = File.OpenRead(asset)
    //    };
    //    var _ = GitHubTasks.GitHubClient.Repository.Release.UploadAsset(release, releaseAssetUpload).Result;
    //    Logger.Success($"Done Uploading {Path.GetFileName(asset)} to the release.");
    //}
}
