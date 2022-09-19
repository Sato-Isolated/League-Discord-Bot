using System.Net;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using League_Discord_Bot.Json;

namespace League_Discord_Bot.Modules;

public class MmrModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string IronUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791377379497869353/Emblem_Iron.png";

    private const string BronzeUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791377369566281748/Emblem_Bronze.png";

    private const string SilverUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791376720967368724/Emblem_Silver.png";

    private const string GoldUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791377378962047036/Emblem_Gold.png";

    private const string PlatinumUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791377382132547584/Emblem_Platinum.png";

    private const string DiamondUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791377375622725652/Emblem_Diamond.png";

    private const string MasterUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791377381713248317/Emblem_Master.png";

    private const string GrandmasterUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791377379607445564/Emblem_Grandmaster.png";

    private const string ChallengerUrl =
        "https://cdn.discordapp.com/attachments/787493493927968790/791377373044015154/Emblem_Challenger.png";

    private const string Api = "RGAPI-0000000000-000000-000000-000000-000000000000"; //https://developer.riotgames.com

    private InteractionHandler _handler;
    
    public MmrModule(InteractionHandler handler)
    {
        _handler = handler;
    }
    
    public InteractionService Commands { get; set; }

    private static string StripHtml(string input)
    {
        return Regex.Replace(input, "<.*?>", string.Empty);
    }

    [SlashCommand("mmr", "Check le MMR")]
    [Obsolete("Obsolete")]
    public async Task MmrAsync(string text)
    {
        var client = new WebClient();
        client.Headers.Add("user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.102 Safari/537.36 OPR/90.0.4480.100");
        var myJson = client.DownloadString($"https://euw.whatismymmr.com/api/v1/summoner?name={text}");
        var welcome = JsonMmr.Mode.FromJson(myJson);
        var summ = "";
        if (welcome.Ranked?.Summary is not null) summ = welcome.Ranked.Summary;

        var embed = new EmbedBuilder
        {
            Title = text,
            Description = $"I Will Check Your MMR. {Environment.NewLine}{Environment.NewLine}{StripHtml(summ)}"
        };
        // Or with methods
        dynamic aramMmr;
        dynamic normalMmr;
        dynamic rankedMmr;

        if (welcome.Aram.Avg is not null)
            aramMmr = welcome.Aram.Avg;
        else
            aramMmr = "N/A";

        if (welcome.Normal.Avg is not null)
            normalMmr = welcome.Normal.Avg;
        else
            normalMmr = "N/A";

        if (welcome.Ranked.Avg is not null)
            rankedMmr = welcome.Ranked.Avg;
        else
            rankedMmr = "N/A";

        dynamic aramRank = welcome.Aram.ClosestRank ?? "N/A";

        dynamic normalRank = welcome.Normal.ClosestRank ?? "N/A";

        dynamic rankedRank = welcome.Ranked.ClosestRank ?? "N/A";


        embed.AddField("Aram MMR", $"{aramMmr}", true)
            .AddField("Normal MMR", $"{normalMmr}", true)
            .AddField("Ranked MMR", $"{rankedMmr}", true)
            .AddField("Aram Rank", $"{aramRank}", true)
            .AddField("Normal Rank", $"{normalRank}", true)
            .AddField("Ranked Rank", $"{rankedRank}", true)
            .WithColor(Color.Blue);

        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("rank", "Check les stats ranked")]
    [Obsolete("Obsolete")]
    public async Task RankCommand2(string text)
    {
        try
        {
            var client = new WebClient();
            client.Headers.Add("user-agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.102 Safari/537.36 OPR/90.0.4480.100");
            var myJson =
                client.DownloadString(
                    $"https://euw1.api.riotgames.com/lol/summoner/v4/summoners/by-name/{text}?api_key={Api}");
            var summs = SummonerByName.FromJson(myJson);
            var leagueentries =
                client.DownloadString(
                    $"https://euw1.api.riotgames.com/lol/league/v4/entries/by-summoner/{summs.Id}?api_key={Api}");

            var league = LeagueEntries.FromJson(leagueentries);

            var solo = league.Single(x => x.QueueType == "RANKED_SOLO_5x5");

            var numOfGames = solo.Wins + solo.Losses;
            var winRate = (float)solo.Wins / (float)numOfGames * 100;

            var rank = solo.Tier switch
            {
                "IRON" => IronUrl,
                "BRONZE" => BronzeUrl,
                "SILVER" => SilverUrl,
                "GOLD" => GoldUrl,
                "PLATINUM" => PlatinumUrl,
                "DIAMOND" => DiamondUrl,
                "MASTER" => MasterUrl,
                "GRANDMASTER" => GrandmasterUrl,
                "CHALLENGER" => ChallengerUrl,
                _ => IronUrl
            };

            var embed = new EmbedBuilder
            {
                Title = $"{summs.Name}'s Ranked Stats",
                Color = Color.Blue,
                ThumbnailUrl = rank
            };
            // Or with methods
            embed.AddField("Rank", $"{solo.Tier} {solo.Rank}\n {solo.LeaguePoints} LP", true)
                .AddField("Stats", $"**Wins:** {solo.Wins}\n**Losses**: {solo.Losses}\n**Win Rate:** {winRate}%", true);

            await RespondAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            await RespondAsync("There is no data for this player");
        }
    }

    [SlashCommand("tftrank", "Check les stats TFT")]
    [Obsolete("Obsolete")]
    public async Task TftranTask(string name)
    {
        var client = new WebClient();
        client.Headers.Add("user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.102 Safari/537.36 OPR/90.0.4480.100");
        var myJson =
            client.DownloadString(
                $"https://euw1.api.riotgames.com/tft/summoner/v1/summoners/by-name/{name}?api_key={Api}");
        var summs = TftRank.FromJson(myJson);
        var leagueentries =
            client.DownloadString(
                $"https://euw1.api.riotgames.com/tft/league/v1/entries/by-summoner/{summs.Id}?api_key={Api}");

        var league = LeagueEntries.FromJson(leagueentries);

        var solo = league.Single(x => x.QueueType == "RANKED_TFT");

        var numOfGames = solo.Wins + solo.Losses;
        var winRate = (float)solo.Wins / (float)numOfGames * 100;

        var rank = solo.Tier switch
        {
            "IRON" => IronUrl,
            "BRONZE" => BronzeUrl,
            "SILVER" => SilverUrl,
            "GOLD" => GoldUrl,
            "PLATINUM" => PlatinumUrl,
            "DIAMOND" => DiamondUrl,
            "MASTER" => MasterUrl,
            "GRANDMASTER" => GrandmasterUrl,
            "CHALLENGER" => ChallengerUrl,
            _ => IronUrl
        };

        var embed = new EmbedBuilder
        {
            Title = $"{summs.Name}'s Ranked TFT Stats",
            Color = Color.Blue,
            ThumbnailUrl = rank
        };
        // Or with methods
        embed.AddField("Rank", $"{solo.Tier} {solo.Rank}\n {solo.LeaguePoints} LP", true)
            .AddField("Stats", $"**Wins:** {solo.Wins}\n**Losses**: {solo.Losses}\n**Win Rate:** {winRate}%", true);

        await RespondAsync(embed: embed.Build());
    }

    private static Task<string>? Downloadstring(string name)
    {
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("user-agent",
                "Csharp SeraphBot v1.0.3");
            var tempsumms = name;

            if (tempsumms != null && tempsumms.Any(char.IsWhiteSpace))
            {
                var replace = tempsumms.Replace(" ", "%20");
                tempsumms = replace;
            }

            var uri = new Uri(string.Format("https://euw.whatismymmr.com/api/v1/summoner?name={0}", tempsumms));
            return client.GetStringAsync(uri);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return null;
        }
    }

   [SlashCommand("checkranked", "Check les stats de la partie")]
    public async Task CheckMMR(string name)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("user-agent",
            "Csharp SeraphBot v1.0.3");
        var myJson =
            client.GetStringAsync(
                $"https://euw1.api.riotgames.com/lol/summoner/v4/summoners/by-name/{name}?api_key={Api}");
        var summs = SummonerByName.FromJson(await myJson);
        var leagueentries =
            client.GetStringAsync(
                $"https://euw1.api.riotgames.com/lol/spectator/v4/active-games/by-summoner/{summs.Id}?api_key={Api}");

        var league = ObserversMatch.FromJson(await leagueentries);
        var parti = league.Participants;
        var summonersList = new List<string>();
        var mmrAvergare = new List<string>();
        var rankAverage = new List<string>();
            foreach (var summoners in parti)
            {
                var pe = summoners.SummonerName;
                var tempJson = await Downloadstring(pe);
                var welcome = JsonMmr.Mode.FromJson(tempJson);

                rankAverage.Add(welcome.Ranked?.ClosestRank);
                summonersList.Add(summoners.SummonerName);

                var p = welcome.Ranked.Avg;

                if (welcome.Ranked?.Avg is null or 0)
                    p = 0;
                mmrAvergare.Add(p.ToString());
            }
       

            var embed = new EmbedBuilder { Title = $"{summs.Name}'s Game Stats", Color = Color.Blue };
            var mmravg = long.Parse(mmrAvergare[0]);
            var mmravg1 = long.Parse(mmrAvergare[1]);
            var mmravg2 = long.Parse(mmrAvergare[2]);
            var mmravg3 = long.Parse(mmrAvergare[3]);
            var mmravg4 = long.Parse(mmrAvergare[4]);
            var mmravg5 = long.Parse(mmrAvergare[5]);
            var mmravg6 = long.Parse(mmrAvergare[6]);
            var mmravg7 = long.Parse(mmrAvergare[7]);
            var mmravg8 = long.Parse(mmrAvergare[8]);
            var mmravg9 = long.Parse(mmrAvergare[9]);


            var mmrteam1 = (mmravg + mmravg1 + mmravg2 + mmravg3 + mmravg4) / 5;

            var mmrteam2 = (mmravg5 + mmravg6 + mmravg7 + mmravg8 + mmravg9) / 5;

            embed.AddField("Team 1", $"{summonersList[0]} : {mmrAvergare[0]} {rankAverage[0]}" +
                                     $"\n{summonersList[1]} : {mmrAvergare[1]} {rankAverage[1]}" +
                                     $"\n{summonersList[2]} : {mmrAvergare[2]} {rankAverage[2]}" +
                                     $"\n{summonersList[3]} : {mmrAvergare[3]} {rankAverage[3]}" +
                                     $"\n{summonersList[4]} : {mmrAvergare[4]} {rankAverage[4]}" +
                                     $"\n\nMMR Of the team : {mmrteam1}"
                , true).AddField("Team 2", $"\n{summonersList[5]} : {mmrAvergare[5]} {rankAverage[5]}" +
                                           $"\n{summonersList[6]} : {mmrAvergare[6]} {rankAverage[6]}" +
                                           $"\n{summonersList[7]} : {mmrAvergare[7]} {rankAverage[7]}" +
                                           $"\n{summonersList[8]} : {mmrAvergare[8]} {rankAverage[8]}" +
                                           $"\n{summonersList[9]} : {mmrAvergare[9]} {rankAverage[9]}" +
                                           $"\n\nMMR Of the team : {mmrteam2}", true);

            await ReplyAsync(embed: embed.Build());
        }

    [SlashCommand("checkaram", "Check les stats de la partie")]
    public async Task CheckMMRAram(string name)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("user-agent",
            "Csharp SeraphBot v1.0.3");
        var myJson =
            client.GetStringAsync(
                $"https://euw1.api.riotgames.com/lol/summoner/v4/summoners/by-name/{name}?api_key={Api}");
        var summs = SummonerByName.FromJson(await myJson);
        var leagueentries =
            client.GetStringAsync(
                $"https://euw1.api.riotgames.com/lol/spectator/v4/active-games/by-summoner/{summs.Id}?api_key={Api}");

        var league = ObserversMatch.FromJson(await leagueentries);
        var parti = league.Participants;
        var summonersList = new List<string>();
        var mmrAvergare = new List<string>();
        var rankAverage = new List<string>();
        foreach (var summoners in parti)
        {
            var pe = summoners.SummonerName;
            var tempJson = await Downloadstring(pe);
            var welcome = JsonMmr.Mode.FromJson(tempJson);

            rankAverage.Add(welcome.Aram?.ClosestRank);
            summonersList.Add(summoners.SummonerName);

            var p = welcome.Aram.Avg;

            if (welcome.Aram?.Avg is null or 0)
                p = 0;
            mmrAvergare.Add(p.ToString());
        }


        var embed = new EmbedBuilder { Title = $"{summs.Name}'s Game Stats", Color = Color.Blue };
        var mmravg = long.Parse(mmrAvergare[0]);
        var mmravg1 = long.Parse(mmrAvergare[1]);
        var mmravg2 = long.Parse(mmrAvergare[2]);
        var mmravg3 = long.Parse(mmrAvergare[3]);
        var mmravg4 = long.Parse(mmrAvergare[4]);
        var mmravg5 = long.Parse(mmrAvergare[5]);
        var mmravg6 = long.Parse(mmrAvergare[6]);
        var mmravg7 = long.Parse(mmrAvergare[7]);
        var mmravg8 = long.Parse(mmrAvergare[8]);
        var mmravg9 = long.Parse(mmrAvergare[9]);


        var mmrteam1 = (mmravg + mmravg1 + mmravg2 + mmravg3 + mmravg4) / 5;

        var mmrteam2 = (mmravg5 + mmravg6 + mmravg7 + mmravg8 + mmravg9) / 5;

        embed.AddField("Team 1", $"{summonersList[0]} : {mmrAvergare[0]} {rankAverage[0]}" +
                                 $"\n{summonersList[1]} : {mmrAvergare[1]} {rankAverage[1]}" +
                                 $"\n{summonersList[2]} : {mmrAvergare[2]} {rankAverage[2]}" +
                                 $"\n{summonersList[3]} : {mmrAvergare[3]} {rankAverage[3]}" +
                                 $"\n{summonersList[4]} : {mmrAvergare[4]} {rankAverage[4]}" +
                                 $"\n\nMMR Of the team : {mmrteam1}"
            , true).AddField("Team 2", $"\n{summonersList[5]} : {mmrAvergare[5]} {rankAverage[5]}" +
                                       $"\n{summonersList[6]} : {mmrAvergare[6]} {rankAverage[6]}" +
                                       $"\n{summonersList[7]} : {mmrAvergare[7]} {rankAverage[7]}" +
                                       $"\n{summonersList[8]} : {mmrAvergare[8]} {rankAverage[8]}" +
                                       $"\n{summonersList[9]} : {mmrAvergare[9]} {rankAverage[9]}" +
                                       $"\n\nMMR Of the team : {mmrteam2}", true);

        await ReplyAsync(embed: embed.Build());
    }
}
