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
    {   ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        //private Bot _bot;							//Pointer to our bot class
        private Random _rand;

        private Player _target;                     //The player we're currently stalking
        public Player owner;
        public Helpers.ObjectState _targetPoint;
        private Team _targetTeam;
        private Vehicle _roamChief;
        public BotType type;
        protected bool bOverriddenPoll;         //Do we have custom actions for poll?

        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player
        public Script_Eol _baseScript;
        public int _tickLastWander;
        protected int _tickLastCollision;

        private WeaponController _weaponClose;  //Our weapon for close range
        private WeaponController _weaponFar;    //Our weapon for anything that is not close range

        private bool _bPatrolEnemy;
        protected SteeringController steering;	//System for controlling the bot's steering
        private float _seperation;
        private int _tickNextStrafeChange;          //The last time we changed strafe direction
        private bool _bStrafeLeft;                  //Are we strafing left or right?
        private int _tickLastRadarDot;


        public Aliens(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_Eol BaseScript, Player _owner)
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

            _baseScript = BaseScript;
            owner = _owner;
        }

        public void init()
        {
            WeaponController.WeaponSettings settings = new WeaponController.WeaponSettings();
            settings.aimFuzziness = 10;

            _weapon.setSettings(settings);

            _tickLastWander = Environment.TickCount;
            _tickLastCollision = Environment.TickCount;

            base.poll();
        }

        /// <summary>
        /// Allows the script to maintain itself
        /// </summary>
        public override bool poll()
        {
            short x = 0, y = 0;

            if (bOverriddenPoll)
                return base.poll();

            //Dead? Do nothing
            if (IsDead)
            {//Dead
                steering.steerDelegate = null; //Stop movements  
                _baseScript.alienBots[_team]--; //Signal to our captain we died
                if (_baseScript.alienBots[_team] < 0)
                    _baseScript.alienBots[_team] = 0;
                bCondemned = true; //Make sure the bot gets removed in polling
                return base.poll();
            }

            //Find out if our owner is gone
            if (owner == null && !_team._name.Contains("Bot Team -"))
            {//Find a new owner if not a bot team
                if (_team.ActivePlayerCount >= 0)
                    owner = _team.ActivePlayers.Last();
                else
                {
                    kill(null);
                    _baseScript.alienBots[_team]--; //Signal to our captain we died
                    _baseScript.alienBots[_team] = 0; //Signal to our captain we died
                    bCondemned = true; //Make sure the bot gets removed in polling
                    return base.poll();
                }
            }

            //Find out if our captain died
            if (!_baseScript.alienChiefBots.ContainsKey(_team))
            {
                kill(null);
                _baseScript.alienBots[_team]--; //Signal to our captain we died
                _baseScript.alienBots[_team] = 0; //Signal to our captain we died
                bCondemned = true; //Make sure the bot gets removed in polling
                return base.poll();
            }

            //Get a list of all the roaming captains in the arena
            IEnumerable<Vehicle> roamingChiefs = _arena.Vehicles.Where(v => v._type.Id == _baseScript._roamChiefs);


            //Find our HQ
            foreach (Vehicle v in roamingChiefs)
            {
                if (v._team == _team)
                {//We found it
                    x = v._state.positionX;
                    y = v._state.positionY;
                    _roamChief = v;
                }
            }

            //Check if a chief was found, if not return to polling
            if (_roamChief == null)
                return base.poll();


            double distance = Math.Pow((Math.Pow(_state.positionX - _roamChief._state.positionX, 2) + Math.Pow(_state.positionY - _roamChief._state.positionY, 2)) / 2, 0.5);

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
                    if (distance < 100)
                    {
                        _targetPoint = null;
                        patrolChief(now);
                    }
                    else if (distance >= 100 && distance <= 1200)
                    {
                        _targetPoint = null;
                        returntoChief(now);
                    }
                    else if(distance > 1200)
                    {
                        steering.steerDelegate = null; //Stop movements  
                        _baseScript.alienBots[_team]--; //Signal to our captain we died
                        if (_baseScript.alienBots[_team] < 0)
                            _baseScript.alienBots[_team] = 0;
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