﻿/*
 * Copyright (C) 2013-2015 RelentlessZero
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RelentlessZero.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RelentlessZero.Network
{
    public class PacketHeader
    {
        [JsonProperty(PropertyName = "msg")]
        public string Msg { get; set; }
    }

    // ----------------------------------------------------------------
    // Sub Packet Structures
    // ----------------------------------------------------------------

    public class PacketIdolTypes
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "idol1")]
        public int Idol1 { get; set; }
        [JsonProperty(PropertyName = "idol2")]
        public int Idol2 { get; set; }
        [JsonProperty(PropertyName = "idol3")]
        public int Idol3 { get; set; }
        [JsonProperty(PropertyName = "idol4")]
        public int Idol4 { get; set; }
        [JsonProperty(PropertyName = "idol5")]
        public int Idol5 { get; set; }
    }

    public class PacketFullRoom
    {
        [JsonProperty(PropertyName = "room")]
        public PacketRoom Room { get; set; }
        [JsonProperty(PropertyName = "numberOfUsers")]
        public int NumberOfUsers { get; set; }
    }
    
    public class PacketProfile
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "adminRole")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AdminRole AdminRole { get; set; }
        [JsonProperty(PropertyName = "featureType")]
        public string FeatureType { get; set; }
    }

    public class PacketProfileData
    {
        [JsonProperty(PropertyName = "gold")]
        public uint Gold { get; set; }
        [JsonProperty(PropertyName = "shards")]
        public uint Shards { get; set; }
        [JsonProperty(PropertyName = "starterDeckVersion")]
        public uint StarterDeckVersion { get; set; }
        [JsonProperty(PropertyName = "spectatePermission")]
        public string SpectatePermission { get; set; }
        [JsonProperty(PropertyName = "acceptTrades")]
        public bool AcceptTrades { get; set; }
        [JsonProperty(PropertyName = "acceptChallenges")]
        public bool AcceptChallenges { get; set; }
        [JsonProperty(PropertyName = "rating")]
        public uint Rating { get; set; }
    }

    public class PacketRoom
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "autoIncrement")]
        public bool AutoIncrement { get; set; }
    }

    public class PacketRoomInfoProfile
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "acceptChallenges")]
        public bool AcceptChallenges { get; set; }
        [JsonProperty(PropertyName = "acceptTrades")]
        public bool AcceptTrades { get; set; }
        [JsonProperty(PropertyName = "adminRole")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AdminRole AdminRole { get; set; }
        [JsonProperty(PropertyName = "featureType")]
        public string FeatureType { get; set; }
    }

    // ----------------------------------------------------------------
    // Packet Effects Structures
    // ----------------------------------------------------------------

    // custom serializer for effects subpackets. Meh.
    public class JsonPacketEffectSerializer : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
                PacketEffect effect = (PacketEffect)value;
                PacketEffectAttribute effectAttribute = effect.GetType().GetCustomAttribute<PacketEffectAttribute>();
                if (effectAttribute == null)
                    throw new NotSupportedException("All PacketEffect classes must have a PacketEffectAttribute !");

                writer.WriteStartObject();
                writer.WritePropertyName(effectAttribute.Name);
                writer.WriteStartObject();
                foreach (PropertyInfo info in value.GetType().GetProperties())
                {
                    string propertyName;
                    JsonPropertyAttribute propertyAttribute = info.GetCustomAttribute<JsonPropertyAttribute>();
                    if (propertyAttribute != null)
                        propertyName = propertyAttribute.PropertyName;
                    else
                        propertyName = info.Name;
                    writer.WritePropertyName(propertyName);

                    JsonConverterAttribute conversionAttribute = info.GetCustomAttribute<JsonConverterAttribute>();
                    if (conversionAttribute != null)
                        writer.WriteRawValue(JsonConvert.SerializeObject(info.GetValue(value), Formatting.None, (JsonConverter)Activator.CreateInstance(conversionAttribute.ConverterType)));
                    else
                        writer.WriteRawValue(JsonConvert.SerializeObject(info.GetValue(value)));
                }
                writer.WriteEndObject();
                writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // effects packets are sent by server and should never be received from client
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(PacketEffect).IsAssignableFrom(objectType);
        }
    }

    [JsonConverter(typeof(JsonPacketEffectSerializer))]
    public abstract class PacketEffect {}

    [PacketEffect("IdolUpdateEffect")]
    public class PacketIdolUpdateEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "idol")]
        public Idol Idol { get; set; }
    }

    [PacketEffect("MulliganDisabledEffect")]
    public class PacketMulliganDisabledEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        public PlayerColor Color { get; set; }
    }

    [PacketEffect("EndGame")]
    public class PacketEndGameEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "winner")]
        public PlayerColor Winner { get; set; }
        [JsonProperty(PropertyName = "whiteStats")]
        public PlayerStats WhiteStats { get; set; }
        [JsonProperty(PropertyName = "blackStats")]
        public PlayerStats BlackStats { get; set; }
        [JsonProperty(PropertyName = "whiteGoldReward")]
        public GoldReward WhiteGoldReward { get; set; }
        [JsonProperty(PropertyName = "blackGoldReward")]
        public GoldReward BlackGoldReward { get; set; }
    }

    [PacketEffect("SurrenderEffect")]
    public class PacketSurrenderEffect : PacketEffect
    {
        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PlayerColor Color { get; set; }
    }


    // ----------------------------------------------------------------
    // Packet Structures
    // ----------------------------------------------------------------

    [Packet("ActivateGame", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketActivateGame : PacketHeader { }

    [Packet("AvatarTypes", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketAvatarTypes : PacketHeader
    {
        [JsonProperty(PropertyName = "types")]
        public List<AvatarPartTemplate> Types { get; set; }
    }

    [Packet("BattleRedirect", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketBattleRedirect : PacketHeader
    {
        [JsonProperty(PropertyName = "ip")]
        public string IP { get; set; }
        [JsonProperty(PropertyName = "port")]
        public uint Port { get; set; }
    }

    [Packet("CardTypes", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketCardTypes : PacketHeader
    {
        [JsonProperty(PropertyName = "cardTypes")]
        public List<ScrollTemplate> CardTypes { get; set; }
    }

    [Packet("Connect", PacketDirection.ClientToServer, SessionType.Lobby | SessionType.Battle, false)]
    public class PacketConnect
    {
        [JsonProperty(PropertyName = "email")]
        public string Username { get; set; }
        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }
        [JsonProperty(PropertyName = "authHash")]
        public string AuthHash { get; set; }
    }

    [Packet("DeckCards", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketDeckCards : PacketHeader
    {
        [JsonProperty(PropertyName = "deck")]
        public string Deck { get; set; }
        [JsonProperty(PropertyName = "cards")]
        public List<ScrollInstance> Scrolls { get; set; }
    }

    [Packet("DeckDelete", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketDeckDelete : PacketHeader
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }

    [Packet("DeckList", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketDeckList : PacketHeader
    {
        [JsonProperty(PropertyName = "decks")]
        public List<Deck> Decks { get; set; }
    }

    [Packet("DeckSave", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketDeckSave : PacketHeader
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "decks")]
        public List<ulong> Scrolls { get; set; }
    }

    [Packet("DeckValidate", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketDeckValidate : PacketHeader
    {
        [JsonProperty(PropertyName = "cards", NullValueHandling = NullValueHandling.Ignore)]
        public List<ulong> Scolls { get; set; }
        [JsonProperty(PropertyName = "errors", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Errors { get; set; }
    }

    [Packet("DidYouKnow", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby, false)]
    public class PacketDidYouKnow : PacketHeader
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }
        [JsonProperty(PropertyName = "hint")]
        public string Hint { get; set; }
    }

    [Packet("Fail", PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketFail : PacketHeader
    {
        [JsonProperty(PropertyName = "op")]
        public string Origin { get; set; }
        [JsonProperty(PropertyName = "info")]
        public string Info { get; set; }
    }

    [Packet("FatalFail", PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketFatalFail : PacketFail { }

    [Packet("FirstConnect", PacketDirection.ClientToServer, SessionType.Lobby, false)]
    public class PacketFirstConnect : PacketConnect { }

    [Packet("GameChallenge", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketGameChallenge : PacketHeader
    {
        [JsonProperty(PropertyName = "from")]
        public PacketProfile From { get; set; }
        [JsonProperty(PropertyName = "isParentalConsentNeeded")]
        public bool ParentalConsent { get; set; }
    }

    [Packet("GameChallengeAccept", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketGameChallengeAccept : PacketHeader
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "deck")]
        public string Deck { get; set; }
    }

    [Packet("GameChallengeDecline", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketGameChallengeDecline : PacketHeader
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
    }

    [Packet("GameChallengeRequest", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketGameChallengeRequest : PacketHeader
    {
        [JsonProperty(PropertyName = "customGameId")]
        public int CustomGameId { get; set; }
        [JsonProperty(PropertyName = "chooseDeck")]
        public bool ChooseDeck { get; set; }
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "deck")]
        public string Deck { get; set; }
    }

    [Packet("GameChallengeResponse", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketGameChallengeResponse : PacketHeader
    {
        [JsonProperty(PropertyName = "from")]
        public PacketProfile From { get; set; }
        [JsonProperty(PropertyName = "to")]
        public PacketProfile To { get; set; }
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
    }

    [Packet("GameInfo", PacketDirection.ServerToClient, SessionType.Battle)]
    public class PacketGameInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "white")]
        public string White { get; set; }
        [JsonProperty(PropertyName = "black")]
        public string Black { get; set; }
        [JsonProperty(PropertyName = "gameType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BattleType GameType { get; set; }
        [JsonProperty(PropertyName = "gameId")]
        public uint GameId { get; set; }
        [JsonProperty(PropertyName = "color")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PlayerColor Color { get; set; }
        [JsonProperty(PropertyName = "roundTimerSeconds")]
        public int roundTimerSeconds { get; set; }
        [JsonProperty(PropertyName = "phase")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BattlePhase Phase { get; set; }
        [JsonProperty(PropertyName = "whiteAvatar")]
        public Avatar WhiteAvatar { get; set; }
        [JsonProperty(PropertyName = "blackAvatar")]
        public Avatar BlackAvatar { get; set; }
        [JsonProperty(PropertyName = "whiteIdolTypes")]
        public PacketIdolTypes WhiteIdolTypes { get; set; }
        [JsonProperty(PropertyName = "blackIdolTypes")]
        public PacketIdolTypes BlackIdolTypes { get; set; }
        [JsonProperty(PropertyName = "customSettings")]
        public List<string> CustomSettings { get; set; } // TODO : gather more info and change type
        [JsonProperty(PropertyName = "rewardForIdolKill")]
        public uint RewardForIdolKill { get; set; }
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }
        [JsonProperty(PropertyName = "port")]
        public uint Port { get; set; }
        [JsonProperty(PropertyName = "whiteIdols")]
        public Idol[] WhiteIdols { get; set; }
        [JsonProperty(PropertyName = "blackIdols")]
        public Idol[] BlackIdols { get; set; }
        [JsonProperty(PropertyName = "refId")]
        public int RefId { get; set; }
        [JsonProperty(PropertyName = "maxTierRewardMultiplier")]
        public float MaxTierRewardMultiplier { get; set; }
        [JsonProperty(PropertyName = "tierRewardMultiplierDelta")]
        public List<float> TierRewardMultiplierDelta { get; set; }
    }

    [Packet("GameMatchQueueStatus", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketGameMatchQueueStatus : PacketHeader
    {
        [JsonProperty(PropertyName = "inQueue")]
        public bool InQueue { get; set; }
        [JsonProperty(PropertyName = "gameType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BattleType GameType { get; set; }
    }

    [Packet("JoinLobby", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketJoinLobby { }

    [Packet("JoinBattle", PacketDirection.ClientToServer, SessionType.Battle)]
    public class PacketJoinBattle { }

    [Packet("LeaveGame", PacketDirection.ClientToServer, SessionType.Battle)]
    public class PacketLeaveGame { }

    [Packet("LibraryView", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketLibraryView : PacketHeader
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint ProfileId { get; set; }
        [JsonProperty(PropertyName = "cards")]
        public List<ScrollInstance> Cards { get; set; }
    }

    [Packet("LobbyLookup", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lookup, false)]
    public class PacketLobbyLookup : PacketHeader
    {
        [JsonProperty(PropertyName = "ip")]
        public string Ip { get; set; }
        [JsonProperty(PropertyName = "port")]
        public int Port { get; set; }
    }

    [Packet("NewEffects", PacketDirection.ServerToClient, SessionType.Battle)]
    public class PacketNewEffects : PacketHeader
    {
        [JsonProperty(PropertyName = "effects")]
        public List<PacketEffect> Effects = new List<PacketEffect>(); 
    }

    [Packet("Ok", PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketOk : PacketHeader
    {
        [JsonProperty(PropertyName = "op")]
        public string Origin { get; set; }
    }

    [Packet("OverallStats", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketOverallStats : PacketHeader
    {
        [JsonProperty(PropertyName = "loginsLast24h")]
        public uint PlayersOnline24h { get; set; }
        [JsonProperty(PropertyName = "nrOfProfiles")]
        public int PlayersOnline { get; set; }
        [JsonProperty(PropertyName = "serverName")]
        public string ServerName { get; set; }

        // TODO: implement this correctly
        [JsonProperty(PropertyName = "topRanked")]
        public string[] TopRanked { get; set; }
        [JsonProperty(PropertyName = "weeklyWinners")]
        public string[] WeeklyWinners { get; set; }
    }

    [Packet("Ping", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketPing : PacketHeader
    {
        [JsonProperty(PropertyName = "time")]
        public uint Time { get; set; }
    }

    [Packet("PlaySinglePlayerQuickMatch", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketPlaySinglePlayerQuickMatch : PacketHeader
    {
        [JsonProperty(PropertyName = "robotName")]
        public string RobotName { get; set; }
        [JsonProperty(PropertyName = "deck")]
        public string Deck { get; set; }
    }

    [Packet("ProfileDataInfo", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketProfileDataInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "profileData")]
        public PacketProfileData ProfileData { get; set; }
    }

    [Packet("ProfileInfo", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketProfileInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "profile")]
        public PacketProfile Profile { get; set; }
    }

    [Packet("RoomChatMessage", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketRoomChatMessage : PacketHeader
    {
        [JsonProperty(PropertyName = "roomName")]
        public string RoomName { get; set; }
        [JsonProperty(PropertyName = "from")]
        public string From { get; set; }
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }

    [Packet("RoomEnter", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketRoomEnter : PacketHeader
    {
        [JsonProperty(PropertyName = "roomName")]
        public string RoomName { get; set; }
    }

    [Packet("RoomEnterFree", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketRoomEnterFree : PacketRoomEnter { }

    [Packet("RoomEnterMulti", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketRoomEnterMulti
    {
        [JsonProperty(PropertyName = "roomNames")]
        public List<string> RoomNames { get; set; }
    }

    [Packet("RoomExit", PacketDirection.ClientToServer, SessionType.Lobby)]
    public class PacketRoomExit : PacketRoomEnter { }

    [Packet("RoomInfo", PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketRoomInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "roomName")]
        public string RoomName { get; set; }
        [JsonProperty(PropertyName = "reset")]
        public bool Reset { get; set; }
        [JsonProperty(PropertyName = "updated")]
        public List<PacketRoomInfoProfile> Updated { get; set; }
        [JsonProperty(PropertyName = "removed")]
        public List<PacketRoomInfoProfile> Removed { get; set; }
    }

    [Packet("RoomsList", PacketDirection.ClientToServer | PacketDirection.ServerToClient, SessionType.Lobby)]
    public class PacketRoomsList : PacketHeader
    {
        [JsonProperty(PropertyName = "rooms")]
        public List<PacketFullRoom> Rooms { get; set; }
    }

    [Packet("ServerInfo", PacketDirection.ServerToClient, SessionType.Lookup | SessionType.Lobby | SessionType.Battle, false)]
    public class PacketServerInfo : PacketHeader
    {
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }
        [JsonProperty(PropertyName = "assetURL")]
        public string AssetUrl { get; set; }
        [JsonProperty(PropertyName = "newsURL")]
        public string NewsUrl { get; set; }
        [JsonProperty(PropertyName = "roles")]
        public string Roles { get; set; }
    }

    [Packet("Surrender", PacketDirection.ClientToServer, SessionType.Battle)]
    public class PacketSurrender : PacketHeader { }
}
