using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;

using InfServer.Game;
using InfServer.Protocol;
using InfServer.Scripting;
using InfServer.Bots;

using Assets;
using Axiom.Math;
using Bnoerj.AI.Steering;

namespace InfServer.Script.GameType_Eol
{
    //Captain bot for flag chasing pirates
    class RoamingCaptain : Bot
    {
        ///////////////////////////////////////////////////
        // Member variables
        ///////////////////////////////////////////////////
        private Player targetEnemy;             //The enemy that is by the flag
        //private Vehicle vHq;                    //Our HQ
        private int _stalkRadius = 250;
        private int _flagRadius = 50;
        protected Script_Eol _baseScript;			//The Eol script

        private Random _rand = new Random(System.Environment.TickCount);

        protected SteeringController steering;	//System for controlling the bot's steering
        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player   
        protected int _tickLastCollision;
        protected int _tickLastSpawn;               //Tick at which we spawned a bot
        protected int lastCheckedLevel;
        private float _seperation;
        public const int c_pathUpdateInterval = 15000;
        private const int c_MaxPath = 800;
        public int _tickLastWander;
        public Helpers.ObjectState _targetPoint;
        private Team _targetTeam;
        protected int _tickLastRoamCaptain = 0;
        private int _tickLastRadarDot;
        private int _tickLastTarget;

        private int _optimalDistance = 50;         //The optimal distance from the player we want to be
        private int _optimalDistanceTolerance = 40; //The distance tolerance as we're moving back towards the player
        private int _distanceTolerance = 120;       //The tolerance from the optimal distance we accept

        private int _tickNextStrafeChange;          //The last time we changed strafe direction

        private bool _bStrafeLeft;                  //Are we strafing left or right?
        private bool _bChasing;						//Are we chasing the player back into optimal range?

        List<Arena.FlagState> flags;
        List<Arena.FlagState> enemyflags;
        List<Arena.FlagState> capturedflags;


        private WeaponController _weaponClose;  //Our weapon for close range
        private WeaponController _weaponFar;    //Our weapon for anything that is not close range

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////

        /// <summary>
        /// Generic constructor
        /// </summary>
        public RoamingCaptain(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_Eol BaseScript)
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random();

           flags = new List<Arena.FlagState>();
           enemyflags = new List<Arena.FlagState>();
           capturedflags = new List<Arena.FlagState>();

            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;

            //Our weapon to use when we are close to the enemy
            _weaponClose = new WeaponController(_state, new WeaponController.WeaponSettings());
            _weaponFar = new WeaponController(_state, new WeaponController.WeaponSettings());

            //Equip our normal weapon
            if (type.InventoryItems[0] != 0)
                _weaponFar.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            //Setup our second weapon
            if (type.InventoryItems[1] != 0)
                _weaponClose.equip(AssetManager.Manager.getItemByID(type.InventoryItems[1]));

            _tickLastWander = Environment.TickCount;
            _tickLastRoamCaptain = Environment.TickCount;

            _baseScript = BaseScript;
        }
        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {
            int now = Environment.TickCount;

            //_movement.stop();
            //steering.steerDelegate = null;

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


            if (_movement.bCollision && now - _tickLastCollision < 500)
            {
                steering.steerDelegate = delegate (InfantryVehicle vehicle)
                {
                    Vector3 seek = vehicle.SteerForFlee(steering._lastCollision);
                    return seek;
                };

                _tickLastCollision = now;
                return base.poll();
            }

           //Find out of we are suppose to be attacking anyone
            if (targetEnemy == null || !isValidTarget(targetEnemy))
            {  
                targetEnemy = getTargetPlayer();
                if (targetEnemy == null)
                {
                    _targetPoint = pushToEnemyFlag();
                }
            }

            //Do we have a target?
            if (targetEnemy != null)
            {//Yes
                //Find out how far we are from enemy
                double distance = Math.Pow((Math.Pow(_state.positionX - targetEnemy._state.positionX, 2) + Math.Pow(_state.positionY - targetEnemy._state.positionY, 2)) / 2, 0.5);

                //If we are on top of it we just spawned and should be moved somehow
                if (distance == 0)
                {//does not work for warping a bot
                    _state.positionX = Convert.ToInt16(_state.positionX + 45);
                    _state.positionY = Convert.ToInt16(_state.positionY + 45);
                }


                if (distance <= 100)
                    _weapon = _weaponClose;
                else
                    _weapon = _weaponFar;

                if (_weapon.ableToFire())
                {
                    int aimResult = _weapon.getAimAngle(targetEnemy._state);

                    if (_weapon.isAimed(aimResult))
                    {   //Spot on! Fire?
                        _itemUseID = _weapon.ItemID;
                        _weapon.shotFired();
                    }

                    steering.bSkipAim = true;
                    steering.angle = aimResult;
                }
                else
                {
                    steering.bSkipAim = false;
                }

                if ((_bChasing && distance < (_optimalDistance + _distanceTolerance) &&
                    distance > (_optimalDistance - _distanceTolerance))
                    ||
                    (!_bChasing && distance < (_optimalDistance + _optimalDistanceTolerance) &&
                    distance > (_optimalDistance - _optimalDistanceTolerance)))
                {   //We're in dueling range, and must no longer be chasing
                    _bChasing = false;

                }
                else
                    //We need to get in range!
                    _bChasing = true;

                if (distance > _optimalDistance)
                    _movement.thrustForward();
                else
                    _movement.thrustBackward();
            }
            else if (_targetPoint != null)
            {
                
                //Maintain roaming bots
                if (_baseScript.roamBots.ContainsKey(_team) && _baseScript.roamBots[_team] < _baseScript._maxRoamPerTeam && now - _tickLastSpawn > 15000)
                {//Bot team 
                    _baseScript.addBotRoam(null, _state, _team);
                    _tickLastSpawn = now;
                }
                //What is our distance to the target?
                double distance = (_state.position() - _targetPoint.position()).Length;

                //Are we there yet?
                if (distance < _flagRadius)
                {
                    if (now - _tickLastPath > Script_Eol.c_defenseRoamPathUpdateInterval)
                    {   //Update it!
                        _tickLastPath = int.MaxValue;

                        _arena._pathfinder.queueRequest(
                            (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                            (short)((_targetPoint.positionX + _rand.Next(-75, 75)) / 16), (short)((_targetPoint.positionY + _rand.Next(-75, 75)) / 16),
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
                    //Navigate around
                    if (_path == null)
                        _targetPoint = pushToEnemyFlag();
                    else
                        steering.steerDelegate = steerAlongPath;

                }
                else if (distance >= _flagRadius)
                {
                    if (now - _tickLastPath > Script_Eol.c_defenseRoamPathUpdateInterval)
                    {   //Update it!
                        _tickLastPath = int.MaxValue;

                        _arena._pathfinder.queueRequest(
                            (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                            (short)(_targetPoint.positionX / 16), (short)(_targetPoint.positionY / 16),
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
                    //Navigate around
                    if (_path == null)
                        _targetPoint = pushToEnemyFlag();
                    else
                        steering.steerDelegate = steerAlongPath;
                }
            }
            //Handle normal functionality
            return base.poll();
        }

        public Helpers.ObjectState pushToEnemyFlag()
        {
            int now = Environment.TickCount;
            Helpers.ObjectState target = null;

            if (flags.Count() > 0)
                flags.Clear();

            if (enemyflags.Count() > 0)
                enemyflags.Clear();


            enemyflags = _arena._flags.Values.OrderBy(f => f.posX).ToList();
            flags = enemyflags.Where(f => f.posX >= _baseScript._minX && f.posX <= _baseScript._maxX && f.posY >= _baseScript._minY && f.posY <= _baseScript._maxY).ToList();

            /*foreach (Arena.FlagState fs in _arena._flags.Values)
                if (fs.bActive && (fs => fs.team != null))
                    flags.Add(fs);*/

            int count = flags.Count;

            if (count > 0)
            {
                Random r = new Random();
                int _randFlag = r.Next(0, count);

                target = new Helpers.ObjectState();
                target.positionX = flags[_randFlag].posX;
                target.positionY = flags[_randFlag].posY;

                return target;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected bool isValidTarget(Player enemy)
        {	//Don't shoot a dead player
            if (enemy.IsDead)
                return false;

            //Is it too far away?
            if (Helpers.distanceTo(this, enemy) > _stalkRadius)
                return false;

            //We must have vision on it
            bool bVision = Helpers.calcBresenhemsPredicate(_arena,
                _state.positionX, _state.positionY, enemy._state.positionX, enemy._state.positionY,
                delegate(LvlInfo.Tile t)
                {
                    return !t.Blocked;
                }
            );

            return bVision;
        }
        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        /// <summary>
        /// Obtains a suitable target player
        /// </summary>
        protected Player getTargetPlayer()
        {
            Player target = null;
            //Make a list of players around us within a radius
            List<Player> inTrackingRange =
                _arena.getPlayersInRange(_state.positionX, _state.positionY, _stalkRadius);

            //Check if anyone was found
            if (inTrackingRange.Count == 0)
                return null;

            //Sort by distance to bot
            inTrackingRange.Sort(
                delegate (Player p, Player q)
                {
                    return Comparer<double>.Default.Compare(
                        Helpers.distanceSquaredTo(_state, p._state), Helpers.distanceSquaredTo(_state, q._state));
                }
            );

            //Go through all the players and find the closest one that is not on our team and is not dead            
            foreach (Player p in inTrackingRange)
            {
                //See if they are dead
                if (p.IsDead)
                    continue;

                //See if they are on our team
                if (p._team == _team)
                    continue;

                //Find a clear path
                double dist = Helpers.distanceSquaredTo(_state, p._state);
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, p._state.positionX, p._state.positionY,
                    delegate (LvlInfo.Tile t)
                    {
                        return !t.Blocked;
                    }
                );

                if (!bClearPath)
                    continue;

                target = p;

                if (target != null) _targetPoint = null;
            }
            return target;
        }


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
        /// Moves the bot on a persuit course towards the player, while keeping seperated from otherbots
        /// </summary>
        public Vector3 steerForPersuePlayer(InfantryVehicle vehicle)
        {
            if (targetEnemy == null)
                return Vector3.Zero;

            List<Vehicle> bots = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, 400,
                                                                delegate (Vehicle v)
                                                                { return (v is Bot); });
            IEnumerable<IVehicle> gbots = bots.ConvertAll<IVehicle>(
                delegate (Vehicle v)
                {
                    return (v as Bot).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(_seperation, -0.707f, gbots);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(targetEnemy._baseVehicle.Abstract, 0.2f);

            return (seperationSteer * 0.6f) + pursuitSteer;
        }
        #endregion
    }
}
