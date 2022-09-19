#nullable enable
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace League_Discord_Bot.Json;

public partial class LeagueEntries
{
    [JsonProperty("leagueId")] public Guid? LeagueId { get; set; }

    [JsonProperty("queueType")] public string? QueueType { get; set; }

    [JsonProperty("tier")] public string? Tier { get; set; }

    [JsonProperty("rank")] public string? Rank { get; set; }

    [JsonProperty("summonerId")] public string? SummonerId { get; set; }

    [JsonProperty("summonerName")] public string? SummonerName { get; set; }

    [JsonProperty("leaguePoints")] public long? LeaguePoints { get; set; }

    [JsonProperty("wins")] public long? Wins { get; set; }

    [JsonProperty("losses")] public long? Losses { get; set; }

    [JsonProperty("veteran")] public bool? Veteran { get; set; }

    [JsonProperty("inactive")] public bool? Inactive { get; set; }

    [JsonProperty("freshBlood")] public bool? FreshBlood { get; set; }

    [JsonProperty("hotStreak")] public bool? HotStreak { get; set; }
}

public partial class LeagueEntries
{
    public static LeagueEntries[] FromJson(string json)
    {
        return JsonConvert.DeserializeObject<LeagueEntries[]>(json, Converter.Settings);
    }
}

public partial class SummonerByName
{
    [JsonProperty("id")] public string? Id { get; set; }

    [JsonProperty("accountId")] public string? AccountId { get; set; }

    [JsonProperty("puuid")] public string? Puuid { get; set; }

    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("profileIconId")] public long? ProfileIconId { get; set; }

    [JsonProperty("revisionDate")] public long? RevisionDate { get; set; }

    [JsonProperty("summonerLevel")] public long? SummonerLevel { get; set; }
}

public partial class TftRank
{
    [JsonProperty("id")] public string? Id { get; set; }

    [JsonProperty("accountId")] public string? AccountId { get; set; }

    [JsonProperty("puuid")] public string? Puuid { get; set; }

    [JsonProperty("name")] public string? Name { get; set; }

    [JsonProperty("profileIconId")] public long? ProfileIconId { get; set; }

    [JsonProperty("revisionDate")] public long? RevisionDate { get; set; }

    [JsonProperty("summonerLevel")] public long? SummonerLevel { get; set; }
}

public class TftEntries
{
    [JsonProperty("leagueId")] public Guid? LeagueId { get; set; }

    [JsonProperty("queueType")] public string? QueueType { get; set; }

    [JsonProperty("tier")] public string? Tier { get; set; }

    [JsonProperty("rank")] public string? Rank { get; set; }

    [JsonProperty("summonerId")] public string? SummonerId { get; set; }

    [JsonProperty("summonerName")] public string? SummonerName { get; set; }

    [JsonProperty("leaguePoints")] public long? LeaguePoints { get; set; }

    [JsonProperty("wins")] public long? Wins { get; set; }

    [JsonProperty("losses")] public long? Losses { get; set; }

    [JsonProperty("veteran")] public bool? Veteran { get; set; }

    [JsonProperty("inactive")] public bool? Inactive { get; set; }

    [JsonProperty("freshBlood")] public bool? FreshBlood { get; set; }

    [JsonProperty("hotStreak")] public bool? HotStreak { get; set; }
}

public partial class ObserversMatch
{
    [JsonProperty("gameId")] public long GameId { get; set; }

    [JsonProperty("mapId")] public long MapId { get; set; }

    [JsonProperty("gameMode")] public string? GameMode { get; set; }

    [JsonProperty("gameType")] public string? GameType { get; set; }

    [JsonProperty("gameQueueConfigId")] public long GameQueueConfigId { get; set; }

    [JsonProperty("participants")] public Participant[]? Participants { get; set; }

    [JsonProperty("observers")] public Observers? Observers { get; set; }

    [JsonProperty("platformId")] public string? PlatformId { get; set; }

    [JsonProperty("bannedChampions")] public BannedChampion[]? BannedChampions { get; set; }

    [JsonProperty("gameStartTime")] public long GameStartTime { get; set; }

    [JsonProperty("gameLength")] public long GameLength { get; set; }
}

public class BannedChampion
{
    [JsonProperty("championId")] public long ChampionId { get; set; }

    [JsonProperty("teamId")] public long TeamId { get; set; }

    [JsonProperty("pickTurn")] public long PickTurn { get; set; }
}

public class Observers
{
    [JsonProperty("encryptionKey")] public string? EncryptionKey { get; set; }
}

public class Participant
{
    [JsonProperty("teamId")] public long TeamId { get; set; }

    [JsonProperty("spell1Id")] public long Spell1Id { get; set; }

    [JsonProperty("spell2Id")] public long Spell2Id { get; set; }

    [JsonProperty("championId")] public long ChampionId { get; set; }

    [JsonProperty("profileIconId")] public long ProfileIconId { get; set; }

    [JsonProperty("summonerName")] public string? SummonerName { get; set; }

    [JsonProperty("bot")] public bool Bot { get; set; }

    [JsonProperty("summonerId")] public string? SummonerId { get; set; }

    [JsonProperty("gameCustomizationObjects")]
    public object[]? GameCustomizationObjects { get; set; }

    [JsonProperty("perks")] public Perks? Perks { get; set; }
}

public class Perks
{
    [JsonProperty("perkIds")] public long[]? PerkIds { get; set; }

    [JsonProperty("perkStyle")] public long PerkStyle { get; set; }

    [JsonProperty("perkSubStyle")] public long PerkSubStyle { get; set; }
}

public partial class ObserversMatch
{
    public static ObserversMatch FromJson(string json)
    {
        return JsonConvert.DeserializeObject<ObserversMatch>(json, Converter.Settings);
    }
}

public partial class TftRank
{
    public static TftRank FromJson(string json)
    {
        return JsonConvert.DeserializeObject<TftRank>(json, Converter.Settings);
    }
}

public partial class SummonerByName
{
    public static SummonerByName FromJson(string json)
    {
        return JsonConvert.DeserializeObject<SummonerByName>(json, Converter.Settings);
    }
}

internal static class Converter
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal } }
    };
}