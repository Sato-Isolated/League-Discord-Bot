using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.Enums;
using Discord;
using Discord.Interactions;

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