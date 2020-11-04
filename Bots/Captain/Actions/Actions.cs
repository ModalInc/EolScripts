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
    public partial class Captain : Bot
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
                    {
                        steering.steerDelegate = steerForHQ;
                    }
                    //Too short?
                    else if (distance < runDist && _state.health <= 35)
                    {
                        steering.bSkipRotate = true;
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
                        if (_target._state.positionZ != _state.positionZ)
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
                    //_target = null;
                }
            }
        }

        public void patrolHQ(int now)
        {
            Random _rand = new Random();
            //Maintain defense bots
            if (owner == null && _baseScript.botCount.ContainsKey(_team) && _baseScript.botCount[_team] < _baseScript._maxDefenseBots && _baseScript.botCount[_team] < _baseScript._maxDefPerTeam && now - _tickLastSpawn > 4000)
            {//Bot team 
             //should probably get rid of owner for all bots
                _baseScript.addBot(null, _state, _team);
                _tickLastSpawn = now;
            }

            if (_targetPoint == null)
                _targetPoint = getTargetPoint();

            bool bClearPath = false;
            bClearPath = Helpers.calcBresenhemsPredicate(
                   _arena, _state.positionX, _state.positionY, _targetPoint.positionX, _targetPoint.positionY,
                   delegate (LvlInfo.Tile t)
                   {
                       return !t.Blocked;
                   }
               );
            if (bClearPath)
            {
                //Persue directly!
                steering.steerDelegate = steerForWalkabout;
            }
            else
            {
                //Does our path need to be updated?
                if (now - _tickLastPath > 10000)
                {
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

                //Navigate to him
                if (_path == null)
                    //If we can't find out way to him, just mindlessly walk in his direction for now
                    steering.steerDelegate = steerForWalkabout;
                else
                    steering.steerDelegate = steerAlongPath;
            }
        }

        public void ReturnToHQ(int now)
        {
            if (_targetPoint == null)
                _targetPoint = getTargetHQ();

            bool bClearPath = false;
            bClearPath = Helpers.calcBresenhemsPredicate(
                    _arena, _state.positionX, _state.positionY, _targetPoint.positionX, _targetPoint.positionY,
                    delegate (LvlInfo.Tile t)
                    {
                        return !t.Blocked;
                    }
                );
            if (bClearPath)
            {
                //Persue directly!
                steering.steerDelegate = steerForWalkabout;
            }
            else
            {
                //Does our path need to be updated?
                if (now - _tickLastPath > 10000)
                {
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

                //Navigate to him
                if (_path == null)
                    //If we can't find out way to him, just mindlessly walk in his direction for now
                    steering.steerDelegate = steerForHQ;
                else
                    steering.steerDelegate = steerAlongPath;
            }
            _tickLastReturn = now;
        }

        public Helpers.ObjectState getTargetPoint()
        {
            Helpers.ObjectState target = new Helpers.ObjectState();
            if (myvHq == null)
                return null;

            int blockedAttempts = 30;
            short pX;
            short pY;
            while (true)
            {
                pX = myvHq._state.positionX;
                pY = myvHq._state.positionY;
                Helpers.randomPositionInArea(_arena, 1000, ref pX, ref pY);
                if (_arena.getTile(pX, pY).Blocked)
                {
                    blockedAttempts--;
                    if (blockedAttempts <= 0)
                        //Consider the spawn to be blocked
                        return null;
                    continue;
                }

                target.positionX = pX;
                target.positionY = pY;
                break;
            }
            return target;
        }

        public Helpers.ObjectState getTargetHQ()
        {

            Helpers.ObjectState target = new Helpers.ObjectState();
            if (myvHq == null)
                return null;

            int blockedAttempts = 30;
            short pX;
            short pY;
            while (true)
            {
                pX = myvHq._state.positionX;
                pY = myvHq._state.positionY;
                Helpers.randomPositionInArea(_arena, 10, ref pX, ref pY);
                if (_arena.getTile(pX, pY).Blocked)
                {
                    blockedAttempts--;
                    if (blockedAttempts <= 0)
                        //Consider the spawn to be blocked
                        return null;
                    continue;
                }

                target.positionX = pX;
                target.positionY = pY;
                break;
            }
            return target;
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
