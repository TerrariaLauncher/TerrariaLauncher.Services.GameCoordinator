using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Services
{
    public class Player
    {
        public string Name { get; set; }
        public IPAddress IP { get; set; }
        public Guid GameCoordinatorId { get; set; }
    }

    public class Playing
    {
        private ConcurrentDictionary<string, Player> players = new ConcurrentDictionary<string, Player>();

        /// <summary>
        /// Add a new player into the player list.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>Return true if the player doesn't exist on the player list, otherwise return false.</returns>
        public bool Join(Player player)
        {
            return this.players.TryAdd(player.Name, player);
        }

        /// <summary>
        /// Remove a player out of the player list.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="player"></param>
        /// <returns>Return true if the player exists on the player list, otherwise return false.</returns>
        public bool Leave(string name, out Player player)
        {
            return this.players.TryRemove(name, out player);
        }
    }
}
