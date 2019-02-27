using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;
using Assets;
using InfServer.Logic;

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
        public int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _tickSectorScramble;        // The tick at which we check for scrambling of area
        private int _sectorDamage;
        public bool _bBoundariesDrawn;

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


        string sectorA = "Sector A";
        string sectorB = "Sector B";
        string sectorC = "Sector C";
        string sectorD = "Sector D";
        string allSector = "All Sectors";

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
            _config = arena._server._zoneConfig;
            _baseScript = baseScript;
            _rand = new Random();
            _bBoundariesDrawn = false;
            _activeTeams = new List<Team>();
            _activeSectors.Clear();
            _sectorDamage = Environment.TickCount;
            _tickSectorStart = Environment.TickCount;
            _tickSectorExpand = Environment.TickCount;
            _tickSectorScramble = Environment.TickCount;

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
        }

        public bool Poll(int now)
        {
            int playing = _arena.PlayerCount;
            if (_arena._bGameRunning)
            {
                if (!_bBoundariesDrawn)
                {
                    drawCurrentSector();
                    drawCurrentRectangleSector();
                }
                _bBoundariesDrawn = true;
            }
            
            if (playing > 0)
            {
                gameSetup();
            }
            if ((now - _sectorDamage >= 1000) && (now - _tickSectorStart >= 5000))
            {
                foreach (Player player in _arena.PlayersIngame)
                {
                    if (!player.inArea(_tLeft, _tRight, _bLeft, _bRight))
                        player.heal(oobEffect, player);
                }
                _sectorDamage = now;
            }


        }

        public bool whichSector()
        {
            int playing = _arena.PlayerCount;
            if (playing < 30)
            {
                _activeSectors.Clear();
                var sectUnder30 = _rand.Next(_sectorsToUse.Count);
                sectorName = (_sectorsToUse[sectUnder30]);
                _activeSectors.Add(sectorName);
                if (sectorName == sectorA) {
                    _tLeft = aTL;
                    _bLeft = aBL;
                    _tRight = aTR;
                    _bRight = aBR;
                    Sectors(_tLeft, _bLeft, tRight, _bRight);
                    _arena.setTicker(1, 3, 15 * 100, "Radiation warning! Only sector " + sectorName + " is open");
                }
                else if (sectorName == sectorB) {
                    _tLeft = bTL;
                    _bLeft = bBL;
                    _tRight = bTR;
                    _bRight = bBR;
                    Sectors(_tLeft, _bLeft, tRight, _bRight);
                    _arena.setTicker(1, 3, 15 * 100, "Radiation warning! Only sector " + sectorName + " is open");
                }
                else if (sectorName == sectorC) {
                    _tLeft = cTL;
                    _bLeft = cBL;
                    _tRight = cTR;
                    _bRight = cBR;
                    Sectors(_tLeft, _bLeft, tRight, _bRight);
                    _arena.setTicker(1, 3, 15 * 100, "Radiation warning! Only sector " + sectorName + " is open");
                }
                else if (sectorName == sectorD) {
                    _tLeft = dTL;
                    _bLeft = dBL;
                    _tRight = dTR;
                    _bRight = dBR;
                    Sectors(_tLeft, _bLeft, tRight, _bRight);
                    _arena.setTicker(1, 3, 15 * 100, "Radiation warning! Only sector " + sectorName + " is open");
                }
            }

            if (playing >= 30 && playing < 60)
            {
                _activeSectors.Clear();
                if (_activeSectors.Count == 0)
                {
                    var sectUnder30 = _rand.Next(_sectorsToUse.Count);
                    sectorName = _sectorsToUse[sectUnder30];
                    _activeSectors.Add(sectorName);
                    if (sectorName == "Sector A" || sectorName == "Sector D")
                    {
                        var sectUnder60 = _rand.Next(_sectorsToUseAD.Count);
                        sectorNameExp = _sectorsToUseAD[sectUnder60];
                        _activeSectors.Add(sectorNameExp);
                        _arena.setTicker(1, 3, 15 * 100, "Radiation warning! Only sectors " + sectorName + "and " + sectorNameExp + " are open");
                        if (sectorName == "Sector A" && sectorNameExp == "Sector B")
                        {
                            Sectors(aTL, bBL, aTR, bBR);
                            _tLeft = aTL;
                            _tRight = bTR;
                            _bLeft = aBL;
                            _bRight = bBR;
                        }
                        if (sectorName == "Sector A" && sectorNameExp == "Sector C")
                        {
                            Sectors(aTL, aBL, cTR, cBR);
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                        }
                        if (sectorName == "Sector D" && sectorNameExp == "Sector C")
                        {
                            Sectors(cTL, dBL, cTR, dBR);
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                        }
                        if (sectorName == "Sector D" && sectorNameExp == "Sector B")
                        {
                            Sectors(bBL, bTL, dTR, dBR);
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                        }
                    }
                    else if (sectorName == "Sector B" || sectorName == "Sector C")
                    {
                        var sectUnder60 = _rand.Next(_sectorsToUseAD.Count);
                        sectorNameExp = _sectorsToUseAD[sectUnder60];
                        _activeSectors.Add(sectorNameExp);
                        _arena.setTicker(1, 3, 15 * 100, "Radiation warning! Only sectors " + sectorName + "and " + sectorNameExp + " are open");
                        if (sectorName == "Sector B" && sectorNameExp == "Sector A")
                        {
                            Sectors(aTL, bBL, aTR, bBR);
                            _tLeft = aTL;
                            _tRight = bTR;
                            _bLeft = aBL;
                            _bRight = bBR;
                        }
                        if (sectorName == "Sector B" && sectorNameExp == "Sector D")
                        {
                            Sectors(bBL, bTL, dTR, dBR);
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                        }
                        if (sectorName == "Sector C" && sectorNameExp == "Sector A")
                        {
                            Sectors(aTL, aBL, cTR, cBR);
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                        }
                        if (sectorName == "Sector C" && sectorNameExp == "Sector D")
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
                    if (sectorName == "Sector A" || sectorName == "Sector D")
                    {
                        var sectUnder60 = _rand.Next(_sectorsToUseAD.Count);
                        sectorNameExp = _sectorsToUseAD[sectUnder60];
                        _activeSectors.Add(sectorNameExp);
                        _arena.setTicker(1, 3, 15 * 100, "Radiation warning! Only sectors " + sectorName + "and " + sectorNameExp + " are open");
                        if (sectorName == "Sector A" && sectorNameExp == "Sector B")
                        {
                            Sectors(aTL, bBL, aTR, bBR);
                            _tLeft = aTL;
                            _tRight = bTR;
                            _bLeft = aBL;
                            _bRight = bBR;
                        }
                        if (sectorName == "Sector A" && sectorNameExp == "Sector C")
                        {
                            Sectors(aTL, aBL, cTR, cBR);
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                        }
                        if (sectorName == "Sector D" && sectorNameExp == "Sector C")
                        {
                            Sectors(cTL, dBL, cTR, dBR);
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                        }
                        if (sectorName == "Sector D" && sectorNameExp == "Sector B")
                        {
                            Sectors(bBL, bTL, dTR, dBR);
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                        }
                    }
                    else if (sectorName == "Sector B" || sectorName == "Sector C")
                    {
                        var sectUnder60 = _rand.Next(_sectorsToUseAD.Count);
                        sectorNameExp = _sectorsToUseAD[sectUnder60.Item1];
                        _activeSectors.Add(sectorNameExp);
                        _arena.setTicker(1, 3, 15 * 100, "Radiation warning! Only sectors " + sectorName + "and " + sectorNameExp + " are open");
                        if (sectorName == "Sector B" && sectorNameExp == "Sector A")
                        {
                            Sectors(aTL, bBL, aTR, bBR);
                            _tLeft = aTL;
                            _tRight = bTR;
                            _bLeft = aBL;
                            _bRight = bBR;
                        }
                        if (sectorName == "Sector B" && sectorNameExp == "Sector D")
                        {
                            Sectors(bBL, bTL, dTR, dBR);
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                        }
                        if (sectorName == "Sector C" && sectorNameExp == "Sector A")
                        {
                            Sectors(aTL, aBL, cTR, cBR);
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                        }
                        if (sectorName == "Sector C" && sectorNameExp == "Sector D")
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
                string sectorName = allSector;
                _activeSectors.Add(allSector);
                _arena.setTicker(1, 3, 15 * 100, "All Sectors are currently open with low radiation levels");
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
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _bottomRightx, _bottomRighty, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1469, bottomRightx, bottomRighty, 0, fireAngle, 0);// vis


            state.positionX = _topRightx;
            state.positionY = _topRighty;
            target.positionX = _bottomRightx;
            target.positionY = _bottomRighty;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);  //Right, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _topRightx, _topRighty, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _bottomRightx, _bottomRighty, 0, fireAngle, 0);

            state.positionX = _topLeftx;
            state.positionY = _topLefty;
            target.positionX = _bottomLeftx;
            target.positionY = _bottomLefty;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000); //Left, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _topLeftx, _topLefty, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _bottomLeftx, _bottomLefty, 0, fireAngle, 0); // vis


            state.positionX = _bottomLeftx;
            state.positionY = _bottomLefty;
            target.positionX = _topLeftx;
            target.positionY = _topLefty;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 20000 / 1000);  //Left, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 1452, _bottomLeftx, _bottomLefty, 0, fireAngle, 0);
            Helpers.Player_RouteExplosion(_arena.Players, 1469, _topLeftx, _topLefty, 0, fireAngle, 0); // vis
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
                Helpers.Player_RouteExplosion(_arena.Players, 1470, circleMarkLocation, _topRightx, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 1470, circleMarkLocation, _topRightx, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }
            circleMarkLocation = _bottomLeftx;
            while (circleMarkLocation < _bottomRightx)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 1470, _bottomRightx, circleMarkLocation, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 1470, _bottomRightx, circleMarkLocation, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }

        }

        public void gameSetup()
        {
            int now = Environment.TickCount;
            int playing = _arena.PlayerCount;

            if (now - _tickGameStart > 216000000000 && playing > 0)
            {
                if ( _basescript._activeCrowns.Count == 0)
                {
                    _activeSectors.Clear();

                    whichSector();
                }
                
            }
        }

        public void gameStart()
        {	//We've started!
            _tickGameStart = Environment.TickCount;
        }

        public bool gamesEnd()
        {
            _tickGameStart = 0;
            _activeSectors.Clear();
            _bBoundariesDrawn = false;
            return true;
        }

        public bool gameReset()
        {	//Game reset, perhaps start a new one
            _tickGameStart = 0;
            _activeSectors.Clear();
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
