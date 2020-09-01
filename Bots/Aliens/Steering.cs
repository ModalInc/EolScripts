using System;
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
    public partial class Aliens : Bot
    {
        #region Steer Delegates
        /// <summary>
        /// Steers the bot along the defined path
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
        /// Moves the bot on a persuit course towards the player, while keeping seperated from other bots
        /// </summary>
        public Vector3 steerForPersuePlayer(InfantryVehicle vehicle)
        {
            if (_target == null)
                return Vector3.Zero;

            List<Vehicle> Alienss = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, 500,
                                                                delegate (Vehicle v)
                                                                { return (v is Aliens); });
            IEnumerable<IVehicle> AlienBots = Alienss.ConvertAll<IVehicle>(
                delegate (Vehicle v)
                {
                    return (v as Aliens).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(_seperation, -0.707f, AlienBots);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_target._baseVehicle.Abstract, 0.2f);

            return (seperationSteer * 2.3f) + pursuitSteer;
        }

        /// <summary>
        /// Keeps the bot around a specific player
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {

            Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_roamChief.Abstract, 0.2f);

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
