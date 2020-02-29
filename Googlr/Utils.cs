namespace Googlr
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Flurl.Http;

    using Microsoft.Extensions.Configuration;

    using Newtonsoft.Json.Linq;

    public static class Utils
    {
        public const string Space = " ";
        public const string ErrorPrefix = "err:";

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
                        var vex = await fex.GetResponseJsonAsync<JObject>();
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
            ColorConsole.WriteLine(msg.White().OnRed());
        }

        public static IConfigurationRoot GetConfiguration()
        {
            string env = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var isDev = string.IsNullOrEmpty(env) || env.Equals("development", StringComparison.OrdinalIgnoreCase);
            var builder = new ConfigurationBuilder();
            if (isDev)
            {
                builder.AddUserSecrets<Program>();
            }

            return builder.Build();
        }

        public static string Encode(this string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(bytes);
        }

        public static string Decode(this string base64Text)
        {
            var bytes = System.Convert.FromBase64String(base64Text);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        // Credit: https://stackoverflow.com/a/29689349
        public static IEnumerable<string> ToWrappedLines(this string text, int maxWidth = 150, int leftPad = 5)
        {
            var words = text.Split(' ');
            var lines = words.Skip(1).Aggregate(words.Take(1).ToList(), (l, w) =>
            {
                if (l.Last().Length + w.Length >= maxWidth)
                {
                    l.Add(w);
                }
                else
                {
                    l[l.Count - 1] += Space + w;
                }

                return l;
            });

            return lines.Select(l => string.Empty.PadLeft(leftPad) + l);
        }

        public static string ToWrappedText(this string text, int maxWidth = 150, int leftPad = 5, char wrapDelimiter = '\n')
        {
            return string.Join(wrapDelimiter, text.ToWrappedLines(maxWidth, leftPad));
        }
    }
}
