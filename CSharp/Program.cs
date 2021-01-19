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
using PasteMystNet;
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
            if (message.ToString().HasCodeblockFormat())
                PasteMyst.PasteCodeblock(message.ToString().ExtractCodeblockContent());

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
    }

    static class PasteMyst
    {
        public static void PasteCodeblock(string discordMessage)
        {
            var paste = new PasteMystPasteForm
            {
                ExpireDuration = PasteMystExpiration.OneYear,
                Pasties = new[]
                {
                    new PasteMystPastyForm
                    {
                        Language = "Autodetect",
                        Code = ExtractCodeblockContent(discordMessage)
                    }
                }
            };
            paste.PostPasteAsync();
        }
        private static readonly Regex codeblockRegex = new Regex(
            @"^(?:\`){1,3}(\w+?(?:\n))?([^\`]*)\n?(?:\`){1,3}$",
            RegexOptions.Singleline);
        public static bool HasCodeblockFormat(this string content)
            => codeblockRegex.IsMatch(content);
        public static string ExtractCodeblockContent(this string content)
            => ExtractCodeblockContent(content, out string _);
        private static string ExtractCodeblockContent(string content, out string format)
        {
            Match m = codeblockRegex.Match(content);
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
