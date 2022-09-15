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

Task("clean")
    .Does(() =>
{
    foreach(var buildDir in buildDirs)
    {
        CleanDirectory(buildDir);
    }
});

Task("restore")
    .IsDependentOn("clean")
    .Does(() =>
{
    DotNetRestore("./StackExchange.Redis.Resilience.sln");
});

Task("build")
    .IsDependentOn("restore")
    .Does(() =>
{
    DotNetBuild("./StackExchange.Redis.Resilience.sln", new DotNetCoreBuildSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("--no-restore"),
    });
});

Task("build-source-generator")
    .Does(() =>
{
    DotNetBuild("./StackExchange.Redis.Resilience.SourceGenerator/StackExchange.Redis.Resilience.SourceGenerator.csproj", new DotNetCoreBuildSettings
    {
        Configuration = "Release"
    });
});

Task("test")
    .IsDependentOn("build")
    .Does(() =>
{
    DotNetTest("./StackExchange.Redis.Resilience.Tests/StackExchange.Redis.Resilience.Tests.csproj", new DotNetCoreTestSettings
    {
        Configuration = configuration,
        NoBuild = true
    });
});

Task("generate-async")
    .IsDependentOn("restore")
    .Does(() =>
{
    DotNetTool("async-generator");
});

Task("generate-source")
    .IsDependentOn("build-source-generator")
    .Does(() =>
{
    DotNetExecute("./StackExchange.Redis.Resilience.SourceGenerator/bin/Release/net6.0/StackExchange.Redis.Resilience.SourceGenerator.dll");
});

//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

Task("cleanPackages")
    .Does(() =>
{
    CleanDirectory(PACKAGE_DIR);
});

Task("pack")
    .IsDependentOn("cleanPackages")
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

Task("publish")
    .IsDependentOn("pack")
    .Does(() =>
{
    foreach(var package in System.IO.Directory.GetFiles(PACKAGE_DIR, "*.nupkg").Where(o => !o.Contains("symbols")))
    {
        DotNetNuGetPush(package, new DotNetNuGetPushSettings()
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
