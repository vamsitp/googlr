namespace Googlr
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Flurl.Http;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Newtonsoft.Json.Linq;

    using Serilog;
    using Serilog.Core;
    using Serilog.Events;

    public static class Utils
    {
        private const string Space = " ";
        public const string TimestampTemplate = "{Timestamp:dd-MMM-yyyy HH:mm:ss} | ";
        public const string OutputTemplate = "[{Level:u3}] {Message}{NewLine}{Exception}";//// | {MachineName} | {EnvironmentUserName}
        private static readonly string CallingPath = Path.GetDirectoryName(new Uri(Assembly.GetCallingAssembly().CodeBase).LocalPath);
        private static HttpClient httpClient = new HttpClient();

        private static LoggingLevelSwitch logLevelSwitch;

        public static LogEventLevel LoggerLevel
        {
            get
            {
                return logLevelSwitch.MinimumLevel;
            }

            set
            {
                logLevelSwitch.MinimumLevel = value;
            }
        }

        public static string UserName
        {
            get
            {
                var userName = Environment.UserName;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    userName = Environment.GetEnvironmentVariable("USERNAME");
                }

                return userName;
            }
        }

        private static IConfigurationRoot Configuration { get; set; }

        public static async Task<string> ToFullStringAsync(this Exception e, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            if (e != null)
            {
                var message = e.Message;
                var fex = e as FlurlHttpException;
                if (fex != null)
                {
                    try
                    {
                        var vex = await fex.Call?.Response?.GetJsonAsync<JObject>();
                        message = vex?.SelectToken("..message")?.Value<string>() ?? vex?.SelectToken("..Message")?.Value<string>() ?? e.Message;
                    }
                    catch
                    {
                        message = fex.Message;
                    }
                }

                var lines = string.Join(" > ", new StackTrace(e, true)?.GetFrames()?.Where(x => x.GetFileLineNumber() > 0)?.Select(x => $"{x.GetMethod()}#{x.GetFileLineNumber()}"));
                return $"{message} ({(string.IsNullOrWhiteSpace(lines) ? member + "#" + line : member + "#" + line + " - " + lines)})";
            }

            return string.Empty;
        }

        public static async Task LogError(this Exception ex, string prefix = null)
        {
            var err = await ex.ToFullStringAsync();
            var msg = prefix == null ? err : $"{prefix}: {err}";
            Serilog.Log.Error(msg);
            ColorConsole.WriteLine(msg.White().OnRed());
        }

        public static void Log(bool newLine = true, params ColorToken[] messages)
        {
            var message = string.Join("", messages);
            if (newLine)
            {
                Serilog.Log.Information(message);
                ColorConsole.WriteLine(messages);
            }
            else
            {
                Serilog.Log.Information(message);
                ColorConsole.Write(messages);
            }
        }

        public static bool ContainsPhrase(this string text, string phrase)
        {
            var result = phrase?.Split(new[] { Space }, StringSplitOptions.RemoveEmptyEntries).All(x => text.Split(new[] { Space }, StringSplitOptions.RemoveEmptyEntries).Contains(x, StringComparer.OrdinalIgnoreCase)) == true;
            return result;
        }

        public static string Sanitize(this string value, string prefix)
        {
            return value?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true ? value : (prefix + (value?.TrimStart('#') ?? string.Empty));
        }

        public static async Task<bool> UrlExists(this string url)
        {
            try
            {
                using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string LastUrlSegment(this string url)
        {
            return url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault(x => !x.StartsWith("?"));
        }

        public static string ToFullPath(this string file)
        {
            try
            {
                var value = Path.IsPathRooted(file) ? file : Path.Combine(CallingPath, file);
                return value;
            }
            catch
            {
                // Do nothing
            }

            return file;
        }

        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body)
        {
            List<Exception> exceptions = null;
            foreach (var item in source)
            {
                try
                {
                    await body(item);
                }
                catch (Exception exc)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(exc);
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

        public static void ThrowIfNull(this object arg, string argName, [CallerMemberName] string member = "")
        {
            if (arg == null)
            {
                throw new ArgumentNullException($"{member}: {argName}");
            }
        }

        public static void SetLogger()
        {
            logLevelSwitch = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Debug };
            var logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(logLevelSwitch)
            .WriteTo.Async(l => l.Debug(outputTemplate: TimestampTemplate + OutputTemplate))
            // .WriteTo.Async(l => l.Console(outputTemplate: OutputTemplate, theme: AnsiConsoleTheme.Literate))
            // .WriteTo.Async(l => l.ApplicationInsights(outputTemplate: OutputTemplate))
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.FromLogContext()
            .CreateLogger();
            Serilog.Log.Logger = logger;
        }

        public static ServiceProvider ConfigureAndGetServiceProvider(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .ConfigureServices()
                .BuildServiceProvider();
        }

        public static IServiceCollection ConfigureServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            return serviceCollection;
        }
    }
}
