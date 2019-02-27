#! "netcoreapp2.0"
#tool nuget:?package=Cake.Bakery&version=0.3.0
#addin nuget:?package=Cake.GitVersioning&version=3.0.6-beta
#addin nuget:?package=Cake.Incubator&version=3.1.0

var TARGET = Argument ("t", Argument ("target", "nuget"));
var CONFIGURATION = Argument("c", Argument("configuration", Argument("Configuration", "Release")));
var SOLUTION_PATH = Argument("p", Argument("project", "./source/ARKit-CoreLocation.sln"));
var IS_CLOUD_BUILD = EnvironmentVariable("TF_BUILD", false);
var VERBOSITY = Argument<Verbosity> ("v", Argument<Verbosity> ("verbosity", IS_CLOUD_BUILD ? Verbosity.Diagnostic : Verbosity.Normal));
var ARTIFACTS_DIR = IS_CLOUD_BUILD ? EnvironmentVariable("BUILD_ARTIFACTSTAGINGDIRECTORY") : $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.nuget/packages/";

Task ("clean").Does (() =>
{
    MSBuild(SOLUTION_PATH, c =>
		c.SetConfiguration(CONFIGURATION)
            .WithTarget("Clean"));
});

Task ("libs").Does(() =>
{
	MSBuild(SOLUTION_PATH, c =>
		c.SetConfiguration(CONFIGURATION)
            .SetVerbosity(VERBOSITY)
            .WithRestore()
			.WithTarget("Build")
			.WithProperty("DesignTimeBuild", "false"));
});

Task ("nuget").IsDependentOn("libs").Does(() =>
{
    EnsureDirectoryExists(ARTIFACTS_DIR);
    var gitVersion = GitVersioningGetVersion($"{new FilePath(SOLUTION_PATH).GetDirectory()}");
	MSBuild(SOLUTION_PATH, c =>
		c.SetConfiguration(CONFIGURATION)
            .SetVerbosity(VERBOSITY)
			.WithTarget("Pack")
            .WithProperty("Version", gitVersion.Version.ToString())
            .WithProperty("AssemblyVersion", gitVersion.AssemblyVersion.ToString())
            .WithProperty("AssemblyFileVersion", gitVersion.AssemblyFileVersion.ToString())
            .WithProperty("AssemblyInformationalVersion", gitVersion.AssemblyInformationalVersion.ToString())
            .WithProperty("PackageVersion", gitVersion.NuGetPackageVersion.ToString())
            .WithProperty("PackageOutputPath", ARTIFACTS_DIR)
			.WithProperty("DesignTimeBuild", "false"));
});

Task ("publish").IsDependentOn("nuget").Does(() => 
{
    if (!IS_CLOUD_BUILD) 
    {
        Error("Script is not being run by a build task.");
        return;
    }

    var feed = new {
        Name = EnvironmentVariable("FEED_NAME") ?? Argument<string>("FEED_NAME"),
        Source = EnvironmentVariable("FEED_SOURCE") ?? Argument<string>("FEED_SOURCE"),
        Username = EnvironmentVariable("FEED_USERNAME") ?? Argument<string>("FEED_USERNAME"),
        Password = EnvironmentVariable("FEED_PASSWORD") ?? Argument<string>("FEED_PASSWORD"),
    };

    NuGetAddSource(feed.Name, feed.Source, new NuGetSourcesSettings {
        Password = feed.Password,
        UserName = feed.Username,
        Verbosity = NuGetVerbosity.Detailed,
    });
    
    NuGetPush(GetFiles($"{ARTIFACTS_DIR}/*.nupkg"), new NuGetPushSettings {
        ApiKey = "VSTS",
        Source =  feed.Name,
        Verbosity = NuGetVerbosity.Detailed,
    });
});

RunTarget (TARGET);
