using System.Text;
using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.Enums;
using Discord;
using Discord.Interactions;

namespace League_Discord_Bot.Modules;

public class LeagueModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly Dictionary<Tier, string> RankEmblems = new()
    {
        { Tier.IRON, "https://cdn.discordapp.com/attachments/787493493927968790/791377379497869353/Emblem_Iron.png" },
        { Tier.BRONZE, "https://cdn.discordapp.com/attachments/787493493927968790/791377369566281748/Emblem_Bronze.png" },
        { Tier.SILVER, "https://cdn.discordapp.com/attachments/787493493927968790/791376720967368724/Emblem_Silver.png" },
        { Tier.GOLD, "https://cdn.discordapp.com/attachments/787493493927968790/791377378962047036/Emblem_Gold.png" },
        { Tier.PLATINUM, "https://cdn.discordapp.com/attachments/787493493927968790/791377382132547584/Emblem_Platinum.png" },
        { Tier.DIAMOND, "https://cdn.discordapp.com/attachments/787493493927968790/791377375622725652/Emblem_Diamond.png" },
        { Tier.MASTER, "https://cdn.discordapp.com/attachments/787493493927968790/791377381713248317/Emblem_Master.png" },
        { Tier.GRANDMASTER, "https://cdn.discordapp.com/attachments/787493493927968790/791377379607445564/Emblem_Grandmaster.png" },
        { Tier.CHALLENGER, "https://cdn.discordapp.com/attachments/787493493927968790/791377373044015154/Emblem_Challenger.png" }
    };
    private static readonly RiotGamesApi Api = RiotGamesApi.NewInstance("RGAPI-0000000000-000000-000000-000000-000000000000");//https://developer.riotgames.com

    private InteractionHandler _handler;
    
    public LeagueModule(InteractionHandler handler)
    {
        _handler = handler;
    }
    
    public InteractionService Commands { get; set; }


    [SlashCommand("rank", "Check les stats ranked")]
    public async Task RankCommand(string name)
    {
        try
        {
            var summs = await Api.SummonerV4().GetBySummonerNameAsync(PlatformRoute.EUW1, name);
            var leagueentries = await Api.LeagueV4().GetLeagueEntriesForSummonerAsync(PlatformRoute.EUW1, summs.Id);
            var solo = leagueentries.Single(x => x.QueueType == QueueType.RANKED_SOLO_5x5);
            var numOfGames = solo.Wins + solo.Losses;
            var winRate = solo.Wins / (float)numOfGames * 100;
            var rank = RankEmblems.GetValueOrDefault(solo.Tier.Value, "Unranked");

            var embed = new EmbedBuilder
            {
                Title = $"{summs.Name}'s Ranked Stats",
                Color = Color.Blue,
                ThumbnailUrl = rank
            };
            embed.AddField("Rank", $"{solo.Tier} {solo.Rank}\n {solo.LeaguePoints} LP", true)
                .AddField("Stats", $"**Wins:** {solo.Wins}\n**Losses**: {solo.Losses}\n**Win Rate:** {winRate}%", true);

            await RespondAsync(embed: embed.Build());
        }
        catch (Exception)
        {
            await RespondAsync("Le joueur n'a pas fait de ranked", ephemeral: true);
        }
    }


    [SlashCommand("lg", "Check les stats de la game en cours")]
    public async Task LiveGame(string name)
    {
        var summs = await Api.SummonerV4().GetBySummonerNameAsync(PlatformRoute.EUW1, name);
        var spect = await Api.SpectatorV4().GetCurrentGameInfoBySummonerAsync(PlatformRoute.EUW1, summs.Id);

        var embed = new EmbedBuilder
        {
            Title = $"{summs.Name}'s Live Game",
            Color = Color.Blue
        };

        var playerteam1 = new StringBuilder();
        var playerteam2 = new StringBuilder();
        string Gamemode = null;
        if (spect.GameMode is GameMode.ARAM or GameMode.CLASSIC)
        {
            Gamemode = spect.GameMode.ToString();
            foreach (var sp in spect.Participants)

                if (sp.TeamId == Team.Blue)
                    playerteam1.AppendLine(sp.SummonerName + " " + (ChampEnumName)sp.ChampionId);

                else if (sp.TeamId == Team.Red)
                    playerteam2.AppendLine(sp.SummonerName + " " + (ChampEnumName)sp.ChampionId);
        }

        embed.AddField("GameMode", Gamemode)
            .AddField("Blue Team", playerteam1.ToString(), true)
            .AddField("Red Team", playerteam2.ToString(), true);

        await RespondAsync(embed: embed.Build());
    }

    static double CalculateDifference(double num1, double num2) => Math.Abs(num1 - num2);

    [SlashCommand("stalk", "Stalk une personne")]
    public async Task Stalking(string name)
    {
        try
        {
            var summs = await Api.SummonerV4().GetBySummonerNameAsync(PlatformRoute.EUW1, name);
            var spect = await Api.SpectatorV4().GetCurrentGameInfoBySummonerAsync(PlatformRoute.EUW1, summs.Id);
            var leagueentries = await Api.LeagueV4().GetLeagueEntriesForSummonerAsync(PlatformRoute.EUW1, summs.Id);
            var LeagueEntry = leagueentries.Single(x => x.QueueType == QueueType.RANKED_SOLO_5x5);
            var lp = LeagueEntry.LeaguePoints;

            var rank = RankEmblems.GetValueOrDefault(LeagueEntry.Tier.Value, "Unranked");
            await RespondAsync($"Lancement du stalking de {name}", ephemeral: true);


            while (true)
            {
                if (spect != null)
                {
                    var match = await Api.MatchV5().GetMatchAsync(RegionalRoute.EUROPE, "EUW1_" + spect.GameId);
                    if (match is not null)
                    {
                        var participant = match.Info.Participants.Single(x => x.SummonerName == name);
                        var winloose = participant.Win;
                        var cs = participant.TotalMinionsKilled;
                        var gold = participant.GoldEarned;
                        var kill = participant.Kills;
                        var death = participant.Deaths;
                        var assist = participant.Assists;
                        var role = participant.Role;
                        var champ = participant.ChampionName;
                        var damage = participant.TotalDamageDealtToChampions;
                        var wards = participant.WardsPlaced;
                        var duration = TimeSpan.FromSeconds(match.Info.GameDuration);
                        var dpm = (double)damage / duration.Minutes;
                        var gametime = duration.Minutes + " minutes " + duration.Seconds + " secondes";
                        var resultat = winloose ? "gagner" : "perdre";
                        var deathtimer = TimeSpan.FromSeconds(participant.TotalTimeSpentDead);
                        Color color;
                        var LpAfterMatch =
                            await Api.LeagueV4().GetLeagueEntriesForSummonerAsync(PlatformRoute.EUW1, summs.Id);
                        var RefreshedLeaguePoint = LpAfterMatch.Single(x => x.QueueType == QueueType.RANKED_SOLO_5x5);
                        var leaguePoints = RefreshedLeaguePoint.LeaguePoints;

                        Console.WriteLine("leaguePoints " + leaguePoints);
                        string lea;
                        if (winloose)
                        {
                            color = Color.Green;
                            lea = $"+ {CalculateDifference(lp, leaguePoints)}";
                        }
                        else
                        {
                            color = Color.Red;
                            lea = $"- {CalculateDifference(lp, leaguePoints)}";
                        }

                        if (role == "CARRY") role = "ADC";

                        var embed = new EmbedBuilder
                        {
                            Title =
                                $"{summs.Name} vient de {resultat} une partie en {role}{Environment.NewLine}avec {champ}",
                            Color = color,
                            ThumbnailUrl = rank,
                            Description =
                                $"{summs.Name} est actuellement {RefreshedLeaguePoint.Tier} {RefreshedLeaguePoint.Rank} {leaguePoints} LP ({lea})"
                        };

                        var kda = (kill + assist) / (double)death;
                        embed.AddField("Kill", kill, true)
                            .AddField("Death", death, true)
                            .AddField("Assist", assist, true)
                            .AddField("KDA", kda.ToString("F2"), true)
                            .AddField("Cs", cs, true)
                            .AddField("Gold", gold, true)
                            .AddField("Game Time", gametime, true)
                            .AddField("Death Timer",
                                deathtimer.Minutes + " minutes " + deathtimer.Seconds + " secondes", true)
                            .AddField("DPM", dpm.ToString("F2"), true)
                            .AddField("Wards", wards, true);

                        await ReplyAsync(embed: embed.Build());
                        break;
                    }
                }

                Thread.Sleep(15000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public enum ChampEnumName
    {
        Aatrox = 266,
        Ahri = 103,
        Akali = 84,
        Akshan = 166,
        Alistar = 12,
        Amumu = 32,
        Anivia = 34,
        Annie = 1,
        Aphelios = 523,
        Ashe = 22,
        AurelionSol = 136,
        Azir = 268,
        Bard = 432,
        BelVeth = 200,
        Blitzcrank = 53,
        Brand = 63,
        Braum = 201,
        Caitlyn = 51,
        Camille = 164,
        Cassiopeia = 69,
        ChoGath = 31,
        Corki = 42,
        Darius = 122,
        Diana = 131,
        Draven = 119,
        DrMundo = 36,
        Ekko = 245,
        Elise = 60,
        Evelynn = 28,
        Ezreal = 81,
        Fiddlesticks = 9,
        Fiora = 114,
        Fizz = 105,
        Galio = 3,
        Gangplank = 41,
        Garen = 86,
        Gnar = 150,
        Gragas = 79,
        Graves = 104,
        Gwen = 887,
        Hecarim = 120,
        Heimerdinger = 74,
        Illaoi = 420,
        Irelia = 39,
        Ivern = 427,
        Janna = 40,
        JarvanIV = 59,
        Jax = 24,
        Jayce = 126,
        Jhin = 202,
        Jinx = 222,
        KaiSa = 145,
        Kalista = 429,
        Karma = 43,
        Karthus = 30,
        Kassadin = 38,
        Katarina = 55,
        Kayle = 10,
        Kayn = 141,
        Kennen = 85,
        KhaZix = 121,
        Kindred = 203,
        Kled = 240,
        KogMaw = 96,
        KSante = 897,
        LeBlanc = 7,
        LeeSin = 64,
        Leona = 89,
        Lillia = 876,
        Lissandra = 127,
        Lucian = 236,
        Lulu = 117,
        Lux = 99,
        Malphite = 54,
        Malzahar = 90,
        Maokai = 57,
        MasterYi = 11,
        MissFortune = 21,
        Wukong = 62,
        Mordekaiser = 82,
        Morgana = 25,
        Nami = 267,
        Nasus = 75,
        Nautilus = 111,
        Neeko = 518,
        Nidalee = 76,
        Nilah = 895,
        Nocturne = 56,
        NunuAndWillump = 20,
        Olaf = 2,
        Orianna = 61,
        Ornn = 516,
        Pantheon = 80,
        Poppy = 78,
        Pyke = 555,
        Qiyana = 246,
        Quinn = 133,
        Rakan = 497,
        Rammus = 33,
        RekSai = 421,
        Rell = 526,
        RenataGlasc = 888,
        Renekton = 58,
        Rengar = 107,
        Riven = 92,
        Rumble = 68,
        Ryze = 13,
        Samira = 360,
        Sejuani = 113,
        Senna = 235,
        Seraphine = 147,
        Sett = 875,
        Shaco = 35,
        Shen = 98,
        Shyvana = 102,
        Singed = 27,
        Sion = 14,
        Sivir = 15,
        Skarner = 72,
        Sona = 37,
        Soraka = 16,
        Swain = 50,
        Sylas = 517,
        Syndra = 134,
        TahmKench = 223,
        Taliyah = 163,
        Talon = 91,
        Taric = 44,
        Teemo = 17,
        Thresh = 412,
        Tristana = 18,
        Trundle = 48,
        Tryndamere = 23,
        TwistedFate = 4,
        Twitch = 29,
        Udyr = 77,
        Urgot = 6,
        Varus = 110,
        Vayne = 67,
        Veigar = 45,
        VelKoz = 161,
        Vex = 711,
        Vi = 254,
        Viego = 234,
        Viktor = 112,
        Vladimir = 8,
        Volibear = 106,
        Warwick = 19,
        Xayah = 498,
        Xerath = 101,
        XinZhao = 5,
        Yasuo = 157,
        Yone = 777,
        Yorick = 83,
        Yuumi = 350,
        Zac = 154,
        Zed = 238,
        Zeri = 221,
        Ziggs = 115,
        Zilean = 26,
        Zoe = 142,
        Zyra = 143,
    }
}
