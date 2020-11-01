﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Script.GameType_Eol
{   // Script Class
    /// Provides the interface between the script and bot
    ///////////////////////////////////////////////////////
    public partial class BasicDefense : Bot
    {
        
        protected IEnumerable<Player> enemiesInRange()
        {
            IEnumerable<Player> sorted = _arena.getPlayersInRange(_state.positionX, _state.positionY, c_playerMaxRangeEnemies)
                .Where(p => p._team != _team && !p.IsDead);

            return sorted;
        }

        protected IEnumerable<Player> friendliesInRange()
        {
            IEnumerable<Player> players = _arena.getPlayersInRange(_state.positionX, _state.positionY, c_playerMaxRangeEnemies)
                .Where(p => p._team == _team && !p.IsDead);

            return players;
        }

        protected IEnumerable<Vehicle> getFriendlyBotsInRange()
        {
            List<Vehicle> bots = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 1000,
                                        delegate (Vehicle v)
                                        { return (v is Bot); }).Where(b => b._team == _team).ToList();
            return bots;
        }

        protected IEnumerable<Vehicle> getEnemyBotsInRange()
        {
            List<Vehicle> bots = _arena.getVehiclesInRange(_state.positionX, _state.positionY, 1000,
                                        delegate (Vehicle v)
                                        { return (v is Bot); }).Where(b => b._team != _team).ToList();
            return bots;
        }

        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected Player getTargetPlayer(ref bool bInSight)
        {

            Player target = null;
            double lastDist = double.MaxValue;
            bInSight = false;

            foreach (Player p in _arena.PlayersIngame)
            {   //Find the closest player
                if (p.IsDead)
                    continue;

                if (_arena.getTerrain(p._state.positionX, p._state.positionY).safety)
                    continue;

                int distance = (int)(_state.position().Distance(p._state.position()) * 500);
                if (p.activeUtilities.Any(util => util != null && distance >= util.cloakDistance && util.cloakDistance != -1))
                    continue;

                double dist = Helpers.distanceSquaredTo(_state, p._state);
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, p._state.positionX, p._state.positionY,
                    delegate (LvlInfo.Tile t)
                    {
                        return !t.Blocked;
                    }
                );

                if ((!bInSight || (bInSight && bClearPath)) && lastDist > dist)
                {
                    bInSight = bClearPath;
                    lastDist = dist;
                    target = p;
                }
            }

            return target;
        }
    }
}
