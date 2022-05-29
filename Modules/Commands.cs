using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Commands;
using ImageProcessor;
using ImageProcessor.Imaging.Filters.Photo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Mika_Bot.Modules
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commandService;
        public Commands(CommandService commandService)
        {
            _commandService = commandService;
        }

        private ulong mikaUID = 452415473687068672;

        [Command("search")]
        [Summary("Suche nach deinem Lieblingssong auf YouTube.")]
        public async Task YouTubeSearch([Remainder]string query)
        {
            var youtube = new YoutubeClient();
            var videos = youtube.Search.GetVideosAsync(query);

            int i = 0;
            string[,] resultArray = new string[5, 2];

            if (!Program.searchResults.ContainsKey(Context.Guild.Id))
            {
                Program.searchResults.Add(Context.Guild.Id, null);
            }

            EmbedBuilder embedBuilder = new EmbedBuilder();

            await foreach (var video in videos)
            {
                if (i >= 5) break;
                resultArray[i, 0] = video.Title;
                resultArray[i, 1] = video.Url;

                embedBuilder.AddField((i+1) + ". " + video.Title, video.Url);
                
                /*foreach (var t in video.Thumbnails)
                {
                    resultArray[i, 2] = t.Url;
                    break;
                }

                embedBuilders[i].ImageUrl = resultArray[i, 2];*/

                i++;
            }
            
            await ReplyAsync(embed: embedBuilder.Build());

            Program.searchResults[Context.Guild.Id] = resultArray;
        }

        [Command("ping")]
        [Summary("Lass' uns Ping-Pong spielen.")]
        public async Task Ping()
        {
            await ReplyAsync($":ping_pong: (Ping: {Context.Client.Latency} ms)");
        }

        [Command("help")]
        [Summary("Drei mal darfst du raten, was dieser Befehl macht.")]
        public async Task Help(string arg = null)
        {
            var commands = _commandService.Commands;
            EmbedBuilder embedBuilder = new EmbedBuilder();

            Hashtable commandList = new Hashtable();

            foreach (CommandInfo command in commands)
            {
                commandList.Add(command.Name, command.Summary);
            }

            if (arg == null)
            {
                string valueText = "";

                foreach (CommandInfo command in commands)
                {

                    if (command.Aliases.Count < 2)
                    {
                        valueText += command.Name + "\n";
                    }
                    else
                    {
                        string title = "";

                        foreach (var alias in command.Aliases)
                        {
                            title += alias + ", ";
                        }

                        title = title.Remove(title.Length - 2, 2);

                        valueText += title + "\n";
                    }
                }

                embedBuilder.AddField("Alle Befehle", valueText);
                embedBuilder.Description = "Du kannst 'mika help {Befehl}' eingeben, um mehr darüber herauszufinden.";
            }
            else if (commandList.ContainsKey(arg))
            {
                embedBuilder.AddField(arg, (string)commandList[arg]);
            }
            else
            {
                embedBuilder.AddField("What?", $"Es gibt keinen Befehl, der '{arg}' heißt.");
                embedBuilder.ImageUrl = "https://cdn.discordapp.com/attachments/870777345512984628/980558844990201876/flat_750x_075_f-pad_750x1000_f8f8f8-removebg.png";
            }

            embedBuilder.Color = new Color(255, 0, 0);
            var author = new EmbedAuthorBuilder();
            author.WithName("By Mika");
            author.WithIconUrl(Context.Client.GetUser(mikaUID).GetAvatarUrl());
            embedBuilder.Author = author;
            embedBuilder.ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl();

            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("join", RunMode = RunMode.Async)]
        [Summary("Rufe mich in den Voice Channel, wo du gerade bist.")]
        public async Task Join(IVoiceChannel channel = null, IVoiceChannel channel1 = null)
        {
            channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
            channel1 = Context.Guild.CurrentUser.VoiceChannel;

            if (channel1 != null && channel1.Id == channel.Id) { await ReplyAsync("Ich bin bereits in diesem Voicechannel, du Baka!"); return; }
            if (channel == null) { await ReplyAsync("Du musst in einem Voice Channel sein, wo ich joinen kann!"); return; }

            var audioClient = await channel.ConnectAsync();

            await ReplyAsync($"Sprachkanal **{channel.Name}** beigetreten.");
        }

        [Command("leave", RunMode = RunMode.Async)]
        [Summary("Wenn du kein Bock mehr auf mich hast.")]
        public async Task Leave(IVoiceChannel channel = null)
        {
            channel = Context.Guild.CurrentUser.VoiceChannel;

            if (channel == null) { await ReplyAsync("Es gibt keinen Sprachkanal zu leaven!"); return; }

            await channel.DisconnectAsync();
            await ReplyAsync("Sprachkanal verlassen.");
        }

        [Command("suicide")]
        [Summary("(×﹏×)")]
        public async Task Suicide()
        {
            if (Context.User.Id == mikaUID)
            {
                await ReplyAsync("Beim nächsten mal will ich dich nicht enttäuschen!");
                await Context.Client.StopAsync();
                Environment.Exit(0);
            }
            else
            {
                await ReplyAsync("Nur Mika-Sama kann mir diesen Befehl geben!");
            }
        }

        [Command("filter", RunMode = RunMode.Async)]
        [Summary("Cooler Befehl für coole Filter für dein Profilbild!\n" +
                 "Bisher gibt es invert, comic, und neutral, falls du einfach dein" +
                 " geiles Profilbild in groß sehen willst :)\n" +
                 "Wenn du jemanden dabei pingst, kannst du das mit seinem Profilbild machen.")]
        public async Task Filter(string arg, [Remainder]string args = null)
        {
            ImageFactory iF = new ImageFactory();

            Discord.WebSocket.SocketUser user = Context.Message.Author;

            if (Context.Message.MentionedUsers.Count > 0)
            {
                var mUsers = Context.Message.MentionedUsers;

                foreach (var u in mUsers)
                {
                    user = u;
                    break;
                }
            }

            string url = user.GetAvatarUrl();
            url = url.Remove(url.Length - 3, 3) + "1024";

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(new Uri(url), Path.GetFullPath(@$"assets\{user.AvatarId}.png"));  
            }

            iF.Load(@$"assets\{user.AvatarId}.png");

            switch (arg)
            {
                case "invert":
                    iF.Filter(MatrixFilters.Invert);
                    break;

                case "comic":
                    iF.Filter(MatrixFilters.Comic);
                    break;

                case "neutral":
                    break;

                default:
                    await ReplyAsync($"Kein Plan was du mit '{arg}' meinst.");
                    return;
            }
            
            iF.Save(Path.GetFullPath(@$"assets\avatar.png"));

            await Context.Channel.SendFileAsync(Path.GetFullPath(@$"assets\avatar.png"));
            File.Delete(Path.GetFullPath(@$"assets\avatar.png"));
            File.Delete(Path.GetFullPath(@$"assets\{user.AvatarId}.png"));
        }

        [Command("loop")]
        [Summary("Wenn du diesen einen Song einfach zu sehr magst.")]
        public async Task Loop(string arg = "")
        {
            if (!Program.loopEnabled.ContainsKey(Context.Guild.Id)) Program.loopEnabled.Add(Context.Guild.Id, null);
            bool loopEnabled = false;
            if (Program.loopEnabled[Context.Guild.Id] == null) loopEnabled = false;
            else loopEnabled = (bool)Program.loopEnabled[Context.Guild.Id];

            if (arg == "on")
            {
                Program.loopEnabled[Context.Guild.Id] = true;
                await ReplyAsync("Loop an");
                return;
            }
            else if (arg == "off")
            {
                Program.loopEnabled[Context.Guild.Id] = false;
                await ReplyAsync("Loop aus");
                return;
            }
            else if (arg != "")
            {
                await ReplyAsync("Gib entweder 'on' oder 'off' an.");
                return;
            }
            else
            {
                if (loopEnabled) await ReplyAsync("Loop ist an");
                else await ReplyAsync("Loop ist aus");
            }
        }
        /*
        [Command("stop", RunMode = RunMode.Async)]
        [Summary("Dieser Befehl war schwieriger zu programmieren als gedacht...")]
        public async Task Stop()
        {
            if (!Program.streams.ContainsKey(Context.Guild.Id)) Program.streams.Add(Context.Guild.Id, null);

            if (Program.streams[Context.Guild.Id] == null)
            {
                await ReplyAsync("Was soll ich bitte stoppen?!");
                return;
            }

            var stream = (AudioOutStream)Program.streams[Context.Guild.Id];

            stream.Close();
            Program.streams[Context.Guild.Id] = null;
            Program.currentSong[Context.Guild.Id] = null;
        }

        [Command("skip")]
        [Summary("Klappt noch nicht... Diese Scheiße (und stop) ist richtig beschissen zu programmieren.")]
        public async Task Skip()
        {
            if (Program.currentSong[Context.Guild.Id] != null)
            {
                await Stop();
                Play();
            }
            else
            {
                await ReplyAsync("Es gibt gerade nichts zu skippen.");
            }
            
        }
        */
        [Command("nowplaying")]
        [Alias("np")]
        [Summary("Frag mich nach dem aktuellen Song.")]
        public async Task CurrentSong()
        {
            if (!Program.currentSong.ContainsKey(Context.Guild.Id)) Program.currentSong.Add(Context.Guild.Id, null);
            var currentSong = (string[])Program.currentSong[Context.Guild.Id];

            EmbedBuilder embedBuilder = new EmbedBuilder();

            if (currentSong == null)
            {
                embedBuilder.AddField("Nichts da...", "Ich spiele nichts, wenn du nichts reinstellst du 5head.");
                await ReplyAsync(embed: embedBuilder.Build());
            }
            else
            {
                embedBuilder.AddField(currentSong[0], currentSong[1]);
                embedBuilder.ImageUrl = currentSong[2];
                await ReplyAsync(embed: embedBuilder.Build());
            }
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Streame einen Song direkt von YouTube.\n" +
                 "Falls du vorher nach Songs gesucht haben solltest, " +
                 "kannst du statt der URL 1 - 5 eingeben, um den Song automatisch reinzustellen.")]
        public async Task Play(string parameter = null, IVoiceChannel channel = null)
        {
            channel = Context.Guild.CurrentUser.VoiceChannel;

            if (channel == null)
            {
                await Join();

                if (Context.Guild.CurrentUser.VoiceChannel == null) return;
            }

            if (parameter == "1" || parameter == "2" || parameter == "3" || parameter == "4" || parameter == "5")
            {
                var resultList = (string[,])Program.searchResults[Context.Guild.Id];

                if (resultList == null) { await ReplyAsync("Du hast nichts gesucht!"); return; }

                parameter = resultList[int.Parse(parameter) - 1, 1];

                Program.searchResults[Context.Guild.Id] = null;
            }
            else if (parameter == null)
            {
                if (!Program.queue.ContainsKey(Context.Guild.Id)) Program.queue.Add(Context.Guild.Id, new List<string[]>());

                var queue = (List<string[]>)Program.queue[Context.Guild.Id];

                if (queue.Count == 0)
                {
                    await ReplyAsync("Die Queue ist leer. Es gibt nichts abzuspielen.");
                }

                parameter = queue[0][1];
                queue.RemoveAt(0);

                Program.queue[Context.Guild.Id] = queue;
            }

            await ReplyAsync("Lade...");

            var youtube = new YoutubeClient();
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(parameter);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var stream = youtube.Videos.Streams.GetAsync(streamInfo);

            MemoryStream memoryStream = new MemoryStream();
#pragma warning disable CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            Cli.Wrap("ffmpeg")
                .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                .WithStandardInputPipe(PipeSource.FromStream(await stream))
                .WithStandardOutputPipe(PipeTarget.ToStream(memoryStream))
                .ExecuteAsync();
#pragma warning restore CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.

            await Task.Delay(500);

            var videos = youtube.Search.GetVideosAsync(parameter);
            string[] currentSong = new string[3];

            await foreach (var video in videos)
            {
                currentSong[0] = video.Title;
                currentSong[1] = video.Url;
                
                foreach (var t in video.Thumbnails)
                {
                    currentSong[2] = t.Url;
                }

                break;
            }

            if (!Program.currentSong.ContainsKey(Context.Guild.Id)) Program.currentSong.Add(Context.Guild.Id, null);
            Program.currentSong[Context.Guild.Id] = currentSong;

            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.AddField(currentSong[0], currentSong[1]);
            embedBuilder.ImageUrl = currentSong[2];
            embedBuilder.Title = "Jetzt spielt:";
            await ReplyAsync(embed: embedBuilder.Build());

            using (var discord = Context.Guild.AudioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                if (!Program.streams.ContainsKey(Context.Guild.Id)) Program.streams.Add(Context.Guild.Id, null);
                Program.streams[Context.Guild.Id] = discord;

                try { await discord.WriteAsync(memoryStream.ToArray(), 0, (int)memoryStream.Length); }
                catch (AccessViolationException exc)
                {
                    await ReplyAsync(exc.Message + "\n\n" + exc.StackTrace);
                    return;
                }
                finally 
                { 
                    await discord.FlushAsync();
                    Program.streams[Context.Guild.Id] = null;
                    Program.currentSong[Context.Guild.Id] = null;

                    if (!Program.loopEnabled.ContainsKey(Context.Guild.Id)) Program.loopEnabled.Add(Context.Guild.Id, null);
                    bool loopEnabled = false;
                    if (Program.loopEnabled[Context.Guild.Id] == null) loopEnabled = false;
                    else loopEnabled = (bool)Program.loopEnabled[Context.Guild.Id];

                    var queue = (List<string[]>)Program.queue[Context.Guild.Id];

                    if (loopEnabled) 
                    
                    { 
                        Play(parameter);
                    }
                    else if (queue != null && queue.Count > 0)
                    {
                        Play(null);
                    }
                }
            }
        }

        [Command("queue")]
        [Summary("Erstelle eine Liste an Songs, die ich der Reihe nach abspielen soll.\n" +
                 "Schreib einfach nur 'mika queue' um dir die aktuelle Queue anzusehen " +
                 "oder gib eine URL bzw. 1 - 5 an, falls du davor 'mika search' benutzt haben solltest, " +
                 "um einen Song der Queue hinzuzufügen.")]
        public async Task Queue([Remainder]string query = null)
        {
            if (query == null)
            {
                if (!Program.queue.ContainsKey(Context.Guild.Id)) Program.queue.Add(Context.Guild.Id, new List<string[]>());

                EmbedBuilder embedBuilder = new EmbedBuilder();

                var i = 1;

                var elements = (List<string[]>)Program.queue[Context.Guild.Id];

                if (elements.Count == 0)
                {
                    await ReplyAsync(embed: embedBuilder.AddField("Die Queue ist leer", "Füg doch einfach einen Song mit 'mika queue add' hinzu!").Build());
                    return;
                }

                foreach (string[] element in elements)
                {
                    embedBuilder.AddField(i + ". " + element[0], element[1]);  
                    i++;
                }

                await ReplyAsync(embed: embedBuilder.Build());
            }
            else if (query != null)
            {
                if (query == "1" || query == "2" || query == "3" || query == "4" || query == "5")
                {
                    var resultList = (string[,])Program.searchResults[Context.Guild.Id];

                    if (resultList == null) { await ReplyAsync("Du hast nichts gesucht!"); return; }

                    query = resultList[int.Parse(query) - 1, 1];

                    Program.searchResults[Context.Guild.Id] = null;
                }

                var youtube = new YoutubeClient();
                var videos = youtube.Search.GetVideosAsync(query);
                var i = 0;
                var resultText = "";

                await foreach (var video in videos)
                {
                    if (i >= 1) break;
                    resultText += video.Title;
                    i++;
                }

                if (!Program.queue.ContainsKey(Context.Guild.Id)) Program.queue.Add(Context.Guild.Id, new List<string[]>());

                string[] videoInfo = new string[2] { resultText, query };

                List<string[]> list = (List<string[]>)Program.queue[Context.Guild.Id];

                list.Add(videoInfo);

                Program.queue[Context.Guild.Id] = list;

                await ReplyAsync($"**{videoInfo[0]}** zur Queue hinzugefügt");
            }
        }
    }
}
