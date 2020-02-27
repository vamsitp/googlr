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
                    "\n--------------------------------------------------------------".Green(),
                    "\nEnter your search-term",
                    "\nEnter ", "c".Green(), " to clear the console",
                    "\nEnter ", "q".Green(), " to quit",
                    "\nEnter ", "?".Green(), " to print this help"
                });
        }

        private static async Task StartAsync()
        {
            var parser = new Parser();
            var results = new List<(string link, string title, string summary)>();
            ColorConsole.WriteLine("Hey ", $"{Utils.UserName}".Green(), "!");
            PrintHelp();
            do
            {
                ColorConsole.Write("\n> ".Green());
                var key = Console.ReadLine()?.Trim();
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
                        var indexSearch = key.Replace(" ", ".");
                        if (int.TryParse(indexSearch, out var index))
                        {
                            // https://github.com/dotnet/runtime/issues/28005
                            Process.Start(new ProcessStartInfo { FileName = results[index].link, UseShellExecute = true });
                        }
                        else
                        {
                            results = await parser.GetSearchResults(key);
                            foreach (var result in results.Select((value, i) => new { index = i + 1, value }))
                            {
                                ColorConsole.WriteLine("\n", result.index.ToString().PadLeft(3).Green(), ". ", result.value.title.Black().OnWhite());
                                ColorConsole.WriteLine(string.Empty.PadLeft(5), result.value.link.Blue());
                                ColorConsole.WriteLine(string.Empty.PadLeft(5), result.value.summary);
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
    }
}
