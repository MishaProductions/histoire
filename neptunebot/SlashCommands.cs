using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Net;

namespace neptunebot
{
    public class SlashCommands : ApplicationCommandModule
    {
        private static readonly DiscordColor AccentColor = new DiscordColor(168, 20, 191);
        public static string IconURL = "https://cdn.discordapp.com/icons/843480835571318834/d722cc717bc3e11d1d3a66d3ca118efd.png";
        #region Commands
        #region Levels
        [SlashCommand("top", "Shows the users with most amount of levels")]
        public async Task TopCommand(InteractionContext ctx)
        {
            try
            {
                DataStorageServerMember[] sorted = DataStorage.DB.Member.OrderBy(c => c.Level).Reverse().ToArray();
                var embed = new DiscordEmbedBuilder();

                string outt = "```";
                for (int i2 = 0; i2 < 10; i2++)
                {

                    if (i2 >= sorted.Length)
                    {
                        break;
                    }
                    var u2 = sorted[i2];
                    try
                    {
                        var u = await Program.client.ShardClients[0].GetUserAsync(u2.ID);

                        if (u != null)
                        {
                            outt += $"{i2 + 1}. {u.Username} {u2.Level}\n";
                        }

                    }
                    catch { }

                }
                outt += "```";

                bool found = false;
                int i = 0;
                foreach (var item in DataStorage.DB.Member)
                {
                    if (item.ID == ctx.User.Id)
                    {
                        found = true;
                        break;
                    }

                    i++;
                }

                if (!found)
                {
                    DataStorage.DB.Member.Add(new DataStorageServerMember() { Level = 0, ID = ctx.User.Id });
                    i = DataStorage.DB.Member.Count - 1;
                }
                embed.AddField("Your XP", DataStorage.DB.Member[i].Level.ToString());
                embed.AddField("Top 10 members", outt);
                embed.WithColor(DiscordColor.Purple);
                embed.WithTitle("Top members");

                await ctx.CreateResponseAsync(embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion

        #region Moderation

        [SlashCommand("ban", "Bans a user")]  //Banning users
        public async Task BanUser(InteractionContext userToRunCommand, [Option("user", "User to ban")] DiscordUser userToBan, [Option("reason", "Reason why to ban this user")] string reason, [Option("days", "Amount of days of messages to purge")] long messageDays = 0)
        {
            var result = new DiscordInteractionResponseBuilder();

            if (!await HasPerm(userToRunCommand, Permissions.BanMembers))
            {
                result.AddEmbed(CreateEmbed("Access denied.", "You need the ban permission to use this command.", DiscordColor.Red));
                await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
                return;
            }
            if (Program.MainGuild == null)
                throw new Exception("main guild cannot be null");
            DiscordMember? memberToBan = null;
            bool exists = true;
            try
            {
                memberToBan = await Program.MainGuild.GetMemberAsync(userToBan.Id, true);
            }
            catch
            {
                exists = false;
            }

            if (memberToBan == null)
            {
                exists = false;
            }

            if (!exists)
            {
                var usr = await Program.client.ShardClients[0].GetUserAsync(userToBan.Id, true);
                await Program.MainGuild.BanMemberAsync(userToBan.Id, (int)messageDays, reason);

                //Return result to the command
                result.AddEmbed(CreateEmbed($"Banned {usr.Username}",
                    "Banned for: " + reason + " DM failed as user may have DMs turned off or they aren't in the server.",
                    AccentColor));
                await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
                return;
            }
            if (memberToBan == null)
            {
                //impossible but vs thinks it is (of course it does this - n3ptune)
                return;
            }

            var DMEmbed = CreateEmbed($"You have been banned in {memberToBan.Guild.Name}", "You were banned for: " + reason, DiscordColor.Red);

            bool canDM = true;
            try
            {
                var dmChannel = await memberToBan.CreateDmChannelAsync();
                if (dmChannel != null)
                {
                    await dmChannel.SendMessageAsync(DMEmbed);
                }
                else
                {
                    canDM = false;
                }
            }
            catch
            {
                canDM = false;
            }

            //Ban the user
            try
            {
                await memberToBan.BanAsync((int)messageDays, reason);
            }
            catch (Exception er)
            {
                result.AddEmbed(CreateEmbed($"Error while banning {memberToBan.Username}#{memberToBan.Discriminator}", $"```{er.Message}```", DiscordColor.Red));
                await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
                return;
            }

            //Return result to the command
            result.AddEmbed(CreateEmbed($"Banned {memberToBan.Username}",
                "Banned for: " + reason + (canDM ? "" : "." + "**DM failed**, as user may have DMs turned off or they aren't in the server."),
                AccentColor));
            await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
        }

        [SlashCommand("unban", "Unbans a user.")] //Unbanning command, as I'm learning c# at the time, this may look different to other commands.
        public async Task UnbanCommand(InteractionContext ctx, [Option("user", "The user to unban.")] DiscordUser user)

        {
            var banList = await ctx.Guild.GetBansAsync();
            var bannedUser = banList.FirstOrDefault(x => x.User.Id == user.Id);

            if (bannedUser == null)
            {

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("This user is not banned."));
                return;
            }

            await ctx.Guild.UnbanMemberAsync(user.Id, "Unbanned by command."); //Unbans the member

            var embed = new DiscordEmbedBuilder()
               .WithTitle("User Unbanned")
               .WithDescription($"{user.Username} has been unbanned.")
               .WithAuthor("n3ptune's nation", null, IconURL)
               .WithColor(AccentColor);

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }



        [SlashCommand("kick", "Kicks a user")] //Kicking command
        public async Task KickUser(InteractionContext userToRunCommand, [Option("user", "User to kick")] DiscordUser userToKick, [Option("reason", "Reason why to kick this user")] string reason)
        {
            var result = new DiscordInteractionResponseBuilder();

            if (!await HasPerm(userToRunCommand, Permissions.KickMembers))
            {
                result.AddEmbed(CreateEmbed("Access denied.", "You need the kick permission to use this command.", DiscordColor.Red));
                await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
                return;
            }
            if (Program.MainGuild == null)
                throw new Exception("main guild cannot be null");
            DiscordMember? memberToKick = null;
            bool exists = true;
            try
            {
                memberToKick = await Program.MainGuild.GetMemberAsync(userToKick.Id, true);
            }
            catch
            {
                exists = false;
            }

            if (memberToKick == null)
            {
                exists = false;
            }

            if (!exists)
            {
                var usr = await Program.client.ShardClients[0].GetUserAsync(userToKick.Id, true);

                //Return result to the command
                result.AddEmbed(CreateEmbed($"Kicked {usr.Username}#{usr.Discriminator}",
                    "Kicked for: " + reason + ". DM failed as user may have DMs turned off or they aren't in the server.",
                    AccentColor));
                await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
                return;
            }
            if (memberToKick == null)
            {

                return;
            }

            var DMEmbed = CreateEmbed($"You have been kicked from {memberToKick.Guild.Name}", "You were kicked for: " + reason, DiscordColor.Red);

            bool canDM = true;
            try
            {
                var dmChannel = await memberToKick.CreateDmChannelAsync();
                if (dmChannel != null)
                {
                    await dmChannel.SendMessageAsync(DMEmbed);
                }
                else
                {
                    canDM = false;
                }
            }
            catch
            {
                canDM = false;
            }

            //kick the user
            try
            {
                await memberToKick.RemoveAsync();
            }

            catch (Exception er)
            {
                result.AddEmbed(CreateEmbed($"Error while kicking {memberToKick.Username}", $"```{er.Message}```", DiscordColor.Red));
                await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
                return;
            }

            //Return result to the command
            result.AddEmbed(CreateEmbed($"Kicked {memberToKick.Username}",
                "Kicked for: " + reason + (canDM ? "" : ". DM failed as user may have DMs turned off or they aren't in the server.."),
                AccentColor));
            await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
        }

        [SlashCommand("mute", "Mutes a user in the server")] //timing out and mute are pratcially the same thing, however in DSharpPlus,
                                                             //they use the mute variable as for "mutung" people from VCs.
        public async Task Tiemeout(InteractionContext ctx, [Option("user", "The user to mute")] DiscordUser user,
                                                       [Option("duration", "How long should the user be muted for (in seconds)")] long duration)
        {
            await ctx.DeferAsync();

            if (ctx.Member.Permissions.HasPermission(Permissions.ManageMessages))


            {
                var timeDuration = DateTime.Now + TimeSpan.FromSeconds(duration); //Timing out the user
                var member = (DiscordMember)user;
                await member.TimeoutAsync(timeDuration);

                var timeoutMessage = new DiscordEmbedBuilder() //Muted User embed
                .WithAuthor("n3ptune's nation", null, IconURL)
                .WithColor(AccentColor)
                .WithTitle("Muted " + member.Username)
                .WithDescription($"{member.Username} has been muted for " + TimeSpan.FromSeconds(duration).ToString() + " seconds");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(timeoutMessage));
            }

            else
            {
                var noPermissionMessage = new DiscordEmbedBuilder() //No permssion embed
                .WithAuthor("n3ptune's nation", null, IconURL)
                .WithColor(DiscordColor.Red)
                .WithTitle("Access denied")
                .WithDescription("You need the Manage Messages Permission to run this command.");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noPermissionMessage));


            }

        }

        [SlashCommand("warn", "warns a user in the server")] //warning command
        public async Task Warn(InteractionContext ctx, [Option("user", "The user to warn")] DiscordUser user,
                                                       [Option("reason", "Reasons to warn this user")] string reason)
        {
           await ctx.DeferAsync();
            if (ctx.Member.Permissions.HasPermission(Permissions.ManageMessages))
            {
                var member = (DiscordMember)user;

                var warningMessage = new DiscordEmbedBuilder() //Warn User embed
                .WithAuthor("n3ptune's nation", null, IconURL)
                .WithColor(AccentColor)
                .WithTitle("Warned " + member.Username)
                .WithDescription($"{member.Username} has been warned for " + reason);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(warningMessage));
            }
            else
            {
                var noPermissionMessage = new DiscordEmbedBuilder() //No permssion embed
               .WithAuthor("n3ptune's nation", null, IconURL)
               .WithColor(DiscordColor.Red)
               .WithTitle("Access denied")
               .WithDescription("You need the Manage Messages Permission to run this command.");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noPermissionMessage));
            }


        }

        [SlashCommand("lock", "Locks the channel")]
        [SlashRequirePermissions(Permissions.ManageChannels)]
        public async Task Lock(InteractionContext ctx, [Option("channel", "The channel to lock")] DiscordChannel channel)

        {
            await ctx.DeferAsync();
            if(ctx.Member.Permissions.HasPermission(Permissions.ManageMessages))
              
                {
                    ulong roleId = 1184115419842351145; //Make sure this ID matches the ID in production use.
                    var role = ctx.Guild.GetRole(roleId);

                    if (role == null)
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("Invaild Role, please make sure the correct `roleId` is supplied"));
                        return;
                    }

                    // Create an overwrite to deny send_messages for the specified role
                    var overwrite = new DiscordOverwriteBuilder(role).Deny(Permissions.SendMessages);

                    // Modify the channel permissions
                    await channel.ModifyAsync(x => x.PermissionOverwrites = new List<DiscordOverwriteBuilder> { overwrite });

                //Sends the message
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .WithContent("The channel has been locked from sending messages.."));
            }
            else
            {
                var noPermissionMessage = new DiscordEmbedBuilder() //No permssion embed
               .WithAuthor("n3ptune's nation", null, IconURL)
               .WithColor(DiscordColor.Red)
               .WithTitle("Access denied")
               .WithDescription("You need the Manage Messages Permission to run this command.");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noPermissionMessage));
            }
        }

        [SlashCommand("unlock", "Unlocks the channel")]
        [SlashRequirePermissions(Permissions.ManageChannels)]
        public async Task Unlock(InteractionContext ctx, [Option("channel", "The channel to unlock")] DiscordChannel channel)

        {
            await ctx.DeferAsync();
            if (ctx.Member.Permissions.HasPermission(Permissions.ManageMessages))

            {
                ulong roleId = 1184115419842351145; //Make sure this ID matches the ID in production use.
                var role = ctx.Guild.GetRole(roleId);

                if (role == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("Invaild Role, please make sure the correct `roleId` is supplied"));
                    return;
                }

                // Create an overwrite to deny send_messages for the specified role
                var overwrite = new DiscordOverwriteBuilder(role).Allow(Permissions.SendMessages);

                // Modify the channel permissions
                await channel.ModifyAsync(x => x.PermissionOverwrites = new List<DiscordOverwriteBuilder> { overwrite });

                //Sends the message
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .WithContent("The channel has been unlocked from sending messages.."));
            }
            else
            {
                var noPermissionMessage = new DiscordEmbedBuilder() //No permssion embed
               .WithAuthor("n3ptune's nation", null, IconURL)
               .WithColor(DiscordColor.Red)
               .WithTitle("Access denied")
               .WithDescription("You need the Manage Messages Permission to run this command.");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(noPermissionMessage));
            }
        }
        #endregion
        #region Verification
        [SlashCommand("verify", "Verifiy yourself into the server")] //Verify Command
        public async Task VerifyUser(InteractionContext ctx)
        
        {
            var role = ctx.Guild.GetRole(1184115419842351145);
            await ctx.Member.GrantRoleAsync(role).ConfigureAwait(false);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are now verified, welcome to the server!"));
        }
        #endregion
        #region Utils
        [SlashCommand("about", "Shows information about this bot")]
        public async Task AboutCommand(InteractionContext userToRunCommand) //Bot Shutdown.
        {
            var result = new DiscordInteractionResponseBuilder(); //About Bot
            DiscordEmbedBuilder b = new DiscordEmbedBuilder()
            .WithTitle("About \"Histoire\"")
            .WithColor(AccentColor)
            .AddField("Version","v2.6")
            .AddField("Last updated", "12/12/2023 3:00PM UTC")
            .AddField("Development", "Histoire was created and by mishaproductions and is maintaned by n3ptune_cpu, for n3ptune's nation.")
            .WithAuthor("n3ptune's nation", null, IconURL);

            result.AddEmbed(b);
            await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
        }
        [SlashCommand("stop", "Shuts down the bot. Only bot maintainers can use this command")]
        public async Task StopCommand(InteractionContext userToRunCommand) //Bot Shutdown.
        {
            var result = new DiscordInteractionResponseBuilder();

            if (userToRunCommand.User.Id != 480146132526170129)
                if (userToRunCommand.User.Id != 531139663096840192)
                {
                result.AddEmbed(CreateEmbed("Access denied.", "You do not have the permission to run this command.", DiscordColor.Red));
                await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
                return;
            }

            result.AddEmbed(CreateEmbed("Done", "The bot was shut down successfully", DiscordColor.Green));
            await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
            DataStorage.Save();
            Environment.Exit(0);


        }

        [SlashCommand("system", "Shows information about the system running the bot")]
        public async Task SystemCommand(InteractionContext userToRunCommand)
        {
            var result = new DiscordInteractionResponseBuilder(); //About system
            DiscordEmbedBuilder b = new DiscordEmbedBuilder()
            .WithTitle("About the system running Histoire")
            .WithColor(AccentColor)
            .AddField("Hostname", Dns.GetHostName())
            .AddField("Processor", "Intel Xeon E3-1246 v3 @ 3.50GHz")
            .AddField("Special Thanks", "Thanks to Julia for allowing us to use her server to host Histoire.")
            .WithAuthor("n3ptune's nation", null, IconURL);

            result.AddEmbed(b);
            await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
        }

        [SlashCommand("invite", "Provides the server invite link")]
        public async Task InviteCommand(InteractionContext userToRunCommand)
        {
            var result = new DiscordInteractionResponseBuilder(); //Server invite command
            DiscordEmbedBuilder b = new DiscordEmbedBuilder()
            .WithTitle("Server Invite")
            .WithColor(AccentColor)
            .WithDescription("Feel free to invite your friends in with this [link](https://discord.gg/2NUhUHtt2y)")
            .WithAuthor("n3ptune's nation", null, IconURL);

            result.AddEmbed(b);
            await userToRunCommand.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, result);
        }

        #endregion
        #endregion
        #region Util Methods
        private DiscordEmbedBuilder CreateEmbed(string title, string content, DiscordColor color)
        {
            DiscordEmbedBuilder b = new DiscordEmbedBuilder()
                .WithTitle(title)
                .WithDescription(content)
                .WithColor(color)
                .WithAuthor("n3ptune's nation", null, IconURL);

            return b;
        }
        private async Task<bool> HasPerm(InteractionContext ctx, Permissions Permissions)
        {
            if (ctx.Guild == null)
            {
                return false;
            }

            DiscordMember usr = ctx.Member;
            if (usr == null)
            {
                return false;
            }

            Permissions pusr = ctx.Channel.PermissionsFor(usr);
            DiscordMember bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id).ConfigureAwait(continueOnCapturedContext: false);
            if (bot == null)
            {
                return false;
            }

            Permissions pbot = ctx.Channel.PermissionsFor(bot);
            bool usrok = ctx.Guild.OwnerId == usr.Id;
            bool botok = ctx.Guild.OwnerId == bot.Id;
            if (!usrok)
            {
                usrok = (pusr & Permissions.Administrator) != Permissions.None || (pusr & Permissions) == Permissions;
            }

            if (!botok)
            {
                botok = (pbot & Permissions.Administrator) != Permissions.None || (pbot & Permissions) == Permissions;
            }

            return usrok && botok;
        }
        #endregion
    }
}