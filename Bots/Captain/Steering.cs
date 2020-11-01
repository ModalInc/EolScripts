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
    public partial class Captain : Bot
    {
        #region Steer Delegates
        /// <summary>
        /// Steers the zombie along the defined path
        /// </summary>
        public Vector3 steerAlongPath(InfantryVehicle vehicle)
        {	//Are we at the end of the path?
            if (_pathTarget >= _path.Count)
            {	//Invalidate the path
                _path = null;
                _tickLastPath = 0;
                return Vector3.Zero;
            }

            //Find the nearest path point
            Vector3 point = _path[_pathTarget];

            //Are we close enough to go to the next?
            if (_pathTarget < _path.Count && vehicle.Position.Distance(point) < 0.8f)
                point = _path[_pathTarget++];

            return vehicle.SteerForSeek(point);
        }

        /// <summary>
        /// Moves the medic on a persuit course towards the player, while keeping seperated from other medics
        /// </summary>
        public Vector3 steerForPersuePlayer(InfantryVehicle vehicle)
        {
            if (_target == null)
                return Vector3.Zero;

            List<Vehicle> Captains = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, 500,
                                                                delegate (Vehicle v)
                                                                { return (v is Captain); });
            IEnumerable<IVehicle> CaptainBots = Captains.ConvertAll<IVehicle>(
                delegate (Vehicle v)
                {
                    return (v as Captain).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(_seperation, -0.707f, CaptainBots);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_target._baseVehicle.Abstract, 2.2f);

            return (seperationSteer * 2.3f) + pursuitSteer;
        }

        /// <summary>
        /// Keeps the bot around a specific player
        /// </summary>
        public Vector3 steerForWalkabout(InfantryVehicle vehicle)
        {
           if (_targetPoint == null)
                return Vector3.Zero;

            Vector3 pos = new Vector3(((float)_targetPoint.positionX) / 100.0, ((float)_targetPoint.positionY) / 100.0, 0);

            Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 seekSteer = vehicle.SteerForSeek(pos);
           // Vector3 pursuitSteer = vehicle.SteerForPursuit(seekSteer, 0.2f);

            return (wanderSteer * 1.6f) + seekSteer;

        }

        public Vector3 steerForHQ(InfantryVehicle vehicle)
        {
            if (myvHq == null)
                return Vector3.Zero;

            Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(myvHq.Abstract, 0.2f);

            return (wanderSteer * 1.6f) + pursuitSteer;

        }

        /// <summary>
        /// Keeps the bot around a specific player
        /// </summary>
        public Vector3 strafeForCombat(InfantryVehicle vehicle)
        {
            if (_target == null)
                return Vector3.Zero;

            Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_target._baseVehicle.Abstract, 0.2f);

            return (wanderSteer * 1.6f) + pursuitSteer;
        }

        #endregion
    }
}