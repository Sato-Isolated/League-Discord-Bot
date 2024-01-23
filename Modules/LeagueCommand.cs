using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;
using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.Enums;
using CsvHelper;
using Discord;
using Discord.Interactions;
using Newtonsoft.Json;

namespace League_Discord_Bot.Modules;

public class LeagueCommand : InteractionModuleBase<SocketInteractionContext>
{
    public static readonly Dictionary<Tier, string> RankEmblems = new()
    {
        {
            Tier.IRON, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691200690032764/iron.png"
        },
        {
            Tier.BRONZE, "https://cdn.discordapp.com/attachments/1198690598350237866/1198690969076371667/bronze.png"
        },
        {
            Tier.SILVER, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691236895268864/silver.png"
        },
        {
            Tier.GOLD, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691182075715624/gold.png"
        },
        {
            Tier.PLATINUM, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691225037971486/platinum.png"
        },
        {
            Tier.EMERALD, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691169094340750/emerald.png"
        },
        {
            Tier.DIAMOND, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691174169464923/diamond.png"
        },
        {
            Tier.MASTER, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691213239406653/master.png"
        },
        {
            Tier.GRANDMASTER, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691186458775643/grandmaster.png"
        },
        {
            Tier.CHALLENGER, "https://cdn.discordapp.com/attachments/1198690598350237866/1198691005424214186/challenger.png"
        }
    };

    public static readonly RiotGamesApi Api =
        RiotGamesApi.NewInstance(
            "RGAPI-0000000000-000000-000000-000000-000000000000"); //https://developer.riotgames.com

    private InteractionHandler _handler;

    public LeagueCommand(InteractionHandler handler)
    {
        _handler = handler;
    }

    public InteractionService Commands { get; set; }

    [SlashCommand("rank", "Check les stats ranked")]
    public async Task RankCommand(string text, string tagline, QueueType select)
    {
        try
        {
            var summs = await Api.AccountV1().GetByRiotIdAsync(RegionalRoute.EUROPE, text, tagline);
            var puuid = await Api.SummonerV4().GetByPUUIDAsync(PlatformRoute.EUW1, summs.Puuid);
            var leagueentries = await Api.LeagueV4().GetLeagueEntriesForSummonerAsync(PlatformRoute.EUW1, puuid.Id);

            var solo = leagueentries.Single(x => x.QueueType == select);

            var numOfGames = solo.Wins + solo.Losses;
            var winRate = solo.Wins / (float)numOfGames * 100;


            var rank = RankEmblems.GetValueOrDefault(solo.Tier.Value, "Unranked");

            var embed = new EmbedBuilder
            {
                Title = $"{summs.GameName}#{summs.TagLine}'s Ranked Stats",
                Color = Color.Blue,
                ThumbnailUrl = rank
            };

            embed.AddField("Rank", $"{solo.Tier} {solo.Rank}\n {solo.LeaguePoints} LP", true)
                .AddField("Stats", $"**Wins:** {solo.Wins}\n**Losses**: {solo.Losses}\n**Win Rate:** {winRate:0.00}%", true);

            await RespondAsync(embed: embed.Build());

        }
        catch
        {
            await RespondAsync("Le joueur n'a pas fait de ranked", ephemeral: true);
        }
    }

    public static string GetDescription(Champion enumValue)
    {
        return enumValue.GetType()
                   .GetMember(enumValue.ToString())
                   .FirstOrDefault()
                   ?.GetCustomAttribute<DisplayAttribute>()
                   ?.Description
               ?? string.Empty;
    }

    [SlashCommand("makecsv", "blablabla csv aram")]
    public async Task makecsv(string text, string tagline)
    {
        try
        {
            await RespondAsync("Preparation", ephemeral: true);
            var summs = await Api.AccountV1().GetByRiotIdAsync(RegionalRoute.EUROPE, text, tagline);
            var puuid = await Api.SummonerV4().GetByPUUIDAsync(PlatformRoute.EUW1, summs.Puuid);

            DateTime starttime = new DateTime(2023, 12, 31);
            DateTime endtime = new DateTime(2024, 1, 1);
            DateTime endtimefinal = DateTime.Today;
            var FilePath = $"{text}.csv";
            var stats = new List<GameStats>();

            var pathFolder = Path.Combine("Game", text);

            Directory.CreateDirectory(pathFolder);
            var nbgame = 0;
            var spectFile = Path.Combine(pathFolder, "games.txt");
            var gameAlreadyCheck = File.Exists(spectFile) ? (await File.ReadAllLinesAsync(spectFile)).ToList() : new List<string>();
            var nbentries = 0;
            while (starttime <= endtimefinal)
            {
                long starttamp = ((DateTimeOffset)starttime.ToUniversalTime()).ToUnixTimeSeconds();
                long endtamp = ((DateTimeOffset)endtime.ToUniversalTime()).ToUnixTimeSeconds();
                var leagueentries = await Api.MatchV5().GetMatchIdsByPUUIDAsync(RegionalRoute.EUROPE, puuid.Puuid, 100, endtamp, Queue.HOWLING_ABYSS_5V5_ARAM, starttamp);
                nbentries += leagueentries.Length;
        
                if (File.Exists(FilePath))
                {
                    using (var reader = new StreamReader(FilePath))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        stats = csv.GetRecords<GameStats>().ToList();
                    }
                }

                foreach (var spect in leagueentries)
                {
                    nbgame += 1;
                    await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Content = $"{nbgame} / {nbentries}";
                    });
                    await Task.Delay(1000);
                    if (gameAlreadyCheck.Contains(spect))
                    {
                        continue;
                    }
                    var match = await Api.MatchV5().GetMatchAsync(RegionalRoute.EUROPE, spect);
                    if (match is not null)
                    {
                        var gameMatch = Path.Combine(pathFolder, $"{spect}.json");
                        await File.WriteAllTextAsync(gameMatch, JsonConvert.SerializeObject(match));

                        var participant = match.Info.Participants.Single(x => x.RiotIdGameName == text);
                        var winloose = participant.Win;
                        var firstBlood = participant.FirstBloodKill;
                        var kill = participant.Kills;
                        var death = participant.Deaths;
                        var assist = participant.Assists;
                        var champ = participant.ChampionName;
                        var damage = participant.TotalDamageDealtToChampions;
                        var duration = TimeSpan.FromSeconds(match.Info.GameDuration);
                        var dpm = (double)damage / duration.Minutes;
                        var dk = participant.DoubleKills;
                        var tk = participant.TripleKills;
                        var qk = participant.QuadraKills;
                        var pk = participant.PentaKills;

                        var newStats = new GameStats
                        {
                            Champ = champ,
                            Game = 1,
                            WinRate = winloose ? 100 : 0,
                            Win = winloose ? 1 : 0,
                            Loose = winloose ? 0 : 1,
                            FirstBlood = firstBlood ? 1 : 0,
                            KDA = death == 0 ? kill + assist : (kill + assist) / (double)death,
                            Kill = kill,
                            Death = death,
                            Assist = assist,
                            DPM = dpm,
                            DoubleKill = dk,
                            TripleKill = tk,
                            QuadraKill = qk,
                            PentaKill = pk
                        };

                        var oldStats = stats.FirstOrDefault(s => s.Champ == newStats.Champ);
                        if (oldStats != null)
                        {
                            oldStats.Game += 1;

                            if (winloose)
                                oldStats.Win += 1;
                            else
                                oldStats.Loose += 1;

                            var numOfGames = oldStats.Win + oldStats.Loose;
                            var winRate = oldStats.Win / (float)numOfGames * 100;
                            oldStats.WinRate = Convert.ToDouble(winRate.ToString("F1"));

                            if (firstBlood is true)
                                oldStats.FirstBlood += 1;

                            oldStats.Kill += kill;
                            oldStats.Death += death;
                            oldStats.Assist += assist;
                            var kda = (oldStats.Kill + oldStats.Assist) / (double)oldStats.Death;
                            oldStats.KDA = Convert.ToDouble(kda.ToString("F1"));
                            oldStats.DPM = Convert.ToDouble(((oldStats.DPM * (oldStats.Game - 1) + dpm) / oldStats.Game).ToString("F1"));
                            oldStats.DoubleKill += dk;
                            oldStats.TripleKill += tk;
                            oldStats.QuadraKill += qk;
                            oldStats.PentaKill += pk;
                        }
                        else
                        {
                            stats.Add(newStats);
                        }
                        gameAlreadyCheck.Add(spect);
                    }
                }

                starttime = starttime.AddDays(1);
                endtime = endtime.AddDays(1);
            }

            await File.WriteAllLinesAsync(spectFile, gameAlreadyCheck);

            using (var writer = new StreamWriter(FilePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                await csv.WriteRecordsAsync(stats);
            }
            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = "Finish";
            });
        }
        catch (Exception ex)
        {
            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content =  ex.ToString();
            });
        }
    }

    public class GameStats
    {
        public string Champ { get; set; }
        public int Game { get; set; }
        public double WinRate { get; set; }
        public int Win { get; set; }
        public int Loose { get; set; }
        public int FirstBlood { get; set; }
        public double KDA { get; set; }
        public int Kill { get; set; }
        public int Death { get; set; }
        public int Assist { get; set; }
        public int DoubleKill { get; set; }
        public int TripleKill { get; set; }
        public int QuadraKill { get; set; }
        public int PentaKill { get; set; }

        public double DPM { get; set; }
    }

    [SlashCommand("lg", "Check les stats de la game en cours")]
    public async Task LiveGame(string text, string tagline)
    {
        try
        {
            var summs = await Api.AccountV1().GetByRiotIdAsync(RegionalRoute.EUROPE, text, tagline);
            var puuid = await Api.SummonerV4().GetByPUUIDAsync(PlatformRoute.EUW1, summs.Puuid);
            var spect = await Api.SpectatorV4().GetCurrentGameInfoBySummonerAsync(PlatformRoute.EUW1, puuid.Id);

            var embed = new EmbedBuilder
            {
                Title = $"{puuid.Name}'s Live Game",
                Color = Color.Blue
            };

            var playerteam1 = new StringBuilder();
            var playerteam2 = new StringBuilder();
            string gamemode = null;
            if (spect.GameMode is GameMode.ARAM or GameMode.CLASSIC)
            {
                gamemode = spect.GameMode.ToString();
                foreach (var sp in spect.Participants)

                    if (sp.TeamId == Team.Blue)
                        playerteam1.AppendLine(sp.SummonerName + " - " + GetDescription(sp.ChampionId));

                    else if (sp.TeamId == Team.Red)
                        playerteam2.AppendLine(sp.SummonerName + " - " + GetDescription(sp.ChampionId));
            }

            embed.AddField("GameMode", gamemode)
                .AddField("Blue Team", playerteam1.ToString(), true)
                .AddField("Red Team", playerteam2.ToString(), true);

            await RespondAsync(embed: embed.Build(), ephemeral: true);

        }
        catch
        {
            await RespondAsync($"{text}#{tagline} n'est pas en game actuellement.");
        }
    }

    public static double CalculateDifference(double num1, double num2)
    {
        return Math.Abs(num1 - num2);
    }

    [SlashCommand("stalk", "Stalk une personne")]
    public async Task Stalking(string name, string tagline)
    {
        try
        {
            LeagueMethod.TempRankedName = name;
            LeagueMethod.TempRankedTagline = tagline;
            await RespondAsync("Stalk", ephemeral: true);
            await LeagueMethod.StalkingRanked(name, tagline);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    [SlashCommand("ssr", "Arrete le stalking d'une personne")]
    public async Task StopStalkingRanked()
    {
        try
        {
            LeagueMethod.RecursiveRanked = true;
            await RespondAsync("Stop Stalk", ephemeral: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    [SlashCommand("stalkaram", "Stalk une personne")]
    public async Task StalkingAram(string name, string tagline)
    {
        try
        {
            LeagueMethod.TempAramName = name;
            LeagueMethod.TempAramTagline = tagline;
            await RespondAsync("Stalk", ephemeral: true);
            await LeagueMethod.StalkingAram(name, tagline);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    [SlashCommand("ssa", "Arrete le stalking d'une personne")]
    public async Task StopStalkingAram()
    {
        try
        {
            LeagueMethod.RecursiveAram = true;
            await RespondAsync("Stop Stalk", ephemeral: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}