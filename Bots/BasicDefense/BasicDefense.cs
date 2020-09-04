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
    public partial class BasicDefense : Bot
    {   ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        //private Bot _bot;							//Pointer to our bot class
        private Random _rand;

        private Player _target;                     //The player we're currently stalking
        public Player owner;
        public Helpers.ObjectState _targetPoint;
        public Vehicle _targetLocation;
        private Team _targetTeam;
        private Vehicle vHq;                    //Our HQ
        public BotType type;
        protected bool bOverriddenPoll;         //Do we have custom actions for poll?

        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player
        public Script_Eol _baseScript;
        public int _tickLastWander;
        protected int _tickLastCollision;
        protected Vector3 roampos;


        private bool _bPatrolEnemy;
        protected SteeringController steering;	//System for controlling the bot's steering
        private float _seperation;
        private int _tickNextStrafeChange;          //The last time we changed strafe direction
        private bool _bStrafeLeft;                  //Are we strafing left or right?
        private int _tickLastRadarDot;


        public BasicDefense(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_Eol BaseScript, Player _owner)
            : base(type, state, arena,
            new SteeringController(type, state, arena))
        {
            Random rnd = new Random();
            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;
            _rand = new Random();
            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            _actionQueue = new List<Action>();

            _baseScript = BaseScript;
            owner = _owner;
          
        }

        public void init()
        {
            WeaponController.WeaponSettings settings = new WeaponController.WeaponSettings();
            settings.aimFuzziness = 10;

            _weapon.setSettings(settings);

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
            //Get a list of all the HQs in the arena
            IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _baseScript._hqVehId);
            //Find our HQ
            foreach (Vehicle v in hqs)
            {
                if (v._team == _team)
                {//
                    vHq = v;
                }
            }
            double distance = Math.Pow((Math.Pow(_state.positionX - vHq._state.positionX, 2) + Math.Pow(_state.positionY - vHq._state.positionY, 2)) / 2, 0.5);
            //Dead? Do nothing
            if (IsDead)
            {
                steering.steerDelegate = null; //Stop movements  
                _baseScript.botCount[_team]--; //Signal to our captain we died
                if (_baseScript.botCount[_team] < 0)
                    _baseScript.botCount[_team] = 0;
                bCondemned = true; //Make sure the bot gets removed in polling
                return base.poll();
            }


            //Check if an HQ was found, if not return to polling
            if (vHq == null)
                return base.poll();

            //Find out if our owner is gone
            if (owner == null && !_team._name.Contains("Bot Team -"))
            {//Find a new owner if not a bot team
                if (_team.ActivePlayerCount >= 0)
                    owner = _team.ActivePlayers.Last();
                else
                {
                    kill(null);
                    _baseScript.botCount[_team]--; //Signal to our captain we died
                    _baseScript.botCount[_team] = 0; //Signal to our captain we died
                    bCondemned = true; //Make sure the bot gets removed in polling
                    return base.poll();
                }
            }

            //Find out if our captain died
            /*if (!_baseScript.captainBots.ContainsKey(_team))
            {
                kill(null);
                _baseScript.botCount[_team]--; //Signal to our captain we died
                _baseScript.botCount[_team] = 0; //Signal to our captain we died
                bCondemned = true; //Make sure the bot gets removed in polling
                return base.poll();
            }*/

            //Do we have an HQ to defend?
            if (_baseScript._hqs[_team] == null)
            {
                kill(null);
                bCondemned = true;
                _baseScript.botCount[_team]--; //Signal to our captain we died
                _baseScript.botCount[_team] = 0; //Signal to our captain we died
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

            if (_movement.bCollision && now - _tickLastCollision < 350)
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
                    if (distance < 300)
                        patrolHQ(now);
                    else if (distance >= 300 && distance < 1200)
                        ReturnToHQ(now);
                    else if (distance >= 1200)
                    {
                        steering.steerDelegate = null; //Stop movements  
                        _baseScript.botCount[_team]--; //Signal to our captain we died
                        if (_baseScript.botCount[_team] < 0)
                            _baseScript.botCount[_team] = 0;
                        bCondemned = true; //Make sure the bot gets removed in polling
                    }
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