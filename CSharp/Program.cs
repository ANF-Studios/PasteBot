using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace PasteBot
{
    public class Program
    {
        private DiscordSocketClient client;
        private CommandService command;
        private IServiceProvider service;
        private const string LoginToken = "Token"; //Make sure to replace the token with your bot's token
        //You can get it from https://discord.com/developers/

        public static void Main()
        => new Program().StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            command = new CommandService();
            client = new DiscordSocketClient();

            service = new ServiceCollection()
            .AddSingleton(client)
            .AddSingleton(command)
            .BuildServiceProvider();

            client.Log += LogMessage;
            await RegisterCommandsAsync();
            await client.LoginAsync(TokenType.Bot, LoginToken);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        public async Task RegisterCommandsAsync()
        {
            client.MessageReceived += HandleCommandAsync;
            await command.AddModulesAsync(Assembly.GetEntryAssembly(), service);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            SocketUserMessage message = arg as SocketUserMessage;
            var context = new SocketCommandContext(client, message);
            if (message.Author.IsBot || message.Author.IsWebhook || message == null) return;

            int argPos = 0;
            if (message.HasStringPrefix("pb ", ref argPos))
            {
                var result = await command.ExecuteAsync(context, argPos, service);
                if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
                if (result.Error.Equals(CommandError.UnmetPrecondition)) await message.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private Task LogMessage(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        [Serializable]
        public struct PasteMystCreateInfo
        {
            [JsonPropertyName("code")]
            public string Code { get; set; }
            [JsonPropertyName("expiresIn")]
            public string ExpiresIn { get; set; }
        }

        [Serializable]
        public struct PasteMystResultInfo
        {
            [JsonPropertyName("id")]
            public string ID { get; set; }
            [JsonPropertyName("createdAt")]
            public long CreatedAt { get; set; }
            [JsonPropertyName("expiresAt")]
            public string ExpiresAt { get; set; }
            [JsonPropertyName("code")]
            public string Code { get; set; }
        }

        private const string API_BASEPOINT = "https://paste.myst.rs/api/";
        private const string PASTEMYST_BASE_URL = "https://paste.myst.rs/";

        private static readonly Regex _codeblockRegex = new Regex(@"^(?:\`){1,3}(\w+?(?:\n))?([^\`]*)\n?(?:\`){1,3}$", RegexOptions.Singleline);

        private async Task CheckMessage(SocketMessage sm)
        {
            if (!(sm is SocketUserMessage msg) || msg.Author.IsBot)
                return;

            int msgWrappedLineCount = msg.Content
                .Split(new[] { '\n', '\r' })
                .Select(l => (int)Math.Ceiling(l.Length / 47f)) // This could be turned into parameter, currently it's amount of characters that fit on mobile with 100% scale
                .Sum();

            if (HasCodeblockFormat(msg.Content))
            {
                string url = await PasteMessage(msg);
                await msg.DeleteAsync();

                var SuccessfullyPastes = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithAuthor(msg.Author.Username, msg.Author.GetAvatarUrl())
                .WithDescription($"Massive code pasted. View it [here]({url})")
                .WithFooter("Powered by PasteMyst")
                .Build();

                await msg.Channel.SendMessageAsync(embed: SuccessfullyPastes);
            }
        }

        public async Task<string> PasteMessage(IMessage msg)
        {
            string code = msg.Content;
            if (HasCodeblockFormat(code))
            {
                code = ExtractCodeblockContent(code);
            }

            PasteMystCreateInfo createInfo = new PasteMystCreateInfo
            {
                Code = HttpUtility.UrlPathEncode(code),
                ExpiresIn = "never"
            };

            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(API_BASEPOINT);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Connection.Add(HttpMethod.Post.ToString().ToUpper());

            StringContent content = new StringContent(JsonSerializer.Serialize(createInfo), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync("paste", content);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            PasteMystResultInfo result = JsonSerializer.Deserialize<PasteMystResultInfo>(json);
            Uri pasteUri = new Uri(new Uri(PASTEMYST_BASE_URL), result.ID);
            return pasteUri.ToString();
        }

        private static bool HasCodeblockFormat(string content)
            => _codeblockRegex.IsMatch(content);
        private static string ExtractCodeblockContent(string content)
            => ExtractCodeblockContent(content, out string _);
        private static string ExtractCodeblockContent(string content, out string format)
        {
            Match m = _codeblockRegex.Match(content);
            if (m.Success)
            {
                // If 2 capture is present, the message only contains content, no format
                if (m.Groups.Count == 2)
                {
                    format = string.Empty;
                    return m.Groups[1].Value;
                }
                // If 3 captures are present, the message contains content and a format
                if (m.Groups.Count == 3)
                {
                    format = m.Groups[1].Value;
                    return m.Groups[2].Value;
                }
            }
            format = null;
            return null;
        }
    }
}
