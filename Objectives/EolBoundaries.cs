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
        private int _drawnSector;
        public bool _bBoundariesDrawn;
        public bool _gameBegun;
        public bool bOneSector;
        public bool bTwoSector;
        public bool bAllSectors;
        public bool bbetweengames;

        public short _top;
        public short _bottom;
        public short _left;
        public short _right;

        private List<Team> _activeTeams;
        public List<string> _activeSectors;
        public string sectUnder30;
        public string sectUnder60;
        Random _rand;

        public List<WarpPoint> _warpsSecA;
        public List<WarpPoint> _warpsSecB;
        public List<WarpPoint> _warpsSecC;
        public List<WarpPoint> _warpsSecD;
        public List<WarpPoint> _warpsSecAB;
        public List<WarpPoint> _warpsSecAC;
        public List<WarpPoint> _warpsSecBD;
        public List<WarpPoint> _warpsSecCD;
        public List<WarpPoint> _warpsSecAll;

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

        public struct WarpPoint
        {
            public short x;
            public short y;
            public int radius;
            public WarpPoint(short x, short y, int radius)
            {
                this.x = x;
                this.y = y;
                this.radius = radius;
            }
        }
        #region Warp Points
        //Warp Points for sectors//
        //Sector A
        WarpPoint SecA1 = new WarpPoint(1648, 2192, 250);
        WarpPoint SecA2 = new WarpPoint(7808, 1536, 250);
        WarpPoint SecA3 = new WarpPoint(656, 4688, 250);
        //Sector B
        WarpPoint SecB1 = new WarpPoint(4832, 8528, 250);
        WarpPoint SecB2 = new WarpPoint(8160, 12128, 250);
        WarpPoint SecB3 = new WarpPoint(4512, 12912, 250);
        //Sector C
        WarpPoint SecC1 = new WarpPoint(11632, 2320, 250);
        WarpPoint SecC2 = new WarpPoint(15520, 5024, 250);
        WarpPoint SecC3 = new WarpPoint(21120, 4288, 250);
        //Sector D
        WarpPoint SecD1 = new WarpPoint(11296, 10000, 250);
        WarpPoint SecD2 = new WarpPoint(13104, 13728, 250);
        WarpPoint SecD3 = new WarpPoint(18848, 8320, 250);
        //Sectors A and B Joined
        WarpPoint SecAB1 = new WarpPoint(1648, 2192, 250);
        WarpPoint SecAB2 = new WarpPoint(7808, 1536, 250);
        WarpPoint SecAB3 = new WarpPoint(656, 4688, 250);
        WarpPoint SecAB4 = new WarpPoint(4832, 8528, 250);
        WarpPoint SecAB5 = new WarpPoint(8160, 12128, 250);
        WarpPoint SecAB6 = new WarpPoint(4512, 12912, 250);
        //Sectors A and C Joined
        WarpPoint SecAC1 = new WarpPoint(1648, 2192, 250);
        WarpPoint SecAC2 = new WarpPoint(7808, 1536, 250);
        WarpPoint SecAC3 = new WarpPoint(656, 4688, 250);
        WarpPoint SecAC4 = new WarpPoint(11632, 2320, 250);
        WarpPoint SecAC5 = new WarpPoint(15520, 5024, 250);
        WarpPoint SecAC6 = new WarpPoint(21120, 4288, 250);
        //Sectors C and D Joined
        WarpPoint SecCD1 = new WarpPoint(11632, 2320, 250);
        WarpPoint SecCD2 = new WarpPoint(15520, 5024, 250);
        WarpPoint SecCD3 = new WarpPoint(21120, 4288, 250);
        WarpPoint SecCD4 = new WarpPoint(11296, 10000, 250);
        WarpPoint SecCD5 = new WarpPoint(13104, 13728, 250);
        WarpPoint SecCD6 = new WarpPoint(18848, 8320, 250);
        //Sectors B and D Joined
        WarpPoint SecBD1 = new WarpPoint(4832, 8528, 250);
        WarpPoint SecBD2 = new WarpPoint(8160, 12128, 250);
        WarpPoint SecBD3 = new WarpPoint(4512, 12912, 250);
        WarpPoint SecBD4 = new WarpPoint(11296, 10000, 250);
        WarpPoint SecBD5 = new WarpPoint(13104, 13728, 250);
        WarpPoint SecBD6 = new WarpPoint(18848, 8320, 250);
        //All sectors
        WarpPoint SecAll1 = new WarpPoint(7536, 1536, 500);
        WarpPoint SecAll2 = new WarpPoint(5184, 7520, 500);
        WarpPoint SecAll3 = new WarpPoint(1776, 6256, 1500);
        WarpPoint SecAll4 = new WarpPoint(7872, 5328, 2000);
        WarpPoint SecAll5 = new WarpPoint(8480, 6096, 4000);
        #endregion

        #region boundary points
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
        Point bBL = new Point(1, 14368);
        Point bTR = new Point(8736, 6320);
        Point bBR = new Point(8736, 14368);
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
        #endregion 

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
            _drawnSector = Environment.TickCount;
            _gameBegun = false;
            bOneSector = false;
            bTwoSector = false;
            bAllSectors = false;
            bbetweengames = false;
        }


        public bool Poll(int now)
        {
            int playing = _arena.PlayerCount;
            gameStart();

            if (_gameBegun == true)
            {
                if (!_bBoundariesDrawn)
                {
                    whichSector();
                    _bBoundariesDrawn = true;
                }
                
            }

            if ((now - _drawnSector >= 4100) && _bBoundariesDrawn == true)
            {
                drawCurrentSector();
                _drawnSector = now;
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
            


            return true;
        }

        
        #region Sector workings
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
            if (playing < 2) //30
            {
                sectUnder30 = _sectorsToUse.OrderBy(s => _rand.Next()).First();
                _activeSectors.Add(sectUnder30);
                if (sectUnder30 == sectorA) { 
                    _tLeft = aTL;
                    _bLeft = aBL;
                    _tRight = aTR;
                    _bRight = aBR;
                    Sectors(_tLeft, _bLeft, _tRight, _bRight);
                    _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " is open", _config.flag.victoryWarningBong);
                    bOneSector = true;
                    bTwoSector = false;
                    bAllSectors = false;
                }
                else if (sectUnder30 == sectorB)
                {
                    _tLeft = bTL;
                    _bLeft = bBL;
                    _tRight = bTR;
                    _bRight = bBR;
                    Sectors(_tLeft, _bLeft, _tRight, _bRight);
                    _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " is open", _config.flag.victoryWarningBong);
                    bOneSector = true;
                    bTwoSector = false;
                    bAllSectors = false;
                }
                else if (sectUnder30 == sectorC)
                {
                    _tLeft = cTL;
                    _bLeft = cBL;
                    _tRight = cTR;
                    _bRight = cBR;
                    Sectors(_tLeft, _bLeft, _tRight, _bRight);
                    _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " is open", _config.flag.victoryWarningBong);
                    bOneSector = true;
                    bTwoSector = false;
                    bAllSectors = false;
                }
                else if (sectUnder30 == sectorD)
                {
                    _tLeft = dTL;
                    _bLeft = dBL;
                    _tRight = dTR;
                    _bRight = dBR;
                    Sectors(_tLeft, _bLeft, _tRight, _bRight);
                    _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " is open", _config.flag.victoryWarningBong);
                    bOneSector = true;
                    bTwoSector = false;
                    bAllSectors = false;
                }
            }

            if (playing >= 2 && playing < 60) //30
            {
                sectUnder30 = _sectorsToUse.OrderBy(s => _rand.Next()).First();
                _activeSectors.Add(sectUnder30);
                if (_activeSectors.Count() == 0)
                {

                    if (sectUnder30 == sectorA || sectUnder30 == sectorD)
                    {
                        sectUnder60 = _sectorsToUseAD.OrderBy(s => _rand.Next()).First();
                        _activeSectors.Add(sectUnder60);
                        if (sectUnder30 == sectorA && sectUnder60 == sectorB)
                        {
                            _tLeft = aTL;
                            _tRight = aTR;
                            _bLeft = bBL;
                            _bRight = bBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " and " + sectUnder60 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorA && sectUnder60 == sectorC)
                        {
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " and " + sectUnder60 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorD && sectUnder60 == sectorC)
                        {
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder60 + " and " + sectUnder30 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorD && sectUnder60 == sectorB)
                        {
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder60 + " and " + sectUnder30 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                    }
                    else if (sectUnder30 == sectorB || sectUnder30 == sectorC)
                    {
                        sectUnder60 = _sectorsToUseBC.OrderBy(s => _rand.Next()).First();
                        _activeSectors.Add(sectUnder60);
                        if (sectUnder30 == sectorB && sectUnder60 == sectorA)
                        {
                            _tLeft = aTL;
                            _tRight = aTR;
                            _bLeft = bBL;
                            _bRight = bBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder60 + " and " + sectUnder30 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorB && sectUnder60 == sectorD)
                        {
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " and " + sectUnder60 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorC && sectUnder60 == sectorA)
                        {
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder60 + " and " + sectUnder30 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorC && sectUnder60 == sectorD)
                        {
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " and " + sectUnder60 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                    }
                }
                else
                {
                    if (sectUnder30 == sectorA || sectUnder30 == sectorD)
                    {
                        sectUnder60 = _sectorsToUseAD.OrderBy(s => _rand.Next()).First();
                        _activeSectors.Add(sectUnder60);
                        if (sectUnder30 == sectorA && sectUnder60 == sectorB)
                        {
                            _tLeft = aTL;
                            _tRight = aTR;
                            _bLeft = bBL;
                            _bRight = bBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " and " + sectUnder60 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorA && sectUnder60 == sectorC)
                        {
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " and " + sectUnder60 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorD && sectUnder60 == sectorC)
                        {
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder60 + " and " + sectUnder30 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorD && sectUnder60 == sectorB)
                        {
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder60 + " and " + sectUnder30 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                    }
                    else if (sectUnder30 == sectorB || sectUnder30 == sectorC)
                    {
                        sectUnder60 = _sectorsToUseBC.OrderBy(s => _rand.Next()).First();
                        _activeSectors.Add(sectUnder60);
                        if (sectUnder30 == sectorB && sectUnder60 == sectorA)
                        {
                            _tLeft = aTL;
                            _tRight = aTR;
                            _bLeft = bBL;
                            _bRight = bBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder60 + " and " + sectUnder30 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorB && sectUnder60 == sectorD)
                        {
                            _tLeft = bTL;
                            _tRight = dTR;
                            _bLeft = bBL;
                            _bRight = dBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " and " + sectUnder60 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorC && sectUnder60 == sectorA)
                        {
                            _tLeft = aTL;
                            _tRight = cTR;
                            _bLeft = aBL;
                            _bRight = cBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder60 + " and " + sectUnder30 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                        if (sectUnder30 == sectorC && sectUnder60 == sectorD)
                        {
                            _tLeft = cTL;
                            _tRight = cTR;
                            _bLeft = dBL;
                            _bRight = dBR;
                            Sectors(_tLeft, _bLeft, _tRight, _bRight);
                            _arena.sendArenaMessage("Radiation warning! Only " + sectUnder30 + " and " + sectUnder60 + " are open", _config.flag.victoryWarningBong);
                            bOneSector = false;
                            bTwoSector = true;
                            bAllSectors = false;
                        }
                    }
                }
            }
            if (playing > 60)
            {
                Sectors(fTL, fBL, fTR, fBR);
                _activeSectors.Add(allSector);
                _arena.sendArenaMessage("All Sectors are currently open with low radiation levels", _config.flag.victoryWarningBong);
                bOneSector = false;
                bTwoSector = false;
                bAllSectors = true;
            }
        }

        public void Sectors(Point topLeft, Point bottomLeft, Point topRight, Point bottomRight)
        {

            _top = topLeft.y;
            _bottom = bottomLeft.y;
            _left = topLeft.x;
            _right = topRight.x;

            _topLeftx = topLeft.x;
            _topLefty = topLeft.y;
            _topRightx = topRight.x;
            _topRighty = topRight.y;
            _bottomLeftx = bottomLeft.x;
            _bottomLefty = bottomLeft.y;
            _bottomRightx = bottomRight.x;
            _bottomRighty = bottomRight.y;
            drawCurrentSector();
            _drawnSector = Environment.TickCount;
            _bBoundariesDrawn = false;
        }

        public void drawCurrentSector()
        {
            drawCurrentRecSectorSides();
            drawCurrentRecSector();
        }

        public void drawCurrentRecSectorSides()
        {
            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();
            short _middle = (short)(_top + (_bottom - _top) / 2);
            short _middleTop = (short)(_top + (_middle - _top) / 2);
            short _middleBottom = (short)(_middle + (_bottom - _middle) / 2);

            state.positionX = _right;
            state.positionY = _bottom;
            target.positionX = _right;
            target.positionY = _top;

            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 2000 / 100);
            fireAngle = Helpers.computeLeadFireAngle(state, target, 2000 / 100); // Right, Bottom to Top
            Helpers.Player_RouteExplosion(_arena.Players, 2552, _right, _bottom, 0, fireAngle, 0);
            //Helpers.Player_RouteExplosion(_arena.Players, 2552, _right, _middle, 0, fireAngle, 0); //Middle
            //Helpers.Player_RouteExplosion(_arena.Players, 2552, _right, _middleTop, 0, fireAngle, 0); //MiddleTop
            //Helpers.Player_RouteExplosion(_arena.Players, 2552, _right, _middleBottom, 0, fireAngle, 0); //MiddleBottom
            //Helpers.Player_RouteExplosion(_arena.Players, 2552, _right, _bottom, 0, fireAngle, 0);//vis

            state.positionX = _left;
            state.positionY = _top;
            target.positionX = _left;
            target.positionY = _bottom;

            fireAngle = Helpers.computeLeadFireAngle(state, target, 2000 / 100); //Left, Top to Bottom
            Helpers.Player_RouteExplosion(_arena.Players, 2552, _left, _top, 0, fireAngle, 0);
            //Helpers.Player_RouteExplosion(_arena.Players, 2552, _left, _middle, 0, fireAngle, 0); // Middle
            //Helpers.Player_RouteExplosion(_arena.Players, 2552, _left, _middleTop, 0, fireAngle, 0); // MiddleTop
            //Helpers.Player_RouteExplosion(_arena.Players, 2552, _left, _middleBottom, 0, fireAngle, 0); // MiddleBottom
            //Helpers.Player_RouteExplosion(_arena.Players, 2552, _left, _top, 0, fireAngle, 0); // vis

        }

        public void drawCurrentRecSector()
        {
            short circleMarkLocation = _left;
            short distanceBetweenCircleMarks = 500;

            Helpers.ObjectState state = new Helpers.ObjectState();
            Helpers.ObjectState target = new Helpers.ObjectState();
            state.positionX = _right;
            state.positionY = _top;
            target.positionX = _right;
            target.positionY = _bottom;
            byte fireAngle = Helpers.computeLeadFireAngle(state, target, 2000 / 100);

            while (circleMarkLocation < _right)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 2552, circleMarkLocation, _top, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 2552, circleMarkLocation, _bottom, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }
            circleMarkLocation = _top;
            while (circleMarkLocation < _bottom)
            {
                Helpers.Player_RouteExplosion(_arena.Players, 2552, _left, circleMarkLocation, 0, fireAngle, 0);
                Helpers.Player_RouteExplosion(_arena.Players, 2552, _right, circleMarkLocation, 0, fireAngle, 0);
                circleMarkLocation += distanceBetweenCircleMarks;
            }

        }
        #endregion


        public void gameStart()
        {	//We've started!
            int now = Environment.TickCount;
            _tickEolGameStart = Environment.TickCount;
            _gameBegun = true;
            bbetweengames = false;
            //_arena.flagSpawn();
        }

        public bool gameEnd()
        {
            _tickEolGameStart = 0;
            _bBoundariesDrawn = false;
            _gameBegun = false;
            Sectors(emptyp, emptyp, emptyp, emptyp);
            sectUnder30 = "";
            sectUnder60 = "";
            bbetweengames = true;
            _baseScript._bpylonsSpawned = false;
            //_arena.flagReset();
            return true;
        }

        public bool gameReset()
        {	//Game reset, perhaps start a new one
            _tickEolGameStart = 0;
            _bBoundariesDrawn = false;
            _gameBegun = false;
            Sectors(emptyp, emptyp, emptyp, emptyp);
            sectUnder30 = "";
            sectUnder60 = "";
            bbetweengames = true;
            //_arena.flagReset();
            return true;
        }

        #region Player Events

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

        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            
            Helpers.ObjectState warpPoint;
            if (_gameBegun && bbetweengames == false)
            {
                if (portal.GeneralData.Name.Contains("MapPortal"))
                {
                    if (bOneSector == true)
                    {
                        if (sectUnder30 == sectorA)
                        {
                            var listNums = Enumerable.Range(1, 3).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecA1.x, SecA1.y, SecA1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecA2.x, SecA2.y, SecA2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecA3.x, SecA3.y, SecA3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }


                        }
                        if (sectUnder30 == sectorB)
                        {
                            var listNums = Enumerable.Range(1, 3).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecB1.x, SecB1.y, SecB1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecB2.x, SecB2.y, SecB2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecB3.x, SecB3.y, SecB3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorC)
                        {
                            var listNums = Enumerable.Range(1, 3).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecC1.x, SecC1.y, SecC1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecC2.x, SecC2.y, SecC2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecC3.x, SecC3.y, SecC3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorD)
                        {
                            var listNums = Enumerable.Range(1, 3).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecD1.x, SecD1.y, SecD1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecD2.x, SecD2.y, SecD2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecD3.x, SecD3.y, SecD3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                    }
                    else if (bTwoSector == true)
                    {
                        if (sectUnder30 == sectorA && sectUnder60 == sectorB)
                        {
                            var listNums = Enumerable.Range(1, 6).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB1.x, SecAB1.y, SecAB1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB2.x, SecAB2.y, SecAB2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB3.x, SecAB3.y, SecAB3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB4.x, SecAB4.y, SecAB4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB5.x, SecAB5.y, SecAB5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 6:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB6.x, SecAB6.y, SecAB6.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorA && sectUnder60 == sectorC)
                        {
                            var listNums = Enumerable.Range(1, 6).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC1.x, SecAC1.y, SecAC1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC2.x, SecAC2.y, SecAC2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC3.x, SecAC3.y, SecAC3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC4.x, SecAC4.y, SecAC4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC5.x, SecAC5.y, SecAC5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 6:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC6.x, SecAC6.y, SecAC6.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorC && sectUnder60 == sectorD)
                        {
                            var listNums = Enumerable.Range(1, 6).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD1.x, SecCD1.y, SecCD1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD2.x, SecCD2.y, SecCD2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD3.x, SecCD3.y, SecCD3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD4.x, SecCD4.y, SecCD4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD5.x, SecCD5.y, SecCD5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 6:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD6.x, SecCD6.y, SecCD6.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorB && sectUnder60 == sectorD)
                        {
                            var listNums = Enumerable.Range(1, 6).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD1.x, SecBD1.y, SecBD1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD2.x, SecBD2.y, SecBD2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD3.x, SecBD3.y, SecBD3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD4.x, SecBD4.y, SecBD4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD5.x, SecBD5.y, SecBD5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 6:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD6.x, SecBD6.y, SecBD6.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorB && sectUnder60 == sectorA)
                        {
                            var listNums = Enumerable.Range(1, 6).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB1.x, SecAB1.y, SecAB1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB2.x, SecAB2.y, SecAB2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB3.x, SecAB3.y, SecAB3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB4.x, SecAB4.y, SecAB4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB5.x, SecAB5.y, SecAB5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 6:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAB6.x, SecAB6.y, SecAB6.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorC && sectUnder60 == sectorA)
                        {
                            var listNums = Enumerable.Range(1, 6).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC1.x, SecAC1.y, SecAC1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC2.x, SecAC2.y, SecAC2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC3.x, SecAC3.y, SecAC3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC4.x, SecAC4.y, SecAC4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC5.x, SecAC5.y, SecAC5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 6:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAC6.x, SecAC6.y, SecAC6.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorD && sectUnder60 == sectorC)
                        {
                            var listNums = Enumerable.Range(1, 6).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD1.x, SecCD1.y, SecCD1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD2.x, SecCD2.y, SecCD2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD3.x, SecCD3.y, SecCD3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD4.x, SecCD4.y, SecCD4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD5.x, SecCD5.y, SecCD5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 6:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecCD6.x, SecCD6.y, SecCD6.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                        if (sectUnder30 == sectorD && sectUnder60 == sectorB)
                        {
                            var listNums = Enumerable.Range(1, 6).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD1.x, SecBD1.y, SecBD1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD2.x, SecBD2.y, SecBD2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD3.x, SecBD3.y, SecBD3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD4.x, SecBD4.y, SecBD4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD5.x, SecBD5.y, SecBD5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 6:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecBD6.x, SecBD6.y, SecBD6.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                    }
                    else if (bAllSectors == true)
                    {
                        if (sectUnder30 == allSector)
                        {
                            var listNums = Enumerable.Range(1, 5).OrderBy(i => _rand.Next()).ToList();
                            int selectedNum = listNums[0];
                            switch (selectedNum)
                            {
                                case 1:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAll1.x, SecAll1.y, SecAll1.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 2:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAll2.x, SecAll2.y, SecAll2.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 3:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAll3.x, SecAll3.y, SecAll3.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 4:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAll4.x, SecAll4.y, SecAll4.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                                case 5:
                                    warpPoint = _baseScript.findOpenWarp(player, _arena, SecAll5.x, SecAll5.y, SecAll5.radius);
                                    if (warpPoint == null)
                                    {
                                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                                        player.sendMessage(-1, "Warp was blocked, please try again");
                                        return false;
                                    }
                                    _baseScript.warp(player, warpPoint);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Log.write(TLog.Normal, String.Format("Could not find open warp for {0} (Warp Blocked)", player._alias));
                        player.sendMessage(-1, "Warp was blocked, please try again");
                        return false;
                    }
                }
                
            }
            else if (bbetweengames == true)
            {
                if (portal.GeneralData.Name.Contains("MapPortal"))
                {
                    player.sendMessage(-1, "Warp was blocked, it will reopen when sectors open.");
                }
            }
            return false;
        }
        #endregion
    }
}
