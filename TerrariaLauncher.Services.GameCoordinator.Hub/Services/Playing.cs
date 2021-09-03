using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Commons.MediatorService;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Services
{
    public class GameCoordinatorPlayer
    {
        public string Name { get; set; }
        public string GameCoordinatorProxyId { get; set; }
        public User User { get; set; }
        public IPEndPoint EndPoint { get; set; }
    }

    public class Playing
    {
        private ConcurrentDictionary<string, GameCoordinatorPlayer> players = new ConcurrentDictionary<string, GameCoordinatorPlayer>();

        /// <summary>
        /// Add a new player into the player list.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>Return true if the player doesn't exist on the player list, otherwise return false.</returns>
        public bool Join(GameCoordinatorPlayer player)
        {
            return this.players.TryAdd(player.Name, player);
        }

        /// <summary>
        /// Remove a player out of the player list.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="player"></param>
        /// <returns>Return true if the player exists on the player list, otherwise return false.</returns>
        public bool Leave(string name, out GameCoordinatorPlayer player)
        {
            return this.players.TryRemove(name, out player);
        }

        public bool Get(string name, out GameCoordinatorPlayer player)
        {
            return this.players.TryGetValue(name, out player);
        }

        public void RemovePlayersBelongToProxyId(string proxyId)
        {
            foreach (var (playerName, player) in this.players)
            {
                if (player.GameCoordinatorProxyId == proxyId)
                {
                    this.players.TryRemove(playerName, out _);
                }
            }
        }
    }
}
