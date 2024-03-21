using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace neptunebot
{
    internal class Program
    {
        public static DiscordShardedClient client;
        public static DiscordGuild? MainGuild;
        public static dynamic config;

        static Program()
        {
            //Load config
            Console.WriteLine("Loading config...");
            dynamic? x = JsonConvert.DeserializeObject(File.ReadAllText("config.json"));
            if (x != null)
            {
                config = x;
                if ((string)config.token == null)
                {
                    SetupConfigJsonMessage("token in config.json");
                }
                else
                {
                    client = new DiscordShardedClient(new DiscordConfiguration()
                    {
                        MinimumLogLevel = LogLevel.Debug,
                        Token = (string)config.token,
                        TokenType = TokenType.Bot,
                        Intents = DiscordIntents.All,
                        ShardCount = 1
                    });
                }

                if ((JArray)config.wordblacklist == null)
                {
                    SetupConfigJsonMessage("wordblacklist array in config.json");
                }
                if ((string)config.IconURL == null)
                {
                    SetupConfigJsonMessage("IconURL in config.json");
                }
                SlashCommands.IconURL = (string)config.IconURL;
            }
            else
            {
                SetupConfigJsonMessage("config");
            }
        }
        static void SetupConfigJsonMessage(string x)
        {
            Console.WriteLine(x + " is null! please setup config.json like this:");
            Console.WriteLine("{");
            Console.WriteLine("\"token\": \"a\"");
            Console.WriteLine("\"IconURL\": \"https://cdn.discordapp.com/icons/843480835571318834/d722cc717bc3e11d1d3a66d3ca118efd.png\",");
            Console.WriteLine("\"wordblacklist\": [\"nepfag\"]");
            Console.WriteLine("}");
            throw new Exception("Please setup config.json");
        }
        static void Main() => MainAsync().GetAwaiter().GetResult();
        static async Task MainAsync()
        {
            client.Ready += Client_Ready;
            client.MessageCreated += Client_MessageCreated;

            var slash = await client.UseSlashCommandsAsync();

            client.ComponentInteractionCreated += async (s, e) =>
            {
                if (e.Interaction.Data.CustomId == "Slash Commands Hanlder")
                {
                    var m = await e.Guild.GetMemberAsync(e.Interaction.User.Id);
                    if (m != null) ;


                }
            };

            //To register them for a single server, recommended for testing
            // slash. += Slash_SlashCommandErrored;
            slash.RegisterCommands<SlashCommands>(843480835571318834);
            //slash.RegisterCommands<SlashCommands>();

            await client.StartAsync();

            await Task.Delay(-1);
        }

        private static async Task Slash_SlashCommandErrored(SlashCommandsExtension sender, DSharpPlus.SlashCommands.EventArgs.SlashCommandErrorEventArgs e)
        {
            Console.WriteLine("ERROR: " + e.Exception);
            await Task.Delay(1);
        }

        private static async Task Client_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Channel.GuildId == 843480835571318834)
            {
                var s = e.Message.Content;
                if (!e.Author.IsBot && e.Guild.OwnerId != e.Message.Author.Id)
                {
                    bool isBadWord = false;
                    foreach (var tok in (JArray)config.wordblacklist)
                    {
                        if (tok != null)
                        {
                            string? word = (string?)tok;
                            if (word != null)
                            {
                                if (s.Contains(word) | s.ToLower().Contains(word))
                                {
                                    isBadWord = true;
                                }
                            }
                        }

                    }
                    if (isBadWord)
                    {
                        var m = await e.Guild.GetMemberAsync(e.Author.Id);
                        await e.Guild.BanMemberAsync(m, 0, "Automatic ban - saying banned word");

                        await e.Channel.SendMessageAsync(new DiscordEmbedBuilder().WithTitle("Auto Moderation System").WithDescription("User: " + m.DisplayName + " with ID " + m.Id + " has been banned due to moderation policies in effect.").WithColor(DiscordColor.Red));

                    }
                    if (e.MentionedUsers.Count >= 12) //How many mentions before the user gets banned?
                    {
                        var m = await e.Guild.GetMemberAsync(e.Author.Id);
                        try
                        {
                            await e.Guild.BanMemberAsync(m, 0, "Automatic ban - mass mention");
                            await e.Channel.SendMessageAsync(new DiscordEmbedBuilder().WithTitle("Auto Moderation System").WithDescription("User: " + m.DisplayName + " with ID " + m.Id + " has been banned due to moderation policies in effect. (mentioned too many users)").WithColor(DiscordColor.Red));
                        }
                        catch
                        {
                            await e.Channel.SendMessageAsync(new DiscordEmbedBuilder().WithTitle("Auto Moderation System").WithDescription("User: " + m.DisplayName + " with ID " + m.Id + " has been banned due to moderation policies in effect. (mentioned too many users). **Ban failed.**").WithColor(DiscordColor.Red));
                        }
                    }


                    else
                    {
                        if (e.Channel.Id != 951975949950390322) //bot commands
                        {
                            var m = await e.Guild.GetMemberAsync(e.Author.Id);

                            //leveling
                            var newLevel = GenLevel();

                            int i = 0;
                            bool found = false;
                            foreach (var item in DataStorage.DB.Member)
                            {
                                if (item.ID == e.Message.Author.Id)
                                {
                                    found = true;
                                    break;
                                }

                                i++;
                            }

                            if (!found)
                            {
                                DataStorage.DB.Member.Add(new DataStorageServerMember() { Level = 0, ID = e.Message.Author.Id });
                                i = DataStorage.DB.Member.Count - 1;
                            }

                            bool cooldown;
                            if (DataStorage.DB.Member[i].LastMessage == DateTime.MinValue)
                            {
                                cooldown = false;
                            }
                            else
                            {
                                if (DataStorage.DB.Member[i].LastMessage.Minute == DateTime.Now.Minute)
                                {
                                    cooldown = true;
                                }
                                else
                                {
                                    cooldown = false;
                                }
                            }

                            if (!cooldown)
                            {
                                DataStorage.DB.Member[i].Level = newLevel + DataStorage.DB.Member[i].Level;
                                DataStorage.DB.Member[i].LastMessage = DateTime.Now;
                                DataStorage.Save();

                                // assign active role if points is greater than 250
                                if (DataStorage.DB.Member[i].Level > 250)
                                {
                                    await m.GrantRoleAsync(m.Guild.GetRole(1184116868680790016));
                                }
                            }
                        }
                    }
                }
            }
        }

        private static ulong GenLevel()
        {
            Random random = new();
            return (ulong)random.Next(10, 20);
        }

        private static async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            try
            {
                await client.UpdateStatusAsync(new DiscordActivity("Watching n3ptune's nation"), UserStatus.Online);
                MainGuild = await client.ShardClients[0].GetGuildAsync(843480835571318834); //change this in production
                if (MainGuild == null)
                {
                    throw new Exception("Unable to find Main guild");
                }
            }
            catch { }
        }
    }
}