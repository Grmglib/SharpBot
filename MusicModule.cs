using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Lavalink4NET.Player;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using DescriptionAttribute = DSharpPlus.CommandsNext.Attributes.DescriptionAttribute;
using LavalinkTrack = DSharpPlus.Lavalink.LavalinkTrack;

namespace SharpBot
{
    public class MusicModule : BaseCommandModule
    {
        private static List<LavalinkTrack> QueueList = new List<LavalinkTrack>();
        private static CommandContext context = null;
        private static int PlayListSize = 25;

        #region Helpers
        public static void GetVariables()
        {
            PlayListSize = Convert.ToInt32(SavingPlugin.GetVariable("PlayListSize", TypeCode.Int32));
        }

        #endregion Helpers

        #region Events

        public static async Task PlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs args)
        {
            if (QueueList.Count > 0)
            {
                await sender.PlayAsync(QueueList.PopFirst());
            }
        }

        public static async Task PlaybackStarted(LavalinkGuildConnection sender, TrackStartEventArgs args)
        {
            if (context != null)//&& sender.CurrentState.CurrentTrack == null)
            {
                context.RespondAsync($"Now playing {sender.CurrentState.CurrentTrack.Title}\n");
            }
        }

        #endregion Events

        #region Commands

        #region Join

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            context = ctx;
            var lava = ctx.Client.GetLavalink();
            var vs = ctx.Member.VoiceState;
            var channel = vs?.Channel;
            if (channel == null)
            {
                await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":msraisedhand:")} You need to be in a voice channel.");
            }
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }
            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }
            if (ctx.Client.CurrentUser.Presence.Guild.CurrentMember.VoiceState != null)
            {
                await ctx.RespondAsync($"Already connected to {ctx.Client.CurrentUser.Presence.Guild.CurrentMember.VoiceState.Channel.Name}");
                return;
            }
            await node.ConnectAsync(channel);
            await ctx.RespondAsync($"Joined {channel.Name}!");
        }

        #endregion Join

        #region Leave

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            var vs = ctx.Member.VoiceState;
            var channel = vs?.Channel;
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }

            var conn = node.GetGuildConnection(channel.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {channel.Name}!");
        }

        #endregion Leave

        #region Play

        [Command("play"), Description("Plays tracks with supplied Url or searches for keywords")]
        public async Task Play(CommandContext ctx, [Description("URL or title to play from")][RemainingText] string search)
        {
            context = ctx;
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            LavalinkLoadResult finalResult;
            if (conn == null)
            {
                await ctx.RespondAsync("Bot is not connected in a voice channel.");
                return;
            }
            var plainResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Plain);
            if (plainResult.LoadResultType == LavalinkLoadResultType.LoadFailed || plainResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                var ytResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);
                if (ytResult.LoadResultType == LavalinkLoadResultType.LoadFailed || ytResult.LoadResultType == LavalinkLoadResultType.NoMatches)
                {
                    await ctx.RespondAsync($"Track search failed for {search}.");
                    return;
                }
                else
                {
                    finalResult = ytResult;
                }
            }
            else
            {
                finalResult = plainResult;
            }

            if (finalResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                var tracks = finalResult.Tracks;
                int i = 0;
                string message = null;
                foreach (var track in tracks)
                {
                    {
                        if (conn.CurrentState.CurrentTrack == null && i <= PlayListSize)
                        {
                            QueueList.Add(track);
                            i++;
                            await conn.PlayAsync(QueueList.PopFirst());
                        }
                        else if (i <= PlayListSize)
                        {
                            QueueList.Add(track);
                            message += $"{i}.{track.Title} added to Queue\n";
                            i++;
                        }
                    }
                }
                if (message != null)
                {
                    var interactivity = ctx.Client.GetInteractivity();
                    var pages = interactivity.GeneratePagesInEmbed(message);
                    await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
                }
            }
            else
            {
                var tracks = finalResult.Tracks.First();
                string message = null;
                if (conn.CurrentState.CurrentTrack == null)
                {
                    QueueList.Add(tracks);
                    await conn.PlayAsync(QueueList.PopFirst());
                }
                else if (conn.CurrentState.CurrentTrack != null)
                {
                    QueueList.Add(tracks);
                    message += $"{tracks.Title} added to Queue\n";
                }
                await ctx.RespondAsync(message);
            }
        }

        #endregion Play

        #region Pause

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
        }

        #endregion Pause

        #region Resume

        [Command("resume")]
        public async Task Resume(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.ResumeAsync();
        }

        #endregion Resume

        #region Skip

        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            await conn.StopAsync();
        }

        [Command("skip")]
        public async Task Skip(CommandContext ctx, int index)
        {
            index--;
            var track = QueueList.PopAt(index);
            ctx.RespondAsync($"Removed {track.Title} from queue");
        }
        #endregion Skip

        #region Skip To

        [Command("skipto")]
        public async Task SkipTo(CommandContext ctx,int index)
        {
            index--;
            var track = QueueList.PopAt(index);
            QueueList.Insert(0, track);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            await conn.StopAsync();

        }
        #endregion

        #region Stop

        [Command("stop")]
        public async Task Stop(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            await conn.DisconnectAsync();
        }

        #endregion Stop

        #region Volume

        [Command("volume")]
        public async Task SetVolume(CommandContext ctx, int volume)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }
            if (volume > 150 || volume < 0)
            {
                await ctx.RespondAsync("Volume can only be set between 0 and 150");
                return;
            }
            await conn.SetVolumeAsync(volume);
            await ctx.RespondAsync($"Volume setted to {volume}");
        }

        #endregion Volume

        #region Show Queue

        [Command("queue")]
        public async Task ShowQueue(CommandContext ctx)
        {
            int i = 0;
            string message = null;

            if (QueueList.Count == 0)
            {
                await ctx.RespondAsync("The Queue is empty");
            }
            else if (QueueList.Count > 0)
            {
                foreach (var item in QueueList)
                {
                    i++;
                    message += $"{i}.{item.Title}\n";
                }
                var interactivity = ctx.Client.GetInteractivity();
                var pages = interactivity.GeneratePagesInEmbed(message);
                await ctx.Channel.SendPaginatedMessageAsync(ctx.Member, pages);
            }
           
        }

        #endregion Show Queue

        #region Clear Queue

        [Command("clearqueue")]
        public async Task Clear(CommandContext ctx)
        {
            QueueList.Clear();
            await ctx.RespondAsync("All tracks removed from queue");
        }

        #endregion Clear Queue

        #region Playing

        [Command("playing")]
        public async Task Playing(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Bot is not connected to a voice channel.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }
            var track = conn.CurrentState.CurrentTrack;
            var Position = conn.CurrentState.PlaybackPosition;
            DateTime TrackTime = new DateTime(Position.Ticks);

            await ctx.RespondAsync($"Now playing {track.Title}\n" +
                $"Duration: {track.Length} | {TrackTime.ToString(("HH:mm:ss"))}");
        }

        #endregion Playing

        #region Random Queue
        [Command("randomize")]
        public async Task Randomize(CommandContext ctx)
        {
            QueueList.Shuffle();
            await ctx.RespondAsync("Queue randomized");
        }
        #endregion

    #endregion Commands
}


    [Group("s")]
    [RequirePermissions(Permissions.ManageGuild)]
    public class AdminMusicModule : BaseModule
    {
        [Command("pls")]
        public async Task PlaylistSize(CommandContext ctx, int size)
        {
            SavingPlugin.SaveVariable("PlayListSize", TypeCode.Int32, size);
            MusicModule.GetVariables();
            await ctx.RespondAsync($"Playlist size changed to {size}");
        }

       
    }
}