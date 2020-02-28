namespace Googlr
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Flurl.Http;

    using Newtonsoft.Json.Linq;

    public static class Utils
    {
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
    }
}
