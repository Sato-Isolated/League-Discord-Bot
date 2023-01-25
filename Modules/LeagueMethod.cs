using Camille.Enums;
using Discord;
using Discord.Webhook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Camille.RiotGames;

namespace League_Discord_Bot.Modules
{
    internal class LeagueMethod
    {
        public static DiscordWebhookClient Webhook = new DiscordWebhookClient("");
        public static string TempNameRanked;
        public static bool RecursiveRanked = true;
        public static async Task StalkingRanked(string name)
        {
            try
            {
                var summs = await LeagueCommand.Api.SummonerV4().GetBySummonerNameAsync(PlatformRoute.EUW1, name);
                var spect = await LeagueCommand.Api.SpectatorV4()
                    .GetCurrentGameInfoBySummonerAsync(PlatformRoute.EUW1, summs.Id);
                var leagueentries = await LeagueCommand.Api.LeagueV4()
                    .GetLeagueEntriesForSummonerAsync(PlatformRoute.EUW1, summs.Id);
                var LeagueEntry = leagueentries.Single(x => x.QueueType == QueueType.RANKED_SOLO_5x5);
                var lp = LeagueEntry.LeaguePoints;

                while (spect == null)
                {
                    spect = await LeagueCommand.Api.SpectatorV4()
                        .GetCurrentGameInfoBySummonerAsync(PlatformRoute.EUW1, summs.Id);
                    Thread.Sleep(10000);
                }
                var rank = LeagueCommand.RankEmblems.GetValueOrDefault(LeagueEntry.Tier.Value, "Unranked");
                while (true)
                {

                    var match = await LeagueCommand.Api.MatchV5()
                        .GetMatchAsync(RegionalRoute.EUROPE, "EUW1_" + spect.GameId);
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
                        var lpAfterMatch =
                            await LeagueCommand.Api.LeagueV4()
                                .GetLeagueEntriesForSummonerAsync(PlatformRoute.EUW1, summs.Id);
                        var refreshedLeaguePoint = lpAfterMatch.Single(x => x.QueueType == QueueType.RANKED_SOLO_5x5);
                        var leaguePoints = refreshedLeaguePoint.LeaguePoints;

                        Console.WriteLine("leaguePoints " + leaguePoints);
                        string lea;

                        if (winloose)
                        {
                            color = Color.Green;
                            lea = $"+ {LeagueCommand.CalculateDifference(lp, leaguePoints)}";
                        }
                        else
                        {
                            color = Color.Red;
                            lea = $"- {LeagueCommand.CalculateDifference(lp, leaguePoints)}";
                        }

                        if (role == "CARRY") role = "ADC";

                        var embed = new EmbedBuilder
                        {
                            Title =
                                $"{summs.Name} vient de {resultat} une ranked en {role}{Environment.NewLine}avec {champ}",
                            Color = color,
                            ThumbnailUrl = rank,
                            Description =
                                $"{summs.Name} est actuellement {refreshedLeaguePoint.Tier} {refreshedLeaguePoint.Rank} {leaguePoints} LP ({lea})"
                        };

                        var kda = (kill + assist) / (double)death;
                        embed.AddField("Kill", kill, true)
                            .AddField("Death", death, true)
                            .AddField("Assist", assist, true)
                            .AddField("KDA", kda.ToString("F"), true)
                            .AddField("Cs", cs, true)
                            .AddField("Gold", gold, true)
                            .AddField("Game Time", gametime, true)
                            .AddField("Death Timer",
                                deathtimer.Minutes + " minutes " + deathtimer.Seconds + " secondes", true)
                            .AddField("DPM", dpm.ToString("F"), true)
                            .AddField("Wards", wards, true);
                  
                        await Webhook.SendMessageAsync(username: "Seraph",
                            avatarUrl:
                            "https://cdn.discordapp.com/avatars/557906672820158475/e75b6119c6d8ea5ee964192cacbadf2c.webp",
                            embeds: new[] { embed.Build() });
                        break;
                    }

                    Thread.Sleep(10000);
                }
                if (RecursiveRanked is false)
                {
                    throw new Exception("Stop Recursive Method");
                }

                await StalkingRanked(TempNameRanked);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
