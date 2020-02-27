namespace Googlr
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    using ColoredConsole;

    internal class Program
    {
        private const int Batchsize = 10;
        private const string Space = " ";

        private static readonly char[] Separators = new[] { '/', '-' };
        private static string continuationkey = null;

        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            await StartAsync();
            ColorConsole.WriteLine("DONE!".White().OnDarkGreen());
            Console.ReadLine();
        }

        private static void PrintHelp()
        {
            ColorConsole.WriteLine(
                new[]
                {
                    "--------------------------------------------------------------".Green(),
                    "\nEnter the ", "search-phrase".Green(), " for general Search",
                    "\nEnter ", "/".Green(), " followed by the ", "search-phrase".Green(), " for News-search",
                    "\nEnter the ", "index".Green(), " to open the corresponding link",
                    "\nEnter ", "c".Green(), " to clear the console",
                    "\nEnter ", "q".Green(), " to quit",
                    "\nEnter ", "?".Green(), " to print this help"
                });
        }

        private static async Task StartAsync()
        {
            var parser = new Parser();
            await parser.Init();

            var results = new List<SearchInfo>();
            ColorConsole.WriteLine("Hey ", $"{Utils.UserName}".Green(), "!");
            PrintHelp();
            do
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
                else if (key.Equals("q", StringComparison.OrdinalIgnoreCase) || key.StartsWith("quit", StringComparison.OrdinalIgnoreCase) || key.StartsWith("exit", StringComparison.OrdinalIgnoreCase) || key.StartsWith("close", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                else if (key.Equals("?") || key.StartsWith("help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                }
                else if (key.Equals("c", StringComparison.OrdinalIgnoreCase) || key.StartsWith("cls", StringComparison.OrdinalIgnoreCase) || key.StartsWith("clear", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Clear();
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
                            PrintResults(results);
                        }
                        else
                        {
                            var indexSearch = searchTerm.Replace(Space, ".");
                            if (int.TryParse(indexSearch, out var index)) // Index
                            {
                                var paras = await parser.GetSummary(results[index - 1].Link);
                                if (paras != null)
                                {
                                    foreach (var para in paras)
                                    {
                                        ColorConsole.WriteLine("\n", string.Empty.PadLeft(5), para);
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
                                PrintResults(results);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await ex.LogError();
                    }
                }
            }
            while (true);
        }

        private static void PrintResults(List<SearchInfo> results)
        {
            // Credit: https://stackoverflow.com/a/51197184
            var index = 0;
            if (results.Count > Batchsize)
            {
                for (int i = 0; i < Math.Ceiling((decimal)results.Count / Batchsize); i++)
                {
                    var batch = results.Skip(i * Batchsize).Take(Batchsize);
                    index = Iterate(index, batch);
                    Console.WriteLine("\nContinue...");

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
            }
        }

        private static int Iterate(int index, IEnumerable<SearchInfo> batch)
        {
            foreach (var result in batch)
            {
                index++;
                ColorConsole.WriteLine("\n", index.ToString().PadLeft(3).Green(), ". ", result.Title.Black().OnWhite());
                ColorConsole.WriteLine(string.Empty.PadLeft(5), result.Link.Blue());
                ColorConsole.WriteLine(string.Empty.PadLeft(5), result.Time.White().OnGreen(), string.IsNullOrWhiteSpace(result.Time) ? string.Empty : Space, result.Summary);
            }

            return index;
        }
    }
}
