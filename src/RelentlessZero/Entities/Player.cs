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
using RelentlessZero.Database;
using RelentlessZero.Logging;
using RelentlessZero.Managers;
using RelentlessZero.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RelentlessZero.Entities
{
    public class Avatar
    {
        [JsonProperty(PropertyName = "profileId")]
        public uint Id { get; set; }
        [JsonProperty(PropertyName = "head")]
        public uint Head { get; set; }
        [JsonProperty(PropertyName = "body")]
        public uint Body { get; set; }
        [JsonProperty(PropertyName = "leg")]
        public uint Leg { get; set; }
        [JsonProperty(PropertyName = "armBack")]
        public uint ArmBack { get; set; }
        [JsonProperty(PropertyName = "armFront")]
        public uint ArmFront { get; set; }

        public void SetAvatar(uint head, uint body, uint leg, uint armBack, uint armFront)
        {
            Head     = head;
            Body     = body;
            Leg      = leg;
            ArmBack  = armBack;
            ArmFront = armFront;
        }

        public void SaveAvatar()
        {
            string query = "INSERT INTO `account_avatar` (`id`, `head`, `body`, `leg`, `armBack`, `armFront`) VALUES(?, ?, ?, ?, ?, ?) " +
                    "ON DUPLICATE KEY UPDATE `head` = VALUES(`head`), `body` = VALUES(`body`), `leg` = VALUES(`leg`), " + 
                    "`armBack` = VALUES(`armBack`), `armFront` = VALUES(`armFront`);";

            DatabaseManager.Database.Execute(query, Id, Head, Body, Leg, ArmBack, ArmFront);
        }
    }

    public class Player
    {
        public uint Id { get; set; }
        public Session Session { get; set; }
        public AdminRole AdminRole { get; set; }
        public string Username { get; set; }
        public uint Gold { get; set; }
        public uint Shards { get; set; }
        public ushort Rating { get; set; }
        public PlayerFlags Flags { get; set; }

        public Avatar Avatar { get; set; }
        public List<string> CurrentRooms { get; set; }
        public List<ScrollInstance> Scrolls { get; set; }
        public List<Deck> Decks { get; set; }
        public List<ulong> ValidatedDeck { get; set; }

        public Player()
        {
            CurrentRooms  = new List<string>();
            Scrolls       = new List<ScrollInstance>();
            Decks         = new List<Deck>();
            ValidatedDeck = new List<ulong>();
            Avatar        = new Avatar();
        }

        public void OnDisconnect()
        {
            foreach (string roomName in CurrentRooms.ToArray())
                LobbyManager.RemovePlayer(roomName, this);

            SaveScrolls();
            SavePlayer();
        }

        public bool HasFlag(PlayerFlags flag) { return (Flags & flag) != 0; }
        public void RemoveFlag(PlayerFlags flag) { Flags &= ~flag; }

        public ScrollInstance GetScroll(ulong id) { return Scrolls.SingleOrDefault(scroll => scroll.Id == id); }
        public Deck GetDeck(string name) { return Decks.SingleOrDefault(deck => deck.Name == name); }

        public PacketProfile GeneratePacketProfile()
        {
            var packetProfile = new PacketProfile()
            {
                Id          = Id,
                Name        = Username,
                FeatureType = "PREMIUM",
                AdminRole   = AdminRole
            };

            return packetProfile;
        }

        public PacketRoomInfoProfile GenerateRoomInfoProfile()
        {
            var roomInfoProfile = new PacketRoomInfoProfile()
            {
                ProfileId        = Id,
                Name             = Username,
                AcceptChallenges = HasFlag(PlayerFlags.AcceptChallenges),
                AcceptTrades     = HasFlag(PlayerFlags.AcceptTrades),
                AdminRole        = AdminRole,
                FeatureType      = "PREMIUM" // can also be "DEMO"
            };

            return roomInfoProfile;
        }

        public void SendRoomMessage(string roomName, string message, string from = "Scrolls")
        {
            if (!CurrentRooms.Contains(roomName) && roomName != "Message")
                return;

            var roomChatMessage = new PacketRoomChatMessage()
            {
                RoomName = roomName,
                From     = from,
                Text     = message
            };

            Session.Send(roomChatMessage);
        }

        public void LoadScrolls()
        {
            var scrollInstanceResult = DatabaseManager.Database.Select("SELECT `id`, `scrollEntry`, `level`, `timestamp`, `damage`, `destroyed`, `heal`," +
                "`idolKills`, `played`, `sacrificed`,`totalGames`, `unitKills`, `wins`, `tradable` FROM `scroll_instance` WHERE `accountId` = ?", Id);

            if (scrollInstanceResult != null)
            {
                for (int i = 0; i < scrollInstanceResult.Count; i++)
                {
                    ulong scrollId     = scrollInstanceResult.Read<ulong>(i, "id");
                    ushort scrollEntry = scrollInstanceResult.Read<ushort>(i, "scrollEntry");

                    // link scroll template with scroll instance
                    var scrollTemplate = AssetManager.GetScrollTemplate(scrollEntry);
                    if (scrollTemplate == null)
                    {
                        LogManager.Write("Player", "Scroll instance {0} has invalid scroll entry {1}! Skipping.",
                            scrollId, scrollEntry);
                        continue;
                    }

                    var scrollInstance = new ScrollInstance(scrollTemplate);
                    scrollInstance.Id         = scrollId;
                    scrollInstance.Timestamp  = scrollInstanceResult.Read<long>(i, "timestamp");
                    scrollInstance.Level      = scrollInstanceResult.Read<byte>(i, "level");

                    if (scrollInstance.Level > ScrollInstance.MaxLevel)
                    {
                        LogManager.Write("Player", "Scroll instance {0} has invalid scroll level {1}! Skipping.",
                            scrollInstance.Id, scrollInstance.Level);
                        continue;
                    }

                    scrollInstance.Tradable   = scrollInstanceResult.Read<bool>(i, "tradable");
                    scrollInstance.SaveNeeded = false;

                    // tracked scroll stats
                    scrollInstance.Stats.Damage     = scrollInstanceResult.Read<uint>(i, "damage");
                    scrollInstance.Stats.Destroyed  = scrollInstanceResult.Read<uint>(i, "destroyed");
                    scrollInstance.Stats.Heal       = scrollInstanceResult.Read<uint>(i, "heal");
                    scrollInstance.Stats.IdolKills  = scrollInstanceResult.Read<uint>(i, "idolKills");
                    scrollInstance.Stats.Played     = scrollInstanceResult.Read<uint>(i, "played");
                    scrollInstance.Stats.Sacrificed = scrollInstanceResult.Read<uint>(i, "sacrificed");
                    scrollInstance.Stats.TotalGames = scrollInstanceResult.Read<uint>(i, "totalGames");
                    scrollInstance.Stats.UnitKills  = scrollInstanceResult.Read<uint>(i, "unitKills");
                    scrollInstance.Stats.Wins       = scrollInstanceResult.Read<uint>(i, "wins");

                    Scrolls.Add(scrollInstance);
                }
            }
        }

        public void LoadDecks()
        {
            var deckResult = DatabaseManager.Database.Select("SELECT `id`, `name`, `timestamp`, `flags` FROM `account_deck` WHERE `accountId` = ?", Id);
            if (deckResult != null)
            {
                for (int i = 0; i < deckResult.Count; i++)
                {
                    var deck = new Deck(this, deckResult.Read<uint>(i, "id"), deckResult.Read<string>(i, "name"), 
                        deckResult.Read<ulong>(i, "timestamp"), deckResult.Read<DeckFlags>(i, "flags"));

                    // get all scrolls associated with deck
                    var deckScrollResult = DatabaseManager.Database.Select("SELECT `scrollInstance` FROM `account_deck_scroll` WHERE `id` = ?", deck.Id);
                    if (deckScrollResult == null)
                    {
                        LogManager.Write("Player", "Deck instance {0} has no scrolls associated with it! Skipping.", deck.Id);
                        continue;
                    }

                    for (int j = 0; j < deckScrollResult.Count; j++)
                    {
                        uint scrollId      = deckScrollResult.Read<uint>(j, "scrollInstance");
                        var scrollInstance = GetScroll(scrollId);

                        if (scrollInstance == null)
                        {
                            LogManager.Write("Player", "Scroll instance {0} in deck {1} doesn't belong to player or doesn't exist! Skipping.", scrollId, deck.Id);
                            continue;
                        }

                        deck.Scrolls.Add(scrollInstance);
                    }

                    if (deck.Scrolls.Count >= 1)
                    {
                        deck.CalculateResources();
                        Decks.Add(deck);
                    }
                }
            }
        }

        public void SaveScrolls()
        {
            foreach (var scroll in Scrolls)
            {
                // only save scrolls that have been modified since last save
                if (!scroll.SaveNeeded)
                    continue;

                scroll.SaveNeeded = false;

                string query = "INSERT INTO `scroll_instance` (`id`, `accountId`, `scrollEntry`, `level`, `timestamp`, `damage`, `destroyed`, `heal`, " +
                    "`idolKills`, `played`, `sacrificed`, `totalGames`, `unitKills`, `wins`, `tradable`) VALUES(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?) " +
                    "ON DUPLICATE KEY UPDATE `id` = VALUES(`id`), `accountId` = VALUES(`accountId`), `scrollEntry` = VALUES(`scrollEntry`), " +
                    "`level` = VALUES(`level`), `timestamp` = VALUES(`timestamp`), `damage` = VALUES(`damage`), `destroyed` = VALUES (`destroyed`), " +
                    "`heal` = VALUES(`heal`), `idolKills` = VALUES(`idolKills`), `played` = VALUES(`played`), `sacrificed` = VALUES (`sacrificed`), " + 
                    "`totalGames` = VALUES(`totalGames`), `unitKills` = VALUES(`unitKills`), `wins` = VALUES(`wins`), `tradable` = VALUES(`tradable`);";

                DatabaseManager.Database.Execute(query, scroll.Id, Id, scroll.Scroll.Entry, scroll.Level, scroll.Timestamp, scroll.Stats.Damage,
                     scroll.Stats.Destroyed, scroll.Stats.Heal, scroll.Stats.IdolKills, scroll.Stats.Played, scroll.Stats.Sacrificed, scroll.Stats.TotalGames,
                     scroll.Stats.UnitKills, scroll.Stats.Wins, scroll.Tradable);
            }
        }

        public void SavePlayer()
        {
            DatabaseManager.Database.Execute("UPDATE `account_info` SET `gold` = ?, `shards` = ?, `rating` = ?, `flags` = ?", Gold, Shards, Rating, Flags);
        }

        public void CreateScroll(ScrollTemplate scrollTemplate)
        {
            var scrollInstance = new ScrollInstance(scrollTemplate);
            scrollInstance.Id         = AssetManager.GetNewScrollInstanceId();
            scrollInstance.Timestamp  = DateTime.UtcNow.Ticks;
            scrollInstance.SaveNeeded = true;

            Scrolls.Add(scrollInstance);
        }

        public void GiveAllScrolls()
        {
            foreach (var scroll in AssetManager.ScrollTemplateStore)
                CreateScroll(scroll);
        }
    }
}
