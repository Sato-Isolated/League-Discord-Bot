using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.Enums;
using Discord;
using Discord.Interactions;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Color = System.Drawing.Color;

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
                Color = Discord.Color.Blue,
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

    private static string FormatDuration(TimeSpan duration)
    {
        return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
    }

    private static double CalculateKda(int kills, int deaths, int assists)
    {
        var kda = deaths == 0 ? kills + assists : (kills + assists) / (double)deaths;
        return Math.Round(kda, 2);
    }

    private static void UpdateExistingStats(GameStats existingStat, GameStats newStat,
        string formattedDuration)
    {
        existingStat.Game += 1;
        existingStat.Win += newStat.Win;
        existingStat.Loose += newStat.Loose;
        var numOfGames = existingStat.Win + existingStat.Loose;
        existingStat.WinRate = (existingStat.Win / (float)numOfGames * 100).ToString("F1") + "%";

        existingStat.FirstBlood += newStat.FirstBlood;
        existingStat.Kill += newStat.Kill;
        existingStat.Death += newStat.Death;
        existingStat.Assist += newStat.Assist;

        existingStat.Kda = CalculateKda(existingStat.Kill, existingStat.Death, existingStat.Assist);
        existingStat.DoubleKill += newStat.DoubleKill;
        existingStat.TripleKill += newStat.TripleKill;
        existingStat.QuadraKill += newStat.QuadraKill;
        existingStat.PentaKill += newStat.PentaKill;
        existingStat.Dpm =
            ((Convert.ToDouble(existingStat.Dpm) * (existingStat.Game - 1) + Convert.ToDouble(newStat.Dpm)) /
             existingStat.Game).ToString("F2");
        UpdateDuration(existingStat, formattedDuration);
    }


    private GameStats GetStatistiquesFromWorksheet(ExcelWorksheet worksheet, int row)
    {
        return new GameStats
        {
            Champ = worksheet.Cells[row, 1].Value?.ToString() ?? "null",
            Game = TryConvertToInt32(worksheet.Cells[row, 2].Value),
            WinRate = worksheet.Cells[row, 3].Value?.ToString() ?? "0%",
            Win = TryConvertToInt32(worksheet.Cells[row, 4].Value),
            Loose = TryConvertToInt32(worksheet.Cells[row, 5].Value),
            FirstBlood = TryConvertToInt32(worksheet.Cells[row, 6].Value),
            Kda = TryConvertToDouble(worksheet.Cells[row, 7].Value),
            Kill = TryConvertToInt32(worksheet.Cells[row, 8].Value),
            Death = TryConvertToInt32(worksheet.Cells[row, 9].Value),
            Assist = TryConvertToInt32(worksheet.Cells[row, 10].Value),
            DoubleKill = TryConvertToInt32(worksheet.Cells[row, 11].Value),
            TripleKill = TryConvertToInt32(worksheet.Cells[row, 12].Value),
            QuadraKill = TryConvertToInt32(worksheet.Cells[row, 13].Value),
            PentaKill = TryConvertToInt32(worksheet.Cells[row, 14].Value),
            Dpm = worksheet.Cells[row, 15].Value?.ToString() ?? "0",
            DurationAvg = worksheet.Cells[row, 16].Value?.ToString() ?? "00:00"
        };
    }

    private static int TryConvertToInt32(object value)
    {
        return int.TryParse(value?.ToString(), out var result) ? result : 0;
    }

    private static double TryConvertToDouble(object value)
    {
        return double.TryParse(value?.ToString(), out var result) ? result : 0.0;
    }

    private static string GenerateProgressBar(double percentage)
    {
        const int progressBarWidth = 20;
        var completedWidth = (int)(progressBarWidth * percentage / 100);
        var remainingWidth = progressBarWidth - completedWidth;

        return "[" + new string('█', completedWidth) + new string('░', remainingWidth) + "] " +
               percentage.ToString("F2") + "%";
    }

    private static void UpdateDuration(GameStats stat, string formattedDuration)
    {
        if (TimeSpan.TryParseExact(formattedDuration, @"mm\:ss", CultureInfo.InvariantCulture, out var currentDuration))
        {
            if (TimeSpan.TryParseExact(stat.DurationAvg, @"mm\:ss", CultureInfo.InvariantCulture,
                    out var existingDuration))
            {
                var totalDuration = new TimeSpan(0, 0,
                    (int)(existingDuration.TotalSeconds * (stat.Game - 1)) + (int)currentDuration.TotalSeconds);
                var averageDuration = new TimeSpan(0, 0, (int)(totalDuration.TotalSeconds / stat.Game));
                stat.DurationAvg = averageDuration.ToString(@"mm\:ss");
            }
            else
            {
                Console.WriteLine("Error: Invalid format for 'stat.Duration'.");
            }
        }
        else
        {
            Console.WriteLine("Error: Invalid format for 'formattedDuration'.");
        }
    }

    private static int DurationToSeconds(string duration)
    {
        if (TimeSpan.TryParseExact(duration, @"mm\:ss", CultureInfo.InvariantCulture, out var timespan))
        {
            return (int)timespan.TotalSeconds;
        }

        return 0;
    }

    private static double ParseDpm(string dpm)
    {
        if (double.TryParse(dpm, out var result))
        {
            return result;
        }

        return 0;
    }

    [SlashCommand("makeexcel", "blablabla excel file")]
    public async Task MakeExcel(string username, string tagline)
    {
        try
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            await RespondAsync("Preparing", ephemeral: true);
            var summs = await Api.AccountV1().GetByRiotIdAsync(RegionalRoute.EUROPE, username, tagline);
            var puuid = await Api.SummonerV4().GetByPUUIDAsync(PlatformRoute.EUW1, summs.Puuid);


            var folderBase = Path.Combine("Game", username);

            Directory.CreateDirectory(folderBase);
            var pathFile =
                Path.Combine(Environment.CurrentDirectory,
                    $"{username}.xlsx");
            var statistics = new List<GameStats>();

            var nbgame = 0;
            var fileSpect = Path.Combine(folderBase, "games.txt");
            var gamesalreadyplayed = File.Exists(fileSpect)
                ? (await File.ReadAllLinesAsync(fileSpect)).ToList()
                : [];
            var nbentries = 0;
            var alreadycheck = 0;
            string message;

            if (File.Exists(pathFile))
            {
                using (var package = new ExcelPackage(new FileInfo(pathFile)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet != null)
                    {
                        var rowCount = worksheet.Dimension.Rows;
                        for (var row = 2; row <= rowCount; row++)
                        {
                            var statistique = GetStatistiquesFromWorksheet(worksheet, row);
                            statistics.Add(statistique);
                        }
                    }

                    statistics.RemoveAll(s => s.Champ == "Total");

                    worksheet?.Cells.AutoFitColumns();
                }
            }

            var pathFileDate = Path.Combine(folderBase, "lastDate.txt");
            DateTime starttime;
            if (File.Exists(pathFileDate))
            {
                var dateLastExecuted = await File.ReadAllTextAsync(pathFileDate);
                starttime = DateTime.Parse(dateLastExecuted);
            }
            else
            {
                starttime = new DateTime(2024, 1, 10); // start season 14
            }

            var endtime = starttime.AddDays(1);
            var endtimefinal = DateTime.Today;
            var totalDays = (endtimefinal - starttime).TotalDays;
            while (starttime <= endtimefinal)
            {
                var starttamp = ((DateTimeOffset)starttime.ToUniversalTime()).ToUnixTimeSeconds();
                var endtamp = ((DateTimeOffset)endtime.ToUniversalTime()).ToUnixTimeSeconds();
                var leagueentries = await Api.MatchV5().GetMatchIdsByPUUIDAsync(RegionalRoute.EUROPE, puuid.Puuid,
                    100,
                    endtamp, Queue.HOWLING_ABYSS_5V5_ARAM, starttamp);
                nbentries += leagueentries.Length;
                var remainingDays = (endtimefinal - starttime).TotalDays;

                var progressPercentage = 100 - remainingDays / totalDays * 100;

                var progressBar = GenerateProgressBar(progressPercentage);
                foreach (var game in gamesalreadyplayed)
                {
                    if (leagueentries.Contains(game))
                    {
                        nbentries--;
                    }
                }

                foreach (var spect in leagueentries)
                {
                    await Task.Delay(125);

                    if (gamesalreadyplayed.Contains(spect))
                    {
                        continue;
                    }

                    var match = await Api.MatchV5().GetMatchAsync(RegionalRoute.EUROPE, spect);


                    if (match is not null)
                    {
                        var participant = match.Info.Participants.Single(x => x.Puuid == puuid.Puuid);

                        if (participant.GameEndedInEarlySurrender)
                        {
                            nbentries--;
                            message =
                                $"Number Of Game {nbgame} / {nbentries}\n\nNumber Of Game AlreadyChecked {alreadycheck++}\n\n{progressBar}";
                            await ModifyOriginalResponseAsync(properties => { properties.Content = message; });
                            continue;
                        }

                        message = $"Number Of Game {nbgame++} / {nbentries}\n\n{progressBar}";
                        await ModifyOriginalResponseAsync(properties => { properties.Content = message; });

                        var fileMatch = Path.Combine(folderBase, $"{spect}.json");
                        await File.WriteAllTextAsync(fileMatch, JsonConvert.SerializeObject(match));

                        var champ = GetDescription(participant.ChampionId);
                        var duration = TimeSpan.FromSeconds(match.Info.GameDuration);
                        var formattedDuration = FormatDuration(duration);
                        var damagePerMinute = participant.Challenges?.DamagePerMinute ?? 0.0;
                        var newStatistics = new GameStats
                        {
                            Champ = champ,
                            Game = 1,
                            WinRate = (participant.Win ? 100 : 0) + "%",
                            Win = participant.Win ? 1 : 0,
                            Loose = participant.Win ? 0 : 1,
                            FirstBlood = participant.FirstBloodKill ? 1 : 0,
                            Kda = CalculateKda(participant.Kills, participant.Deaths, participant.Assists),
                            Kill = participant.Kills,
                            Death = participant.Deaths,
                            Assist = participant.Assists,
                            DoubleKill = participant.DoubleKills,
                            TripleKill = participant.TripleKills,
                            QuadraKill = participant.QuadraKills,
                            PentaKill = participant.PentaKills,
                            Dpm = damagePerMinute.ToString("F2"),
                            DurationAvg = formattedDuration
                        };

                        var statExisting =
                            statistics.FirstOrDefault(s => s.Champ == newStatistics.Champ);
                        if (statExisting != null)
                        {
                            UpdateExistingStats(statExisting, newStatistics, formattedDuration);
                        }
                        else
                        {
                            statistics.Add(newStatistics);
                        }

                        gamesalreadyplayed.Add(spect);
                    }
                }

                starttime = starttime.AddDays(1);
                endtime = endtime.AddDays(1);
            }

            message = $"Number Of Game {nbgame} / {nbentries}\n\n{GenerateProgressBar(100)}";
            await ModifyOriginalResponseAsync(properties => { properties.Content = message; });
            var statisticsSorted = statistics.OrderByDescending(s => s.Game)
                .ThenByDescending(s => s.Kda).ToList();
            var totalWins = statisticsSorted.Sum(s => s.Win);
            var totalLooses = statisticsSorted.Sum(s => s.Loose);
            var totalGames = totalWins + totalLooses;
            var totalWinRate = totalGames > 0 ? (double)totalWins / totalGames * 100 : 0;
            var totalkda = CalculateKda(statisticsSorted.Sum(s => s.Kill), statisticsSorted.Sum(s => s.Death),
                statisticsSorted.Sum(s => s.Assist));
            var totalDurationInSeconds = statistics.Sum(s => DurationToSeconds(s.DurationAvg) * s.Game);

            var averageDurationInSeconds = totalGames > 0 ? totalDurationInSeconds / totalGames : 0;

            var averageDurationTimespan = TimeSpan.FromSeconds(averageDurationInSeconds);
            var formattedAverageDuration = averageDurationTimespan.ToString(@"mm\:ss");
            var totalWeightedDpm = statistics.Sum(s => ParseDpm(s.Dpm) * DurationToSeconds(s.DurationAvg) * s.Game);

            var averageDpm = totalDurationInSeconds > 0 ? totalWeightedDpm / totalDurationInSeconds : 0;


            var totalStat = new GameStats
            {
                Champ = "Total",
                Game = statistics.Sum(s => s.Game),
                WinRate = $"{totalWinRate:F1}%",
                Win = statistics.Sum(s => s.Win),
                Loose = statistics.Sum(s => s.Loose),
                FirstBlood = statistics.Sum(s => s.FirstBlood),
                Kda = totalkda,
                Kill = statistics.Sum(s => s.Kill),
                Death = statistics.Sum(s => s.Death),
                Assist = statistics.Sum(s => s.Assist),
                DoubleKill = statistics.Sum(s => s.DoubleKill),
                TripleKill = statistics.Sum(s => s.TripleKill),
                QuadraKill = statistics.Sum(s => s.QuadraKill),
                PentaKill = statistics.Sum(s => s.PentaKill),
                Dpm = averageDpm.ToString("F2"),
                DurationAvg = formattedAverageDuration
                //AllInPings = statistics.Sum(s => s.AllInPings),
                //AssistMePings = statistics.Sum(s => s.AssistMePings),
                //BaitPings = statistics.Sum(s => s.BaitPings),
                //BasicPings = statistics.Sum(s => s.BasicPings),
                //CommandPings = statistics.Sum(s => s.CommandPings),
                //DangerPings = statistics.Sum(s => s.DangerPings),
                //EnemyMissingPings = statistics.Sum(s => s.EnemyMissingPings),
                //EnemyVisionPings = statistics.Sum(s => s.EnemyVisionPings),
                //GetBackPings = statistics.Sum(s => s.GetBackPings),
                //HoldPings = statistics.Sum(s => s.HoldPings),
                //NeedVisionPings = statistics.Sum(s => s.NeedVisionPings),
                //OnMyWayPings = statistics.Sum(s => s.OnMyWayPings),
                //VisionClearedPings = statistics.Sum(s => s.VisionClearedPings),
                //PushPings = statistics.Sum(s => s.PushPings)
            };
            statisticsSorted.Add(totalStat);

            const int nombreColonnes = 16;

            void ApplyBorderStyle(ExcelRange range)
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            }

            void ApplyStyleFill(ExcelRange range, Color couleur)
            {
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(couleur);
            }

            using (var package = new ExcelPackage(new FileInfo(pathFile)))
            {
                var worksheet = package.Workbook.Worksheets["Statistiques"] ??
                                package.Workbook.Worksheets.Add("Statistiques");
                worksheet.Cells.Clear();
                worksheet.Cells.LoadFromCollection(statisticsSorted, true);
                var rowCount = worksheet.Dimension.Rows - 1;

                var baseColor = Color.White;
                var alternateColor = ColorTranslator.FromHtml("#ddf2f0");

                for (var row = 1; row <= rowCount + 1; row++)
                {
                    using (var range = worksheet.Cells[row, 1, row, nombreColonnes])
                    {
                        if (row == 1)
                        {
                            ApplyBorderStyle(range);
                            ApplyStyleFill(range, ColorTranslator.FromHtml("#26a69a"));
                            range.Style.Font.Bold = true;
                            range.Style.Font.Color.SetColor(Color.White);
                            continue;
                        }

                        var isRowEmpty = range.Any(c => !string.IsNullOrWhiteSpace(c.Text));
                        if (!isRowEmpty)
                        {
                            continue;
                        }

                        ApplyBorderStyle(range);
                        var color = row % 2 == 0 ? baseColor : alternateColor;
                        ApplyStyleFill(range, color);
                        range.Style.Font.Size = 12;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }
                }

                using (var totalRange = worksheet.Cells[rowCount + 1, 1, rowCount + 1, nombreColonnes])
                {
                    ApplyBorderStyle(totalRange);
                    ApplyStyleFill(totalRange,
                        ColorTranslator.FromHtml("#8cd3cd"));
                    totalRange.Style.Font.Color.SetColor(Color.White);
                    totalRange.Style.Font.Bold = true;
                }

                worksheet.Cells.AutoFitColumns();
                await package.SaveAsync();
            }

            message += "\nFinish";

            await File.WriteAllLinesAsync(fileSpect, gamesalreadyplayed);
            var todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            await File.WriteAllTextAsync(pathFileDate, todayDate);
            using (var fileStream = File.OpenRead(pathFile))
            {
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);

                memoryStream.Position = 0;
                var fileAttachment = new FileAttachment(memoryStream, $"{username}.xlsx");
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = message;
                    properties.Attachments = new[] { fileAttachment };
                    properties.Flags = MessageFlags.None;
                });
                await memoryStream.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = ex.ToString();
            });
        }
    }
    private class GameStats
    {
        public string Champ { get; set; }
        public int Game { get; set; }
        public string WinRate { get; set; }
        public int Win { get; set; }
        public int Loose { get; set; }
        public int FirstBlood { get; set; }
        public double Kda { get; set; }
        public int Kill { get; set; }
        public int Death { get; set; }
        public int Assist { get; set; }
        public int DoubleKill { get; set; }
        public int TripleKill { get; set; }
        public int QuadraKill { get; set; }
        public int PentaKill { get; set; }
        public string Dpm { get; set; }
        public string DurationAvg { get; set; }

        //public int? AllInPings { get; set; }
        //public int? AssistMePings { get; set; }
        //public int? BaitPings { get; set; }
        //public int? BasicPings { get; set; }
        //public int? CommandPings { get; set; }
        //public int? DangerPings { get; set; }
        //public int? EnemyMissingPings { get; set; }
        //public int? EnemyVisionPings { get; set; }
        //public int? GetBackPings { get; set; }
        //public int? HoldPings { get; set; }
        //public int? NeedVisionPings { get; set; }
        //public int? OnMyWayPings { get; set; }
        //public int? VisionClearedPings { get; set; }
        //public int? PushPings { get; set; }
    }

    [SlashCommand("lg", "Check les stats de la game en cours")]
    public async Task LiveGame(string text, string tagline)
    {
        try
        {
            var summs = await Api.AccountV1().GetByRiotIdAsync(RegionalRoute.EUROPE, text, tagline);
            var puuid = await Api.SummonerV4().GetByPUUIDAsync(PlatformRoute.EUW1, summs.Puuid);
            var spect = await Api.SpectatorV5().GetCurrentGameInfoByPuuidAsync(PlatformRoute.EUW1, puuid.Id);

            var embed = new EmbedBuilder
            {
                Title = $"{puuid.AccountId}'s Live Game",
                Color = Discord.Color.Blue
            };

            var playerteam1 = new StringBuilder();
            var playerteam2 = new StringBuilder();
            string gamemode = null;
            if (spect.GameMode is GameMode.ARAM or GameMode.CLASSIC)
            {
                gamemode = spect.GameMode.ToString();
                foreach (var sp in spect.Participants)

                    if (sp.TeamId == Team.Blue)
                        playerteam1.AppendLine(sp.SummonerId + " - " + GetDescription(sp.ChampionId));

                    else if (sp.TeamId == Team.Red)
                        playerteam2.AppendLine(sp.SummonerId + " - " + GetDescription(sp.ChampionId));
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