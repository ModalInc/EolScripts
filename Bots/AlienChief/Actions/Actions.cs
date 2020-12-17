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
    public partial class AlienChief : Bot
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
                    else if (distance < runDist && _state.health <= 35)
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
                        _state.fireAngle = Helpers.computeLeadFireAngle(_state, _target._state, 10000 / 1000);
                        _weapon = _weaponClose;

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
            }
        }

        public void pushToNewPoint(int now)
        {

            //Maintain roaming bots
            if (_baseScript.alienBots.ContainsKey(_team) && _baseScript.alienBots[_team] < _baseScript._maxRoamAliensPerTeam && now - _tickLastSpawn > 15000)
            {//Bot team 

                _baseScript.addBotAlien(null, _state, _team);
                _tickLastSpawn = now;
            }

            if (_targetPoint == null)
                _targetPoint = getTargetPoint();

            if (_targetPoint != null)
            {
                double distance = Math.Pow((Math.Pow(_state.positionX - _targetPoint.positionX, 2) + Math.Pow(_state.positionY - _targetPoint.positionY, 2)) / 2, 0.5);

                if (distance < 50)
                {
                    _targetPoint = null;
                    return;
                }
                else
                {
                    bool bClearPath = Helpers.calcBresenhemsPredicate(
                         _arena, _state.positionX, _state.positionY, _targetPoint.positionX, _targetPoint.positionY,
                         delegate (LvlInfo.Tile t)
                         {
                             return !t.Blocked;
                         }
                          );
                    if (bClearPath)
                    {
                        steering.steerDelegate = steerForPoint;
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

                        //Navigate to base
                        if (_path == null)
                            //If we can't find our way to base, just mindlessly walk in its direction for now
                            steering.steerDelegate = steerForPoint;
                        else
                            steering.steerDelegate = steerAlongPath;
                    }
                }
            }
        }

        public Helpers.ObjectState getTargetPoint()
        {
            Helpers.ObjectState target = new Helpers.ObjectState();
            Helpers.ObjectState postarget = new Helpers.ObjectState();

            Random r1 = new Random();
            postarget.positionX = (short)r1.Next(_baseScript._minX, _baseScript._maxX);
            postarget.positionY = (short)r1.Next(_baseScript._minY, _baseScript._maxY);

            if (postarget == null)
                return null;

            int blockedAttempts = 30;

            while (true)
            {
                Helpers.randomPositionInArea(_arena, 1000, ref postarget.positionX, ref postarget.positionY);
                if (_arena.getTile(postarget.positionX, postarget.positionY).Blocked)
                {
                    blockedAttempts--;
                    if (blockedAttempts <= 0)
                        //Consider the spawn to be blocked
                        return null;
                    continue;
                }

                target.positionX = postarget.positionX;
                target.positionY = postarget.positionY;
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
