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
    public partial class Pylons
    {
        ///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;                //The zone config
        public Script_Eol _baseScript;
        public bool _bpylonsSpawned;
        public bool _gameBegun;

        public Headquarters _hqs;
        public EolBoundaries _eol;

        private string _currentSector1;
        private string _currentSector2;

        public const int _pylonVehID = 622;

        public class pylonObject
        {
            short x;      //X coordinate of pylon
            short y;      //Y coordinate of pylon
            bool exists;//Tells us if the pylon exists on the map

            public pylonObject(short xPos, short yPos)
            {
                exists = false;
                x = xPos;
                y = yPos;
            }
            public short getX()
            { return x; }
            public short getY()
            { return y; }
            public bool bExists()
            { return exists; }
            public void setExists(bool bExists)
            { exists = bExists; }
        }
        public Dictionary<int, pylonObject> _usedpylons;
        public Dictionary<int, pylonObject> _pylons;
        public Dictionary<int, pylonObject> _pylonsA;
        public Dictionary<int, pylonObject> _pylonsB;
        public Dictionary<int, pylonObject> _pylonsC;
        public Dictionary<int, pylonObject> _pylonsD;
        public Dictionary<int, pylonObject> _pylonsAB;
        public Dictionary<int, pylonObject> _pylonsCD;
        public Dictionary<int, pylonObject> _pylonsAC;
        public Dictionary<int, pylonObject> _pylonsBD;


        public Pylons(Arena arena, Script_Eol baseScript)
        {
            _arena = arena;
            _config = arena._server._zoneConfig;
            _baseScript = baseScript;
            _bpylonsSpawned = false;

            Dictionary<int, pylonObject> _pylons = new Dictionary<int, pylonObject>();
            _pylons.Add(0, new pylonObject(512, 480)); // Sector A
            _pylons.Add(1, new pylonObject(2736, 5600)); // Sector A
            _pylons.Add(2, new pylonObject(7856, 5504)); // Sector A
            _pylons.Add(3, new pylonObject(5504, 7040)); // Sector B
            _pylons.Add(4, new pylonObject(8304, 11008));// Sector B
            _pylons.Add(5, new pylonObject(6784, 13808));// Sector B
            _pylons.Add(6, new pylonObject(13765, 1236)); // Sector C
            _pylons.Add(7, new pylonObject(17093, 5076)); // Sector C
            _pylons.Add(8, new pylonObject(14960, 5040)); // Sector C
            _pylons.Add(9, new pylonObject(12549, 6708)); // Sector D
            _pylons.Add(10, new pylonObject(16981, 10580)); // Sector D
            _pylons.Add(11, new pylonObject(18064, 7584)); // Sector D

            Dictionary<int, pylonObject> _pylonsA = new Dictionary<int, pylonObject>();
            _pylonsA.Add(0, new pylonObject(512, 480)); // Sector A
            _pylonsA.Add(1, new pylonObject(2736, 5600)); // Sector A
            _pylonsA.Add(2, new pylonObject(7856, 5504)); // Sector A

            Dictionary<int, pylonObject> _pylonsB = new Dictionary<int, pylonObject>();
            _pylonsB.Add(0, new pylonObject(5504, 7040)); // Sector B
            _pylonsB.Add(1, new pylonObject(8304, 11008));// Sector B
            _pylonsB.Add(2, new pylonObject(6784, 13808));// Sector B

            Dictionary<int, pylonObject> _pylonsC = new Dictionary<int, pylonObject>();
            _pylonsC.Add(0, new pylonObject(13765, 1236)); // Sector C
            _pylonsC.Add(1, new pylonObject(17093, 5076)); // Sector C
            _pylonsC.Add(2, new pylonObject(14960, 5040)); // Sector C

            Dictionary<int, pylonObject> _pylonsD = new Dictionary<int, pylonObject>();
            _pylonsD.Add(0, new pylonObject(12549, 6708)); // Sector D
            _pylonsD.Add(1, new pylonObject(16981, 10580)); // Sector D
            _pylonsD.Add(2, new pylonObject(18064, 7584)); // Sector D

            Dictionary<int, pylonObject> _pylonsAB = new Dictionary<int, pylonObject>();
            _pylonsAB.Add(0, new pylonObject(512, 480)); // Sector A
            _pylonsAB.Add(1, new pylonObject(2736, 5600)); // Sector A
            _pylonsAB.Add(2, new pylonObject(7856, 5504)); // Sector A
            _pylonsAB.Add(3, new pylonObject(5504, 7040)); // Sector B
            _pylonsAB.Add(4, new pylonObject(8304, 11008));// Sector B
            _pylonsAB.Add(5, new pylonObject(6784, 13808));// Sector B

            Dictionary<int, pylonObject> _pylonsAC = new Dictionary<int, pylonObject>();
            _pylonsAC.Add(0, new pylonObject(512, 480)); // Sector A
            _pylonsAC.Add(1, new pylonObject(2736, 5600)); // Sector A
            _pylonsAC.Add(2, new pylonObject(7856, 5504)); // Sector A
            _pylonsAC.Add(3, new pylonObject(13765, 1236)); // Sector C
            _pylonsAC.Add(4, new pylonObject(17093, 5076)); // Sector C
            _pylonsAC.Add(5, new pylonObject(14960, 5040)); // Sector C

            Dictionary<int, pylonObject> _pylonsCD = new Dictionary<int, pylonObject>();
            _pylonsCD.Add(0, new pylonObject(13765, 1236)); // Sector C
            _pylonsCD.Add(1, new pylonObject(17093, 5076)); // Sector C
            _pylonsCD.Add(2, new pylonObject(14960, 5040)); // Sector C
            _pylonsCD.Add(3, new pylonObject(12549, 6708)); // Sector D
            _pylonsCD.Add(4, new pylonObject(16981, 10580)); // Sector D
            _pylonsCD.Add(5, new pylonObject(18064, 7584)); // Sector D

            Dictionary<int, pylonObject> _pylonsBD = new Dictionary<int, pylonObject>();
            _pylonsBD.Add(0, new pylonObject(5504, 7040)); // Sector B
            _pylonsBD.Add(1, new pylonObject(8304, 11008));// Sector B
            _pylonsBD.Add(2, new pylonObject(6784, 13808));// Sector B
            _pylonsBD.Add(3, new pylonObject(12549, 6708)); // Sector D
            _pylonsBD.Add(4, new pylonObject(16981, 10580)); // Sector D
            _pylonsBD.Add(5, new pylonObject(18064, 7584)); // Sector D

            _baseScript._lastPylon = null;
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
                if (!_bpylonsSpawned)
                {
                    addPylons();
                    _bpylonsSpawned = true;
                }

            }
            return true;
        }

        public void addPylons()
        {
            int playing = _arena.PlayerCount;
            Dictionary<int, pylonObject> _usedpylons = new Dictionary<int, pylonObject>();
            if (playing < 30)
            {

                _currentSector1 = _baseScript._currentSector1;
                switch (_currentSector1)
                {
                    case "Sector A":
                        _usedpylons = new Dictionary<int, pylonObject>(_pylonsA);
                        break;
                    case "Sector B":
                        _usedpylons = new Dictionary<int, pylonObject>(_pylonsB);
                        break;
                    case "Sector C":
                        _usedpylons = new Dictionary<int, pylonObject>(_pylonsC);
                        break;
                    case "Sector D":
                        _usedpylons = new Dictionary<int, pylonObject>(_pylonsD);
                        break;
                }
            }
            if (playing >= 30 && playing < 60)
            {
                _currentSector1 = _baseScript._currentSector1;
                _currentSector2 = _baseScript._currentSector2;
                switch (_currentSector1)
                {
                    case "Sector A":
                        switch (_currentSector2)
                        {
                            case "Sector B":
                                _usedpylons = new Dictionary<int, pylonObject>(_pylonsAB);
                                break;
                            case "Sector C":
                                _usedpylons = new Dictionary<int, pylonObject>(_pylonsAC);
                                break;
                        }
                        break;
                    case "Sector B":
                        switch (_currentSector2)
                        {
                            case "Sector A":
                                _usedpylons = new Dictionary<int, pylonObject>(_pylonsAB);
                                break;
                            case "Sector D":
                                _usedpylons = new Dictionary<int, pylonObject>(_pylonsBD);
                                break;
                        }
                        break;
                    case "Sector C":
                        switch (_currentSector2)
                        {
                            case "Sector A":
                                _usedpylons = new Dictionary<int, pylonObject>(_pylonsAC);
                                break;
                            case "Sector D":
                                _usedpylons = new Dictionary<int, pylonObject>(_pylonsCD);
                                break;
                        }
                        break;
                    case "Sector D":
                        switch (_currentSector2)
                        {
                            case "Sector C":
                                _usedpylons = new Dictionary<int, pylonObject>(_pylonsCD);
                                break;
                            case "Sector B":
                                _usedpylons = new Dictionary<int, pylonObject>(_pylonsBD);
                                break;
                        }
                        break;
                }
            }
            if (playing >= 60 && _currentSector1 == "All Sectors")
            {
                _usedpylons = new Dictionary<int, pylonObject>(_pylons);
            }
            foreach (KeyValuePair<int, pylonObject> obj in _usedpylons)
            {
                if (obj.Value.bExists())
                    continue;

                VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(_pylonVehID));
                Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
                newState.positionX = obj.Value.getX();
                newState.positionY = obj.Value.getY();
                newState.positionZ = 0;
                newState.yaw = 0;

                obj.Value.setExists(true);

                //Put them all on one bot team since it doesn't matter who owns the pylon
                _arena.newVehicle(vehicle, _baseScript.botTeam1, null, newState);
            }
        }

        public void spawnEngyBot(Vehicle home)
        {
            if (home._type.Id == _pylonVehID)
            {
                Team team = null;
                _arena.sendArenaMessage("An engineer has been deployed to from the orbiting Pioneer Station.");
                if (_hqs[_baseScript.botTeam1] == null)
                    team = _baseScript.botTeam1;
                else if (_hqs[_baseScript.botTeam2] == null)
                    team = _baseScript.botTeam2;
                else if (_hqs[_baseScript.botTeam3] == null)
                    team = _baseScript.botTeam3;

                Engineer George = _arena.newBot(typeof(Engineer), (ushort)300, team, null, home._state, this) as Engineer;

                //Find the pylon we are about to destroy and mark it as nonexistent
                foreach (KeyValuePair<int, pylonObject> obj in _usedpylons)
                    if (home._state.positionX == obj.Value.getX() && home._state.positionY == obj.Value.getY())
                        obj.Value.setExists(false);

                //Destroy our pylon because we will use our hq to respawn and we dont want any other engineers grabbing this one
                home.destroy(false);

                //Keep track of the engineers
                _baseScript._currentEngineers++;
                _baseScript.engineerBots.Add(team);
            }

            if (home._type.Id == _baseScript._hqVehId)
            {
                Team team = null;
                if (home._team == _baseScript.botTeam1)
                    team = _baseScript.botTeam1;
                else if (home._team == _baseScript.botTeam2)
                    team = _baseScript.botTeam2;
                else if (home._team == _baseScript.botTeam3)
                    team = _baseScript.botTeam3;

                Engineer Filbert = _arena.newBot(typeof(Engineer), (ushort)300, team, null, home._state, this) as Engineer;

                _baseScript._currentEngineers++;
                _baseScript.engineerBots.Add(team);
            }
        }
        public void gameStart()
        {	//We've started!
            _gameBegun = true;
        }

        public bool gamesEnd()
        {
            _bpylonsSpawned = false;
            _gameBegun = false;
            if (_usedpylons.Count() != 0) { _usedpylons.Clear(); }
            return true;
        }

        public bool gameReset()
        {	
            _bpylonsSpawned = false;
            _gameBegun = false;
            if (_usedpylons.Count() != 0) { _usedpylons.Clear(); }
            return true;
        }
    }
}
