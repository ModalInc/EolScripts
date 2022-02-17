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
                        _state.fireAngle = Helpers.computeLeadFireAngle(_state, _target._state, 10000 / 1000);
                        _weapon = _weaponClose;

                        int aimResult = _weapon.getAimAngle(_target._state);

                        if (_weapon.isAimed(aimResult))
                        {   //Spot on! Fire?
                            if (_weapon.ItemID == 3057)
                                _movement.freezeMovement(2000);

                            if (_weapon.ItemID == 1430)
                                _movement.freezeMovement(2000);

                            if (_weapon.ItemID == 1422)
                                _movement.freezeMovement(2000);


                            //steering.steerDelegate = null;

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

        public void patrolHQ(int now)
        {
            int queueCounter = _arena._pathfinder.queueCount();
            if (_targetPoint != null)
            {
                double distance = (_state.position() - _targetPoint.position()).Length;

                if (distance <= 30)
                    _targetPoint = null;
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
                if (now - _tickLastPath > 500)
                {
                    if (queueCounter <= 25)
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
                                       else
                                       {
                                           steering.steerDelegate = null;
                                       }

                                   }
                        );
                    }
                    else
                    {
                        _path = null;
                        steering.steerDelegate = null;
                    }
                    _tickLastPath = now;
                }

                //Navigate to him
                if (_path == null)
                    //If we can't find out way to him, just mindlessly walk in his direction for now
                    steering.steerDelegate = steerForWalkabout;
                else
                    steering.steerDelegate = steerAlongPath;
            }
            _tickLastWander = now;
        }

        public void ReturnToHQ(int now)
        {
            int queueCounter = _arena._pathfinder.queueCount();

            if (_targetPoint != null)
            {
                double distance = (_state.position() - _targetPoint.position()).Length;

                if (distance <= 100)
                    _targetPoint = null;
            }

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
                steering.steerDelegate = steerForHQ;
            }
            else
            {
                //Does our path need to be updated?
                if (now - _tickLastPath > 500)
                {
                    if (queueCounter <= 25)
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
                                       else
                                       {
                                           steering.steerDelegate = null;
                                       }

                                   }
                        );
                    }
                    else
                    {
                        _path = null;
                        steering.steerDelegate = null;
                    }
                    _tickLastPath = now;
                }

                //Navigate to him
                if (_path == null)
                    //If we can't find out way to him, just mindlessly walk in his direction for now
                    steering.steerDelegate = steerForHQ;
                else
                    steering.steerDelegate = steerAlongPath;
            }
        }

        public Helpers.ObjectState getTargetPoint()
        {
            Helpers.ObjectState target = new Helpers.ObjectState();

            if (vHq == null)
                return null;

            int blockedAttempts = 30;
            short pX;
            short pY;
            while (true)
            {
                pX = vHq._state.positionX;
                pY = vHq._state.positionY;
                Helpers.randomPositionInArea(_arena, 1500, ref pX, ref pY);
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

            if (vHq == null)
                return null;

            int blockedAttempts = 30;
            short pX;
            short pY;
            while (true)
            {
                pX = vHq._state.positionX;
                pY = vHq._state.positionY;
                //Helpers.randomPositionInArea(_arena, 10, ref pX, ref pY);
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

        /*public void createTargetVehicle(int id, int x_offset, int y_offset, Team botTeam, Helpers.ObjectState loc)
        {
            VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(id));
            Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
            newState.positionX = Convert.ToInt16(loc.positionX + x_offset);
            newState.positionY = Convert.ToInt16(loc.positionY + y_offset);
            newState.positionZ = loc.positionZ;
            newState.yaw = loc.yaw;

            _arena.newVehicle(vehicle, botTeam, null, newState);

        }*/

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
