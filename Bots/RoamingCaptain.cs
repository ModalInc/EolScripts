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
        public Player owner;					//The headquarters we defend
        private Player targetEnemy;             //The enemy that is by our HQ
        private Vehicle vHq;                    //Our HQ
        private int _stalkRadius = 500;

        private Random _rand = new Random(System.Environment.TickCount);

        protected SteeringController steering;	//System for controlling the bot's steering
        protected Script_Eol eol;    			//The Eol base script
        protected List<Vector3> _path;			//The path to our destination
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player   
        protected int _tickLastCollision;
        protected int _tickLastSpawn;               //Tick at which we spawned a bot
        protected int lastCheckedLevel;
        private float _seperation;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////

        /// <summary>
        /// Generic constructor
        /// </summary>
        public RoamingCaptain(VehInfo.Car type, Helpers.ObjectState state, Arena arena)
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random();

            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;

            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));
        }
        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {

            //Maintain defense bots
            if (owner == null && _baseScript.botCount.ContainsKey(_team) && _baseScript.botCount[_team] < _baseScript._maxRoamingBots && now - _tickLastSpawn > 4000)
            {//Bot team 
             //should probably get rid of owner for all bots
                _baseScript.addBotRoam(null, _state, _team);
                _tickLastSpawn = now;
            }

            //Dead? Do nothing
            if (IsDead)
            {//Dead
                steering.steerDelegate = null; //Stop movements                
                bCondemned = true; //Make sure the bot gets removed in polling
                eol.roamingCaptianBots.Remove(_team);
                eol.roambots.Remove(_team);
                return base.poll();
            }

            //Do we have a flagger to attack?
            if (_baseScript._flags[_team] == null)
            {
                kill(null);
                bCondemned = true;
                eol.roamingCaptianBots.Remove(_team);
                eol.roambots.Remove(_team);
                return false;
            }

            int now = Environment.TickCount;

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

            //Spawn our team or respawn dead teammates

            if (_arena._bGameRunning)
            {
                if (_arena._flags.Where(f => f.Value.team == _team).Count() > 1)
                    pushToEnemyFlag(now);
            }

            //Find out of we are suppose to be attacking anyone
            if (targetEnemy == null || !isValidTarget(targetEnemy))
                targetEnemy = getTargetPlayer();            
            
            //Do we have a target?
            if (targetEnemy != null)
            {//Yes
                //Go and attack them
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, targetEnemy._state.positionX, targetEnemy._state.positionY,
                     delegate(LvlInfo.Tile t)
                     {
                         return !t.Blocked;
                     }
                 );
                if (bClearPath)
                {	//Persue directly!
                    steering.steerDelegate = steerForPersuePlayer;

                    //Can we shoot?
                    if (_weapon.ableToFire())
                    {
                        int aimResult = _weapon.getAimAngle(targetEnemy._state);

                        if (_weapon.isAimed(aimResult))
                        {	//Spot on! Fire?
                            _itemUseID = _weapon.ItemID;
                            _weapon.shotFired();
                        }
                        steering.bSkipAim = true;
                        steering.angle = aimResult;
                    }
                    else
                        steering.bSkipAim = false;
                }
                else //The path is not clear!
                {	//Does our path need to be updated?
                    if (now - _tickLastPath > Script_Eol.c_defensePathUpdateInterval)
                    {
                        //Update it!
                        _tickLastPath = int.MaxValue;

                        _arena._pathfinder.queueRequest(
                            (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                            (short)(targetEnemy._state.positionX / 16), (short)(targetEnemy._state.positionY / 16),
                            delegate(List<Vector3> path, int pathLength)
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

                //Navigate to him
                if (_path == null)
                    //If we can't find out way to him, just mindlessly walk in his direction for now
                    steering.steerDelegate = steerForPersuePlayer;
                else
                    steering.steerDelegate = steerAlongPath;
            }    

            //Handle normal functionality
            return base.poll();
        }

        public void pushToEnemyFlag(int now)
        {
            Arena.FlagState targetFlag;
            List<Arena.FlagState> enemyflags;
            List<Arena.FlagState> flags;

            flags = _arena._flags.Values.OrderBy(f => f.posX).ToList();

            enemyflags = flags.Where(f => f.team != _team).ToList();

            if (enemyflags.Count >= 1)
                targetFlag = enemyflags[_rand.Next(0, 2)];
            else
                targetFlag = enemyflags[0];


            Helpers.ObjectState target = new Helpers.ObjectState();
            target.positionX = targetFlag.posX;
            target.positionY = targetFlag.posY;



            //What is our distance to the target?
            double distance = (_state.position() - target.position()).Length;


            //Does our path need to be updated?
            if (now - _tickLastPath > c_pathUpdateInterval)
            {
                _arena._pathfinder.queueRequest(
                           (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                           (short)(target.positionX / 16), (short)(target.positionY / 16),
                           delegate (List<Vector3> path, int pathLength)
                           {
                               if (path != null)
                               {   //Is the path too long?
                                   if (pathLength > c_MaxPath)
                                   {   //Destroy ourself and let another zombie take our place
                                       //_path = null; Destroying Disasbled for now, may replace with a distance from enemy check
                                       //destroy(true);
                                       _path = path;
                                       _pathTarget = 1;
                                   }
                                   else
                                   {
                                       _path = path;
                                       _pathTarget = 1;
                                   }
                               }

                               _tickLastPath = now;
                           }
                );
            }

            //Navigate to him
            if (_path == null)
                //If we can't find out way to him, just mindlessly walk in his direction for now
                steering.steerDelegate = steerForPersuePlayer;
            else
                steering.steerDelegate = steerAlongPath;
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
                delegate(Player p, Player q)
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

                //Find a clear path
                double dist = Helpers.distanceSquaredTo(_state, p._state);
                bool bClearPath = Helpers.calcBresenhemsPredicate(_arena, _state.positionX, _state.positionY, p._state.positionX, p._state.positionY,
                    delegate(LvlInfo.Tile t)
                    {
                        return !t.Blocked;
                    }
                );

                if (!bClearPath)
                    continue;

                target = p;
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
        /// Keeps the combat bot around the engineer
        /// Change to keeping him around the HQ
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {

            Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(vHq.Abstract, 0.2f);

            return (wanderSteer * 1.6f) + pursuitSteer;
        }
        #endregion

        /// <summary>
        /// Moves the bot on a persuit course towards the player, while keeping seperated from otherbots
        /// </summary>
        public Vector3 steerForPersuePlayer(InfantryVehicle vehicle)
        {

            List<Vehicle> bots = _arena.getVehiclesInRange(vehicle.state.positionX, vehicle.state.positionY, 400,
                                                                delegate(Vehicle v)
                                                                { return (v is Bot); });
            IEnumerable<IVehicle> gbots = bots.ConvertAll<IVehicle>(
                delegate(Vehicle v)
                {
                    return (v as Bot).Abstract;
                }
            );

            Vector3 seperationSteer = vehicle.SteerForSeparation(_seperation, -0.707f, gbots);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(targetEnemy._baseVehicle.Abstract, 0.2f);

            return (seperationSteer * 0.6f) + pursuitSteer;
        }
    }
}
