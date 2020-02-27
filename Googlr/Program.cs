namespace Googlr
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Microsoft.Extensions.DependencyInjection;

    internal class Program
    {
        private static ServiceProvider serviceProvider;

        private static readonly char[] Separators = new[] { '?', '/', ':', '-' };

        private static Parser parser;

        internal static void Main(string[] args)
        {
            try
            {
                Utils.SetLogger();
                serviceProvider = new ServiceCollection().ConfigureAndGetServiceProvider();

                Console.OutputEncoding = System.Text.Encoding.UTF8;

                parser = new Parser();
                StartAsync(args?.FirstOrDefault()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ex.LogError().GetAwaiter().GetResult();
            }
            finally
            {
                ColorConsole.WriteLine("DONE!".White().OnDarkGreen());
                Console.ReadLine();
            }
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

        private static async Task StartAsync(string connectionString = null)
        {
            Utils.Log(messages: new[] { "Hey ", $"{Utils.UserName}".Green(), "!" });
            PrintHelp();
            do
            {
                ColorConsole.Write("\n> ".Green());
                var command = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(command))
                {
                    continue;
                }
                else if (command.Equals("q", StringComparison.OrdinalIgnoreCase) || command.StartsWith("quit", StringComparison.OrdinalIgnoreCase) || command.StartsWith("exit", StringComparison.OrdinalIgnoreCase) || command.StartsWith("close", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                else if (command.Equals("?") || command.StartsWith("help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                }
                else if (command.Equals("c", StringComparison.OrdinalIgnoreCase) || command.StartsWith("cls", StringComparison.OrdinalIgnoreCase) || command.StartsWith("clear", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Clear();
                }
                else
                {
                    var results = await parser.GetSearchResults(command);
                    foreach (var result in results)
                    {
                        ColorConsole.WriteLine(result);
                    }
                }
            }
            while (true);
        }
    }
}
