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
    public partial class EolBoundaries
    {
        ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;                //The zone config
        public Script_Eol _baseScript;

        private int _tickSectorStart;
        private int _tickSectorExpand;			//The tick at which we last checked for sector expansion
        public int _tickEolGameStart;				//The tick at which the game started (0 == stopped)
        private int _tickSectorScramble;        // The tick at which we check for scrambling of area
        private int _sectorDamage;
        public bool _bBoundariesDrawn;
        public bool _gameBegun;

        private List<Team> _activeTeams;
        private List<string> _activeSectors;
        string sectorName;
        string sectorNameExp;
        Random _rand;
        private List<string> _fullSectors;
        private List<string> _sectorsToUse;
        private List<string> _sectorsToUseAD;
        private List<string> _sectorsToUseBC;

        private int _minPlayers = 1;				//The minimum amount of players
        private int _gameCount = 0;
        private ItemInfo.RepairItem oobEffect = AssetManager.Manager.getItemByID(919) as ItemInfo.RepairItem;

        public short _topLeftx;
        public short _topLefty;
        public short _bottomLeftx;
        public short _bottomLefty;
        public short _topRightx;
        public short _topRighty;
        public short _bottomRightx;
        public short _bottomRighty;
        

        public struct Point
        {
            public short x;
            public short y;
            public Point(short x, short y)
            {
                this.x = x;
                this.y = y;
            }
        }


        public string sectorA = "Sector A";
        public string sectorB = "Sector B";
        public string sectorC = "Sector C";
        public string sectorD = "Sector D";
        public string allSector = "All Sectors";

        Point aTL = new Point(1, 1); //Top left of Sector A
        Point aBL = new Point(1, 6320); //Bottom Left of Sector A
        Point aTR = new Point(8736, 1); //Top Right of Sector A
        Point aBR = new Point(8736, 6320); //Bottom Right of Sector A
        Point bTL = new Point(1, 6320);
        Point bBL = new Point(1, 22064);
        Point bTR = new Point(8736, 6320);
        Point bBR = new Point(8736, 22064);
        Point cTL = new Point(8736, 1);
        Point cBL = new Point(8736, 6320);
        Point cTR = new Point(22064, 1);
        Point cBR = new Point(22064, 6320);
        Point dTL = new Point(8736, 6320);
        Point dBL = new Point(8736, 14368);
        Point dTR = new Point(22064, 6320);
        Point dBR = new Point(22064, 14368);
        Point fTL = new Point(1, 1);
        Point fBL = new Point(1, 14368);
        Point fTR = new Point(22064, 1);
        Point fBR = new Point(22064, 14368);
        Point emptyp = new Point(0, 0);
        Point _tLeft;
        Point _tRight;
        Point _bLeft;
        Point _bRight;


        public EolBoundaries(Arena arena, Script_Eol baseScript)
        {
            _arena = arena;
            Sectors(emptyp, emptyp, emptyp, emptyp);
            _config = arena._server._zoneConfig;
            _baseScript = baseScript;
            _rand = new Random();
            _bBoundariesDrawn = false;
            _activeTeams = new List<Team>();
            _sectorDamage = Environment.TickCount;
            _tickSectorStart = Environment.TickCount;
            _tickSectorScramble = Environment.TickCount;
            _gameBegun = false;
        }


        public bool Poll(int now)
        {
            int playing = _arena.PlayerCount;
            if (playing > 0)
            {
                gameStart();
            }

            if (_gameBegun == true)
            {
                if (!_bBoundariesDrawn)
                {
                    whichSector();
                    drawCurrentSector();
                    drawCurrentRectangleSector();
                    _bBoundariesDrawn = true;
                }
                
            }


            if ((now - _sectorDamage >= 1000) && (now - _tickSectorStart >= 5000))
            {
                foreach (Player player in _arena.PlayersIngame)
                {
                    if (player._team._name != "spec")
                    {
                        Helpers.ObjectState state = player.getState();
                        short px = state.positionX;
                        short py = state.positionY;
                        CfgInfo.Terrain terrain = _arena.getTerrain(px, py);
                        if (px <= _topLeftx || py <= _topLefty || px >= _bottomRightx || py >= _bottomRighty)
                        {
                            if (terrain.message != "Pioneer Station")
                            { player.heal(oobEffect, player); }
                        }
                        
                    }
                }
                _sectorDamage = now;
            }

            if (now - _tickEolGameStart > 216000000000 && playing > 0)
            {
                if (_baseScript._activeCrowns.Count == 0 && _gameBegun == true)
                {
                    _activeSectors.Clear();
                    _bBoundariesDrawn = false;
                    gameReset();

                }

            }

            return true;
        }

        

        public void whichSector()
        {
            List<string> _fullSectors = new List<string>();
            _fullSectors.Add(allSector);

            List<string> _sectorsToUse = new List<string>();
            _sectorsToUse.Add(sectorA);
            _sectorsToUse.Add(sectorB);
            _sectorsToUse.Add(sectorC);
            _sectorsToUse.Add(sectorD);

            List<string> _sectorsToUseAD = new List<string>();
            _sectorsToUseAD.Add(sectorB);
            _sectorsToUseAD.Add(sectorC);


            List<string> _sectorsToUseBC = new List<string>();
            _sectorsToUseBC.Add(sectorA);
            _sectorsToUseBC.Add(sectorD);

            List<string> _activeSectors = new List<string>();
            int playing = _arena.PlayerCount;
            if (playing < 30)
            {
                string sectUnder30 = _sectorsToUse.OrderBy(s => _rand.NextDouble()).First();
                _activeSectors.Add(sectUnder30);
                if (sectUnder30 == sectorA) { 
                    _tLeft = aTL;
                    _bLeft = aBL;
                    _tRight = aTR;
                    _bRight = aBR;
                    Sectors(_tLeft, _bLeft, _tRight, _bRight);
                    _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " is open");
                    _arena.setTicker(1, 3, 0, "Radiation warning! Only " + sectUnder30 + " is open");
                }
                else if (sectUnder30 == sectorB)
                {
                    _tLeft = bTL;
                    _bLeft = bBL;
                    _tRight = bTR;
                    _bRight = bBR;
                    Sectors(_tLeft, _bLeft, _tRight, _bRight);
                    _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " is open");
                    _arena.setTicker(1, 3, 0, "Radiation warning! Only " + sectUnder30 + " is open");
                }
                else if (sectUnder30 == sectorC)
                {
                    _tLeft = cTL;
                    _bLeft = cBL;
                    _tRight = cTR;
                    _bRight = cBR;
                    Sectors(_tLeft, _bLeft, _tRight, _bRight);
                    _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " is open");
                    _arena.setTicker(1, 3, 0, "Radiation warning! Only " + sectUnder30 + " is open");
                }
                else if (sectUnder30 == sectorD)
                {
                    _tLeft = dTL;
                    _bLeft = dBL;
                    _tRight = dTR;
                    _bRight = dBR;
                    Sectors(_tLeft, _bLeft, _tRight, _bRight);
                    _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " is open");
                    _arena.setTicker(1, 3, 0, "Radiation warning! Only " + sectUnder30 + " is open");
                }
            }

            if (playing >= 30 && playing < 60)
            {
                string sectUnder30 = _sectorsToUse.OrderBy(s => _rand.NextDouble()).First();
                _activeSectors.Add(sectUnder30);
                if (_activeSectors.Count == 0)
                {
                    
                    if (sectUnder30 == "Sector A" || sectUnder30 == "Sector D")
                    {
                        string sectUnder60 = _sectorsToUseAD.OrderBy(s => _rand.NextDouble()).First(); ;
                        _activeSectors.Add(sectUnder60);
                        _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + "and " + sectUnder60 + " are open");
                        _arena.setTicker(1, 3, 0, "Radiation warning! Only " + sectUnder30 + "and " + sectUnder60 + " are open");
                        if (sectUnder30 == "Sector A" && sectUnder60 == "Sector B")
                        {
                            Sectors(aTL, bBL, aTR, bBR);
                            _tLeft = aTL;
                            _tRight = bTR;
                            _bLeft = aBL;
                            _bRight = bBR;
                        }
                        if (sectUnder30 == "Sector A" && sectUnder60 == "Sector C")
                        {
                            Sectors(aTL, aBL, cTR, cBR);
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                        }
                        if (sectUnder30 == "Sector D" && sectUnder60 == "Sector C")
                        {
                            Sectors(cTL, dBL, cTR, dBR);
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                        }
                        if (sectUnder30 == "Sector D" && sectUnder60 == "Sector B")
                        {
                            Sectors(bBL, bTL, dTR, dBR);
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                        }
                    }
                    else if (sectUnder30 == "Sector B" || sectUnder30 == "Sector C")
                    {
                        string sectUnder60 = _sectorsToUseAD.OrderBy(s => _rand.NextDouble()).First(); ;
                        _activeSectors.Add(sectUnder60);
                        _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + "and " + sectUnder60 + " are open");
                        _arena.setTicker(1, 3, 0, "Radiation warning! Only " + sectUnder30 + "and " + sectUnder60 + " are open");
                        if (sectUnder30 == "Sector B" && sectUnder60 == "Sector A")
                        {
                            Sectors(aTL, bBL, aTR, bBR);
                            _tLeft = aTL;
                            _tRight = bTR;
                            _bLeft = aBL;
                            _bRight = bBR;
                        }
                        if (sectUnder30 == "Sector B" && sectUnder60 == "Sector D")
                        {
                            Sectors(bBL, bTL, dTR, dBR);
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                        }
                        if (sectUnder30 == "Sector C" && sectUnder60 == "Sector A")
                        {
                            Sectors(aTL, aBL, cTR, cBR);
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                        }
                        if (sectUnder30 == "Sector C" && sectUnder60 == "Sector D")
                        {
                            Sectors(cTL, dBL, cTR, dBR);
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                        }
                    }
                }
                else
                {
                    if (sectUnder30 == "Sector A" || sectUnder30 == "Sector D")
                    {
                        string sectUnder60 = _sectorsToUseBC.OrderBy(s => _rand.NextDouble()).First(); ;
                        _activeSectors.Add(sectUnder60);
                        _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + "and " + sectUnder60 + " are open");
                        _arena.setTicker(1, 3, 0, "Radiation warning! Only " + sectUnder30 + "and " + sectUnder60 + " are open");
                        if (sectUnder30 == "Sector A" && sectUnder60 == "Sector B")
                        {
                            Sectors(aTL, bBL, aTR, bBR);
                            _tLeft = aTL;
                            _tRight = bTR;
                            _bLeft = aBL;
                            _bRight = bBR;
                        }
                        if (sectUnder30 == "Sector A" && sectUnder60 == "Sector C")
                        {
                            Sectors(aTL, aBL, cTR, cBR);
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                        }
                        if (sectUnder30 == "Sector D" && sectUnder60 == "Sector C")
                        {
                            Sectors(cTL, dBL, cTR, dBR);
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                        }
                        if (sectUnder30 == "Sector D" && sectUnder60 == "Sector B")
                        {
                            Sectors(bBL, bTL, dTR, dBR);
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                        }
                    }
                    else if (sectUnder30 == "Sector B" || sectUnder30 == "Sector C")
                    {
                        string sectUnder60 = _sectorsToUseBC.OrderBy(s => _rand.NextDouble()).First(); ;
                        _activeSectors.Add(sectUnder60);
                        _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + "and " + sectUnder60 + " are open");
                        _arena.setTicker(1, 3, 0, "Radiation warning! Only " + sectUnder30 + "and " + sectUnder60 + " are open");
                        if (sectUnder30 == "Sector B" && sectUnder60 == "Sector A")
                        {
                            Sectors(aTL, bBL, aTR, bBR);
                            _tLeft = aTL;
                            _tRight = bTR;
                            _bLeft = aBL;
                            _bRight = bBR;
                        }
                        if (sectUnder30 == "Sector B" && sectUnder60 == "Sector D")
                        {
                            Sectors(bBL, bTL, dTR, dBR);
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                        }
                        if (sectUnder30 == "Sector C" && sectUnder60 == "Sector A")
                        {
                            Sectors(aTL, aBL, cTR, cBR);
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                        }
                        if (sectUnder30 == "Sector C" && sectUnder60 == "Sector D")
                        {
                            Sectors(cTL, dBL, cTR, dBR);
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                        }
                    }
                }
            }
            if (playing > 60)
            {
                Sectors(fTL, fBL, fTR, fBR);
                _activeSectors.Add(allSector);
                _arena.sendArenaMessage("All Sectors are currently open with low radiation levels");
                _arena.setTicker(1, 3, 0, "All Sectors are currently open with low radiation levels");
            }
        }

        public void Sectors(Point topLeft, Point bottomLeft, Point topRight, Point bottomRight)
        {
            _topLeftx = topLeft.x;
            _topLefty = topLeft.y;
            _bottomLeftx = bottomLeft.x;
            _bottomLefty = bottomLeft.y;
            _topRightx = topRight.x;
            _topRighty = topRight.y;
            _bottomRightx = bottomRight.x;
            _bottomRighty = bottomRight.y;

            _bBoundariesDrawn = false;
        }

        public void drawCurrentSector()
        {
            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();

            state.positionX = _bottomRightx;
            state.positionY = _bottomRighty;
            target.positionX = _topRightx;
            target.positionY = _topRighty;

            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);
            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); // Right, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _topRightx, _bottomRightx, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _topRighty, _bottomRighty, 0, fireAngle, 0);// vis


            state.positionX = _topRightx;
            state.positionY = _topRighty;
            target.positionX = _bottomRightx;
            target.positionY = _bottomRighty;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);  //Right, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _bottomRightx, _topRightx, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _bottomRighty, _topRighty, 0, fireAngle, 0);

            state.positionX = _topLeftx;
            state.positionY = _topLefty;
            target.positionX = _bottomLeftx;
            target.positionY = _bottomLefty;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); //Left, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _topLeftx, _bottomLeftx, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _topLefty, _bottomLefty, 0, fireAngle, 0); // vis


            state.positionX = _bottomLeftx;
            state.positionY = _bottomLefty;
            target.positionX = _topLeftx;
            target.positionY = _topLefty;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);  //Left, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _bottomLeftx, _topLeftx, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _bottomLefty, _topLefty, 0, fireAngle, 0); // vis
        }

        public void drawCurrentRectangleSector()
        {
            short circleMarkLocation = _topLeftx;
            short distanceBetweenCircleMarks = 100;

            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();
            state.positionX = _topRightx;
            state.positionY = _topRighty;
            target.positionX = _bottomRightx;
            target.positionY = _bottomRighty;
            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);

            while (circleMarkLocation < _topRightx)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 1470, _topLeftx, _topRightx, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 1470, _bottomLeftx, _bottomRightx, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }
            circleMarkLocation = _bottomLeftx;
            while (circleMarkLocation < _bottomRightx)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 1470, _bottomRightx, _bottomLeftx, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 1470, _bottomRightx, _bottomLeftx, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }

        }

        public void gameStart()
        {	//We've started!
            int now = Environment.TickCount;
            _tickEolGameStart = Environment.TickCount;
            _gameBegun = true;
        }

        public bool gamesEnd()
        {
            _tickEolGameStart = 0;
            _activeSectors.Clear();
            _bBoundariesDrawn = false;
            _gameBegun = false;
            Sectors(emptyp, emptyp, emptyp, emptyp);
            return true;
        }

        public bool gameReset()
        {	//Game reset, perhaps start a new one
            _tickEolGameStart = 0;
            _activeSectors.Clear();
            _bBoundariesDrawn = false;
            _gameBegun = false;
            Sectors(emptyp, emptyp, emptyp, emptyp);
            return true;
        }

        public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {
            
            if (_arena._bGameRunning)
            {
                if (killer == null)
                    _arena.sendArenaMessage(String.Format("{0} has been killed by going into a radiated area", victim._alias));
                else if (killer._alias == victim._alias)
                    _arena.sendArenaMessage(String.Format("{0} has been killed by going into a radiated area", victim._alias));
            }

            return true;
        }
    }
}
