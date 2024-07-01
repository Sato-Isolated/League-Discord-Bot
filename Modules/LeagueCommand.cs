using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.MatchV5;
using Camille.RiotGames.SummonerV4;
using Discord;
using Discord.Interactions;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Style;
using Serilog;
using Color = System.Drawing.Color;
using Team = Camille.RiotGames.Enums.Team;

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

    private static string GenerateProgressBar(double percentage)
    {
        if (percentage < 0)
        {
            percentage = 0;
        }
        else if (percentage > 100)
        {
            percentage = 100;
        }

        const int progressBarWidth = 20;
        var completedWidth = Math.Max(0, (int)(progressBarWidth * percentage / 100));
        var remainingWidth = progressBarWidth - completedWidth;

        return "[" + new string('█', completedWidth) + new string('░', remainingWidth) + "] " +
               percentage.ToString("F2") + "%";
    }

    [SlashCommand("makeexcel", "Check les stats ranked")]
    public async Task MakeExcel(string username, string tagline, bool checkOffline)
    {
        try
        {
            Log.Information("Starting MakeExcel for user {Username} with tagline {Tagline}, checkOffline: {CheckOffline}", username, tagline, checkOffline);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            await RespondAsync("Préparation", ephemeral: true);

            Log.Debug("Fetching account information for user {Username}", username);
            var summs = await Api.AccountV1().GetByRiotIdAsync(RegionalRoute.EUROPE, username, tagline);
            Log.Debug("Account information fetched for user {Username}", username);
            var puuid = await Api.SummonerV4().GetByPUUIDAsync(PlatformRoute.EUW1, summs.Puuid);
            Log.Debug("PUUID fetched for user {Username}: {PUUID}", username, puuid.Puuid);

            var dossierBase = Path.Combine("Game", username);
            Directory.CreateDirectory(dossierBase);
            Log.Debug("Created directory {Directory}", dossierBase);
            var cheminFichier = Path.Combine(Environment.CurrentDirectory, $"{username}.xlsm");

            if (checkOffline && File.Exists(cheminFichier))
            {
                Log.Information("Deleting existing Excel file for user {Username} because checkOffline is true", username);
                File.Delete(cheminFichier);
            }

            var nbgame = 0;
            var fichierSpect = Path.Combine(dossierBase, "games.txt");
            var gamesDejaEffectuees = checkOffline ? new List<string>() : (File.Exists(fichierSpect) ? (await File.ReadAllLinesAsync(fichierSpect)).ToList() : new List<string>());
            Log.Debug("Loaded already played games for user {Username}", username);
            var nbentries = 0;
            var alreadycheck = 0;
            string message;

            var cheminFichierDate = Path.Combine(dossierBase, "lastDate.txt");
            DateTime starttime;
            if (checkOffline || !File.Exists(cheminFichierDate))
            {
                starttime = new DateTime(2024, 1, 10); // Default start date
                Log.Debug("No last execution date found or checkOffline is true, using default start date: {DefaultStartDate}", starttime);
            }
            else
            {
                var dateDerniereExecution = await File.ReadAllTextAsync(cheminFichierDate);
                starttime = DateTime.Parse(dateDerniereExecution);
                Log.Debug("Last execution date found: {LastExecutionDate}", starttime);
            }

            var endtimefinal = DateTime.Today;
            var totalDays = (endtimefinal - starttime).TotalDays;
            Log.Debug("Total days to process: {TotalDays}", totalDays);

            var processedMatches = new HashSet<string>(gamesDejaEffectuees);
            var championData = new Dictionary<string, int>();

            using (var package = new ExcelPackage(new FileInfo(cheminFichier)))
            {
                var worksheetMain = package.Workbook.Worksheets["Statistiques"] ?? package.Workbook.Worksheets.Add("Statistiques");
                worksheetMain.Cells.Clear();
                worksheetMain.Cells[1, 1].Value = "Champion";
                worksheetMain.Cells[1, 2].Value = "Lien";

                while (starttime <= endtimefinal)
                {
                    var endtime = starttime.AddDays(1);
                    Log.Information("Processing data for date range {StartDate} to {EndDate}", starttime, endtime);
                    var starttamp = ((DateTimeOffset)starttime.ToUniversalTime()).ToUnixTimeSeconds();
                    var endtamp = ((DateTimeOffset)endtime.ToUniversalTime()).ToUnixTimeSeconds();

                    string[] leagueentries;
                    if (checkOffline)
                    {
                        var allFiles = Directory.GetFiles(dossierBase, "*.json").Select(Path.GetFileNameWithoutExtension).ToArray();
                        leagueentries = allFiles.Where(file => !processedMatches.Contains(file)).ToArray();
                    }
                    else
                    {
                        leagueentries = await Api.MatchV5().GetMatchIdsByPUUIDAsync(RegionalRoute.EUROPE, puuid.Puuid, 100, endtamp, Queue.HOWLING_ABYSS_5V5_ARAM, starttamp);
                    }

                    Log.Debug("Fetched {MatchCount} matches for date range {StartDate} to {EndDate}", leagueentries.Length, starttime, endtime);
                    nbentries += leagueentries.Length;
                    var remainingDays = (endtimefinal - starttime).TotalDays;

                    var progressPercentage = Math.Max(0, 100 - (remainingDays / (double)totalDays * 100));
                    var progressBar = GenerateProgressBar(progressPercentage);

                    foreach (var spect in leagueentries)
                    {
                        if (!checkOffline)
                        {
                            await Task.Delay(125);
                        }

                        if (processedMatches.Contains(spect))
                        {
                            Log.Debug("Skipping already processed match {MatchId}", spect);
                            continue;
                        }

                        Match match = null;
                        var fichierMatch = Path.Combine(dossierBase, $"{spect}.json");

                        if (File.Exists(fichierMatch))
                        {
                            Log.Information("Using offline match data for match {MatchId}", spect);
                            match = JsonConvert.DeserializeObject<Match>(await File.ReadAllTextAsync(fichierMatch));
                        }

                        if (match == null && !checkOffline)
                        {
                            Log.Information("Fetching match data for match {MatchId}", spect);
                            match = await Api.MatchV5().GetMatchAsync(RegionalRoute.EUROPE, spect);
                        }

                        if (match != null)
                        {
                            if (!File.Exists(fichierMatch))
                            {
                                await File.WriteAllTextAsync(Path.Combine(dossierBase, $"{spect}.json"), JsonConvert.SerializeObject(match));
                            }
                            ProcessMatch(match, puuid, ref nbgame, ref nbentries, ref alreadycheck, progressBar, dossierBase, spect, processedMatches, package, worksheetMain, championData);
                        }
                    }

                    starttime = endtime;
                    Log.Debug("Processed matches for date range {StartDate} to {EndDate}", starttime.AddDays(-1), endtime);
                }

                message = $"Number Of Game {nbgame} / {nbentries}\n\n{GenerateProgressBar(100)}";
                await ModifyOriginalResponseAsync(properties => { properties.Content = message; });
                Log.Information("Completed match processing for user {Username}", username);

                worksheetMain.Cells.AutoFitColumns();

                AddVBA(package);
                await package.SaveAsync();
            }

            message += "\nTerminé";

            if (!checkOffline)
            {
                await File.WriteAllLinesAsync(fichierSpect, processedMatches.ToList());
            }

            var dateDuJour = DateTime.Today.ToString("yyyy-MM-dd");
            await File.WriteAllTextAsync(cheminFichierDate, dateDuJour);
            Log.Information("Written last execution date for user {Username}", username);
            using (var fileStream = File.OpenRead(cheminFichier))
            {
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);

                memoryStream.Position = 0;
                var fileAttachment = new FileAttachment(memoryStream, $"{username}.xlsm");
                await ModifyOriginalResponseAsync(properties =>
                {
                    properties.Content = message;
                    properties.Attachments = new[] { fileAttachment };
                    properties.Flags = MessageFlags.None;
                });
                await memoryStream.DisposeAsync();
                Log.Information("File {FileName} attached and response modified for user {Username}", $"{username}.xlsm", username);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in MakeExcel for user {Username} with tagline {Tagline}", username, tagline);
            await ModifyOriginalResponseAsync(properties =>
            {
                properties.Content = "Le joueur n'a pas fait de ranked" + Environment.NewLine + ex;
            });
        }
    }
    private void ProcessMatch(Match match, Summoner puuid, ref int nbgame, ref int nbentries, ref int alreadycheck, string progressBar, string dossierBase, string spect, HashSet<string> processedMatches, ExcelPackage package, ExcelWorksheet worksheetMain, Dictionary<string, int> championData)
    {
        var participant = match.Info.Participants.Single(x => x.Puuid == puuid.Puuid);

        string message;
        if (participant.GameEndedInEarlySurrender)
        {
            nbentries--;
            processedMatches.Add(spect); // Marquer les correspondances d'abandon anticipé comme traitées
            message = $"Number Of Game {nbgame} / {nbentries}\n\nNumber Of Game AlreadyChecked {alreadycheck++}\n\n{progressBar}";
            ModifyOriginalResponseAsync(properties => { properties.Content = message; }).Wait();
            Log.Information("Match {MatchId} ended in early surrender, skipping.", spect);
            return;
        }

        message = $"Number Of Game {nbgame++} / {nbentries}\n\n{progressBar}";
        ModifyOriginalResponseAsync(properties => { properties.Content = message; }).Wait();
        Log.Information("Processing match {MatchId}", spect);

        var fichierMatch = Path.Combine(dossierBase, $"{spect}.json");
        File.WriteAllTextAsync(fichierMatch, JsonConvert.SerializeObject(match)).Wait();

        var champ = GetDescription(participant.ChampionId);
        var champSheet = package.Workbook.Worksheets[champ] ?? package.Workbook.Worksheets.Add(champ);

        // Check and add the champion to the main statistics sheet
        bool champExists = false;
        int mainRow = worksheetMain.Dimension?.Rows + 1 ?? 2;
        int row;
        for (row = 2; row <= (worksheetMain.Dimension?.Rows ?? 1); row++)
        {
            if (worksheetMain.Cells[row, 1].Text == champ)
            {
                champExists = true;
                break;
            }
        }

        if (!champExists)
        {
            mainRow = worksheetMain.Dimension?.Rows + 1 ?? 2;
            worksheetMain.Cells[mainRow, 1].Value = champ;
            worksheetMain.Cells[mainRow, 2].Hyperlink = new ExcelHyperLink($"'{champ.Replace("'", "''")}'!A1", $"Voir détails de {champ}");
        }

        if (champSheet.Dimension == null)
        {
            champSheet.Cells[1, 1].Value = "Game";
            champSheet.Cells[1, 2].Value = "Win";
            champSheet.Cells[1, 3].Value = "Loose";
            champSheet.Cells[1, 4].Value = "FirstBlood";
            champSheet.Cells[1, 5].Value = "Kda";
            champSheet.Cells[1, 6].Value = "Kill";
            champSheet.Cells[1, 7].Value = "Death";
            champSheet.Cells[1, 8].Value = "Assist";
            champSheet.Cells[1, 9].Value = "DoubleKill";
            champSheet.Cells[1, 10].Value = "TripleKill";
            champSheet.Cells[1, 11].Value = "QuadraKill";
            champSheet.Cells[1, 12].Value = "PentaKill";
            champSheet.Cells[1, 13].Value = "Dpm";
            champSheet.Cells[1, 14].Value = "Durée";
        }

        row = champSheet.Dimension?.Rows + 1 ?? 2;
        var duration = TimeSpan.FromSeconds(match.Info.GameDuration);
        var formattedDuration = FormatDuration(duration);
        var damagePerMinute = participant.Challenges?.DamagePerMinute ?? 0.0;

        champSheet.Cells[row, 1].Value = row - 1; // Game number
        champSheet.Cells[row, 2].Value = participant.Win ? 1 : 0;
        champSheet.Cells[row, 3].Value = participant.Win ? 0 : 1;
        champSheet.Cells[row, 4].Value = participant.FirstBloodKill ? 1 : 0;
        champSheet.Cells[row, 5].Value = CalculateKda(participant.Kills, participant.Deaths, participant.Assists);
        champSheet.Cells[row, 6].Value = participant.Kills;
        champSheet.Cells[row, 7].Value = participant.Deaths;
        champSheet.Cells[row, 8].Value = participant.Assists;
        champSheet.Cells[row, 9].Value = participant.DoubleKills;
        champSheet.Cells[row, 10].Value = participant.TripleKills;
        champSheet.Cells[row, 11].Value = participant.QuadraKills;
        champSheet.Cells[row, 12].Value = participant.PentaKills;
        champSheet.Cells[row, 13].Value = damagePerMinute.ToString("F2");
        champSheet.Cells[row, 14].Value = formattedDuration; // Utiliser la durée exacte du match

        if (!championData.ContainsKey(champ))
        {
            championData[champ] = row;
        }
        else
        {
            championData[champ] = row;
        }

        champSheet.Cells.AutoFitColumns();
        package.Save();

        processedMatches.Add(spect);
        Log.Debug("Added match {MatchId} to already processed games", spect);
    }


    private void AddVBA(ExcelPackage package)
    {
        var workbook = package.Workbook;
        if (workbook.VbaProject == null)
        {
            workbook.CreateVBAProject();
        }

        var vbaProject = workbook.VbaProject;
        var module = vbaProject.Modules.AddModule("GenerateChartsModule");

        string vbaCode = @"
Sub GenerateAllChartsForAllSheets()
    Dim ws As Worksheet
    Dim chart As ChartObject
    Dim lastRow As Long
    Dim cell As Range
    Dim totalWins As Long
    Dim totalLosses As Long
    Dim winRate As Double
    Dim wordArt As Shape
    Dim hyperlinkCell As Range

    ' Loop through all worksheets
    For Each ws In ThisWorkbook.Worksheets
        If ws.Name <> ""Statistiques"" Then
            ' Determine the last row with data in column A
            lastRow = ws.Cells(ws.Rows.Count, ""A"").End(xlUp).Row
            
            ' Ensure DPM column is treated as numbers
            For Each cell In ws.Range(""M2:M"" & lastRow)
                cell.Value = CDbl(cell.Value)
            Next cell
            
            ' Add the Kill chart
            Set chart = ws.ChartObjects.Add(0, 0, 600, 400)
            With chart
                ' Position the chart in columns O to W and rows 0 to 20
                .Left = ws.Cells(1, 15).Left
                .Top = ws.Cells(1, 15).Top
                .Width = ws.Range(""O1:W1"").Width
                .Height = ws.Range(""A1:A20"").Height
                With .Chart
                    .ChartType = xlLine
                    .SetSourceData Source:=Union(ws.Range(""A1:A"" & lastRow), ws.Range(""F1:F"" & lastRow))
                    .HasTitle = True
                    .ChartTitle.Text = ""Kills Metrics""
                    .ChartTitle.Font.Size = 14
                    .ChartTitle.Font.Bold = True
                    .Axes(xlCategory, xlPrimary).HasTitle = True
                    .Axes(xlCategory, xlPrimary).AxisTitle.Text = ""Game Number""
                    .Axes(xlCategory, xlPrimary).AxisTitle.Font.Size = 12
                    .Axes(xlCategory, xlPrimary).AxisTitle.Font.Bold = True
                    .Axes(xlValue, xlPrimary).HasTitle = True
                    .Axes(xlValue, xlPrimary).AxisTitle.Text = ""Kills""
                    .Axes(xlValue, xlPrimary).AxisTitle.Font.Size = 12
                    .Axes(xlValue, xlPrimary).AxisTitle.Font.Bold = True
                    .Axes(xlValue, xlPrimary).MinimumScale = 0 ' Set Y-axis to start from zero
                    .ChartStyle = 236 ' Set the chart style to style number 10
                    With .SeriesCollection(1)
                        .Name = ""Kills""
                        .XValues = ws.Range(""A2:A"" & lastRow) ' Set X-axis values to Game Numbers
                        .Values = ws.Range(""F2:F"" & lastRow) ' Set Y-axis values to Kills
                        .Format.Line.Weight = 2.5
                        .MarkerStyle = xlMarkerStyleCircle
                        .MarkerSize = 8
                    End With
                End With
            End With
            
            ' Add the Death chart
            Set chart = ws.ChartObjects.Add(0, 0, 600, 400)
            With chart
                ' Position the chart in columns X to AF and rows 0 to 20
                .Left = ws.Cells(1, 24).Left
                .Top = ws.Cells(1, 24).Top
                .Width = ws.Range(""X1:AF1"").Width
                .Height = ws.Range(""A1:A20"").Height
                With .Chart
                    .ChartType = xlLine
                    .SetSourceData Source:=Union(ws.Range(""A1:A"" & lastRow), ws.Range(""G1:G"" & lastRow))
                    .HasTitle = True
                    .ChartTitle.Text = ""Deaths Metrics""
                    .ChartTitle.Font.Size = 14
                    .ChartTitle.Font.Bold = True
                    .Axes(xlCategory, xlPrimary).HasTitle = True
                    .Axes(xlCategory, xlPrimary).AxisTitle.Text = ""Game Number""
                    .Axes(xlCategory, xlPrimary).AxisTitle.Font.Size = 12
                    .Axes(xlCategory, xlPrimary).AxisTitle.Font.Bold = True
                    .Axes(xlValue, xlPrimary).HasTitle = True
                    .Axes(xlValue, xlPrimary).AxisTitle.Text = ""Deaths""
                    .Axes(xlValue, xlPrimary).AxisTitle.Font.Size = 12
                    .Axes(xlValue, xlPrimary).AxisTitle.Font.Bold = True
                    .Axes(xlValue, xlPrimary).MinimumScale = 0 ' Set Y-axis to start from zero
                    .ChartStyle = 236 ' Set the chart style to style number 10
                    With .SeriesCollection(1)
                        .Name = ""Deaths""
                        .XValues = ws.Range(""A2:A"" & lastRow) ' Set X-axis values to Game Numbers
                        .Values = ws.Range(""G2:G"" & lastRow) ' Set Y-axis values to Deaths
                        .Format.Line.Weight = 2.5
                        .MarkerStyle = xlMarkerStyleCircle
                        .MarkerSize = 8
                    End With
                End With
            End With
            
            ' Add the Assist chart
            Set chart = ws.ChartObjects.Add(0, 0, 600, 400)
            With chart
                ' Position the chart in columns O to W and rows 21 to 41
                .Left = ws.Cells(21, 15).Left
                .Top = ws.Cells(21, 15).Top
                .Width = ws.Range(""O21:W21"").Width
                .Height = ws.Range(""A21:A41"").Height
                With .Chart
                    .ChartType = xlLine
                    .SetSourceData Source:=Union(ws.Range(""A1:A"" & lastRow), ws.Range(""H1:H"" & lastRow))
                    .HasTitle = True
                    .ChartTitle.Text = ""Assists Metrics""
                    .ChartTitle.Font.Size = 14
                    .ChartTitle.Font.Bold = True
                    .Axes(xlCategory, xlPrimary).HasTitle = True
                    .Axes(xlCategory, xlPrimary).AxisTitle.Text = ""Game Number""
                    .Axes(xlCategory, xlPrimary).AxisTitle.Font.Size = 12
                    .Axes(xlCategory, xlPrimary).AxisTitle.Font.Bold = True
                    .Axes(xlValue, xlPrimary).HasTitle = True
                    .Axes(xlValue, xlPrimary).AxisTitle.Text = ""Assists""
                    .Axes(xlValue, xlPrimary).AxisTitle.Font.Size = 12
                    .Axes(xlValue, xlPrimary).AxisTitle.Font.Bold = True
                    .Axes(xlValue, xlPrimary).MinimumScale = 0 ' Set Y-axis to start from zero
                    .ChartStyle = 236 ' Set the chart style to style number 10
                    With .SeriesCollection(1)
                        .Name = ""Assists""
                        .XValues = ws.Range(""A2:A"" & lastRow) ' Set X-axis values to Game Numbers
                        .Values = ws.Range(""H2:H"" & lastRow) ' Set Y-axis values to Assists
                        .Format.Line.Weight = 2.5
                        .MarkerStyle = xlMarkerStyleCircle
                        .MarkerSize = 8
                    End With
                End With
            End With
            
            ' Add the DPM chart
            Set chart = ws.ChartObjects.Add(0, 0, 600, 400)
            With chart
                ' Position the chart in columns X to AF and rows 21 to 41
                .Left = ws.Cells(21, 24).Left
                .Top = ws.Cells(21, 24).Top
                .Width = ws.Range(""X21:AF21"").Width
                .Height = ws.Range(""A21:A41"").Height
                With .Chart
                    .ChartType = xlLine
                    .SetSourceData Source:=Union(ws.Range(""A1:A"" & lastRow), ws.Range(""M1:M"" & lastRow))
                    .HasTitle = True
                    .ChartTitle.Text = ""DPM Metrics""
                    .ChartTitle.Font.Size = 14
                    .ChartTitle.Font.Bold = True
                    .Axes(xlCategory, xlPrimary).HasTitle = True
                    .Axes(xlCategory, xlPrimary).AxisTitle.Text = ""Game Number""
                    .Axes(xlCategory, xlPrimary).AxisTitle.Font.Size = 12
                    .Axes(xlCategory, xlPrimary).AxisTitle.Font.Bold = True
                    .Axes(xlValue, xlPrimary).HasTitle = True
                    .Axes(xlValue, xlPrimary).AxisTitle.Text = ""DPM""
                    .Axes(xlValue, xlPrimary).AxisTitle.Font.Size = 12
                    .Axes(xlValue, xlPrimary).AxisTitle.Font.Bold = True
                    .Axes(xlValue, xlPrimary).MinimumScale = 0 ' Set Y-axis to start from zero
                    .ChartStyle = 236 ' Set the chart style to style number 10
                    With .SeriesCollection(1)
                        .Name = ""DPM""
                        .XValues = ws.Range(""A2:A"" & lastRow) ' Set X-axis values to Game Numbers
                        .Values = ws.Range(""M2:M"" & lastRow) ' Set Y-axis values to DPM
                        .Format.Line.Weight = 2.5
                        .MarkerStyle = xlMarkerStyleCircle
                        .MarkerSize = 8
                    End With
                End With
            End With
            
            ' Calculate total wins and losses
            totalWins = Application.WorksheetFunction.Sum(ws.Range(""B2:B"" & lastRow))
            totalLosses = Application.WorksheetFunction.Sum(ws.Range(""C2:C"" & lastRow))
            
            ' Calculate win rate
            If totalWins + totalLosses > 0 Then
                winRate = (totalWins / (totalWins + totalLosses)) * 100
            Else
                winRate = 0
            End If
            
            ' Add the Win Rate WordArt
            Set wordArt = ws.Shapes.AddTextEffect(msoTextEffect1, ""Winrate: "" & Format(winRate, ""0.00"") & ""%"", ""Arial Black"", 36, msoFalse, msoFalse, ws.Cells(42, 15).Left, ws.Cells(42, 15).Top)
            With wordArt
                .Height = ws.Range(""A42:A62"").Height
                .Width = ws.Range(""O42:W42"").Width
                .TextFrame2.TextRange.Font.Fill.ForeColor.RGB = RGB(0, 102, 204)
                .TextFrame2.TextRange.Font.Bold = msoTrue
                .LockAspectRatio = msoFalse
            End With
            
            ' Add hyperlink to ""Statistiques"" sheet in cell X42
            Set hyperlinkCell = ws.Cells(42, 24)
            ws.Hyperlinks.Add Anchor:=hyperlinkCell, Address:="""", SubAddress:=""Statistiques!A1"", TextToDisplay:=""Go to Statistiques""
            With hyperlinkCell.Font
                .Size = 14
                .Bold = True
                .Color = RGB(0, 102, 204)
            End With
        End If
    Next ws
End Sub



Sub ApplyDarkTheme()
    Dim ws As Worksheet
    Dim rng As Range
    Dim lastRow As Long
    Dim lastCol As Long
    Dim bufferRows As Long
    Dim bufferCols As Long
    
    bufferRows = 67
    bufferCols = 33
    
    For Each ws In ThisWorkbook.Worksheets
        lastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row
        lastCol = ws.Cells(1, ws.Columns.Count).End(xlToLeft).Column

        Set rng = ws.Range(ws.Cells(1, 1), ws.Cells(WorksheetFunction.Min(lastRow + bufferRows, 1000), WorksheetFunction.Min(lastCol + bufferCols, 50)))
        With rng.Interior
            .Pattern = xlSolid
            .Color = RGB(43, 43, 43)
        End With
        With rng.Font
            .Color = RGB(220, 220, 220)
        End With
        With rng.Borders(xlEdgeLeft)
            .LineStyle = xlContinuous
            .Color = RGB(0, 0, 0)
        End With
        With rng.Borders(xlEdgeTop)
            .LineStyle = xlContinuous
            .Color = RGB(0, 0, 0)
        End With
        With rng.Borders(xlEdgeBottom)
            .LineStyle = xlContinuous
            .Color = RGB(0, 0, 0)
        End With
        With rng.Borders(xlEdgeRight)
            .LineStyle = xlContinuous
            .Color = RGB(0, 0, 0)
        End With

        Set rng = ws.Range(ws.Cells(1, 1), ws.Cells(1, lastCol))
        With rng.Interior
            .Pattern = xlSolid
            .Color = RGB(55, 55, 55)
        End With
        With rng.Font
            .Color = RGB(255, 255, 255)
            .Bold = True
        End With
    Next ws
End Sub


";
        module.Code = vbaCode;

        workbook.CodeModule.Code = @"

Private Sub Workbook_SheetActivate(ByVal Sh As Object)
    ActiveWindow.Zoom = 85
End Sub

Private Sub Workbook_Open()
    Call ApplyDarkTheme
    Call GenerateAllChartsForAllSheets
End Sub
";
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