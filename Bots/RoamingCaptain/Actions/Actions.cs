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
    {

        private List<Action> _actionQueue;

        public void fireAtEnemy(int now)
        {
            //Allows the bot to update the target every poll
            _target = null;

            //Get the closest player
            bool bClearPath = false;
            _target = getTargetPlayer(ref bClearPath);


            if (_target != null)
            {
                if (bClearPath)
                {   //What is our distance to the target?
                    double distance = (_state.position() - _target._state.position()).Length;
                    bool bFleeing = false;

                    //Too far?
                    if (distance > farDist)
                        steering.steerDelegate = steerForPersuePlayer;

                    //Too short?
                    else if (distance < runDist && _state.health <= 65)
                    {
                        bFleeing = true;
                        steering.steerDelegate = delegate (InfantryVehicle vehicle)
                        {
                            if (_target != null)
                                return vehicle.SteerForFlee(_target._state.position());
                            else
                                return Vector3.Zero;
                        };
                    }
                    //Quite short?
                    else if (distance < shortDist)
                    {
                        steering.bSkipRotate = true;
                        steering.steerDelegate = delegate (InfantryVehicle vehicle)
                        {
                            if (_target != null)
                                return vehicle.SteerForFlee(_target._state.position());
                            else
                                return Vector3.Zero;
                        };
                    }
                    //Just right
                    else
                        steering.steerDelegate = null;
                    

                        

                    //Can we shoot?
                    if (!bFleeing && _weapon.ableToFire() && distance < fireDist)
                    {
                        if (_target._state.positionZ < 10)
                            _weapon = _weaponClose;
                        else
                            _weapon = _weaponFar;

                        int aimResult = _weapon.getAimAngle(_target._state);

                        if (_weapon.isAimed(aimResult))
                        {   //Spot on! Fire?
                            _itemUseID = _weapon.ItemID;
                            _weapon.shotFired();
                        }

                        steering.bSkipAim = true;
                        steering.angle = aimResult;
                    }
                    else
                        steering.bSkipAim = false;
                        
                }
                else
                {
                    updatePath(now);

                    //Navigate to him
                    if (_path == null)
                        //If we can't find out way to him, just mindlessly walk in his direction for now
                        steering.steerDelegate = steerForPersuePlayer;
                    else
                        steering.steerDelegate = steerAlongPath;
                }


            }
        }

        public void pushToEnemyFlag(int now)
        {

            //Maintain roaming bots
            if (_baseScript.capRoamBots.ContainsKey(_team) && _baseScript.roamBots.ContainsKey(_team) && _baseScript.roamBots[_team] < _baseScript._maxRoamPerTeam && now - _tickLastSpawn > 15000)
            {//Bot team 
                _baseScript.addBotRoam(null, _state, _team);
                _tickLastSpawn = now;
            }

            Arena.FlagState targetFlag;
            List<Arena.FlagState> enemyflags = new List<Arena.FlagState>();
            List<Arena.FlagState> flags = new List<Arena.FlagState>();

            if (flags.Count() > 0)
                flags.Clear();

            if (enemyflags.Count() > 0)
                enemyflags.Clear();


            enemyflags = _arena._flags.Values.OrderBy(f => f.posX).ToList();
            flags = enemyflags.Where(f => f.posX >= _baseScript._minX && f.posX <= _baseScript._maxX && f.posY >= _baseScript._minY && f.posY <= _baseScript._maxY).ToList();

            int count = flags.Count;

            Random r = new Random();
            int _randFlag = r.Next(0, count);
            targetFlag = flags[_randFlag];

            Helpers.ObjectState target = new Helpers.ObjectState();
            target.positionX = targetFlag.posX;
            target.positionY = targetFlag.posY;

            bool bClearPath = Helpers.calcBresenhemsPredicate(
                   _arena, _state.positionX, _state.positionY, target.positionX, target.positionY,
                   delegate (LvlInfo.Tile t)
                   {
                       return !t.Blocked;
                   }
               );
            if (bClearPath)
            {
                //Persue directly!
                steering.steerDelegate = steerForFollowOwner;
            }
            else
            {
                //Does our path need to be updated?
                if (now - _tickLastPath > 1000)
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
                    steering.steerDelegate = steerForFollowOwner;
                else
                    steering.steerDelegate = steerAlongPath;
            }
        }

        public class Action
        {
            public Priority priority;
            public Type type;

            public Action(Priority pr, Type ty)
            {
                type = ty;
                priority = pr;
            }

            public enum Priority
            {
                None,
                Low,
                Medium,
                High
            }

            public enum Type
            {
                fireAtEnemy,
                retreat
            }
        }
    }
}
