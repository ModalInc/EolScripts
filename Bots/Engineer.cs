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
    //Captain bot for perimeter defense bots
    //Will spawn other bots to defend a perimeter and defend his team's HQ
    class Engineer : Bot
    {
        ///////////////////////////////////////////////////
        // Member variables
        ///////////////////////////////////////////////////

        protected SteeringController steering;	//System for controlling the bot's steering
        protected Script_Eol _baseScript;			//The Eol script
        protected List<Vector3> _path;			//The path to our destination

        protected Vehicle _nextRet;             //The next turret we are running to
        protected int _pathTarget;				//The next target node of the path
        protected int _tickLastPath;			//The time at which we last made a path to the player   
        protected int _tickLastRet;             //Last time we ran to a ret
        protected int _tickLastHeal;            //Last time we healed a ret
        protected int _tickLastCollision;
        private float _seperation;
        private bool _hq;                       //Tells us if HQ exists
        public int _pylonLocation;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////

        /// <summary>
        /// Generic constructor
        /// </summary>
        public Engineer(VehInfo.Car type, Helpers.ObjectState state, Arena arena, Script_Eol BaseScript)
            : base(type, state, arena,
                    new SteeringController(type, state, arena))
        {
            Random rnd = new Random();

            _seperation = (float)rnd.NextDouble();
            steering = _movement as SteeringController;

            if (type.InventoryItems[0] != 0)
                _weapon.equip(AssetManager.Manager.getItemByID(type.InventoryItems[0]));

            _baseScript = BaseScript;
            _tickLastRet = 0;
            _tickLastHeal = 0;
        }
        /// <summary>
        /// Looks after the bot's functionality
        /// </summary>
        public override bool poll()
        {
            //Dead? Do nothing
            if (IsDead)
            {
                steering.steerDelegate = null;
                bCondemned = true;
                _baseScript._currentEngineers--;
                _baseScript.engineerBots.Remove(_team);
                return base.poll();
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

            IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _baseScript._hqVehId);
            foreach (Vehicle hq in hqs)
            {
                if (hq._team == _team)
                {
                    _hq = true;
                    break;
                }
            }

            //Lets build ourselves an HQ
            if (!_hq)
            {
                _hq = true; //Mark it as existing
                _pylonLocation = _baseScript._pylonLocation;
                //Create their HQ
                createVehicle(620, 0, 0, _team); //Build our HQ which will spawn our captain
                Random _rand = new Random();
                int rand;
                #region base turret sets
                switch (_pylonLocation)
                {
                    case 1:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -133, 20, _team);
                                //Build two MGs
                                createVehicle(400, -264, 84, _team);
                                createVehicle(400, 245, 84, _team);
                                //Build a sentry
                                createVehicle(402, 0, 50, _team);
                                //Build two plasma
                                createVehicle(700, 0, 132, _team);
                                createVehicle(700, 229, -156, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -133, 20, _team);
                                //Build two MGs
                                createVehicle(400, -264, 84, _team);
                                createVehicle(400, 245, 84, _team);
                                //Build a sentry
                                createVehicle(402, 0, 50, _team);
                                //Build two plasma
                                createVehicle(700, 0, 132, _team);
                                createVehicle(700, 229, -156, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -133, 20, _team);
                                //Build two MGs
                                createVehicle(400, -264, 84, _team);
                                createVehicle(400, 245, 84, _team);
                                //Build a sentry
                                createVehicle(402, 0, 50, _team);
                                //Build two plasma
                                createVehicle(700, 0, 132, _team);
                                createVehicle(700, 229, -156, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -133, 20, _team);
                                //Build two MGs
                                createVehicle(400, -264, 84, _team);
                                createVehicle(400, 245, 84, _team);
                                //Build a sentry
                                createVehicle(402, 0, 50, _team);
                                //Build two plasma
                                createVehicle(700, 0, 132, _team);
                                createVehicle(700, 229, -156, _team);
                                break;
                        }
                        break;
                    case 2:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, 293, 4, _team);
                                //Build two MGs
                                createVehicle(400, 168, -92, _team);
                                createVehicle(400, 168, 96, _team);
                                //Build a sentry
                                createVehicle(402, 50, 0, _team);
                                //Build two plasma
                                createVehicle(700, 90, 0, _team);
                                createVehicle(700, 195, -100, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, 293, 4, _team);
                                //Build two MGs
                                createVehicle(400, 168, -92, _team);
                                createVehicle(400, 168, 96, _team);
                                //Build a sentry
                                createVehicle(402, 50, 0, _team);
                                //Build two plasma
                                createVehicle(700, 90, 0, _team);
                                createVehicle(700, 195, -100, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, 293, 4, _team);
                                //Build two MGs
                                createVehicle(400, 168, -92, _team);
                                createVehicle(400, 168, 96, _team);
                                //Build a sentry
                                createVehicle(402, 50, 0, _team);
                                //Build two plasma
                                createVehicle(700, 90, 0, _team);
                                createVehicle(700, 195, -100, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, 293, 4, _team);
                                //Build two MGs
                                createVehicle(400, 168, -92, _team);
                                createVehicle(400, 168, 96, _team);
                                //Build a sentry
                                createVehicle(402, 50, 0, _team);
                                //Build two plasma
                                createVehicle(700, 90, 0, _team);
                                createVehicle(700, 195, -100, _team);
                                break;
                        }
                        break;
                    case 3:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, 320, -32, _team);
                                //Build two MGs
                                createVehicle(400, 90, 0, _team);
                                createVehicle(400, 368, 0, _team);
                                //Build a sentry
                                createVehicle(402, 0, -80, _team);
                                //Build two plasma
                                createVehicle(700, 208, -64, _team);
                                createVehicle(700, -48, -32, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, 320, -32, _team);
                                //Build two MGs
                                createVehicle(400, 90, 0, _team);
                                createVehicle(400, 368, 0, _team);
                                //Build a sentry
                                createVehicle(402, 0, -80, _team);
                                //Build two plasma
                                createVehicle(700, 208, -64, _team);
                                createVehicle(700, -48, -32, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, 320, -32, _team);
                                //Build two MGs
                                createVehicle(400, 90, 0, _team);
                                createVehicle(400, 368, 0, _team);
                                //Build a sentry
                                createVehicle(402, 0, -80, _team);
                                //Build two plasma
                                createVehicle(700, 208, -64, _team);
                                createVehicle(700, -48, -32, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, 320, -32, _team);
                                //Build two MGs
                                createVehicle(400, 90, 0, _team);
                                createVehicle(400, 368, 0, _team);
                                //Build a sentry
                                createVehicle(402, 0, -80, _team);
                                //Build two plasma
                                createVehicle(700, 208, -64, _team);
                                createVehicle(700, -48, -32, _team);
                                break;
                        }
                        break;
                    case 4:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -80, 0, _team);
                                //Build two MGs
                                createVehicle(400, 0, -80, _team);
                                createVehicle(400, 0, 80, _team);
                                //Build a sentry
                                createVehicle(402, -240, -90, _team);
                                //Build two plasma
                                createVehicle(700, -195, -100, _team);
                                createVehicle(700, -195, 100, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -80, 0, _team);
                                //Build two MGs
                                createVehicle(400, 0, -80, _team);
                                createVehicle(400, 0, 80, _team);
                                //Build a sentry
                                createVehicle(402, -240, -90, _team);
                                //Build two plasma
                                createVehicle(700, -195, -100, _team);
                                createVehicle(700, -195, 100, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -80, 0, _team);
                                //Build two MGs
                                createVehicle(400, 0, -80, _team);
                                createVehicle(400, 0, 80, _team);
                                //Build a sentry
                                createVehicle(402, -240, -90, _team);
                                //Build two plasma
                                createVehicle(700, -195, -100, _team);
                                createVehicle(700, -195, 100, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -80, 0, _team);
                                //Build two MGs
                                createVehicle(400, 0, -80, _team);
                                createVehicle(400, 0, 80, _team);
                                //Build a sentry
                                createVehicle(402, -240, -90, _team);
                                //Build two plasma
                                createVehicle(700, -195, -100, _team);
                                createVehicle(700, -195, 100, _team);
                                break;
                        }
                        break;
                    case 5:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -75, -316, _team);
                                //Build two MGs
                                createVehicle(400, 229, -204, _team);
                                createVehicle(400, 181, -428, _team);
                                //Build a sentry
                                createVehicle(402, 101, -108, _team);
                                //Build two plasma
                                createVehicle(700, 69, 84, _team);
                                createVehicle(700, -91, -44, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -75, -316, _team);
                                //Build two MGs
                                createVehicle(400, 229, -204, _team);
                                createVehicle(400, 181, -428, _team);
                                //Build a sentry
                                createVehicle(402, 101, -108, _team);
                                //Build two plasma
                                createVehicle(700, 69, 84, _team);
                                createVehicle(700, -91, -44, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -75, -316, _team);
                                //Build two MGs
                                createVehicle(400, 229, -204, _team);
                                createVehicle(400, 181, -428, _team);
                                //Build a sentry
                                createVehicle(402, 101, -108, _team);
                                //Build two plasma
                                createVehicle(700, 69, 84, _team);
                                createVehicle(700, -91, -44, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -75, -316, _team);
                                //Build two MGs
                                createVehicle(400, 229, -204, _team);
                                createVehicle(400, 181, -428, _team);
                                //Build a sentry
                                createVehicle(402, 101, -108, _team);
                                //Build two plasma
                                createVehicle(700, 69, 84, _team);
                                createVehicle(700, -91, -44, _team);
                                break;
                        }
                        break;
                    case 6:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -75, -25, _team);
                                //Build two MGs
                                createVehicle(400, -75, 50, _team);
                                createVehicle(400, 75, 150, _team);
                                //Build a sentry
                                createVehicle(402, -45, 50, _team);
                                //Build two plasma
                                createVehicle(700, 75, -50, _team);
                                createVehicle(700, 25, 80, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -75, -25, _team);
                                //Build two MGs
                                createVehicle(400, -75, 50, _team);
                                createVehicle(400, 75, 150, _team);
                                //Build a sentry
                                createVehicle(402, -45, 50, _team);
                                //Build two plasma
                                createVehicle(700, 75, -50, _team);
                                createVehicle(700, 25, 80, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -75, -25, _team);
                                //Build two MGs
                                createVehicle(400, -75, 50, _team);
                                createVehicle(400, 75, 150, _team);
                                //Build a sentry
                                createVehicle(402, -45, 50, _team);
                                //Build two plasma
                                createVehicle(700, 75, -50, _team);
                                createVehicle(700, 25, 80, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -75, -25, _team);
                                //Build two MGs
                                createVehicle(400, -75, 50, _team);
                                createVehicle(400, 75, 150, _team);
                                //Build a sentry
                                createVehicle(402, -45, 50, _team);
                                //Build two plasma
                                createVehicle(700, 75, -50, _team);
                                createVehicle(700, 25, 80, _team);
                                break;
                        }
                        break;
                    case 7:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -107, -332, _team);
                                //Build two MGs
                                createVehicle(400, -411, -332, _team);
                                createVehicle(400, -155, 36, _team);
                                //Build a sentry
                                createVehicle(402, 101, 36, _team);
                                //Build two plasma
                                createVehicle(700, -155, -140, _team);
                                createVehicle(700, -59, 52, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -107, -332, _team);
                                //Build two MGs
                                createVehicle(400, -411, -332, _team);
                                createVehicle(400, -155, 36, _team);
                                //Build a sentry
                                createVehicle(402, 101, 36, _team);
                                //Build two plasma
                                createVehicle(700, -155, -140, _team);
                                createVehicle(700, -59, 52, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -107, -332, _team);
                                //Build two MGs
                                createVehicle(400, -411, -332, _team);
                                createVehicle(400, -155, 36, _team);
                                //Build a sentry
                                createVehicle(402, 101, 36, _team);
                                //Build two plasma
                                createVehicle(700, -155, -140, _team);
                                createVehicle(700, -59, 52, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -107, -332, _team);
                                //Build two MGs
                                createVehicle(400, -411, -332, _team);
                                createVehicle(400, -155, 36, _team);
                                //Build a sentry
                                createVehicle(402, 101, 36, _team);
                                //Build two plasma
                                createVehicle(700, -155, -140, _team);
                                createVehicle(700, -59, 52, _team);
                                break;
                        }
                        break;
                    case 8:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -80, -112, _team);
                                //Build two MGs
                                createVehicle(400, -304, 64, _team);
                                createVehicle(400, 320, -64, _team);
                                //Build a sentry
                                createVehicle(402, -128, 48, _team);
                                //Build two plasma
                                createVehicle(700, -176, -48, _team);
                                createVehicle(700, 128, 48, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -80, -112, _team);
                                //Build two MGs
                                createVehicle(400, -304, 64, _team);
                                createVehicle(400, 320, -64, _team);
                                //Build a sentry
                                createVehicle(402, -128, 48, _team);
                                //Build two plasma
                                createVehicle(700, -176, -48, _team);
                                createVehicle(700, 128, 48, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -80, -112, _team);
                                //Build two MGs
                                createVehicle(400, -304, 64, _team);
                                createVehicle(400, 320, -64, _team);
                                //Build a sentry
                                createVehicle(402, -128, 48, _team);
                                //Build two plasma
                                createVehicle(700, -176, -48, _team);
                                createVehicle(700, 128, 48, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -80, -112, _team);
                                //Build two MGs
                                createVehicle(400, -304, 64, _team);
                                createVehicle(400, 320, -64, _team);
                                //Build a sentry
                                createVehicle(402, -128, 48, _team);
                                //Build two plasma
                                createVehicle(700, -176, -48, _team);
                                createVehicle(700, 128, 48, _team);
                                break;
                        }
                        break;
                    case 9:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -144, 64, _team);
                                //Build two MGs
                                createVehicle(400, -160, -208, _team);
                                createVehicle(400, 80, -64, _team);
                                //Build a sentry
                                createVehicle(402, 336, -16, _team);
                                //Build two plasma
                                createVehicle(700, 176, 0, _team);
                                createVehicle(700, 64, 128, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -144, 64, _team);
                                //Build two MGs
                                createVehicle(400, -160, -208, _team);
                                createVehicle(400, 80, -64, _team);
                                //Build a sentry
                                createVehicle(402, 336, -16, _team);
                                //Build two plasma
                                createVehicle(700, 176, 0, _team);
                                createVehicle(700, 64, 128, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -144, 64, _team);
                                //Build two MGs
                                createVehicle(400, -160, -208, _team);
                                createVehicle(400, 80, -64, _team);
                                //Build a sentry
                                createVehicle(402, 336, -16, _team);
                                //Build two plasma
                                createVehicle(700, 176, 0, _team);
                                createVehicle(700, 64, 128, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -144, 64, _team);
                                //Build two MGs
                                createVehicle(400, -160, -208, _team);
                                createVehicle(400, 80, -64, _team);
                                //Build a sentry
                                createVehicle(402, 336, -16, _team);
                                //Build two plasma
                                createVehicle(700, 176, 0, _team);
                                createVehicle(700, 64, 128, _team);
                                break;
                        }
                        break;
                    case 10:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -96, -48, _team);
                                //Build two MGs
                                createVehicle(400, -96, 176, _team);
                                createVehicle(400, -96, 528, _team);
                                //Build a sentry
                                createVehicle(402, 96, -50, _team);
                                //Build two plasma
                                createVehicle(700, -96, 400, _team);
                                createVehicle(700, 96, 112, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -96, -48, _team);
                                //Build two MGs
                                createVehicle(400, -96, 176, _team);
                                createVehicle(400, -96, 528, _team);
                                //Build a sentry
                                createVehicle(402, 96, -50, _team);
                                //Build two plasma
                                createVehicle(700, -96, 400, _team);
                                createVehicle(700, 96, 112, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -96, -48, _team);
                                //Build two MGs
                                createVehicle(400, -96, 176, _team);
                                createVehicle(400, -96, 528, _team);
                                //Build a sentry
                                createVehicle(402, 96, -50, _team);
                                //Build two plasma
                                createVehicle(700, -96, 400, _team);
                                createVehicle(700, 96, 112, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -96, -48, _team);
                                //Build two MGs
                                createVehicle(400, -96, 176, _team);
                                createVehicle(400, -96, 528, _team);
                                //Build a sentry
                                createVehicle(402, 96, -50, _team);
                                //Build two plasma
                                createVehicle(700, -96, 400, _team);
                                createVehicle(700, 96, 112, _team);
                                break;
                        }
                        break;                        
                    case 11:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -96, -0, _team);
                                //Build two MGs
                                createVehicle(400, 112, -128, _team);
                                createVehicle(400, 112, 128, _team);
                                //Build a sentry
                                createVehicle(402, -96, -176, _team);
                                //Build two plasma
                                createVehicle(700, -48, -144, _team);
                                createVehicle(700, 48, 144, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -96, -0, _team);
                                //Build two MGs
                                createVehicle(400, 112, -128, _team);
                                createVehicle(400, 112, 128, _team);
                                //Build a sentry
                                createVehicle(402, -96, -176, _team);
                                //Build two plasma
                                createVehicle(700, -48, -144, _team);
                                createVehicle(700, 48, 144, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -96, -0, _team);
                                //Build two MGs
                                createVehicle(400, 112, -128, _team);
                                createVehicle(400, 112, 128, _team);
                                //Build a sentry
                                createVehicle(402, -96, -176, _team);
                                //Build two plasma
                                createVehicle(700, -48, -144, _team);
                                createVehicle(700, 48, 144, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -96, -0, _team);
                                //Build two MGs
                                createVehicle(400, 112, -128, _team);
                                createVehicle(400, 112, 128, _team);
                                //Build a sentry
                                createVehicle(402, -96, -176, _team);
                                //Build two plasma
                                createVehicle(700, -48, -144, _team);
                                createVehicle(700, 48, 144, _team);
                                break;
                        }
                        break;
                    case 12:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, 336, -16, _team);
                                //Build two MGs
                                createVehicle(400, 336, 224, _team);
                                createVehicle(400, -48, -48, _team);
                                //Build a sentry
                                createVehicle(402, -48, 80, _team);
                                //Build two plasma
                                createVehicle(700, -80, 224, _team);
                                createVehicle(700, 112, 224, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, 336, -16, _team);
                                //Build two MGs
                                createVehicle(400, 336, 224, _team);
                                createVehicle(400, -48, -48, _team);
                                //Build a sentry
                                createVehicle(402, -48, 80, _team);
                                //Build two plasma
                                createVehicle(700, -80, 224, _team);
                                createVehicle(700, 112, 224, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, 336, -16, _team);
                                //Build two MGs
                                createVehicle(400, 336, 224, _team);
                                createVehicle(400, -48, -48, _team);
                                //Build a sentry
                                createVehicle(402, -48, 80, _team);
                                //Build two plasma
                                createVehicle(700, -80, 224, _team);
                                createVehicle(700, 112, 224, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, 336, -16, _team);
                                //Build two MGs
                                createVehicle(400, 336, 224, _team);
                                createVehicle(400, -48, -48, _team);
                                //Build a sentry
                                createVehicle(402, -48, 80, _team);
                                //Build two plasma
                                createVehicle(700, -80, 224, _team);
                                createVehicle(700, 112, 224, _team);
                                break;
                        }
                        break;
                    case 13:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, 112, 144, _team);
                                //Build two MGs
                                createVehicle(400, 256, 112, _team);
                                createVehicle(400, 256, -112, _team);
                                //Build a sentry
                                createVehicle(402, -64, 0, _team);
                                //Build two plasma
                                createVehicle(700, -20, 112, _team);
                                createVehicle(700, -20, -112, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, 112, 144, _team);
                                //Build two MGs
                                createVehicle(400, 256, 112, _team);
                                createVehicle(400, 256, -112, _team);
                                //Build a sentry
                                createVehicle(402, -64, 0, _team);
                                //Build two plasma
                                createVehicle(700, -20, 112, _team);
                                createVehicle(700, -20, -112, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, 112, 144, _team);
                                //Build two MGs
                                createVehicle(400, 256, 112, _team);
                                createVehicle(400, 256, -112, _team);
                                //Build a sentry
                                createVehicle(402, -64, 0, _team);
                                //Build two plasma
                                createVehicle(700, -20, 112, _team);
                                createVehicle(700, -20, -112, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, 112, 144, _team);
                                //Build two MGs
                                createVehicle(400, 256, 112, _team);
                                createVehicle(400, 256, -112, _team);
                                //Build a sentry
                                createVehicle(402, -64, 0, _team);
                                //Build two plasma
                                createVehicle(700, -20, 112, _team);
                                createVehicle(700, -20, -112, _team);
                                break;
                        }
                        break;
                    case 14:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -384, 0, _team);
                                //Build two MGs
                                createVehicle(400, -400, 144, _team);
                                createVehicle(400, 304, 16, _team);
                                //Build a sentry
                                createVehicle(402, 48, -80, _team);
                                //Build two plasma
                                createVehicle(700, -272, -50, _team);
                                createVehicle(700, 256, 144, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -384, 0, _team);
                                //Build two MGs
                                createVehicle(400, -400, 144, _team);
                                createVehicle(400, 304, 16, _team);
                                //Build a sentry
                                createVehicle(402, 48, -80, _team);
                                //Build two plasma
                                createVehicle(700, -272, -50, _team);
                                createVehicle(700, 256, 144, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -384, 0, _team);
                                //Build two MGs
                                createVehicle(400, -400, 144, _team);
                                createVehicle(400, 304, 16, _team);
                                //Build a sentry
                                createVehicle(402, 48, -80, _team);
                                //Build two plasma
                                createVehicle(700, -272, -50, _team);
                                createVehicle(700, 256, 144, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -384, 0, _team);
                                //Build two MGs
                                createVehicle(400, -400, 144, _team);
                                createVehicle(400, 304, 16, _team);
                                //Build a sentry
                                createVehicle(402, 48, -80, _team);
                                //Build two plasma
                                createVehicle(700, -272, -50, _team);
                                createVehicle(700, 256, 144, _team);
                                break;
                        }
                        break;
                    case 15:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, 117, 212, _team);
                                //Build two MGs
                                createVehicle(400, 117, -60, _team);
                                createVehicle(400, -219, 132, _team);
                                //Build a sentry
                                createVehicle(402, -395, 196, _team);
                                //Build two plasma
                                createVehicle(700, 69, 308, _team);
                                createVehicle(700, -75, -76, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, 117, 212, _team);
                                //Build two MGs
                                createVehicle(400, 117, -60, _team);
                                createVehicle(400, -219, 132, _team);
                                //Build a sentry
                                createVehicle(402, -395, 196, _team);
                                //Build two plasma
                                createVehicle(700, 69, 308, _team);
                                createVehicle(700, -75, -76, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, 117, 212, _team);
                                //Build two MGs
                                createVehicle(400, 117, -60, _team);
                                createVehicle(400, -219, 132, _team);
                                //Build a sentry
                                createVehicle(402, -395, 196, _team);
                                //Build two plasma
                                createVehicle(700, 69, 308, _team);
                                createVehicle(700, -75, -76, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, 117, 212, _team);
                                //Build two MGs
                                createVehicle(400, 117, -60, _team);
                                createVehicle(400, -219, 132, _team);
                                //Build a sentry
                                createVehicle(402, -395, 196, _team);
                                //Build two plasma
                                createVehicle(700, 69, 308, _team);
                                createVehicle(700, -75, -76, _team);
                                break;
                        }
                        break;
                    case 16:
                        rand = _rand.Next(0, 4);
                        switch (rand)
                        {
                            case 1://Build a rocket
                                createVehicle(825, -96, 16, _team);
                                //Build two MGs
                                createVehicle(400, 192, -240, _team);
                                createVehicle(400, 192, -96, _team);
                                //Build a sentry
                                createVehicle(402, 80, 64, _team);
                                //Build two plasma
                                createVehicle(700, -64, 64, _team);
                                createVehicle(700, 352, -160, _team);
                                break;
                            case 2://Build a rocket
                                createVehicle(825, -96, 16, _team);
                                //Build two MGs
                                createVehicle(400, 192, -240, _team);
                                createVehicle(400, 192, -96, _team);
                                //Build a sentry
                                createVehicle(402, 80, 64, _team);
                                //Build two plasma
                                createVehicle(700, -64, 64, _team);
                                createVehicle(700, 352, -160, _team);
                                break;
                            case 3://Build a rocket
                                createVehicle(825, -96, 16, _team);
                                //Build two MGs
                                createVehicle(400, 192, -240, _team);
                                createVehicle(400, 192, -96, _team);
                                //Build a sentry
                                createVehicle(402, 80, 64, _team);
                                //Build two plasma
                                createVehicle(700, -64, 64, _team);
                                createVehicle(700, 352, -160, _team);
                                break;
                            case 4://Build a rocket
                                createVehicle(825, -96, 16, _team);
                                //Build two MGs
                                createVehicle(400, 192, -240, _team);
                                createVehicle(400, 192, -96, _team);
                                //Build a sentry
                                createVehicle(402, 80, 64, _team);
                                //Build two plasma
                                createVehicle(700, -64, 64, _team);
                                createVehicle(700, 352, -160, _team);
                                break;
                        }
                        break;
                   
                }
                #endregion

                //////////////////////////////////////////////////
                //                  Generic Base                //
                //////////////////////////////////////////////////

                //Build a rocket
                //createVehicle(825, -75, -25, _team);
                //Build two MGs
                //createVehicle(400, -75, 50, _team);
                //createVehicle(400, 75, 150, _team);
                //Build a sentry
                //createVehicle(402, -45, 50, _team);
                //Build two plasma
                //createVehicle(700, 75, -50, _team);
                //createVehicle(700, 25, 80, _team);


                //Giving them some bounty ??based off population??

                _baseScript._hqs[_team].Bounty = 10000;

                //Captain dBot = _arena.newBot(typeof(Captain), (ushort)161, _team, null, _state, new object[] { this, null }) as Captain;

            }

            //Run around while healing to hopefully avoid being shot            
            IEnumerable<Vehicle> turrets = _arena.Vehicles.Where(v => (v._type.Id == 400 || v._type.Id == 825 || v._type.Id == 700 || v._type.Id == 402 || v._type.Id == 620) && v._team == _team);
            Random _random = new Random(System.Environment.TickCount);

            //Only heal a ret once every 7 seconds
            if (now - _tickLastHeal > 7000)
            {
                foreach (Vehicle v in turrets)
                {//Go through and heal them randomly
                    if (v._team == _team && v._state.health < _arena._server._assets.getVehicleByID(v._type.Id).Hitpoints)
                    {//Turret needs healing
                        v.assignDefaultState();
                        _tickLastHeal = now;
                        break;
                    }
                }
            }
            //Pick another ret to follow every 5 seconds
            if (now - _tickLastRet > 5000)
            {
                try
                {
                    _nextRet = turrets.ElementAt(_random.Next(0, turrets.Count()));
                }
                catch (Exception)
                {
                    _nextRet = null;
                }
                _tickLastRet = now;
            }

            //Run towards a turret
            if (_nextRet != null)
                steering.steerDelegate = steerForFollowOwner;

            //Handle normal functionality
            return base.poll();
        }
        //Creates a wall of vechicles based on length and spacing given
        public void createWall(int id, int x_offset, int y_offset, int length, int xspacing, int yspacing, Team botTeam)
        {
            for (int i = 0; i < length; i++)
            {
                createVehicle(id, x_offset + (i * xspacing), y_offset + (i * yspacing), botTeam);
            }
        }

        //Creates a turrent, offsets are from HQ
        public void createVehicle(int id, int x_offset, int y_offset, Team botTeam)
        {
            VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(id));
            Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
            newState.positionX = Convert.ToInt16(_state.positionX + x_offset);
            newState.positionY = Convert.ToInt16(_state.positionY + y_offset);
            newState.positionZ = _state.positionZ;
            newState.yaw = _state.yaw;

            _arena.newVehicle(vehicle, botTeam, null, newState);

        }

        #region Steer Delegates

        /// <summary>
        /// Keeps the combat bot around the engineer
        /// Change to keeping him around the HQ
        /// </summary>
        public Vector3 steerForFollowOwner(InfantryVehicle vehicle)
        {

            Vector3 wanderSteer = vehicle.SteerForWander(0.5f);
            Vector3 pursuitSteer = vehicle.SteerForPursuit(_nextRet.Abstract, 0.2f);

            return (wanderSteer * 1.6f) + pursuitSteer;
        }
        #endregion
    }
}
