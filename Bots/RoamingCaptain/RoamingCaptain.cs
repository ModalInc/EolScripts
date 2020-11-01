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
    public partial class RoamingCaptain : Bot
    {   ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        //private Bot _bot;							//Pointer to our bot class
        private Random _rand;

        private Player _target;                     //The player we're currently stalking
        public Player _leader;
        public Helpers.ObjectState _targetPoint;
        private Team _targetTeam;
        public BotType type;
        protected bool bOverriddenPoll;         //Do we have custom actions for poll?

        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player
        public Script_Eol _baseScript;
        public int _tickLastWander;
        protected int _tickLastSpawn;               //Tick at which we spawned a roaming attacker bot
        protected int _tickLastCollision;
        private WeaponController _weaponClose;  //Our weapon for close range
        private WeaponController _weaponFar;    //Our weapon for anything that is not close range

        private bool _bPatrolEnemy;
        protected SteeringController steering;	//System for controlling the bot's steering
        private float _seperation;
        private int _tickNextStrafeChange;          //The last time we changed strafe direction
        private bool _bStrafeLeft;                  //Are we strafing left or right?
        private int _tickLastRadarDot;

        List<Arena.FlagState> capturedflags;


        public RoamingCaptain(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_Eol BaseScript)
            : base(type, state, arena,
            new SteeringController(type, state, arena))
        {
            Random rnd = new Random();
            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;
            _rand = new Random();
            //Our weapon to use when we are close to the enemy
            _weaponClose = new WeaponController(_state, new WeaponController.WeaponSettings());
            _weaponFar = new WeaponController(_state, new WeaponController.WeaponSettings());

            //Equip our normal weapon
            if (type.InventoryItems[0] != 0)
                _weaponFar.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            //Setup our second weapon
            if (type.InventoryItems[1] != 0)
                _weaponClose.equip(AssetManager.Manager.getItemByID(type.InventoryItems[1]));

            _actionQueue = new List<Action>();

            capturedflags = new List<Arena.FlagState>();

            _baseScript = BaseScript;

            
        }

        public void init()
        {
            WeaponController.WeaponSettings settings = new WeaponController.WeaponSettings();
            settings.aimFuzziness = 10;

            _weapon.setSettings(settings);

            _tickLastWander = Environment.TickCount;
            _tickLastSpawn = Environment.TickCount;
            _tickLastCollision = Environment.TickCount;

            base.poll();
        }



        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public override bool poll()
        {
            if (bOverriddenPoll)
                return base.poll();

            //Dead? Do nothing
            if (IsDead)
            {//Dead
                steering.steerDelegate = null; //Stop movements                
                bCondemned = true; //Make sure the bot gets removed in polling
                _baseScript.roamingCaptianBots.Remove(_team);
                _baseScript.capRoamBots.Remove(_team);
                _baseScript._currentRoamCaptains--;
                return base.poll();
            }

            if (capturedflags.Count != 0) capturedflags.Clear();

            capturedflags = _arena._flags.Values.OrderBy(f => f.team != null).ToList();

            if (capturedflags.Count() == 0)
            {
                kill(null);
                bCondemned = true;
                _baseScript.roamingCaptianBots.Remove(_team);
                _baseScript.capRoamBots.Remove(_team);
                _baseScript._currentRoamCaptains--;
                return false;
            }

            int now = Environment.TickCount;

            //Radar Dot
            if (now - _tickLastRadarDot >= 900)
            {
                _tickLastRadarDot = now;
                IEnumerable<Player> enemies = _arena.Players.Where(p => p._team != _team);
                //Helpers.Player_RouteExplosion(_team.ActivePlayers, 1131, _state.positionX, _state.positionY, 0, 0, 0);
                //Helpers.Player_RouteExplosion(enemies, 1130, _state.positionX, _state.positionY, 0, 0, 0);
            }

            if (_movement.bCollision && now - _tickLastCollision < 1000)
            {
                steering.steerDelegate = delegate (InfantryVehicle vehicle)
                {
                    Vector3 seek = vehicle.SteerForFlee(steering._lastCollision);
                    return seek;
                };

                _tickLastCollision = now;
                return base.poll();

            }

            pollForActions(now);

            if (_actionQueue.Count() > 0)
            {
                _actionQueue.OrderByDescending(a => a.priority);

                Action currentAction = _actionQueue.First();

                switch (currentAction.type)
                {
                    case Action.Type.fireAtEnemy:
                        {
                            fireAtEnemy(now);
                        }
                        break;

                }
                _actionQueue.Remove(currentAction);
            }
            else
            {
                if (_arena._bGameRunning)
                {
                    pushToEnemyFlag(now);
                }

            }
            //Handle normal functionality
            return base.poll();
        }

        public void updatePath(int now)
        {
            //Does our path need to be updated?
            if (now - _tickLastPath > c_pathUpdateInterval)
            {   //Update it!
                _tickLastPath = int.MaxValue;

                _arena._pathfinder.queueRequest(
                    (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                    (short)(_target._state.positionX / 16), (short)(_target._state.positionY / 16),
                    delegate (List<Vector3> path, int pathLength)
                    {
                        if (path != null)
                        {   
                                _path = path;
                                _pathTarget = 1;
                        }

                        _tickLastPath = now;
                    }
                );
            }
        }
    }
}