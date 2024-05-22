﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using sttz.NiceConsoleLogger;

using static sttz.InstallUnity.UnityReleaseAPIClient;

namespace sttz.InstallUnity
{

/// <summary>
/// Command line interface to the <see cref="sttz.InstallUnity"/> library.
/// </summary>
public class InstallUnityCLI
{
    /// <summary>
    /// The selected action.
    /// </summary>
    public string action;

    /// <summary>
    /// Wether the action was selected explicitly or the default is used.
    /// </summary>
    public bool usingDefaultAction;

    // -------- Options --------

    /// <summary>
    /// Wether to show the help.
    /// </summary>
    public bool help;
    /// <summary>
    /// Wether to print the program version.
    /// </summary>
    public bool version;
    /// <summary>
    /// Verbosity of the output (0 = default, 1 = verbose, 3 = extra verbose)
    /// </summary>
    public int verbose;
    /// <summary>
    /// Assume yes for prompts.
    /// </summary>
    public bool yes;
    /// <summary>
    /// Wether to force an update of the versions cache.
    /// </summary>
    public bool update;
    /// <summary>
    /// Clear the versions cache.
    /// </summary>
    public bool clearCache;
    /// <summary>
    /// Platform of the editor to download.
    /// </summary>
    public Platform platform;
    /// <summary>
    /// Architecture of the editor to download and/or install.
    /// </summary>
    public Architecture architecture;
    /// <summary>
    /// Path to store all data at.
    /// </summary>
    public string dataPath;
    /// <summary>
    /// Set generic program options.
    /// </summary>
    public List<string> options = new List<string>();

    /// <summary>
    /// The original argument parsed into <see cref="versionArgument"/> or <see cref="uriArgument"/>.
    /// </summary>
    public string versionOrUrl;
    /// <summary>
    /// Version argument.
    /// </summary>
    public UnityVersion versionArgument = UnityVersion.Undefined;
    /// <summary>
    /// Url or path argument.
    /// </summary>
    public Uri uriArgument;

    // -- List

    /// <summary>
    /// Wether to list installed versions for list command.
    /// </summary>
    public bool installed;

    // -- Install

    /// <summary>
    /// Packages to install for install command.
    /// </summary>
    public List<string> packages = new List<string>();
    /// <summary>
    /// Wether to only download packages with install command.
    /// </summary>
    public bool download;
    /// <summary>
    /// Wether to only install packages with install command.
    /// </summary>
    public bool install;
    /// <summary>
    /// Uninstall existing installation first.
    /// </summary>
    public bool upgrade;
    /// <summary>
    /// Force redownloading all files.
    /// </summary>
    public bool redownload;
    /// <summary>
    /// Skip size and hash checks for downloads.
    /// </summary>
    public bool yolo;

    // -- Run

    /// <summary>
    /// Run Unity as a child process.
    /// </summary>
    public bool child;
    /// <summary>
    /// Allow newer versions of Unity to open a project.
    /// </summary>
    public AllowNewer allowNewer;
    /// <summary>
    /// Version to upgrade project to when running.
    /// </summary>
    public string upgradeVersion;
    /// <summary>
    /// Arguments to launch Unity with.
    /// </summary>
    public List<string> unityArguments = new List<string>();

    /// <summary>
    /// Specify which newer versions are accepted to open a Unity project.
    /// </summary>
    public enum AllowNewer
    {
        None,
        Hash,
        Build,
        Patch,
        Minor,
        All
    }

    // -- Create

    /// <summary>
    /// Path to create the new project at.
    /// </summary>
    public string projectPath;
    /// <summary>
    /// Type of project to create.
    /// </summary>
    public CreateProjectType projectType;
    /// <summary>
    /// Immediately open the new project.
    /// </summary>
    public bool openProject;

    /// <summary>
    /// The type of project to create.
    /// </summary>
    public enum CreateProjectType
    {
        Basic,
        Minimal
    }

    // -------- Arguments Definition --------

    /// <summary>
    /// Convert the program back into a normalized arguments string.
    /// </summary>
    public override string ToString()
    {
        var cmd = (action != null && !usingDefaultAction) ? action : "";
        if (help) cmd += " --help";
        if (verbose > 0) cmd += string.Concat(Enumerable.Repeat(" --verbose", verbose));
        if (clearCache) cmd += " --clear-cache";
        if (update) cmd += " --update";
        if (dataPath != null) cmd += " --data-path " + dataPath;
        if (options.Count > 0) cmd += " --opt " + string.Join(" ", options);
        if (platform != Platform.None) cmd += " --platform " + platform;
        if (architecture != Architecture.None) cmd += " --arch " + platform;

        if (versionOrUrl != null) cmd += " " + versionOrUrl;

        if (installed) cmd += " --installed";

        if (packages.Count > 0) cmd += " --packages " + string.Join(" ", packages);
        if (download) cmd += " --download";
        if (install) cmd += " --install";
        if (upgrade) cmd += " --upgrade";

        if (child) cmd += " --child";
        if (allowNewer != AllowNewer.None) cmd += " --allow-newer " + allowNewer.ToString().ToLower();
        if (upgradeVersion != null) cmd += " --upgrade " + upgradeVersion;
        if (unityArguments.Count > 0) cmd += " -- " + string.Join(" ", unityArguments);

        return cmd;
    }

    /// <summary>
    /// Name of program used in output.
    /// </summary>
    public const string PROGRAM_NAME = "install-unity";

    /// <summary>
    /// The definition of the program's arguments.
    /// </summary>
    public static Arguments<InstallUnityCLI> ArgumentsDefinition {
        get {
            if (_arguments != null) return _arguments;

            _arguments = new Arguments<InstallUnityCLI>()
                .Action(null, (t, a) => t.action = a)

                .Option((InstallUnityCLI t, bool v) => t.help = v, "h", "?", "help")
                    .Description("Show this help")
                .Option((InstallUnityCLI t, bool v) => t.version = v, "version")
                    .Description("Print the version of this program")
                .Option((InstallUnityCLI t, bool v) => t.verbose++, "v", "verbose").Repeatable()
                    .Description("Increase verbosity of output, can be repeated")
                .Option((InstallUnityCLI t, bool v) => t.yes = v, "y", "yes")
                    .Description("Don't prompt for confirmation (use with care)")
                .Option((InstallUnityCLI t, bool v) => t.update = v, "u", "update")
                    .Description("Force an update of the versions cache")
                .Option((InstallUnityCLI t, bool v) => t.clearCache = v, "clear-cache")
                    .Description("Clear the versions cache before running any commands")
                .Option((InstallUnityCLI t, string v) => t.dataPath = v, "data-path", "datapath")
                    .ArgumentName("<path>")
                    .Description("Store all data at the given path, also don't delete packages after install")
                .Option((InstallUnityCLI t, IList<string> v) => t.options.AddRange(v), "opt").Repeatable()
                    .ArgumentName("<name>=<value>")
                    .Description("Set additional options. Use '--opt list' to show all options and their default value"
                    + " and '--opt save' to create an editable JSON config file.")

                .Action("list", (t, a) => t.action = a)
                    .Description("Get an overview of available or installed Unity versions")
                
                .Option((InstallUnityCLI t, string v) => t.ProcessVersionArgument(v), 0)
                    .ArgumentName("<version>")
                    .Description("Pattern to match Unity version")
                .Option((InstallUnityCLI t, bool v) => t.installed = v, "i", "installed")
                    .Description("List installed versions of Unity")
                .Option((InstallUnityCLI t, Platform v) => t.platform = v, "platform")
                    .Description("Platform to list the versions for (default = current platform)")
                .Option((InstallUnityCLI t, Architecture v) => t.architecture = v, "arch")
                    .Description("Architecture to list the versions for (default = current architecture)")

                .Action("details", (t, a) => t.action = a)
                    .Description("Show version information and all its available packages")

                .Option((InstallUnityCLI t, string v) => t.ProcessVersionArgument(v, allowUrl: true), 0)
                    .ArgumentName("<version>")
                    .Description("Pattern to match Unity version or release notes / unity hub url")
                .Option((InstallUnityCLI t, Platform v) => t.platform = v, "platform")
                    .Description("Platform to show the details for (default = current platform)")
                .Option((InstallUnityCLI t, Architecture v) => t.architecture = v, "arch")
                    .Description("Architecture to show the details for (default = current architecture)")

                .Action("install", (t, a) => t.action = a)
                    .DefaultAction(t => t.usingDefaultAction = true)
                    .Description("Download and install a version of Unity")
                
                .Option((InstallUnityCLI t, string v) => t.ProcessVersionArgument(v, allowUrl: true), 0)
                    .ArgumentName("<version>")
                    .Description("Pattern to match Unity version or release notes / unity hub url")
                .Option((InstallUnityCLI t, IList<string> v) => t.packages.AddRange(v), "p", "packages").Repeatable()
                    .ArgumentName("<name,name>")
                    .Description("Select packages to download and install ('all' selects all available, '~NAME' matches substrings)")
                .Option((InstallUnityCLI t, bool v) => t.download = v, "download")
                    .Description("Only download the packages (requires '--data-path')")
                .Option((InstallUnityCLI t, bool v) => t.install = v, "install")
                    .Description("Install previously downloaded packages (requires '--data-path')")
                .Option((InstallUnityCLI t, bool v) => t.upgrade = v, "upgrade")
                    .Description("Replace existing matching Unity installation after successful install")
                .Option((InstallUnityCLI t, Platform v) => t.platform = v, "platform")
                    .Description("Platform to download the packages for (only valid with '--download', default = current platform)")
                .Option((InstallUnityCLI t, Architecture v) => t.architecture = v, "arch")
                    .Description("Architecture to download the packages for (default = current architecture)")
                .Option((InstallUnityCLI t, bool v) => t.redownload = v, "redownload")
                    .Description("Force redownloading all files")
                .Option((InstallUnityCLI t, bool v) => t.yolo = v, "yolo")
                    .Description("Skip size and hash checks of downloaded files")
                
                .Action("uninstall", (t, a) => t.action = a)
                    .Description("Remove a previously installed version of Unity")
                
                .Option((InstallUnityCLI t, string v) => t.ProcessVersionArgument(v, allowPath: true), 0)
                    .ArgumentName("<version-or-path>")
                    .Description("Pattern to match Unity version or path to installation root")
                    
                .Action("run", (t, a) => t.action = a)
                    .Description("Execute a version of Unity or a Unity project, matching it to its Unity version")
                
                .Option((InstallUnityCLI t, string v) => t.ProcessVersionArgument(v, allowPath: true), 0).Required()
                    .ArgumentName("<version-or-path>")
                    .Description("Pattern to match Unity version or path to a Unity project")
                .Option((InstallUnityCLI t, string v) => t.unityArguments.Add(v), 1).Repeatable()
                    .ArgumentName("<unity-arguments>")
                    .Description("Arguments to launch Unity with (put a -- first to avoid Unity options being parsed as install-unity options)")
                .Option((InstallUnityCLI t, bool v) => t.child = v, "c", "child")
                    .Description("Run Unity as a child process and forward its log output (only errors, use -v to see the full log)")
                .Option((InstallUnityCLI t, AllowNewer v) => t.allowNewer = v, "a", "allow-newer", "allownewer")
                    .ArgumentName("none|hash|build|patch|minor|all")
                    .Description("Allow newer versions of Unity to open a project")
                .Option((InstallUnityCLI t, string v) => t.upgradeVersion = v, "upgrade")
                    .ArgumentName("<version>")
                    .Description("Run the project with the highest installed Unity version matching the pattern")
                
                .Action("create", (t, a) => t.action = a)
                    .Description("Create a new empty Unity project")
                
                .Option((InstallUnityCLI t, string v) => t.ProcessVersionArgument(v), 0).Required()
                    .ArgumentName("<version>")
                    .Description("Pattern to match the Unity version to create the project with")
                .Option((InstallUnityCLI t, string v) => t.projectPath = v, 1).Required()
                    .ArgumentName("<path>")
                    .Description("Path to the new Unity project")
                .Option((InstallUnityCLI t, CreateProjectType v) => t.projectType = v, "type")
                    .ArgumentName("<basic|minimal>")
                    .Description("Type of project to create (basic = standard project, minimal = no packages/modules)")
                .Option((InstallUnityCLI t, bool v) => t.openProject = v, "o", "open")
                    .Description("Open the new project in the editor");

                return _arguments;
        }
    }
    static Arguments<InstallUnityCLI> _arguments;

    /// <summary>
    /// Main entry method.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        enableColors = Environment.GetEnvironmentVariable("CLICOLORS") != "0";

        var cli = new InstallUnityCLI();
        try {
            ArgumentsDefinition.Parse(cli, args);

            if (cli.help) {
                cli.PrintHelp();
                return 0;
            } else if (cli.version) {
                cli.PrintVersion();
                return 0;
            }

            switch (cli.action) {
                case "list":
                    await cli.List();
                    break;
                case "details":
                    await cli.Details();
                    break;
                case "install":
                    if (cli.versionOrUrl == null) {
                        await cli.ListUpdates();
                    } else {
                        await cli.Install();
                    }
                    break;
                case "uninstall":
                    await cli.Uninstall();
                    break;
                case "run":
                    await cli.Run();
                    break;
                case "create":
                    await cli.Create();
                    break;
                default:
                    throw new Exception("Unknown action: " + cli.action);
            }
            return 0;

        } catch (Exception e) {
            Arguments<InstallUnityCLI>.WriteException(e, args, cli.verbose > 0, enableColors);

            if (cli.help) {
                cli.PrintHelp();
            }

            return 1;
        }
    }

    /// <summary>
    /// Print the help for this program.
    /// </summary>
    public void PrintHelp()
    {
        PrintVersion();
        Console.WriteLine();
        Console.WriteLine(ArgumentsDefinition.Help(PROGRAM_NAME, null, null));
    }

    /// <summary>
    /// Return the version of this program.
    /// </summary>
    public string GetVersion(bool includeGitCommit)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(assembly).InformationalVersion;
        
        if (!includeGitCommit) {
            var plusIndex = version.IndexOf('+');
            if (plusIndex > 0) {
                version = version.Substring(0, plusIndex);
            }
        }

        return version;
    }

    /// <summary>
    /// Print the program name and version to the console.
    /// </summary>
    public void PrintVersion()
    {
        Console.WriteLine($"{PROGRAM_NAME} v{GetVersion(verbose > 0)}");
    }

    // -------- Global --------

    UnityInstaller installer;
    ILogger Logger;
    HashSet<UnityVersion> newVersions;

    const string UriSchemeUnityhub = "unityhub";

    public void ProcessVersionArgument(string argument, bool allowPath = false, bool allowUrl = false)
    {
        versionOrUrl = argument;

        versionArgument = new UnityVersion(argument);
        if (versionArgument.IsValid) {
            // Valid Unity version
            return;
        }

        if ((allowPath || allowUrl) && Uri.TryCreate(argument, UriKind.RelativeOrAbsolute, out uriArgument)) {
            if (allowPath && (!uriArgument.IsAbsoluteUri || uriArgument.IsFile)) {
                // Valid file path
                return;
            } else if (allowUrl && uriArgument.IsAbsoluteUri && (
                    uriArgument.Scheme == Uri.UriSchemeHttp || 
                    uriArgument.Scheme == Uri.UriSchemeHttps ||
                    uriArgument.Scheme == UriSchemeUnityhub
            )) {
                // Valid http/https/unityhub URL
                return;
            }
        }

        // Invalid
        var type = "version";
        if (allowPath) type += "/path";
        if (allowUrl) type += "/url";

        var example = "6000, 6000.1.1, 6000b, c36be92430b9";
        if (allowPath) example += ", /path/to/unity";
        if (allowUrl) example += ", https://unity.com/path/to/unity, unityhub://VERSION";

        throw new Exception($"Unrecognized Unity {type} '{argument}' (must be e.g. {example})");
    }

    public async Task<UnityVersion> Setup(bool avoidCacheUpate = false)
    {
        var level = LogLevel.Warning;
        if (verbose >= 3) {
            level = LogLevel.Trace;
        } else if (verbose == 2) {
            level = LogLevel.Debug;
        } else if (verbose == 1) {
            level = LogLevel.Information;
        }

        var factory = new LoggerFactory()
            .AddNiceConsole(level, false);
        Logger = factory.CreateLogger<InstallUnityCLI>();

        Logger.LogInformation($"{PROGRAM_NAME} v{GetVersion(true)}");
        if (level != LogLevel.Warning) Logger.LogInformation($"Log level set to {level}");

        // Create installer instance
        installer = new UnityInstaller(dataPath: dataPath, loggerFactory: factory);

        // Re-set colors based on loaded configuration
        enableColors = enableColors && installer.Configuration.enableColoredOutput;
        if (!enableColors) Logger.LogInformation("Console colors disabled");

        // Set current platform
        if (platform == Platform.None || architecture == Architecture.None) {
            var (defaultPlatform, defaultarch) = await installer.Platform.GetCurrentPlatform();
            if (platform == Platform.None)         platform = defaultPlatform;
            if (architecture == Architecture.None) architecture = defaultarch;
        }
        Logger.LogDebug($"Selected platform {platform}-{architecture}");

        // Parse version argument (positional argument)
        UnityVersion version = versionArgument;
        if (version.type == UnityVersion.Type.Undefined && version.hash == null) {
            // Default to Final version if input does not specify it
            version.type = UnityVersion.Type.Final;
        }

        // Set additional configuration (--opt NAME=VALUE)
        if (options != null) {
            if (options.Contains("list", StringComparer.OrdinalIgnoreCase)) {
                ListOptions(installer.Configuration);
                Environment.Exit(0);
            } else if (options.Contains("save")) {
                var configPath = installer.DataPath ?? installer.Platform.GetConfigurationDirectory();
                configPath = Path.Combine(configPath, UnityInstaller.CONFIG_FILENAME);
                if (File.Exists(configPath)) {
                    Console.WriteLine($"Configuration file already exists:\n{configPath}");
                } else {
                    installer.Configuration.Save(configPath);
                    Console.WriteLine($"Configuration file saved to:\n{configPath}");
                }
                Environment.Exit(0);
            }

            foreach (var option in options) {
                var parts = option.Split('=');
                if (parts.Length != 2) {
                    throw new Exception($"Option needs to be in the format 'NAME=VALUE' (got '{option}')");
                }

                installer.Configuration.Set(parts[0], parts[1]);
            }
        }

        // Clear the versions cache (--clear-cache)
        if (clearCache) {
            installer.Versions.Clear();
            installer.Versions.Save();
        }

        // Update cache if needed or requested (--update)
        var updateType = version.type;
        if (updateType == UnityVersion.Type.Undefined) {
            if (version.hash != null) {
                updateType = UnityVersion.Type.Beta;
            } else {
                updateType = UnityVersion.Type.Final;
            }
        }
        
        IEnumerable<VersionMetadata> newVersionsData;
        if (update || (!avoidCacheUpate && installer.IsCacheOutdated(updateType))) {
            WriteTitle("Updating Cache...");
            newVersionsData = await installer.UpdateCache(platform, architecture, updateType);

            var total = newVersionsData.Count();
            var maxVersions = 10;
            if (total == 0) {
                Console.WriteLine("No new Unity versions");
            } else if (total > 0) {
                Console.WriteLine($"New Unity version{(total > 1 ? "s" : "")}:");
                foreach (var newVersion in newVersionsData.OrderByDescending(m => m.Version).Take(maxVersions)) {
                    var url = Scraper.GetReleaseNotesUrl(newVersion.release.stream, newVersion.Version);
                    Console.WriteLine($"- {newVersion.Version} ({url})");
                }
                if (total - maxVersions > 0) {
                    Console.WriteLine($"And {total - maxVersions} more...");
                }
            }

            if (total <= maxVersions) {
                newVersions = new HashSet<UnityVersion>();
                foreach (var newVersion in newVersionsData.Select(d => d.Version)) {
                    newVersions.Add(newVersion);
                }
            }
        }

        return version;
    }

    async Task<(UnityVersion, VersionMetadata)> SelectAndLoad(UnityVersion version, Uri uri, bool installOnly)
    {
        VersionMetadata metadata;
        
        if (uri != null) {
            if (uri.Scheme == UriSchemeUnityhub) {
                // Got unityhub url as version
                metadata = installer.Scraper.UnityHubUrlToVersion(uri.ToString());
                if (metadata.baseUrl == null) {
                    throw new Exception("Could not parse Unity Hub URL: " + uri.ToString());
                }

            } else {
                // Got http url as version, try to scrape url
                Logger.LogInformation($"Got url instead of version, trying to find version at url...");
                metadata = await installer.Scraper.LoadUrl(uri.ToString());

                if (!metadata.Version.IsValid) {
                    throw new Exception("Could not find version at url: " + uri.ToString());
                }
            }

            version = metadata.Version;

        } else if (version.IsValid) {
            // Locate version in cache or look it up
            metadata = installer.Versions.Find(version);
            if (!metadata.Version.IsValid) {
                if (installOnly) {
                    throw new Exception("Could not find version matching input: " + version);
                }

                try {
                    Logger.LogInformation($"Version {version} not found in cache, trying exact lookup");
                    metadata.release = await installer.Releases.FindRelease(version, platform, architecture);
                } catch (Exception e) {
                    Logger.LogInformation("Failed exact lookup: " + e.Message);
                }

                if (!metadata.Version.IsValid) {
                    throw new Exception("Could not find version matching input: " + version);
                }

                installer.Versions.Add(metadata);
                installer.Versions.Save();
                Console.WriteLine($"Guessed release notes URL to discover {metadata.Version}");
            }

        } else {
            throw new Exception($"Neither Unity version or URL specified");
        }

        if (!metadata.Version.MatchesVersionOrHash(version)) {
            Console.WriteLine();
            ConsoleLogger.WriteLine($"Selected <white bg=darkgray>{metadata.Version}</white> for input {version}");
        }

        // Load packages ini if needed
        var editor = metadata.GetEditorDownload(platform, architecture);
        if (editor == null) {
            if (installOnly) {
                throw new Exception("Packages not found in versions cache (install only): " + version);
            }

            // Try to load version from API
            // The API doesn't allow lookup by hash, so we have to check if after loading
            Logger.LogInformation($"Missing packages for {platform}-{architecture} in cache, loading from Release API");
            var release = await installer.Releases.FindRelease(metadata.Version, platform, architecture);
            if (release != null && (metadata.Version.hash == null || release.version.hash == metadata.Version.hash)) {
                editor = release.downloads.Where(d => d.platform == platform && d.architecture == architecture).FirstOrDefault();
                if (editor != null) {
                    metadata.SetEditorDownload(editor);
                    installer.Versions.Add(metadata);
                    installer.Versions.Save();
                }
            }

            // Fall back to look up version using legacy INI system (for e.g. test releases of the editor)
            if (editor == null && !string.IsNullOrEmpty(metadata.baseUrl)) {
                Logger.LogInformation("Loading packages from legacy INI system");
                metadata = await installer.Scraper.LoadPackages(metadata, platform, architecture);
                installer.Versions.Add(metadata);
                installer.Versions.Save();
            }

            if (editor == null) {
                throw new Exception($"Could not load packages for {metadata.Version} on {platform}-{architecture}");
            }
        }

        return (version, metadata);
    }

    void ListOptions(Configuration config)
    {
        WriteBigTitle("Configuration Options");

        var options = Configuration.ListOptions();
        fieldWidth = options.Max(o => o.name.Length) + 1;
        foreach (var option in options.OrderBy(o => o.name)) {
            WriteField(option.name, $"{option.description} ({option.type.Name} = {config.Get(option.name)})");
        }

        Console.WriteLine();
        Console.WriteLine("To persist options, use '--opt save' and then edit the generated config file.");
    }

    // -------- Updates --------

    static bool IsNewerPatch(UnityVersion version, UnityVersion installed)
    {
        if (version.major != installed.major || version.minor != installed.minor)
            return false;
        
        if (version.patch > installed.patch)
            return true;
        else if (version.patch != installed.patch)
            return false;
        
        var versionType = UnityVersion.GetSortingForType(version.type);
        var installedType = UnityVersion.GetSortingForType(installed.type);
        if (versionType > installedType)
            return true;
        else if (versionType != installedType)
            return false;
        
        if (version.build > installed.build)
            return true;
        else
            return false;
    }

    public async Task ListUpdates()
    {
        Console.WriteLine($"{PROGRAM_NAME} v{GetVersion(false)}, use --help to show usage");

        // Force scanning for prerelease versions
        versionArgument = new UnityVersion(type: UnityVersion.Type.Alpha);

        var version = await Setup();
        var installs = await installer.Platform.FindInstallations();

        if (!installs.Any()) {
            var latest = installer.Versions
                .Where(m => (m.release.stream & ReleaseStream.PrereleaseMask) == 0)
                .OrderByDescending(m => m.Version)
                .FirstOrDefault();

            Console.WriteLine("No installed Unity versions found.");

            if (latest.Version.IsValid) {
                Console.WriteLine();
                Console.WriteLine($"The latest Unity version available is {latest.Version.ToString(verbose > 0)}.");
            }

        } else {
            // Find updates to installed minor Unity versions
            var updates = new List<(Installation install, VersionMetadata update)>();
            foreach (var install in installs.OrderByDescending(i => i.version)) {
                var newerPatch = installer.Versions
                    .Where(m => IsNewerPatch(m.Version, install.version))
                    .FirstOrDefault();
                if (newerPatch.Version.IsValid) {
                    updates.Add((install, newerPatch));
                }
            }

            if (updates.Count == 0) {
                Console.WriteLine("No minor updates to installed Unity versions.");
            } else {
                WriteTitle("Minor updates to installed Unity versions:");
                foreach (var update in updates) {
                    Console.WriteLine($"- {update.install.version.ToString(verbose > 0)} ➤ {update.update.Version.ToString(verbose > 0)}");
                }
            }

            // Compile latest version for each major.minor release
            var mms = installer.Versions
                .OrderByDescending(m => m.Version)
                .GroupBy(m => (major: m.Version.major, minor: m.Version.minor))
                .Select(g => g.First())
                .Reverse();

            // Find major/minor new Unity versions not yet installed
            var latest = installs.OrderByDescending(i => i.version).First();
            var newer = mms
                .Where(m => (m.release.stream & ReleaseStream.PrereleaseMask) == 0 && m.Version.major > latest.version.major);

            if (newer.Any()) {
                WriteTitle("New major Unity versions:");
                foreach (var m in newer) {
                    if (verbose > 0) {
                        Console.WriteLine($"- {m.Version}");
                    } else {
                        Console.WriteLine($"- {m.Version.major}.{m.Version.minor}");
                    }
                }
            }

            // Find alpha/beta/RC versions not yet installed
            var abs = mms
                .Where(m => 
                    (m.release.stream & ReleaseStream.PrereleaseMask) != 0
                    && m.Version > latest.version 
                    && (m.Version.major != latest.version.major
                    || m.Version.minor != latest.version.minor)
                );
            
            if (abs.Any()) {
                WriteTitle("New Unity release candidates, betas and alphas:");
                foreach (var m in abs) {
                    if (verbose > 0) {
                        Console.WriteLine($"- {m.Version}");
                    } else {
                        Console.WriteLine($"- {m.Version.major}.{m.Version.minor}{(char)m.Version.type}");
                    }
                }
            }
        }
    }

    // -------- List Versions --------

   const int ListVersionsColumnWidth = 16;
   const int ListVersionsWithHashColumnWith = 31;

    public async Task List()
    {
        var version = await Setup(avoidCacheUpate: installed);
        var installs = await installer.Platform.FindInstallations();

        if (installed) {
            // Use original parsed version to get default undefined for type
            InstalledList(installer, versionArgument, installs);
        } else {
            VersionsTable(installer, version, installs);
        }
    }

    public void InstalledList(UnityInstaller installer, UnityVersion version, IEnumerable<Installation> installations)
    {
        foreach (var install in installations.OrderByDescending(i => i.version)) {
            if (!version.FuzzyMatches(install.version)) continue;
            Console.WriteLine($"{install.version}\t{install.path}");
        }
    }

    public void VersionsTable(UnityInstaller installer, UnityVersion version, IEnumerable<Installation> installations = null)
    {
        if (version.type == UnityVersion.Type.Undefined) {
            version.type = UnityVersion.Type.Final;
        }

        HashSet<UnityVersion> installed = null;
        if (installations != null) {
            installed = installations.Select(i => i.version).ToHashSet();
        }

        // First sort versions into rows and columns based on major and minor version
        var majorRows = new List<List<List<VersionMetadata>>>();
        var currentRow = new List<List<VersionMetadata>>();
        var currentList = new List<VersionMetadata>();
        int lastMajor = -1, lastMinor = -1;
        foreach (var metadata in installer.Versions) {
            if (!version.FuzzyMatches(metadata.Version)) continue;
            var other = metadata.Version;

            if (lastMinor < 0) lastMinor = other.minor;
            else if (lastMinor != other.minor) {
                lastMinor = other.minor;
                currentRow.Add(currentList);
                currentList = new List<VersionMetadata>();
            }

            if (lastMajor < 0) lastMajor = other.major;
            else if (lastMajor != other.major) {
                lastMajor = other.major;
                majorRows.Add(currentRow);
                currentRow = new List<List<VersionMetadata>>();
            }

            currentList.Add(metadata);
        }
        if (currentList.Count > 0) currentRow.Add(currentList);
        if (currentRow.Count > 0) majorRows.Add(currentRow);

        if (currentRow.Count == 0) {
            Console.WriteLine("No versions found for input version: " + version);
            return;
        }

        // Write the generated columns line by line, wrapping to buffer size
        var colWidth = (verbose > 0 ? ListVersionsWithHashColumnWith : ListVersionsColumnWidth);
        var maxColumns = Math.Max(Console.BufferWidth / colWidth, 1);
        foreach (var majorRow in majorRows) {
            // Major version separator / title
            var major = majorRow[0][0].Version.major;
            WriteBigTitle(major.ToString());
            
            var groupCount = (majorRow.Count - 1) / maxColumns + 1;
            for (var g = 0; g < groupCount; g++) {
                var maxCount = majorRow.Skip(g * maxColumns).Take(maxColumns).Max(l => l.Count);
                for (int r = -1; r < maxCount; r++) {
                    var columnOffset = g * maxColumns;
                    for (int c = columnOffset; c < columnOffset + maxColumns && c < majorRow.Count; c++) {
                        if (r == -1) {
                            // Minor version title
                            Console.SetCursorPosition((c - columnOffset) * colWidth, Console.CursorTop);

                            SetColors(ConsoleColor.White, ConsoleColor.DarkGray);
                            var minorVersion = majorRow[c][0].Version;
                            var title = minorVersion.major + "." + minorVersion.minor;
                            Console.Write(title);

                            ResetColor();
                            continue;
                        }
                        if (r >= majorRow[c].Count) continue;
                        Console.SetCursorPosition((c - columnOffset) * colWidth, Console.CursorTop);

                        var m = majorRow[c][r];
                        Console.Write(m.Version.ToString(verbose > 0));

                        var isNewVersion = (newVersions != null && newVersions.Contains(m.Version));
                        var isInstalled = (installed != null && installed.Contains(m.Version));

                        if (isNewVersion || isInstalled) {
                            SetColors(ConsoleColor.White, ConsoleColor.DarkGray);
                            Console.Write(" ");
                            if (isInstalled)  Console.Write("✓︎");
                            if (isNewVersion) Console.Write("⬆︎");
                            ResetColor();
                        }
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }
    }

    // -------- Version Details --------

    public async Task Details()
    {
        var version = await Setup();
        (_, var metadata) = await SelectAndLoad(version, uriArgument, false);
        ShowDetails(installer, metadata);
    }

    void ShortModulesList(VersionMetadata metadata)
    {
        var editor = metadata.GetEditorDownload(platform, architecture);

        var list = editor.AllModules.Values
            .Where(p => !p.hidden)
            .Select(p => p.id + (p.preSelected ? "*" : ""))
            .OrderBy(p => p)
            .Prepend("unity");
        if (list.Any()) {
            Console.WriteLine(list.Count() + " Packages: " + string.Join(", ", list));
            Console.WriteLine();
        }

        list = editor.AllModules.Values
            .Where(p => p.hidden)
            .Select(p => p.id + (p.preSelected ? "*" : ""))
            .OrderBy(p => p);
        if (list.Any()) {
            Console.WriteLine(list.Count() + " Hidden Packages: " + string.Join(", ", list));
            Console.WriteLine();
        }

        Console.WriteLine("* = default package");
        Console.WriteLine();
    }

    void DetailedModulesList(IEnumerable<Module> modules)
    {
        fieldWidth = 14;
        foreach (var module in modules) {
            SetColors(ConsoleColor.DarkGray, ConsoleColor.DarkGray);
            Console.Write("--------------- ");
            SetForeground(ConsoleColor.White);
            Console.Write($"{module.name} [{module.id}] " + (module.preSelected ? " *" : ""));
            ResetColor();
            Console.WriteLine();

            WriteField("Description", module.description);
            WriteField("URL", module.url);
            WriteField("Required", (module.required ? "yes" : null));
            WriteField("Hidden", (module.hidden ? "yes" : null));
            WriteField("Size", $"{Helpers.FormatSize(module.downloadSize.GetBytes())} ({Helpers.FormatSize(module.installedSize.GetBytes())} installed)");

            if (module.eula?.Length > 0) {
                WriteField("EULA", module.eula[0].message);
                foreach (var eula in module.eula) {
                    WriteField("", eula.label + ": " + eula.url);
                }
            }

            if (module.subModules?.Count > 0)
                WriteField("Sub-Modules", string.Join(", ", module.subModules.Select(m => m.name)));
            if (module.parentModuleId != null)
                WriteField("Parent Module", module.parentModuleId);

            WriteField("Hash", module.integrity);

            Console.WriteLine();
        }
    }

    void EditorPackageDetails(EditorDownload module)
    {
        fieldWidth = 14;

        SetColors(ConsoleColor.DarkGray, ConsoleColor.DarkGray);
        Console.Write("--------------- ");
        SetForeground(ConsoleColor.White);
        Console.Write($"Unity Editor [{module.Id}]" + " *");
        ResetColor();
        Console.WriteLine();

        WriteField("Description", "Main Unity Editor package");
        WriteField("URL", module.url);
        WriteField("Size", $"{Helpers.FormatSize(module.downloadSize.GetBytes())} ({Helpers.FormatSize(module.installedSize.GetBytes())} installed)");

        WriteField("Hash", module.integrity);

        Console.WriteLine();
    }

    void ShowDetails(UnityInstaller installer, VersionMetadata metadata)
    {
        var editor = metadata.GetEditorDownload(platform, architecture);

        WriteBigTitle($"Details for Unity {metadata.Version} {editor.platform}-{editor.architecture}");

        if (metadata.baseUrl != null) {
            Console.WriteLine("Base URL: " + metadata.baseUrl);
        }

        var releaseNotes = Scraper.GetReleaseNotesUrl(metadata.release.stream, metadata.Version);
        if (releaseNotes != null) {
            Console.WriteLine("Release notes: " + releaseNotes);
        }

        Console.WriteLine();

        ShortModulesList(metadata);

        EditorPackageDetails(editor);
        DetailedModulesList(IterateModulesRecursive(editor.modules, hidden: false));

        var hidden = IterateModulesRecursive(editor.modules, hidden: true);
        if (hidden.Any()) {
            WriteTitle("Hidden Packages");
            Console.WriteLine();
            DetailedModulesList(hidden);
        }
    }

    IEnumerable<Module> IterateModulesRecursive(IEnumerable<Module> modules, bool hidden)
    {
        foreach (var module in modules) {
            if (module.hidden == hidden)
                yield return module;

            if (module.subModules != null) {
                foreach (var subModule in IterateModulesRecursive(module.subModules, hidden)) {
                    yield return subModule;
                }
            }
        }
    }

    // -------- Install --------

    public async Task Install()
    {
        // Determine operation (based on --download and --install)
        var op = UnityInstaller.InstallStep.None;
        if (download) op |= UnityInstaller.InstallStep.Download;
        if (install)  op |= UnityInstaller.InstallStep.Install;

        if (op == UnityInstaller.InstallStep.None) {
            op = UnityInstaller.InstallStep.DownloadAndInstall;
        }

        var version = await Setup(op == UnityInstaller.InstallStep.Install);
        Logger.LogInformation($"Install steps: {op}");

        if (op != UnityInstaller.InstallStep.DownloadAndInstall && dataPath == null) {
            throw new Exception("'--download' and '--install' require '--data-path' to be set.");
        }

        if (upgrade && op == UnityInstaller.InstallStep.Download) {
            throw new Exception("'--upgrade' cannot be used with '--download'");
        }

        if (op != UnityInstaller.InstallStep.Download) {
            var installable = await installer.Platform.GetInstallableArchitectures();
            if (!installable.HasFlag(architecture)) {
                throw new Exception($"Cannot install {architecture} on the current platform ({platform}).");
            }
        }

        VersionMetadata metadata;
        (version, metadata) = await SelectAndLoad(version, uriArgument, op == UnityInstaller.InstallStep.Install);
        var editor = metadata.GetEditorDownload(platform, architecture);

        // Determine packages to install (-p / --packages or defaultPackages option)
        IEnumerable<string> selection = packages;
        if (string.Equals(selection.FirstOrDefault(), "all", StringComparison.OrdinalIgnoreCase)) {
            Logger.LogInformation("Found 'all', selecting all available packages");
            selection = editor.AllModules.Keys;
        } else if (!selection.Any()) {
            Console.WriteLine();
            if (installer.Configuration.defaultPackages != null) {
                Console.WriteLine("Selecting configured packages (select packages with '--packages', see available packages with 'details')");
                selection = installer.Configuration.defaultPackages;
            } else {
                Console.WriteLine("Selecting default packages (select packages with '--packages', see available packages with 'details')");
                selection = installer.GetDefaultPackages(metadata, platform, architecture);
            }
        }

        var notFound = new List<string>();
        var resolved = installer.ResolvePackages(metadata, platform, architecture, selection, notFound: notFound);

        // Check version to be installed against already installed
        Installation uninstall = null;
        if (upgrade || (op & UnityInstaller.InstallStep.Install) > 0) {
            var freshInstall = resolved.Any(p => p is EditorDownload);
            var installs = await installer.Platform.FindInstallations();
            var existing = installs.FirstOrDefault(i => i.version == metadata.Version);
            if (!freshInstall && existing == null) {
                throw new Exception($"Installing additional packages but Unity {metadata.Version} hasn't been installed yet (add the 'Unity' package to install it).");
            } else if (freshInstall && existing != null) {
                if (upgrade) {
                    Console.WriteLine($"Unity {metadata.Version} already installed at '{existing.path}', nothing to upgrade.");
                    Environment.Exit(0);
                } else {
                    throw new Exception($"Unity {metadata.Version} already installed at '{existing.path}' (remove the 'Unity' package to install additional packages).");
                }
            }

            // Find version to upgrade
            if (upgrade) {
                uninstall = installs
                    .Where(i => i.version <= metadata.Version)
                    .OrderByDescending(i => i.version)
                    .FirstOrDefault();
                Console.WriteLine();
                if (uninstall != null) {
                    Console.WriteLine($"Will be upgrading Unity {uninstall.version} at path: {uninstall.path}");
                } else {
                    Console.WriteLine($"No installed version matches {version}, nothing to upgrade.");
                }
            } else if (!freshInstall) {
                Console.WriteLine($"Will be installing additional packages for Unity {existing.version} at path: {existing.path}");
            }
        }

        WriteTitle("Selected packages:");
        long totalSpace = 0, totalDownload = 0;
        var packageList = resolved.OrderBy(p => p.Id);
        foreach (var package in packageList.Where(p => !(p is Module module) || !module.addedAutomatically)) {
            totalSpace += package.installedSize.GetBytes();
            totalDownload += package.downloadSize.GetBytes();
            Console.WriteLine($"- {package.Id} ({Helpers.FormatSize(package.downloadSize.GetBytes())})");
        }

        var deps = packageList.OfType<Module>().Where(m => m.addedAutomatically);
        if (deps.Any()) {
            WriteTitle("Additional dependencies:");
            Console.WriteLine("Dependencies are added automatically, prefix a package with = to install only that package.");
            foreach (var package in deps) {
                totalSpace += package.installedSize.GetBytes();
                totalDownload += package.downloadSize.GetBytes();
                Console.WriteLine($"- {package.name} ({Helpers.FormatSize(package.downloadSize.GetBytes())}) [from {package.parentModule.Id}]");
            }
        }

        Console.WriteLine();

        if (op == UnityInstaller.InstallStep.Download) {
            Console.WriteLine($"Will download {Helpers.FormatSize(totalDownload)}");
        } else if (op == UnityInstaller.InstallStep.Install) {
            Console.WriteLine($"Will install {Helpers.FormatSize(totalSpace)}");
        } else {
            Console.WriteLine($"Will download {Helpers.FormatSize(totalDownload)} and install {Helpers.FormatSize(totalSpace)}");
        }

        if (notFound.Count > 0) {
            Console.WriteLine();
            Logger.LogWarning("Following packages were not found: " + string.Join(", ", notFound.ToArray()));
        }

        // Make user accept additional EULAs
        var hasEula = false;
        foreach (var module in resolved.OfType<Module>()) {
            if (module.eula == null || module.eula.Length == 0) continue;
            hasEula = true;
            Console.WriteLine();
            SetForeground(ConsoleColor.Yellow);
            Console.WriteLine($"Installing '{module.name}' requires accepting following EULA(s).");
            foreach (var eula in module.eula) {
                Console.WriteLine(eula.message);
                Console.WriteLine($"- {eula.label}: {eula.url}");
            }
            ResetColor();
        }

        if (hasEula && !yes) {
            if (Console.IsOutputRedirected) {
                throw new Exception("The above EULA(s) need to be accepted (run the command interactively or use the -y option to accept all prompts).");
            } else {
                var response = Helpers.ConsolePrompt($"Do you agree to the above EULA(s)?", "yN");
            if (response != 'y') {
                Environment.Exit(1);
                }
            }
        }

        // Request password before download so the download & installation can go on uninterrupted
        if ((op & UnityInstaller.InstallStep.Install) > 0) {
            if (Console.IsOutputRedirected && !await installer.Platform.IsAdmin()) {
                throw new Exception($"The command needs to be run as root when installing.");
            } else if (!await installer.Platform.PromptForPasswordIfNecessary()) {
                Logger.LogInformation("Failed password prompt too many times");
                Environment.Exit(1);
            }
        }

        // Do the magic
        var downloadPath = installer.GetDownloadDirectory(metadata);
        Logger.LogInformation($"Downloading packages to '{downloadPath}'");

        var existingFile = Downloader.ExistingFile.Undefined;
        if (redownload) existingFile = Downloader.ExistingFile.Redownload;
        else if (yolo)  existingFile = Downloader.ExistingFile.Skip;

        Installation installed = null;
        var queue = installer.CreateQueue(metadata, platform, architecture, downloadPath, resolved);
        if (installer.Configuration.progressBar && !Console.IsOutputRedirected && verbose < 2) {
            var processTask = installer.Process(op, queue, existingFile);

            try {
                var refreshInterval = installer.Configuration.progressRefreshInterval;
                var statusInterval = installer.Configuration.statusRefreshEvery;
                var updateCount = 0L;
                while (!processTask.IsCompleted) {
                    WriteQueueStatus(queue, ++updateCount, statusInterval);
                    await Task.Delay(refreshInterval);
                }
                ClearQueueStatus(queue);
            } catch (Exception e) {
                Logger.LogWarning($"Progress bar disabled due to exception during rendering: {(verbose > 0 ? e.ToString() : e.Message)}");
                installed = await processTask;
            }

            if (processTask.IsFaulted) {
                throw processTask.Exception;
            } else {
                installed = processTask.Result;
            }
        } else {
            Logger.LogInformation("Progress bar is disabled");
            installed = await installer.Process(op, queue, existingFile);
        }

        if (dataPath == null) {
            Logger.LogInformation("Cleaning up downloaded packages ('--data-path' not set)");
            installer.CleanUpDownloads(queue);
        }

        if (uninstall != null) {
            Console.WriteLine($"Uninstalling old version...");
            await installer.Platform.Uninstall(uninstall);
            await installer.Platform.MoveInstallation(installed, uninstall.path);
        }

        if (op == UnityInstaller.InstallStep.Download) {
            WriteTitle($"Packages downloaded to '{downloadPath}'");
        } else {
            WriteTitle($"Unity {installed.version} installed to: {installed.path}");
        }
    }

    static readonly char[] SubProgress = new char[] { '▏', '▎', '▍', '▌', '▋', '▊', '▉', '█' };

    void WriteQueueStatus(UnityInstaller.Queue queue, long updateCount, int statusInterval)
    {
        var longestName = Math.Max(queue.items.Max(i => i.package.Id.Length), 12);
        var longestStatus = queue.items.Max(i => i.status?.Length ?? 37);

        Console.Write(new string(' ', Console.BufferWidth));
        if (Console.CursorLeft != 0) {
            Console.WriteLine();
        }

        var barStartCol = 4 + 1 + longestName + 1;
        foreach (var item in queue.items) {
            SetColors(ConsoleColor.White, ConsoleColor.DarkGray);
            switch (item.currentState) {
                case UnityInstaller.QueueItem.State.WaitingForDownload:
                    Console.Write("IDLE");
                    break;
                case UnityInstaller.QueueItem.State.Hashing:
                    Console.Write("HASH");
                    break;
                case UnityInstaller.QueueItem.State.Downloading:
                    Console.Write("DWNL");
                    break;
                case UnityInstaller.QueueItem.State.WaitingForInstall:
                    Console.Write("WAIT");
                    break;
                case UnityInstaller.QueueItem.State.Installing:
                    var pos = (int)((updateCount / statusInterval) % 4);
                    var str = new string(' ', pos) + "·" + new string(' ', 3 - pos);
                    Console.Write(str);
                    break;
                case UnityInstaller.QueueItem.State.Complete:
                    Console.Write("CMPL");
                    break;
            }
            ResetColor();

            Console.Write(" ");
            Console.Write(item.package.Id);
            Console.Write(new string(' ', Math.Max(barStartCol - Console.CursorLeft, 0)));

            var progressWidth = Console.BufferWidth - longestName - 6; // 4 for status, 2 padding
            if (progressWidth > 38) {
                if (item.currentState == UnityInstaller.QueueItem.State.Hashing 
                        || item.currentState == UnityInstaller.QueueItem.State.Downloading) {
                    if (updateCount % statusInterval == 0 || item.status == null) {
                        var bytes = item.downloader.BytesProcessed;
                        var total = item.downloader.BytesTotal;
                        var speed = (long)item.downloader.BytesPerSecond;
                        item.status = $" {Helpers.FormatSize(bytes),9} of {Helpers.FormatSize(total),9} @ {Helpers.FormatSize(speed),9}/s";
                    }

                    var barLength = progressWidth - Math.Max(longestStatus, item.status.Length) - 3;
                    if (barLength >= 10) {
                        Console.Write("║");

                        if (item.downloader.BytesTotal > 0) {
                            var progress = (float)item.downloader.BytesProcessed / item.downloader.BytesTotal;
                            var fractionalWidth = progress * barLength;
                            var subIndex = (int)((fractionalWidth % 1) * SubProgress.Length);
                            Console.Write(new string('█', (int)fractionalWidth));
                            Console.Write(SubProgress[subIndex]);
                            Console.Write(new string('·', barLength - (int)fractionalWidth));
                        } else {
                            Console.Write(new string('~', barLength));
                        }

                        Console.Write("║");
                        Console.Write(new string(' ', Math.Max(Console.BufferWidth - Console.CursorLeft - item.status.Length, 0)));
                    }
                    Console.Write(item.status);
                } else {
                    Console.Write(new string(' ', progressWidth));
                }
            }

            if (Console.CursorLeft != 0) {
                Console.WriteLine();
            }
        }

        Console.Write(new string(' ', Console.BufferWidth));
        if (Console.CursorLeft != 0) {
            Console.WriteLine();
        }

        var lines = 2 + queue.items.Count;
        Console.SetCursorPosition(0, Console.CursorTop - lines);
    }

    void ClearQueueStatus(UnityInstaller.Queue queue)
    {
        var lines = 2 + queue.items.Count;
        for (int i = 0; i < lines; i++) {
            Console.Write(new string(' ', Console.BufferWidth));
            if (Console.CursorLeft != 0) {
                Console.WriteLine();
            }
        }
        Console.SetCursorPosition(0, Console.CursorTop - lines);
    }

    // -------- Uninstall --------

    public async Task Uninstall()
    {
        await Setup(avoidCacheUpate: true);

        var installs = await installer.Platform.FindInstallations();
        if (!installs.Any()) {
            throw new Exception("Could not find any installed versions of Unity");
        }

        Installation uninstall = null;
        if (versionArgument.IsValid) {
            foreach (var install in installs) {
                if (versionArgument.FuzzyMatches(install.version)) {
                    if (uninstall != null) {
                        throw new Exception($"Version {version} is ambiguous between\n"
                            + $"{uninstall.version} at '{uninstall.path}' and\n"
                            + $"{install.version} at '{install.path}'\n"
                            + "(use exact version or path instead).");
                    }
                    uninstall = install;
                }
            }
        } else if (uriArgument != null) {
            var fullPath = Path.GetFullPath(uriArgument.ToString());
            foreach (var install in installs) {
                var fullInstallPath = Path.GetFullPath(install.path);
                if (fullPath == fullInstallPath) {
                    uninstall = install;
                    break;
                }
            }
        } else {
            throw new Exception($"Neither Unity verison or path is specified");
        }

        if (uninstall == null) {
            throw new Exception("No matching version found to uninstall.");
        }

        if (!yes && !Console.IsOutputRedirected) {
            var response = Helpers.ConsolePrompt($"Uninstall Unity {uninstall.version} at '{uninstall.path}'?", "yN");
            if (response == 'N') {
                Environment.Exit(1);
            }
        }

        await installer.Platform.Uninstall(uninstall);
        Console.WriteLine($"Uninstalled Unity {uninstall.version} at path: {uninstall.path}");
    }

    // -------- Run --------

    public async Task Run()
    {
        var version = await Setup(avoidCacheUpate: true);

        var installs = await installer.Platform.FindInstallations();
        if (!installs.Any()) {
            throw new Exception("Could not find any installed versions of Unity");
        }

        Installation installation = null;
        string projectPath = null;
        if (uriArgument != null) {
            // Argument is path to project
            version = default;

            projectPath = uriArgument.ToString();
            if (!Directory.Exists(projectPath)) {
                throw new Exception($"Project path '{projectPath}' does not exist.");
            }

            var versionPath = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
            if (!File.Exists(versionPath)) {
                throw new Exception($"ProjectVersion.txt not found at expected path: {versionPath}");
            }

            // Use full path, Unity doesn't properly recognize short relative paths
            // (as of Unity 2019.3)
            projectPath = Path.GetFullPath(projectPath);

            var lines = File.ReadAllLines(versionPath);
            foreach (var line in lines) {
                if (line.StartsWith("m_EditorVersion:") || line.StartsWith("m_EditorVersionWithRevision:")) {
                    var colonIndex = line.IndexOf(':');
                    var versionString = line.Substring(colonIndex + 1).Trim();
                    version = new UnityVersion(versionString);
                    if (version.hash != null) break;
                }
            }

            if (!version.IsValid) {
                throw new Exception("Could not parse version from ProjectVersion.txt: " + versionPath);
            }

            if (upgradeVersion != null && allowNewer != AllowNewer.None) {
                throw new Exception("--upgrade and --allow-newer options cannot be used together");
            }

            if (upgradeVersion != null) {
                var uprgadeToVersion = new UnityVersion(upgradeVersion);
                if (!uprgadeToVersion.IsValid) {
                    throw new Exception("Not a valid --upgrade Unity version pattern: " + upgradeVersion);
                }

                // Use version explicitly set with --upgrade
                foreach (var install in installs.OrderByDescending(i => i.version)) {
                    // Prevent downgrading project
                    if (version > install.version)
                        break;

                    if (uprgadeToVersion.FuzzyMatches(install.version)) {
                        installation = install;
                    }
                }
            } else {
                // Check if an installed version matches the allowed upgrade path
                var allowedVersion = version;
                if (allowNewer >= AllowNewer.Hash)  allowedVersion.hash = null;
                if (allowNewer >= AllowNewer.Build) allowedVersion.build = -1;
                if (allowNewer >= AllowNewer.Patch) { allowedVersion.patch = -1; allowedVersion.type = UnityVersion.Type.Undefined; }
                if (allowNewer >= AllowNewer.Minor) allowedVersion.minor = -1;
                if (allowNewer >= AllowNewer.All)   allowedVersion.major = -1;
                foreach (var install in installs.OrderByDescending(i => i.version)) {
                    // Prevent downgrading project
                    if (version > install.version)
                        break;
                    // Exact match trumps fuzzy
                    if (version.FuzzyMatches(install.version)) {
                        installation = install;
                    }
                    // Fuzzy match only newest version
                    if (installation == null && allowedVersion.FuzzyMatches(install.version, allowTypeUpgrade: false)) {
                        installation = install;
                    }
                }
            }

            var projectPathSet = false;
            for (var i = 0; i < unityArguments.Count; i++) {
                if (string.Equals(unityArguments[i], "-projectPath", StringComparison.OrdinalIgnoreCase)) {
                    if (i + 1 >= unityArguments.Count) {
                        throw new Exception("-projectPath has no argument.");
                    }
                    Logger.LogWarning($"-projectPath already set, overwriting with '{projectPath}'");
                    unityArguments[i + 1] = projectPath;
                    projectPathSet = true;
                    break;
                }
            }
            if (!projectPathSet) {
                unityArguments.Add("-projectPath");
                unityArguments.Add(projectPath);
            }

            if (allowNewer != AllowNewer.None || upgradeVersion != null) {
                unityArguments.Add("-skipUpgradeDialogs");
            }

        } else if (version.IsValid) {
            // Argument is version pattern
            foreach (var install in installs.OrderByDescending(i => i.version)) {
                if (version.FuzzyMatches(install.version)) {
                    if (installation != null) {
                        if (!version.IsFullVersion) continue;
                        throw new Exception($"Version {version} is ambiguous between\n"
                            + $"{installation.version} at '{installation.path}' and\n"
                            + $"{install.version} at '{install.path}'\n"
                            + "(use exact version).");
                    }
                    installation = install;
                }
            }

        } else {
            throw new Exception($"Neither Unity version or project path specified");
        }

        if (projectPath != null) {
            var projectName = Path.GetFileName(projectPath);
            if (installation == null) {
                Logger.LogError($"Could not run project '{projectName}', Unity {version} not installed");

                var next = installs.Where(i => i.version > version).OrderBy(i => i.version).FirstOrDefault();
                if (next == null || !next.version.IsValid) {
                    Environment.Exit(1);
                } else {
                    var allowUpgrade = false;
                    if (Console.LargestWindowWidth > 0 && !Console.IsInputRedirected && !Console.IsOutputRedirected) {
                        Console.Write($"Do you want to open the project with the next available version {next.version}? [Ny] ");
                        if (string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase)) {
                            allowUpgrade = true;
                        }
                    }

                    if (allowUpgrade) {
                        installation = next;
                        if (!unityArguments.Contains("-skipUpgradeDialogs")) {
                            unityArguments.Add("-skipUpgradeDialogs");
                        }

                    } else {
                        var allow = "all";
                        if (next.version.minor != version.minor) allow = "minor";
                        else if (next.version.patch != version.patch || next.version.type != version.type) allow = "patch";
                        else if (next.version.build != version.build) allow = "build";
                        else allow = "hash";

                        Console.WriteLine($"Use '--allow-newer {allow}' to run with the newer installed version {next.version}");
                        Environment.Exit(1);
                    }
                }
            }
            if (version != installation.version) {
                Logger.LogWarning($"Upgrading project from Unity {version.ToString(verbose > 0)} to {installation.version.ToString(verbose > 0)}");
            }
            Console.WriteLine($"Will run project '{projectName}' with Unity {installation.version.ToString(verbose > 0)} at path '{installation.path}'");
        } else {
            if (installation == null) {
                Logger.LogError($"Could not run Unity {version}: Not matching version installed");
                Environment.Exit(1);
            }
            Console.WriteLine($"Will run Unity {installation.version.ToString(verbose > 0)} at path '{installation.path}'");
        }

        for (int i = 0; i < unityArguments.Count; i++) {
            unityArguments[i] = Helpers.EscapeArgument(unityArguments[i]);
        }

        Logger.LogInformation($"Unity arguments: '{string.Join(" ", unityArguments)}'");

        await installer.Platform.Run(installation, unityArguments, child);
    }

    // -------- Create --------

    public async Task Create()
    {
        await Setup(avoidCacheUpate: true);

        var installs = await installer.Platform.FindInstallations();
        if (!installs.Any()) {
            throw new Exception("Could not find any installed versions of Unity");
        }

        // Determine Unity installation to create project with
        Installation installation = null;
        foreach (var install in installs.OrderByDescending(i => i.version)) {
            if (versionArgument.FuzzyMatches(install.version)) {
                if (installation != null) {
                    if (!versionArgument.IsFullVersion) continue;
                    throw new Exception($"Version {version} is ambiguous between\n"
                        + $"{installation.version} at '{installation.path}' and\n"
                        + $"{install.version} at '{install.path}'\n"
                        + "(use exact version).");
                }
                installation = install;
            }
        }
        if (installation == null) {
            throw new Exception($"Could not run Unity {version}: Not installed");
        }

        // Check project path
        var fullPath = Path.GetFullPath(projectPath);
        var assetsPath = Path.Combine(fullPath, "Assets");
        var settingsPath = Path.Combine(fullPath, "ProjectSettings");
        if (!Directory.Exists(fullPath)) {
            Directory.CreateDirectory(fullPath);
        } else if (Directory.Exists(assetsPath) || Directory.Exists(settingsPath)) {
            throw new Exception($"Cannot overwrite existing project at: {projectPath}");
        }

        // Setup project
        Console.WriteLine($"Creating new Unity {versionArgument.ToString(false)} project at path: {fullPath}");
        Directory.CreateDirectory(assetsPath);
        Directory.CreateDirectory(settingsPath);

        if (projectType == CreateProjectType.Minimal) {
            // Create an empty manifest to prevent setup of default packages/modules
            string packagesPath;
            if (installation.version.major >= 2018) {
                packagesPath = Path.Combine(fullPath, "Packages");
            } else {
                packagesPath = Path.Combine(fullPath, "UnityPackageManager");
            }
            if (!Directory.Exists(packagesPath)) {
                Directory.CreateDirectory(packagesPath);
            }
            var manifestPath = Path.Combine(packagesPath, "manifest.json");
            File.WriteAllText(manifestPath, @"{ ""dependencies"": { } }");
        }

        var versionPath = Path.Combine(settingsPath, "ProjectVersion.txt");
        File.WriteAllText(versionPath, $"m_EditorVersion: {installation.version.ToString(false)}");

        // Let Unity create project
        var arguments = new List<string>();
        arguments.Add("-projectPath");
        arguments.Add(Helpers.EscapeArgument(fullPath));

        if (openProject) {
            Console.WriteLine($"Opening new project");
            await installer.Platform.Run(installation, arguments, false);
        } else {
            arguments.Add("-quit");
            arguments.Add("-batchmode");
            await installer.Platform.Run(installation, arguments, true);
            Console.WriteLine($"Created new project");
        }
    }

    // -------- Console --------

    public static bool enableColors;

    public static void WriteBigTitle(string title)
    {
        var padding = (Console.BufferWidth - title.Length - 4) / 2;
        var paddingStr = new string('—', Math.Max(padding, 0));

        Console.WriteLine();

        SetColors(ConsoleColor.Black);
        Console.Write(paddingStr);

        SetColors(ConsoleColor.White, ConsoleColor.DarkGray);
        Console.Write("  " + title + "  ");

        SetColors(ConsoleColor.Black);
        Console.Write(paddingStr);

        ResetColor();
        Console.WriteLine();
        Console.WriteLine();
    }

    public static void WriteTitle(string title)
    {
        Console.WriteLine();

        SetColors(ConsoleColor.White, ConsoleColor.DarkGray);
        Console.Write(title);

        ResetColor();
        Console.WriteLine();
    }

    public static int fieldWidth = -1;

    public static void WriteField(string title, string value, bool writeEmpty = false)
    {
        if (!writeEmpty && string.IsNullOrEmpty(value)) return;

        if (fieldWidth > 0) {
            var padding = fieldWidth - title.Length;
            if (padding > 0) {
                Console.Write(new string(' ', padding));
            }
        }

        SetForeground(ConsoleColor.DarkGray);
        Console.Write(title);
        Console.Write(": ");
        ResetColor();

        Console.Write(value);
        Console.WriteLine();
    }

    public static void SetForeground(ConsoleColor color)
    {
        if (enableColors) {
            Console.ForegroundColor = color;
        }
    }

    public static void SetBackground(ConsoleColor color)
    {
        if (enableColors) {
            Console.BackgroundColor = color;
        }
    }

    public static void SetColors(ConsoleColor fg, ConsoleColor? bg = null)
    {
        if (enableColors) {
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg ?? fg;
        }
    }

    public static void ResetColor()
    {
        if (enableColors) {
            Console.ResetColor();
        }
    }
}

}
