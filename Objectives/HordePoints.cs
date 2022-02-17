using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Eol
{
    public class HordePoints
    {
        public short posX;
        public short posY;
        public int height;
        public int width;
        public bool active;
        public bool _isBeingCaptured;
        public Team team;
        private Arena _arena;
        private int tickLastWave;
        private int tickStartCapture;
        public event Action<Vehicle> Captured;	//Called when the point has been captured
        private Script_Eol _baseScript;
        IEnumerable<Vehicle> hps;
        public Vehicle _hPoint;
        private int tickLastReward;

        //Settings
        private int _CaptureRadius = 600;
        private int _CaptureTime = 5;

        public HordePoints(Arena arena, Vehicle hPoint, Script_Eol script)
        {
            _arena = arena;
            _hPoint = hPoint;
            tickLastWave = 0;
            hps = _arena.Vehicles.Where(v => v._type.Id == 468);
            _baseScript = script;

            posX = hPoint._state.positionX;
            posY = hPoint._state.positionY;
            team = hPoint._team;
        }
        
        public void poll(int now)
        {
            if (hps.Count() != 0)
            {
                List<Player> playersInArea = new List<Player>();
                List<Vehicle> HordePoints = new List<Vehicle>();
                List<Vehicle> botsInArea = new List<Vehicle>();
                int attackerplayers = 0;
                int attackerbots = 0;
                int biggestattacker = 0;
                int defenderplayers = 0;
                int defenderbots = 0;
                int[] botids = new int[] { 142, 144, 145, 146, 147, 157 };
                List<Team> playerteamsinarea = new List<Team>();
                Dictionary<Team, int> teamcounts = new Dictionary<Team, int>();

                playersInArea.Clear();
                HordePoints.Clear();
                botsInArea.Clear();
                playerteamsinarea.Clear();
                teamcounts.Clear();

                playersInArea = _arena.getPlayersInRange(posX, posY, 500).Where(p => !p.IsDead).ToList();
                botsInArea = _arena.getVehiclesInRange(posX, posY, 500).Where(v => v._id == 142 || v._id == 144 || v._id == 145 || v._id == 146 || v._id == 147 || v._id == 157).ToList();

                if (playersInArea.Count() != 0)
                {
                    foreach (Player pt in playersInArea)
                        playerteamsinarea.Add(pt._team);
                }

                if (playerteamsinarea.Count() != 0)
                {
                    foreach (Team t in playerteamsinarea)
                    {
                        if (!teamcounts.ContainsKey(t))
                        {
                            teamcounts.Add(t, 1);
                        }
                        else
                        {
                            int count = 0;
                            teamcounts.TryGetValue(t, out count);
                            teamcounts.Remove(t);
                            teamcounts.Add(t, count + 1);
                        }
                    }
                }

                Team attacker = teamcounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

                attackerplayers = playersInArea.Count(p => p._team != team);
                attackerbots = botsInArea.Count(v => v._team != team);
                defenderplayers = playersInArea.Count(p => p._team == team);

                if (attackerplayers > attackerbots)
                {
                    biggestattacker = attackerplayers;
                }
                else if (attackerbots > attackerplayers)
                {
                    biggestattacker = attackerbots;
                }


                if (now - tickLastReward > 2000)
                {
                    if (_hPoint._team != null)
                    {
                        if (_hPoint._team != _baseScript.botTeam4)
                        {

                            int cashReward = 1250;
                            int expReward = 950;
                            int pointReward = 1500;

                            foreach (Player player in playersInArea.Where(p => p._team == team))
                            {
                                player.sendMessage(0, String.Format("&SCU ownership reward: (Cash={0} Experience={1} Points={2})", cashReward, expReward, pointReward));
                                player.Cash += cashReward;
                                player.Experience += expReward;
                                player.BonusPoints += pointReward;

                                player.syncState();
                            }
                        }
                    }

                    tickLastReward = now;
                }

                if (biggestattacker == 0 && defenderplayers == 0)
                {
                    tickStartCapture = 0;
                }

                if (biggestattacker > defenderplayers)
                {

                    if (now - tickLastWave >= 2500)
                    {
                        Helpers.Player_RouteExplosion(_arena.Players, 3095, posX, posY, 0, 0, 0);
                        tickLastWave = now;
                    }

                    if (tickStartCapture != 0 && biggestattacker > 0)
                    {
                        int quickCaptureMod = ((biggestattacker - 1) * 1000);
                        tickStartCapture -= quickCaptureMod;
                    }

                    if (tickStartCapture != 0 && now - tickStartCapture >= 10000)
                    {
                        _arena.triggerMessage(0, 500, String.Format("{0} has taken control of the SCU.", attacker));
                        _hPoint._team = attacker;
                        _hPoint._type.Name = "SCU [" + attacker + "]";

                        tickStartCapture = 0;

                        int cashReward = 1250;
                        int expReward = 950;
                        int pointReward = 1500;

                        foreach (Player player in playersInArea.Where(p => p._team == team))
                        {
                            player.sendMessage(0, String.Format("&SCU Capture reward: (Cash={0} Experience={1} Points={2})", cashReward, expReward, pointReward));
                            player.Cash += cashReward;
                            player.Experience += expReward;
                            player.BonusPoints += pointReward;

                            player.syncState();
                        }
                    }

                }

                if (defenderplayers > biggestattacker)
                {
                    if (now - tickLastWave >= 1500)
                    {
                        tickStartCapture = 0;
                    }
                }
                else
                {
                    if (biggestattacker == defenderplayers && biggestattacker > 0 && defenderplayers > 0)
                    {
                        if (now - tickLastWave >= 1500)
                        {
                            Helpers.Player_RouteExplosion(_arena.Players, 1095, posX, posY, 0, 0, 0);
                        }
                        tickStartCapture = 0;
                    }
                }
            }
        }
    }
}
