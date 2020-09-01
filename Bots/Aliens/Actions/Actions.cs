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

        public void pushToEnemyFlag(int now, short x, short y)
        {

            Double distance = Math.Pow((Math.Pow(_state.positionX - _roamChief._state.positionX, 2) + Math.Pow(_state.positionY - _roamChief._state.positionY, 2)) / 2, 0.5);

            if (distance <= 99)
            {

                bool bClearPath = Helpers.calcBresenhemsPredicate(
                    _arena, _state.positionX, _state.positionY, x, y,
                    delegate (LvlInfo.Tile t)
                    {
                        return !t.Blocked;
                    }
                );
                if (bClearPath)
                    //Persue directly!
                    steering.steerDelegate = steerForFollowOwner;

                else
                {
                    if (now - _tickLastPath > c_pathUpdateInterval)
                    {   //Update it!
                        _tickLastPath = int.MaxValue;

                        _arena._pathfinder.queueRequest(
                            (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                            (short)((_state.positionX + _rand.Next(-150, 150)) / 16), (short)((_state.positionY + _rand.Next(-150, 150)) / 16),
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
                        //If we can't find our way to captain, just mindlessly walk in its direction for now
                        steering.steerDelegate = steerForFollowOwner;
                    else
                        steering.steerDelegate = steerAlongPath;
                }
            }
            else if(distance >= 100 && distance <= 1200)
            {//Go back to captain
             //Check again for enemies?
             //?
             //Find a clear path back
             //Get a list of all the roaming captains in the arena
                
                bool bClearPath = Helpers.calcBresenhemsPredicate(
                    _arena, _state.positionX, _state.positionY, x, y,
                    delegate (LvlInfo.Tile t)
                    {
                        return !t.Blocked;
                    }
                );
                if (bClearPath)
                    //Persue directly!
                    steering.steerDelegate = steerForFollowOwner;

                else
                {//Find a new path to captain
                 //Does our path need to be updated?
                    if (now - _tickLastPath > c_pathUpdateInterval)
                    {   //Update it!
                        _tickLastPath = int.MaxValue;

                        _arena._pathfinder.queueRequest(
                            (short)(_state.positionX / 16), (short)(_state.positionY / 16),
                            (short)(x / 16), (short)(y / 16),
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
                        steering.steerDelegate = steerForFollowOwner;
                    else
                        steering.steerDelegate = steerAlongPath;

                }
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
