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
                //   ApplyDarkThemeToAllWorksheets(package);

                // Add charts to each champion sheet
                AddChartsToChampionSheets(package, championData);

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

    private void AddChartsToChampionSheets(ExcelPackage package, Dictionary<string, int> championData)
    {
        foreach (var champ in championData.Keys)
        {
            var champSheet = package.Workbook.Worksheets[champ];
            if (champSheet != null)
            {
                var lastRow = championData[champ];
                if (lastRow > 1) // Ensure there are at least two rows of data to plot
                {
                    AddChartToSheet(champSheet, lastRow);
                }
            }
        }

        // Add VBA code
        AddVBA(package);
    }


    private void AddChartToSheet(ExcelWorksheet sheet, int lastRow)
    {
        var chart = sheet.Drawings.AddChart("PerformanceChart", eChartType.Line);
        chart.Title.Text = "Performance Metrics";
        chart.SetPosition(1, 0, 16, 0); // Position chart to the right of the data
        chart.SetSize(800, 400); // Size of the chart

        // Customize chart area
        chart.Border.LineStyle = eLineStyle.Solid;
        chart.Border.Fill.Color = Color.Gray;
        chart.Fill.Color = Color.FromArgb(240, 240, 240);

        // Customize plot area
        chart.PlotArea.Border.LineStyle = eLineStyle.Solid;
        chart.PlotArea.Border.Fill.Color = Color.Gray;
        chart.PlotArea.Fill.Color = Color.FromArgb(220, 220, 220);

        // Customize title
        chart.Title.Font.Size = 14;
        chart.Title.Font.Bold = true;
        chart.Title.Font.Color = Color.DarkBlue;

        // Customize axes
        chart.XAxis.Title.Text = "Game Number";
        chart.XAxis.Title.Font.Size = 12;
        chart.XAxis.Title.Font.Bold = true;
        chart.XAxis.Title.Font.Color = Color.DarkBlue;
        chart.YAxis.Title.Text = "Count";
        chart.YAxis.Title.Font.Size = 12;
        chart.YAxis.Title.Font.Bold = true;
        chart.YAxis.Title.Font.Color = Color.DarkBlue;

        // Add data series with improved visuals
        var winLossSeries = chart.Series.Add(sheet.Cells[2, 2, lastRow, 2], sheet.Cells[2, 1, lastRow, 1]); // Win/Loss data
        winLossSeries.Header = "Win/Loss";
        winLossSeries.Border.LineStyle = eLineStyle.Solid;
        winLossSeries.Border.Fill.Color = Color.Blue;
        winLossSeries.Border.Width = 2;

        var killSeries = chart.Series.Add(sheet.Cells[2, 6, lastRow, 6], sheet.Cells[2, 1, lastRow, 1]); // Kill data
        killSeries.Header = "Kills";
        killSeries.Border.LineStyle = eLineStyle.Solid;
        killSeries.Border.Fill.Color = Color.Green;
        killSeries.Border.Width = 2;

        var deathSeries = chart.Series.Add(sheet.Cells[2, 7, lastRow, 7], sheet.Cells[2, 1, lastRow, 1]); // Death data
        deathSeries.Header = "Deaths";
        deathSeries.Border.LineStyle = eLineStyle.Solid;
        deathSeries.Border.Fill.Color = Color.Red;
        deathSeries.Border.Width = 2;

        var assistSeries = chart.Series.Add(sheet.Cells[2, 8, lastRow, 8], sheet.Cells[2, 1, lastRow, 1]); // Assist data
        assistSeries.Header = "Assists";
        assistSeries.Border.LineStyle = eLineStyle.Solid;
        assistSeries.Border.Fill.Color = Color.Purple;
        assistSeries.Border.Width = 2;

        // Add a legend
        chart.Legend.Position = eLegendPosition.Bottom;
        chart.Legend.Border.LineStyle = eLineStyle.Solid;
        chart.Legend.Border.Fill.Color = Color.Gray;
        chart.Legend.Font.Size = 10;

        // Adjust the chart style for better visibility
        chart.Style = eChartStyle.Style26;
        chart.DisplayBlanksAs = eDisplayBlanksAs.Gap;
        chart.YAxis.MinValue = 0;

        // Format the gridlines
        chart.XAxis.MajorGridlines.Fill.Color = Color.LightGray;
        chart.YAxis.MajorGridlines.Fill.Color = Color.LightGray;
    }


    private void AddVBA(ExcelPackage package)
    {
        var workbook = package.Workbook;
        if (workbook.VbaProject == null)
        {
            workbook.CreateVBAProject();
        }

        var vbaProject = workbook.VbaProject;
        var module = vbaProject.Modules.AddModule("ApplyDarkThemeModule");

        // VBA code to apply dark theme and customize charts
        string vbaCode = @"
Sub ApplyDarkTheme()
    Dim ws As Worksheet
    Dim rng As Range
    Dim lastRow As Long
    Dim lastCol As Long
    Dim bufferRows As Long
    Dim bufferCols As Long
    
    bufferRows = 50
    bufferCols = 20
    
    For Each ws In ThisWorkbook.Worksheets
        lastRow = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row
        lastCol = ws.Cells(1, ws.Columns.Count).End(xlToLeft).Column

        ' Apply dark theme to the range
        Set rng = ws.Range(ws.Cells(1, 1), ws.Cells(WorksheetFunction.Min(lastRow + bufferRows, 1000), WorksheetFunction.Min(lastCol + bufferCols, 50)))
        With rng.Interior
            .Pattern = xlSolid
            .Color = RGB(43, 43, 43) ' Dark gray background
        End With
        With rng.Font
            .Color = RGB(220, 220, 220) ' Light gray font
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

        ' Apply dark theme to header row
        Set rng = ws.Range(ws.Cells(1, 1), ws.Cells(1, lastCol))
        With rng.Interior
            .Pattern = xlSolid
            .Color = RGB(55, 55, 55) ' Slightly lighter gray for header
        End With
        With rng.Font
            .Color = RGB(255, 255, 255) ' White font
            .Bold = True
        End With
    Next ws
End Sub

Sub CustomizeCharts()
    Dim ws As Worksheet
    Dim ch As ChartObject
    For Each ws In ThisWorkbook.Worksheets
        For Each ch In ws.ChartObjects
            With ch.Chart
                .ChartStyle = 201 ' Custom style for better visuals
                .Axes(xlCategory).MajorGridlines.Format.Line.ForeColor.RGB = RGB(200, 200, 200)
                .Axes(xlValue).MajorGridlines.Format.Line.ForeColor.RGB = RGB(200, 200, 200)
                .SetElement (msoElementChartTitleCenteredOverlay)
                .Axes(xlCategory).HasTitle = True
                .Axes(xlCategory).AxisTitle.Text = ""Game Number""
                .Axes(xlValue).HasTitle = True
                .Axes(xlValue).AxisTitle.Text = ""Count""
                
                ' Customize the series
                With .SeriesCollection(1)
                    .Format.Line.ForeColor.RGB = RGB(0, 112, 192) ' Change line color
                    .MarkerStyle = xlMarkerStyleCircle
                    .MarkerSize = 8
                    .MarkerBackgroundColor = RGB(0, 112, 192)
                    .MarkerForegroundColor = RGB(255, 255, 255)
                    .HasTrendlines = True
                    .Trendlines(1).Type = xlMovingAvg
                    .Trendlines(1).Period = 2
                End With
                
                With .SeriesCollection(2)
                    .Format.Line.ForeColor.RGB = RGB(0, 176, 80) ' Change line color
                    .MarkerStyle = xlMarkerStyleSquare
                    .MarkerSize = 8
                    .MarkerBackgroundColor = RGB(0, 176, 80)
                    .MarkerForegroundColor = RGB(255, 255, 255)
                    .HasTrendlines = True
                    .Trendlines(1).Type = xlExponential
                End With
                
                ' Add more series customization if needed
                
            End With
        Next ch
    Next ws
End Sub

";
        module.Code = vbaCode;

        // Add event to run VBA on workbook open
        workbook.CodeModule.Code = @"
Private Sub Workbook_Open()
    Call ApplyDarkTheme
    Call CustomizeCharts
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