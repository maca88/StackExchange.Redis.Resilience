#tool nuget:?package=NUnit.ConsoleRunner&version=3.10.0
#tool nuget:?package=CSharpAsyncGenerator.CommandLine&version=0.17.1
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var netfx = Argument("netfx", "net472");

//////////////////////////////////////////////////////////////////////
// CONSTANTS
//////////////////////////////////////////////////////////////////////

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDirs = new List<string>()
{
    Directory("./StackExchange.Redis.Resilience/bin") + Directory(configuration),
    Directory("./StackExchange.Redis.Resilience.Tests/bin") + Directory(configuration)
};

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    foreach(var buildDir in buildDirs)
    {
        CleanDirectory(buildDir);
    }
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./StackExchange.Redis.Resilience.sln");
});

Task("RestoreCore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore("./StackExchange.Redis.Resilience.sln");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    MSBuild("./StackExchange.Redis.Resilience.sln", settings =>
        settings.SetConfiguration(configuration));
});

Task("BuildCore")
    .IsDependentOn("RestoreCore")
    .Does(() =>
{
    DotNetCoreBuild("./StackExchange.Redis.Resilience.sln", new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("--no-restore"),
    });
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./StackExchange.Redis.Resilience.Tests/bin/" + configuration + $"/{netfx}/*.Tests.dll", new NUnit3Settings
    {
        NoResults = true
    });
});

Task("TestCore")
    .IsDependentOn("BuildCore")
    .Does(() =>
{
    DotNetCoreTest("./StackExchange.Redis.Resilience.Tests/StackExchange.Redis.Resilience.Tests.csproj", new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true
    });
});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("CleanPackages")
    .Does(() =>
{
    CleanDirectory(PACKAGE_DIR);
});

Task("Pack")
    .IsDependentOn("CleanPackages")
    .Description("Creates NuGet packages")
    .Does(() =>
{
    CreateDirectory(PACKAGE_DIR);

    var projects = new string[]
    {
        "StackExchange.Redis.Resilience/StackExchange.Redis.Resilience.csproj"
    };

    foreach(var project in projects)
    {
        MSBuild(project, new MSBuildSettings {
            Configuration = configuration,
            ArgumentCustomization = args => args
                .Append("/t:pack")
                .Append("/p:PackageOutputPath=\"" + PACKAGE_DIR + "\"")
        });
    }
});

Task("Async")
    .IsDependentOn("Restore")
    .Does(() =>
{
    DotNetCoreExecute("./Tools/CSharpAsyncGenerator.CommandLine.0.17.1/tools/netcoreapp2.1/AsyncGenerator.CommandLine.dll");
});
    
Task("Publish")
    .IsDependentOn("Pack")
    .Does(() =>
{
    foreach(var package in System.IO.Directory.GetFiles(PACKAGE_DIR, "*.nupkg").Where(o => !o.Contains("symbols")))
    {
        NuGetPush(package, new NuGetPushSettings()
        {
            Source = "https://api.nuget.org/v3/index.json"
        });
    }
});



//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
