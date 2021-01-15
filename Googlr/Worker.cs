namespace Googlr
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using NuGet.Common;
    using NuGet.Protocol;
    using NuGet.Protocol.Core.Types;

    public class Worker : BackgroundService
    {
        private const int Batchsize = 10;

        private const int RightPad = 10;
        private const string Name = nameof(Googlr);
        private static readonly char[] Separators = new[] { '/', '-' };

        private readonly ILogger<Worker> logger;

        private IConfigurationSection appSettings = null;

        private IConfigurationRoot config = null;

        private string continuationkey = null;

        private Speaker speaker;

        private int MaxWidth => Console.WindowWidth - RightPad;

        public Worker(ILogger<Worker> logger, IConfiguration config, Speaker speaker)
        {
            this.logger = logger;
            this.config = config as IConfigurationRoot;
            this.appSettings = config?.GetSection("appSettings");
            this.speaker = speaker;
        }

        private Version Version => Assembly.GetEntryAssembly().GetName().Version;

        protected override async Task ExecuteAsync(CancellationToken stopToken)
        {
            // logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await ProcessAsync(stopToken);
        }

        private async Task ProcessAsync(CancellationToken stopToken)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var parser = new Parser();
            await parser.Init();

            var results = new List<SearchInfo>();
            ColorConsole.WriteLine("https://vamsitp.github.io/googlr".DarkGray(),
                "\n--------------------------------------------------------------".Green(),
                "\nHey ", $"{Utils.UserName}".Green(), "!");
            PrintHelp();

            while (!stopToken.IsCancellationRequested)
            {
                // If key was typed during Continue...
                var key = continuationkey;
                continuationkey = null;

                if (string.IsNullOrWhiteSpace(key))
                {
                    ColorConsole.Write("\n> ".Green());
                    key = Console.ReadLine()?.Trim();
                }

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }
                //else if (key.Equals("s", StringComparison.OrdinalIgnoreCase) || key.Equals("u", StringComparison.OrdinalIgnoreCase))
                //{
                //    ColorConsole.Write("> ".Green(), "Hit ", "Enter".Green(), " after closing ", "appSettings".Green());
                //    Process.Start(new ProcessStartInfo { FileName = Path.Combine(AppContext.BaseDirectory, "appSettings.json"), UseShellExecute = true });
                //    config.Reload();
                //    speaker = new Speaker(appSettings);
                //}
                else if (key.Equals("q", StringComparison.OrdinalIgnoreCase) || key.Equals("quit", StringComparison.OrdinalIgnoreCase) || key.Equals("exit", StringComparison.OrdinalIgnoreCase) || key.Equals("close", StringComparison.OrdinalIgnoreCase))
                {
                    ColorConsole.WriteLine("DONE!".White().OnDarkGreen());
                    break;
                }
                else if (key.Equals("?") || key.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                }
                else if (key.Equals("c", StringComparison.OrdinalIgnoreCase) || key.Equals("cls", StringComparison.OrdinalIgnoreCase) || key.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Clear();
                }
                else if (key.Equals("+") || key.Equals("update", StringComparison.OrdinalIgnoreCase))
                {
                    await this.UpdateTool(stopToken);
                }
                else
                {
                    try
                    {
                        var split = key.Split(Separators, 2);
                        var searchTerm = split.LastOrDefault()?.Trim();
                        if (split.Length > 1) // News
                        {
                            results = await parser.GetNewsResults(searchTerm);
                            await PrintResults(results, stopToken);
                        }
                        else
                        {
                            var indexSearch = searchTerm.Replace(Utils.Space, ".");
                            if (int.TryParse(indexSearch, out var index)) // Index
                            {
                                var paras = await parser.GetSummary(results[index - 1].Link);
                                if (paras != null)
                                {
                                    ColorConsole.WriteLine("\n", index.ToString().PadLeft(3).Green(), ". ", $" {results[index - 1].Title} ".Black().OnWhite());
                                    foreach (var para in paras)
                                    {
                                        if (para.StartsWith(Utils.ErrorPrefix))
                                        {
                                            ColorConsole.WriteLine("\n", string.Empty.PadLeft(5), $" {para.Replace(Utils.ErrorPrefix, string.Empty)} ".White().OnRed());
                                        }
                                        else
                                        {
                                            ColorConsole.WriteLine("\n", para.ToWrappedText(MaxWidth));
                                        }
                                    }

                                    ColorConsole.Write("\n", string.Empty.PadLeft(5), "Press ".Green(), "o", " to open the link".Green(), ": ");
                                    if (Console.ReadKey().Key == ConsoleKey.O)
                                    {
                                        Process.Start(new ProcessStartInfo { FileName = results[index - 1].Link, UseShellExecute = true });
                                    }
                                }
                                else
                                {
                                    // https://github.com/dotnet/runtime/issues/28005
                                    Process.Start(new ProcessStartInfo { FileName = results[index - 1].Link, UseShellExecute = true });
                                }
                            }
                            else // Search
                            {
                                results = await parser.GetSearchResults(searchTerm.Trim(Separators));
                                await PrintResults(results, stopToken);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await ex.LogError();
                    }
                }
            }
        }

        private int Iterate(int index, IEnumerable<SearchInfo> batch)
        {
            foreach (var result in batch)
            {
                index++;
                ColorConsole.WriteLine("\n", index.ToString().PadLeft(3).Green(), ". ", $" {result.Title} ".Black().OnWhite());
                ColorConsole.WriteLine(string.Empty.PadLeft(5), result.Link.Split('?').FirstOrDefault().Green());

                var pad = string.IsNullOrWhiteSpace(result.Time) ? string.Empty : Utils.Space;
                ColorConsole.WriteLine(string.Empty.PadLeft(5), $"{pad}{result.Time}{pad}".White().OnDarkGreen(), pad, result.Summary.ToWrappedText(MaxWidth).Trim());
                // await Speak(result);
            }

            return index;
        }

        private static void PrintHelp()
        {
            ColorConsole.WriteLine(
                new[]
                {
                    "--------------------------------------------------------------".Green(),
                    "\nEnter the ", "search-phrase".Green(), " for general Search", " (", "e.g. \"".Green(), "cosmos db", "\"".Green(), " site:".Green(), "stackoverflow.com)",
                    "\nEnter ", "/".Green(), " followed by the ", "search-phrase".Green(), " for News-search", " (", "e.g. ".Green(), "/".Green(), "microsoft azure)",
                    "\nEnter the ", "index".Green(), " to open the corresponding link",
                    "\nEnter ", "c".Green(), " to clear the console",
                    "\nEnter ", "q".Green(), " to quit",
                    "\nEnter ", "+".Green(), " to update Googlr to latest version",
                    "\nEnter ", "?".Green(), " to print this help"
                });
        }

        private async Task<bool> UpdateTool(CancellationToken cancellationToken)
        {
            var check = await this.CheckForUpdates(cancellationToken);
            if (check)
            {
                Parallel.ForEach(
                    new[]
                    {
                        (cmd: "cmd", args: $"/c \"dotnet tool update -g --no-cache --ignore-failed-sources {Name.ToLowerInvariant()}\" & {Name.ToLowerInvariant()}", hide: false),
                        (cmd: "taskkill", args: $"/im {Name.ToLowerInvariant()}.exe /f", hide: true),
                    },
                    task => Process.Start(new ProcessStartInfo { FileName = task.cmd, Arguments = task.args, CreateNoWindow = task.hide, UseShellExecute = !task.hide }));
            }

            return check;
        }

        private async Task<bool> CheckForUpdates(CancellationToken cancellationToken)
        {
            ColorConsole.WriteLine("\nChecking for updates", "...".Cyan());
            var update = false;
            var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            var version = (await resource.GetAllVersionsAsync(Name, new SourceCacheContext(), NullLogger.Instance, cancellationToken)).OrderByDescending(v => v.Version).FirstOrDefault();
            if (version.Version > this.Version)
            {
                ColorConsole.WriteLine($"Update available: ", $"{version}".Cyan());
                update = true;
            }
            else
            {
                ColorConsole.WriteLine($"You are on the latest version: ", $"{version}".Cyan());
            }

            ColorConsole.WriteLine("-------------------------------------------------------".Cyan());
            return update;
        }

        private async Task PrintResults(List<SearchInfo> results, CancellationToken stopToken)
        {
            // Credit: https://stackoverflow.com/a/51197184
            var index = 0;
            if (results.Count > Batchsize)
            {
                for (int i = 0; i < Math.Ceiling((decimal)results.Count / Batchsize); i++)
                {
                    var batch = results.Skip(i * Batchsize).Take(Batchsize);
                    index = Iterate(index, batch);
                    await speaker.Speak(batch, stopToken);
                    ColorConsole.WriteLine("\n cont", "...".Green());

                    var input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        continuationkey = input;
                        break;
                    }
                }
            }
            else
            {
                Iterate(index, results);
                await speaker.Speak(results, stopToken);
            }
        }
    }
}
