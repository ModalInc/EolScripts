using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;
using System.Collections;
using System.CodeDom;
using System.Diagnostics;

namespace InfServer.Script.GameType_Eol
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public class Script_Eol : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config
        private Points _points;                 //Our points
        private Teams _tm;                      //Teams logic
        private ZoneServer _server;

        //Headquarters
        public Headquarters _hqs;               //Our headquarter tracker
        private int[] _hqlevels;                //Bounty required to level up HQs
        public int _hqVehId;                    //The vehicle ID of our HQs
        public int _roamCaptains;
        public int _roamChiefs;
        private int _baseXPReward;              //Base XP reward for HQs
        private int _baseCashReward;            //Base Cash reward for HQs
        private int _basePointReward;           //Base Point reward for HQs
        private int _rewardInterval;            //The interval at which we reward for HQs

        public EolBoundaries _eol;
        public HordePoints _hoardps;

        //public BotSettings BotSettings;

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _lastHQReward;              //The tick at which we last checked for HQ rewards


        //KOTH
        private Team _victoryKothTeam;			//The team currently winning!
        private int _tickGameLastTickerUpdate;  //The tick at which the ticker was last updated
        private long _tickKOTHGameStarting;
        private long _tickKOTHGameStart;				//The tick at which the game started (0 == stopped)
        private int _tickEolGameStart;
        private int _tickKothGameStart;
        private int _minPlayers;                //The minimum amount of players needed for a KOTH game
        private long _KothGameCheck;
        private bool _kothGameRunning;
        private Dictionary<Player, Team> _startTeams;
        private int _lastKOTHGameCheck;

        //EOL
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)

        //CTF
        private int _jackpot;					//The game's jackpot so far
        private bool _firstGame;
        private Team _victoryTeam;				//The team currently winning!
        private int _tickVictoryStart;			//The tick at which the victory countdown began
        private int _tickNextVictoryNotice;		//The tick at which we will next indicate imminent victory
        private int _victoryNotice;				//The number of victory notices we've done
        private int _lastFlagCheck;
        private bool _gameWon = false;
        private int _minPlayersCTF;
        private int _lastFlagReward;

        //Flag Checks for roaming captains
        public List<Arena.FlagState> _flags;

        //Settings
        private int _pointSmallChange;                  //The small change to # of points (ex: kills, turret kills, etc)
        private int _pointPeriodicChange;               //The change to # of points based on periodic flag rewards
        public int _pylonLocation;
        public int _minX;
        public int _maxX;
        public int _minY;
        public int _maxY;
        public int _tickLastMinorPoll;

        //Scores
        private Dictionary<Player, int> _healingDone;   //Keep track of healing done by players

        //KillRewards
        public const int c_baseReward = 5;
        public const double c_pointMultiplier = 2;
        public const double c_cashMultiplier = 1;
        public const double c_expMultiplier = 0.5;
        public const int c_percentOfVictim = 50;
        public const int c_percentOfOwn = 3;
        public const int c_percentOfOwnIncrease = 5;

        private class PlayerCrownStatus
        {
            public bool crown;                  //Player has crown?
            public int crownKills;              //Crown kills without a crown
            public int crownDeaths;             //Times died with a crown (counted until they lose it)
            public int expireTime;              //When the crown will expire
            public PlayerCrownStatus(bool bCrown)
            {
                crown = bCrown;
            }
            public PlayerCrownStatus()
            {
                crown = true;
            }
        }
        private Dictionary<Player, PlayerCrownStatus> _playerCrownStatus;
        public List<Player> _activeCrowns //List of people with a crown
        {
            get { return _playerCrownStatus.Where(p => p.Value.crown).Select(p => p.Key).ToList(); }
        }
        private List<Player> _noCrowns //List of people with no crowns
        {
            get { return _playerCrownStatus.Where(p => !p.Value.crown).Select(p => p.Key).ToList(); }
        }
        private List<Team> _crownTeams;


        /// Stores our player streak information
        private class PlayerStreak
        {
            public ItemInfo.Projectile lastUsedWeap { get; set; }
            public int lastUsedWepKillCount { get; set; }
            public long lastUsedWepTick { get; set; }
            public int lastKillerCount { get; set; }
        }


        private Dictionary<string, PlayerStreak> killStreaks;
        private Player lastKiller;
        public bool _bpylonsSpawned;
        public bool _bbetweengames;
        public bool Min1Timer;
        public bool Min5Timer;
        public bool Min1Send;
        public bool Min5Send;

        public string _currentSector1;
        public string _currentSector2;

        //Bots
        private int _lastBotCheck;
        private int[] _coolArray;
        public const int _pylonVehID = 622;
        int _flagchecking = 82000;

        public int _botlocX;
        public int _botlocY;

        public class pylonObject
        {
            short x;        //X coordinate of pylon
            short y;        //Y coordinate of pylon
            bool exists;    //Tells us if the pylon exists on the map

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

        //Bots
        //Perimeter defense Bots
        public const float c_defenseInitialAmountPP = 2.5f;		//The amount of defense bots per player initially spawned (minimum of 1)
        public const int c_defenseAddTimerGrowth = 18;			//The amount of seconds to add to the new bot timer for each person missing from the team
        public const int c_defenseAddTimer = 36;			    //The amount of seconds between allowing new defense bots
        public const int c_defenseRespawnTimeGrowth = 400;		//The amount of time to add to the respawn timer for each missing player
        public const int c_defenseRespawnTime = 600;		    //The amount of ms between spawning new zombies
        public const int c_defenseMinRespawnDist = 900;			//The minimum distance bot can be spawned from the players
        public const int c_defenseMaxRespawnDist = 1500;		//The maximum distance bot can be spawned from the players
        public const int c_defenseMaxPath = 350;				//The maximum path length before a bot will request a respawn
        public const int c_defensePathUpdateInterval = 1000;	//The amount of ticks before a bot will renew it's path
        public const int c_defenseRoamPathUpdateInterval = 1000;
        public const int c_chiefPathUpdateInterval = 1000;
        public const int c_defenseDistanceLeeway = 500;			//The maximum distance leeway a bot can be from the team before it is respawned
        public const int _checkCaptain = 100000;                 //The tick at which we check for a captain  50000
        public const int _checkEngineer = 55000;                //The tick at which we check for an engineer 70000
        public const int _checkRoamingCaptain = 12000;          //The tick at which we check for a roaming captain  140000
        public const int _checkRoamingChief = 100000;          //The tick at which we check for a roaming captain  100000
        protected int _tickLastEngineer = 0;                    //Last time we checked for an engineer
        protected int _tickLastCaptain = 0;                     //Last time we checked for a captain
        public int _tickLastChief = 0;                       //Last time we checked for an alien chief
        protected int _tickLastRoamingCaptain = 0;              //Last time we checked for a roaming captain
        protected int _lastPylonCheck = 0;                      //Last time we check for bot pylons to build hq's at.

        public const int c_CaptainPathUpdateInterval = 5000;	//The amount of ticks before an engineer's combat bot updates it's path
        public const int c_CaptainRoamPathUpdateInterval = 3000;	//The amount of ticks before a roaming bot looks to it's captain and updates it's path

        public Dictionary<Team, int> botCount;
        public Dictionary<Team, int> captainBots;
        public Dictionary<Team, int> roamBots;
        public Dictionary<Team, int> capRoamBots;
        public List<Team> roamingCaptianBots;
        public List<Team> engineerBots;
        public Dictionary<Team, int> alienBots;
        public Dictionary<Team, int> alienChiefBots;
        public List<Team> roamingAlienBots;

        public List<Bot> _bots;
        public List<Bot> _condemnedBots;

        public int _maxEngineers;                               //Maximum amount of engineer bots that will spawn in game
        public int _maxRoamCaptains;                            //Maximum amount of roaming captain bots that will spawn in game
        public int _maxRoamingBots;
        public int _maxRoamChief;                            //Maximum amount of roaming chief alien bots that will spawn in game
        public int _maxRoamingAliens;
        public int _maxDefenseBots;
        public int _maxDefPerTeam;
        public int _maxRoamPerTeam;
        public int _maxRoamAliensPerTeam;
        public int _currentEngineers = 0;                       //Current amount of engineer bots playing in the game
        public int _currentRoamCaptains = 0;
        public int _currentRoamChief = 0;
        public int[] _lastPylon;                                //Array of all pylons that are being used

        public static TimeSpan _pvpHappyHourStart = TimeSpan.Zero;
        public static TimeSpan _pvpHappyHourEnd = TimeSpan.Zero;

        static public bool _bPvpHappyHour;

        private int _lastPvpHappyHourAlert;
        private int _tickPvpHappyHourStart;

        Dictionary<int, string> startTime;
        Dictionary<int, string> endTime;

        List<HordePoints> _activehPoints;

        //Bot teams
        public Team botTeam1;
        public Team botTeam2;
        public Team botTeam3;
        public Team botTeam4;
        public Team botTeam5;
        public Team botTeam6;
        public Team botTeam7;
        public Team botTeam8;
        public Team botTeam9;

        public string botTeamName1 = "Bot Team - Titan Rebels";
        public string botTeamName2 = "Bot Team - Deeks Bandits";
        public string botTeamName3 = "Bot Team - NewJack Raiders";
        public string botTeamName4 = "Bot Team - Eta";
        public string botTeamName5 = "Bot Team - Theta";
        public string botTeamName6 = "Bot Team - Iota";
        public string botTeamName7 = "Bot Team - Alien Invaders";
        public string botTeamName8 = "Bot Team - Alien Conquers";
        public string botTeamName9 = "Bot Team - Alien Wimps";

        Random _rand;

        public Dictionary<string, Helpers.ObjectState> _lastSpawn;

        ///////////////////////////////////////////////////
        // Member Functions
        ///////////////////////////////////////////////////
        /// <summary>
        /// Performs script initialization
        /// </summary>
        public bool init(IEventObject invoker)
        {	//Populate our variables
            _arena = invoker as Arena;
            _config = _arena._server._zoneConfig;
            _minPlayers = Int32.MaxValue;
            _minPlayersCTF = Int32.MaxValue;
            _activehPoints = new List<HordePoints>();
            _bPvpHappyHour = false;
            //Load up our gametype handlers
            _eol = new EolBoundaries(_arena, this);
            _tm = new Teams(_arena, this);
            _KothGameCheck = 432000000000;
            //Load up Pylons
            _bpylonsSpawned = false;
            _kothGameRunning = false;
            Min1Timer = false;
            Min5Timer = false;
            Min1Send = false;
            Min5Send = false;

            _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
            _crownTeams = new List<Team>();
            _startTeams = new Dictionary<Player, Team>();

            startTime = new Dictionary<int, string>();
            endTime = new Dictionary<int, string>();

            foreach (Arena.FlagState fs in _arena._flags.Values)
            {	//Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < _minPlayersCTF)
                    _minPlayersCTF = fs.flag.FlagData.MinPlayerCount;

                //Register our flag change events
                fs.TeamChange += onFlagChange;
            }
            killStreaks = new Dictionary<string, PlayerStreak>();

            //Headquarters stuff!
            _hqlevels = new int[] { 500, 1000, 2500, 5000, 10000, 15000, 20000, 25000, 30000, 35000 };
            _hqVehId = 469;
            _baseXPReward = 25;
            _baseCashReward = 150;
            _basePointReward = 10;
            _rewardInterval = 90 * 1000; // 90 seconds
            _hqs = new Headquarters(_hqlevels);
            _hqs.LevelModify += onHQLevelModify;

            //Handle bots
            _roamCaptains = 142;
            _roamChiefs = 200;
            captainBots = new Dictionary<Team, int>();  //Keeps track of captain bots
            botCount = new Dictionary<Team, int>(); //Counts of all defense bots and their teams
            roamBots = new Dictionary<Team, int>();
            capRoamBots = new Dictionary<Team, int>();
            engineerBots = new List<Team>();
            roamingCaptianBots = new List<Team>();
            alienBots = new Dictionary<Team, int>();
            alienChiefBots = new Dictionary<Team, int>();
            roamingAlienBots = new List<Team>();
            _currentRoamChief = 0;
            _currentEngineers = 0;                      //The number of engineers currently alive
            _currentRoamCaptains = 0;                   //The number of roaming captains currently alive
            _maxDefPerTeam = 8;
            _maxRoamPerTeam = 10;
            _maxRoamAliensPerTeam = 10;
            if (_bots == null)
                _bots = new List<Bot>();

            _condemnedBots = new List<Bot>();

            botTeam1 = new Team(_arena, _arena._server);
            botTeam1._name = botTeamName1;
            botTeam1._id = (short)_arena.Teams.Count();
            botTeam1._password = "jojotheClown";
            botTeam1._owner = null;
            botTeam1._isPrivate = true;

            botTeam2 = new Team(_arena, _arena._server);
            botTeam2._name = botTeamName2;
            botTeam2._id = (short)_arena.Teams.Count();
            botTeam2._password = "jojotheClown";
            botTeam2._owner = null;
            botTeam2._isPrivate = true;

            botTeam3 = new Team(_arena, _arena._server);
            botTeam3._name = botTeamName3;
            botTeam3._id = (short)_arena.Teams.Count();
            botTeam3._password = "jojotheClown";
            botTeam3._owner = null;
            botTeam3._isPrivate = true;

            botTeam4 = new Team(_arena, _arena._server);
            botTeam4._name = botTeamName4;
            botTeam4._id = (short)_arena.Teams.Count();
            botTeam4._password = "jojotheClown";
            botTeam4._owner = null;
            botTeam4._isPrivate = true;

            botTeam5 = new Team(_arena, _arena._server);
            botTeam5._name = botTeamName5;
            botTeam5._id = (short)_arena.Teams.Count();
            botTeam5._password = "jojotheClown";
            botTeam5._owner = null;
            botTeam5._isPrivate = true;

            botTeam6 = new Team(_arena, _arena._server);
            botTeam6._name = botTeamName6;
            botTeam6._id = (short)_arena.Teams.Count();
            botTeam6._password = "jojotheClown";
            botTeam6._owner = null;
            botTeam6._isPrivate = true;

            botTeam7 = new Team(_arena, _arena._server);
            botTeam7._name = botTeamName7;
            botTeam7._id = (short)_arena.Teams.Count();
            botTeam7._password = "jojotheClown";
            botTeam7._owner = null;
            botTeam7._isPrivate = true;

            botTeam8 = new Team(_arena, _arena._server);
            botTeam8._name = botTeamName8;
            botTeam8._id = (short)_arena.Teams.Count();
            botTeam8._password = "jojotheClown";
            botTeam8._owner = null;
            botTeam8._isPrivate = true;

            botTeam9 = new Team(_arena, _arena._server);
            botTeam9._name = botTeamName9;
            botTeam9._id = (short)_arena.Teams.Count();
            botTeam9._password = "jojotheClown";
            botTeam9._owner = null;
            botTeam9._isPrivate = true;

            _arena.createTeam(botTeam1);
            _arena.createTeam(botTeam2);
            _arena.createTeam(botTeam3);
            _arena.createTeam(botTeam4);
            _arena.createTeam(botTeam5);
            _arena.createTeam(botTeam6);
            _arena.createTeam(botTeam7);
            _arena.createTeam(botTeam8);
            _arena.createTeam(botTeam9);


            _pylons = new Dictionary<int, pylonObject>();
            _pylons.Add(0, new pylonObject(512, 480)); // Sector A
            _pylons.Add(1, new pylonObject(2736, 5600)); // Sector A
            _pylons.Add(2, new pylonObject(4901, 4740)); // Sector A
            _pylons.Add(3, new pylonObject(7445, 2356)); // Sector A
            _pylons.Add(4, new pylonObject(5504, 7040)); // Sector B
            _pylons.Add(5, new pylonObject(8304, 11008));// Sector B
            _pylons.Add(6, new pylonObject(6784, 13808));// Sector B
            _pylons.Add(7, new pylonObject(4117, 9956));// Sector B
            _pylons.Add(8, new pylonObject(13765, 1236)); // Sector C
            _pylons.Add(9, new pylonObject(17093, 5076)); // Sector C
            _pylons.Add(10, new pylonObject(12565, 5252)); // Sector C
            _pylons.Add(11, new pylonObject(18117, 3316)); // Sector C
            _pylons.Add(12, new pylonObject(20661, 7924)); // Sector D
            _pylons.Add(13, new pylonObject(16981, 10580)); // Sector D
            _pylons.Add(14, new pylonObject(18064, 7584)); // Sector D
            _pylons.Add(15, new pylonObject(11957, 13604)); // Sector D

            _pylonsA = new Dictionary<int, pylonObject>();
            _pylonsA.Add(0, new pylonObject(512, 480)); // Sector A
            _pylonsA.Add(1, new pylonObject(2736, 5600)); // Sector A
            _pylonsA.Add(2, new pylonObject(4901, 4740)); // Sector A
            _pylonsA.Add(3, new pylonObject(7445, 2356)); // Sector A

            _pylonsB = new Dictionary<int, pylonObject>();
            _pylonsB.Add(0, new pylonObject(5504, 7040)); // Sector B
            _pylonsB.Add(1, new pylonObject(8304, 11008));// Sector B
            _pylonsB.Add(2, new pylonObject(6784, 13808));// Sector B
            _pylonsB.Add(3, new pylonObject(4117, 9956));// Sector B

            _pylonsC = new Dictionary<int, pylonObject>();
            _pylonsC.Add(0, new pylonObject(13765, 1236)); // Sector C
            _pylonsC.Add(1, new pylonObject(17093, 5076)); // Sector C
            _pylonsC.Add(2, new pylonObject(12565, 5252)); // Sector C
            _pylonsC.Add(3, new pylonObject(18117, 3316)); // Sector C

            _pylonsD = new Dictionary<int, pylonObject>();
            _pylonsD.Add(0, new pylonObject(20661, 7924)); // Sector D
            _pylonsD.Add(1, new pylonObject(16981, 10580)); // Sector D
            _pylonsD.Add(2, new pylonObject(18064, 7584)); // Sector D
            _pylonsD.Add(3, new pylonObject(11957, 13604)); // Sector D

            _pylonsAB = new Dictionary<int, pylonObject>();
            _pylonsAB.Add(0, new pylonObject(512, 480)); // Sector A
            _pylonsAB.Add(1, new pylonObject(2736, 5600)); // Sector A
            _pylonsAB.Add(2, new pylonObject(4901, 4740)); // Sector A
            _pylonsAB.Add(3, new pylonObject(7445, 2356)); // Sector A
            _pylonsAB.Add(4, new pylonObject(5504, 7040)); // Sector B
            _pylonsAB.Add(5, new pylonObject(8304, 11008));// Sector B
            _pylonsAB.Add(6, new pylonObject(6784, 13808));// Sector B
            _pylonsAB.Add(7, new pylonObject(4117, 9956));// Sector B

            _pylonsAC = new Dictionary<int, pylonObject>();
            _pylonsAC.Add(0, new pylonObject(512, 480)); // Sector A
            _pylonsAC.Add(1, new pylonObject(2736, 5600)); // Sector A
            _pylonsAC.Add(2, new pylonObject(4901, 4740)); // Sector A
            _pylonsAC.Add(3, new pylonObject(7445, 2356)); // Sector A
            _pylonsAC.Add(4, new pylonObject(13765, 1236)); // Sector C
            _pylonsAC.Add(5, new pylonObject(17093, 5076)); // Sector C
            _pylonsAC.Add(6, new pylonObject(12565, 5252)); // Sector C
            _pylonsAC.Add(7, new pylonObject(18117, 3316)); // Sector C

            _pylonsCD = new Dictionary<int, pylonObject>();
            _pylonsCD.Add(0, new pylonObject(13765, 1236)); // Sector C
            _pylonsCD.Add(1, new pylonObject(17093, 5076)); // Sector C
            _pylonsCD.Add(2, new pylonObject(12565, 5252)); // Sector C
            _pylonsCD.Add(3, new pylonObject(18117, 3316)); // Sector C
            _pylonsCD.Add(4, new pylonObject(20661, 7924)); // Sector D
            _pylonsCD.Add(5, new pylonObject(16981, 10580)); // Sector D
            _pylonsCD.Add(6, new pylonObject(18064, 7584)); // Sector D
            _pylonsCD.Add(7, new pylonObject(11957, 13604)); // Sector D

            _pylonsBD = new Dictionary<int, pylonObject>();
            _pylonsBD.Add(0, new pylonObject(5504, 7040)); // Sector B
            _pylonsBD.Add(1, new pylonObject(8304, 11008));// Sector B
            _pylonsBD.Add(2, new pylonObject(6784, 13808));// Sector B
            _pylonsBD.Add(3, new pylonObject(4117, 9956));// Sector B
            _pylonsBD.Add(4, new pylonObject(20661, 7924)); // Sector D
            _pylonsBD.Add(5, new pylonObject(16981, 10580)); // Sector D
            _pylonsBD.Add(6, new pylonObject(18064, 7584)); // Sector D
            _pylonsBD.Add(7, new pylonObject(11957, 13604)); // Sector D

            _usedpylons = new Dictionary<int, pylonObject>();
            _lastPylon = null;

            /*startTime.Add(00, "01:00");
            startTime.Add(01, "02:00");
            startTime.Add(02, "03:00");
            startTime.Add(03, "04:00");
            startTime.Add(04, "05:00");
            startTime.Add(05, "06:00");
            startTime.Add(06, "07:00");
            startTime.Add(07, "08:00");
            startTime.Add(08, "09:00");
            startTime.Add(09, "10:00");
            startTime.Add(10, "11:00");
            startTime.Add(11, "12:00");
            startTime.Add(12, "13:00");
            startTime.Add(13, "14:00");
            startTime.Add(14, "15:00");
            startTime.Add(15, "16:00");
            startTime.Add(16, "17:00");
            startTime.Add(17, "18:00");
            startTime.Add(18, "19:00");
            startTime.Add(19, "20:00");
            startTime.Add(20, "21:00");
            startTime.Add(21, "22:00");
            startTime.Add(22, "23:00");
            startTime.Add(23, "00:00");

            endTime.Add(00, "02:00");
            endTime.Add(01, "03:00");
            endTime.Add(02, "04:00");
            endTime.Add(03, "05:00");
            endTime.Add(04, "06:00");
            endTime.Add(05, "07:00");
            endTime.Add(06, "08:00");
            endTime.Add(07, "09:00");
            endTime.Add(08, "10:00");
            endTime.Add(09, "11:00");
            endTime.Add(10, "12:00");
            endTime.Add(11, "13:00");
            endTime.Add(12, "14:00");
            endTime.Add(13, "15:00");
            endTime.Add(14, "16:00");
            endTime.Add(15, "17:00");
            endTime.Add(16, "18:00");
            endTime.Add(17, "19:00");
            endTime.Add(18, "20:00");
            endTime.Add(19, "21:00");
            endTime.Add(20, "22:00");
            endTime.Add(21, "23:00");
            endTime.Add(22, "00:00");
            endTime.Add(23, "01:00");*/

            startTime.Add(00, "01:00");
            startTime.Add(01, "02:00");
            startTime.Add(02, "03:00");
            startTime.Add(03, "04:00");
            startTime.Add(04, "05:00");
            startTime.Add(05, "06:00");
            startTime.Add(06, "07:00");
            startTime.Add(07, "08:00");
            startTime.Add(08, "09:00");
            startTime.Add(09, "10:00");
            startTime.Add(10, "11:00");
            startTime.Add(11, "12:00");
            startTime.Add(12, "13:00");
            startTime.Add(13, "14:00");
            startTime.Add(14, "15:00");
            startTime.Add(15, "16:00");
            startTime.Add(16, "17:00");
            startTime.Add(17, "18:00");
            startTime.Add(18, "19:00");
            startTime.Add(19, "20:00");
            startTime.Add(20, "20:25");
            startTime.Add(21, "21:25");
            startTime.Add(22, "22:25");
            startTime.Add(23, "23:25");

            endTime.Add(00, "02:00");
            endTime.Add(01, "03:00");
            endTime.Add(02, "04:00");
            endTime.Add(03, "05:00");
            endTime.Add(04, "06:00");
            endTime.Add(05, "07:00");
            endTime.Add(06, "08:00");
            endTime.Add(07, "09:00");
            endTime.Add(08, "10:00");
            endTime.Add(09, "11:00");
            endTime.Add(10, "12:00");
            endTime.Add(11, "13:00");
            endTime.Add(12, "14:00");
            endTime.Add(13, "15:00");
            endTime.Add(14, "16:00");
            endTime.Add(15, "17:00");
            endTime.Add(16, "18:00");
            endTime.Add(17, "19:00");
            endTime.Add(18, "20:00");
            endTime.Add(19, "21:00");
            endTime.Add(20, "22:00");
            endTime.Add(21, "22:25");
            endTime.Add(22, "23:25");
            endTime.Add(23, "00:25");

            return true;
        }


        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {	//Should we check game state yet?
            int now = Environment.TickCount;
            int playing = _arena.PlayerCount;

            /*
            bool bPvpHappyHour = isHappyHour(_pvpHappyHourStart, _pvpHappyHourEnd);

            if (!_bPvpHappyHour && now - _lastPvpHappyHourAlert >= 600000 && timeTo(_pvpHappyHourStart).Hours < 1)
            {
                TimeSpan remaining = timeTo(_pvpHappyHourStart);
                _arena.sendArenaMessage(String.Format("&A Pirate Horde will roam in {0} hour(s) & {1} minute(s)",
                        remaining.Hours, remaining.Minutes), 4);

                _lastPvpHappyHourAlert = now;
            }

            if (bPvpHappyHour)
            {
                _bPvpHappyHour = true;

                if (_tickPvpHappyHourStart == 0)
                {
                    _arena.sendArenaMessage(String.Format("&A Pirate Horde is currently roaming Eol"), 4);

                    _tickPvpHappyHourStart = now;
                }

            }
            else
            {
                _bPvpHappyHour = false;
                _tickPvpHappyHourStart = 0;
                IEnumerable<Vehicle> points = _arena.Vehicles.Where(v => v._type.Id == 468);
                IEnumerable<Vehicle> pbb1 = _arena.Vehicles.Where(v => v._type.Id == 159);
                IEnumerable<Vehicle> pbb2 = _arena.Vehicles.Where(v => v._type.Id == 160);
                IEnumerable<Vehicle> pbb3 = _arena.Vehicles.Where(v => v._type.Id == 161);

                if (points.Count() > 0)
                {
                    foreach (Vehicle v in points) { v.destroy(true, true); }
                    foreach (Vehicle v1 in pbb1) { v1.destroy(true, true); }
                    foreach (Vehicle v2 in pbb2) { v2.destroy(true, true); }
                    foreach (Vehicle v3 in pbb3) { v3.destroy(true, true); }
                }
                setTime();
            }


            if (_bPvpHappyHour) { spawnHorde(now); }
            */

            if (_arena._bGameRunning)
            {
                _eol.Poll(now);

                /*
                if (_bPvpHappyHour)
                {
                    if (_activehPoints.Count() > 0)
                    {
                        foreach (HordePoints point in _activehPoints)
                            point.poll(now);
                    }
                }*/
            }

           

            //Do we have enough people to start a game of KOTH?
            if (now - _lastKOTHGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastKOTHGameCheck = now;

            //Do we have enough players to start a game?
            if ((_tickKOTHGameStart == 0 || _tickKOTHGameStarting == 0) && playing < 5)
            {	//Stop the game!
                _arena.setTicker(1, 1, 0, "KOTH - Not Enough Players");
            }
            //Do we have enough players to start a game?
            else if (_tickKOTHGameStart == 0 && _tickKOTHGameStarting == 0 && playing >= 5)
            {	//Great! Get going
                _tickKOTHGameStarting = now;
                _arena.setTicker(1, 1, 15 * 100, "KOTH - Next game: ",
                    delegate ()
                    {	//Trigger the game start
                        startKOTH();
                    }
                );
            }

            if (!_arena._bGameRunning && _tickGameStart == 0 && _tickGameStarting == 0 && playing >= 0)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 0, 15 * 100, "Next game: ",
                    delegate ()
                    {   //Trigger the game start
                        _arena.gameStart();
                    });
            }

            if (playing <= 5)
            {
                //Even out any private teams that are OVER our current limit
                foreach (Team team in _arena.ActiveTeams)
                {
                    List<Player> playersRemoved = new List<Player>();

                    //If they are within our parameter, ignore.
                    if (team.ActivePlayerCount <= 1)
                        continue;

                    int numToRemove = team.ActivePlayerCount - 3;

                    for (int i = 0; i < numToRemove; i++)
                    {
                        Player rndPlayer = team.ActivePlayers.PickRandom();
                        if (rndPlayer == null)
                            continue;

                        pickTeam(rndPlayer);
                        rndPlayer.sendMessage(0, "You've randomly been moved to a new team to keep teams even. Current team limit is 3");
                    }
                }
            }
            else if (playing >= 8 && playing <= 15)
            {
                //Even out any private teams that are OVER our current limit
                foreach (Team team in _arena.ActiveTeams)
                {
                    List<Player> playersRemoved = new List<Player>();

                    //If they are within our parameter, ignore.
                    if (team.ActivePlayerCount <= 1)
                        continue;

                    int numToRemove = team.ActivePlayerCount - 4;

                    for (int i = 0; i < numToRemove; i++)
                    {
                        Player rndPlayer = team.ActivePlayers.PickRandom();
                        if (rndPlayer == null)
                            continue;

                        pickTeam(rndPlayer);
                        rndPlayer.sendMessage(0, "You've randomly been moved to a new team to keep teams even. Current team limit is 4");
                    }
                }
            }

            _maxRoamCaptains = 1;
            _maxRoamingBots = 30;

            if (playing < 30)
            {
                _maxEngineers = 1;
                _maxDefenseBots = 8;
            }
            if (playing >= 30 && playing < 60)
            {
                _maxEngineers = 2;
                _maxDefenseBots = 16;
            }
            if (playing >= 60)
            {
                _maxEngineers = 3;
                _maxDefenseBots = 24;
            }

            if (_arena._bGameRunning)
            {
                if (now - _lastFlagReward > 540000 && !Min1Timer) //1 minute
                    foreach (Team player in _arena.ActiveTeams)
                    {
                        foreach (Arena.FlagState fs in _arena._flags.Values)
                        {
                            if (fs.team != player)
                            {
                                Min1Timer = true;
                                continue;
                            }

                            Min1Send = true;
                        }

                        if (Min1Send == true)
                        {
                            player.sendArenaMessage("Your next flag reward is in 1 minute", _arena._server._zoneConfig.flag.periodicBong);
                            Min1Timer = true;
                        }
                    }
                if (now - _lastFlagReward > 300000 && !Min5Timer) //5 minutes
                    foreach (Team player in _arena.ActiveTeams)
                    {
                        foreach (Arena.FlagState fs in _arena._flags.Values)
                        {
                            if (fs.team != player)
                            {
                                Min5Timer = true;
                                continue;
                            }

                            Min5Send = true;
                        }

                        if (Min5Send == true)
                        {
                            player.sendArenaMessage("Your next flag reward is in 5 minutes", _arena._server._zoneConfig.flag.periodicBong);
                            Min5Timer = true;
                        }
                    }
                if (now - _lastFlagReward > 600000 && Min5Timer && Min1Timer) //10 minutes
                {
                    rewards();
                    Min1Timer = false;
                    Min5Timer = false;
                    Min5Send = false;
                    Min1Send = false;
                }
            }

            if ((now - _lastPylonCheck >= 36000000) && _arena._bGameRunning) //36000000
            {
                if (_bpylonsSpawned == false)
                {
                    addPylons();
                }
                _lastPylonCheck = now;
            }

            if (now - _tickEolGameStart > _KothGameCheck && _arena._bGameRunning) //test 360000
            {
                if (_activeCrowns.Count == 0)
                {
                    preNewSector();
                }
                else
                {
                    //If KOTH game ongoing then delay resetting of check.
                    _KothGameCheck = 36000;
                }
                _tickEolGameStart = now;
            }

            //Should we reward yet for HQs?
            if (now - _lastHQReward > _rewardInterval)
            {   //Reward time!
                IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);

                Player owner = null;
                if (hqs != null)
                {
                    foreach (Vehicle hq in hqs)
                    {   //Reward all HQ teams!
                        if (_hqs[hq._team] == null)
                            //We're not tracking this HQ for some reason... hm...
                            continue;

                        if (_hqs[hq._team].Level == 0)
                        {
                            if (hq._team._name.Contains("Bot Team -"))
                                continue;

                            hq._team.sendArenaMessage("&Headers - Periodic reward. Your Headquarters is still level 0, minimum level is 1 to obtain rewards. Use ?hq to track your HQ's progress.");
                            continue;
                        }

                        //Is this an all-bot team?
                        if (hq._team._name.Contains("Bot Team -"))
                            owner = null;

                        int points = (int)(_basePointReward * 1.5 * _hqs[hq._team].Level);
                        int cash = (int)(_baseCashReward * 1.5 * _hqs[hq._team].Level);
                        int experience = (int)(_baseXPReward * 1.5 * _hqs[hq._team].Level);

                        foreach (Player p in hq._team.ActivePlayers)
                        {
                            p.BonusPoints += points;
                            p.Cash += cash;
                            p.Experience += experience;
                            p.sendMessage(0, "&Headquarters - Periodic reward. Level " + _hqs[hq._team].Level + ": Cash=" + cash + " Experience=" + experience + " Points=" + points);
                        }
                    }
                }
                _lastHQReward = now;

            }


            //Find out if we will be running KOTH games and if we have enough players
            _minPlayers = _config.king.minimumPlayers;
            if (_minPlayers > playing)
            {
                if (_playerCrownStatus == null)
                    _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();

                _crownTeams = new List<Team>();
            }

            //Check for expiring crowners
            if (_tickKothGameStart > 0)
            {
                foreach (var p in _playerCrownStatus)
                {
                    if ((now > p.Value.expireTime || _victoryTeam != null) && p.Value.crown)
                    {
                        p.Value.crown = false;
                        Helpers.Player_Crowns(_arena, true, _activeCrowns);
                        Helpers.Player_Crowns(_arena, false, _noCrowns);
                    }
                }

                //Get a list of teams with crowns and see if there is only one team
                _crownTeams.Clear();

                foreach (Player p in _activeCrowns)
                    if (!_crownTeams.Contains(p._team))
                        _crownTeams.Add(p._team);

                if (_crownTeams.Count == 1)
                {
                    _victoryTeam = _activeCrowns.First()._team;
                    _arena.sendArenaMessage(_victoryTeam._name + " is the winner");
                    gameVictory(_victoryTeam);
                    return true;
                }
            }

            if (_tickKothGameStart > 0 && _activeCrowns.Count <= 1)
            {
                if (_activeCrowns.Count == 1)
                {
                    _victoryTeam = _activeCrowns.First()._team;
                    if (_victoryTeam != null && !_victoryTeam.IsSpec) //Check for team activity
                    {
                        //End the game
                        _arena.sendArenaMessage(_victoryTeam._name + " is the winner");
                        gameVictory(_victoryTeam);
                        return true;
                    }
                }
                else //still bugged, no winner
                {//All our crowners expired at the same time
                    _arena.sendArenaMessage("There was no winner");
                    endKOTH();
                    return true;
                }
                return true;
            }
            //Update our tickers
            updateTickers();
            UpdateCTFTickers();
            UpdateKillStreaks();

            #region Base Bot Spawning
            #region Captain Spawning
            if (now - _tickLastCaptain > _checkCaptain && _arena._bGameRunning)
            {
                IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);
                Player owner = null;

                if (hqs != null)
                {
                    Team team = null;
                    foreach (Vehicle hq in hqs.ToList())
                    {//Handle the captains
                        Helpers.ObjectState openPoint = new Helpers.ObjectState();

                        if (_bots == null)
                            _bots = new List<Bot>();
                        Captain captain = null;

                        team = botTeam1;

                        //See if they have a captain for their HQ, if not spawn one
                        if (team != null && owner == null && captainBots != null && !captainBots.ContainsKey(team))
                        {//It's a bot team
                            openPoint = findOpenWarpBot(_arena, (short)(hq._state.positionX), (short)(hq._state.positionY), 150);

                            //Keep track of the captains
                            if (openPoint != null)
                            {
                                int id = 437;
                                captain = _arena.newBot(typeof(Captain), (ushort)id, team, null, openPoint, this, null) as Captain;
                                _arena.sendArenaMessage("A HQ Captain has been deployed to from the orbiting Pioneer Station.");
                                captainBots.Add(team, 0);
                                _bots.Add(captain);
                                if (botCount.ContainsKey(team))
                                    botCount[team] = 0;
                                else
                                    botCount.Add(team, 0);
                            }
                        }
                    }

                    if (team == null)
                    {
                        captainBots.Clear();
                    }

                }
                _tickLastCaptain = now;
            }
            #endregion

            #region Base Spawning
            if (now - _tickLastEngineer > _checkEngineer && _arena._bGameRunning && playing >= 1)
            {
                if (_bots == null)
                    _bots = new List<Bot>();
                //Should we spawn a bot engineer to go base somewhere?
                if (_currentEngineers < _maxEngineers)
                {//Yes
                    IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);
                    Vehicle home = null;
                    Helpers.ObjectState openPoint = new Helpers.ObjectState();

                    //First find out if we need to respawn to our previous team
                    foreach (Vehicle hq in hqs)
                    {
                        //Check to see if that HQ has an engineer
                        if (engineerBots.Contains(hq._team))
                            continue;

                        if (hq._team == botTeam1 || hq._team == botTeam2 || hq._team == botTeam3)
                        {
                            home = hq;
                        }

                    }

                    if (home == null)
                    {
                        //Find a random pylon to make our new home
                        IEnumerable<Vehicle> pylons = _arena.Vehicles.Where(v => v._type.Id == _pylonVehID);
                        if (pylons.Count() != 0)
                        {
                            if (playing < 30) //30
                            {
                                _currentSector1 = _eol.sectUnder30;
                                if (_currentSector1 == "Sector A")
                                {
                                    pylons = _arena.getVehiclesInArea(1, 1, 8736, 6320).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector B")
                                {
                                    pylons = _arena.getVehiclesInArea(1, 6320, 8736, 14368).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector C")
                                {
                                    pylons = _arena.getVehiclesInArea(8736, 1, 22064, 6320).Where(v => v._type.Id == _pylonVehID);

                                }
                                if (_currentSector1 == "Sector D")
                                {
                                    pylons = _arena.getVehiclesInArea(8736, 6320, 22064, 14368).Where(v => v._type.Id == _pylonVehID);
                                }
                                _rand = new Random(System.Environment.TickCount);
                                int rand = _rand.Next(0, pylons.Count());
                                home = pylons.ElementAt(rand);
                            }
                            if (playing >= 30 && playing < 60) //30
                            {
                                _currentSector1 = _eol.sectUnder30;
                                _currentSector2 = _eol.sectUnder60;
                                if (_currentSector1 == "Sector A" && _currentSector2 == "Sector B")
                                {
                                    pylons = _arena.getVehiclesInArea(1, 1, 8736, 14368).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector A" && _currentSector2 == "Sector C")
                                {
                                    pylons = _arena.getVehiclesInArea(1, 1, 22064, 6320).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector B" && _currentSector2 == "Sector D")
                                {
                                    pylons = _arena.getVehiclesInArea(1, 6320, 22064, 14368).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector C" && _currentSector2 == "Sector D")
                                {
                                    pylons = _arena.getVehiclesInArea(8736, 1, 22064, 14368).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector B" && _currentSector2 == "Sector A")
                                {
                                    pylons = _arena.getVehiclesInArea(1, 1, 8736, 14368).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector C" && _currentSector2 == "Sector A")
                                {
                                    pylons = _arena.getVehiclesInArea(1, 1, 22064, 6320).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector D" && _currentSector2 == "Sector B")
                                {
                                    pylons = _arena.getVehiclesInArea(1, 6320, 22064, 14368).Where(v => v._type.Id == _pylonVehID);
                                }
                                if (_currentSector1 == "Sector D" && _currentSector2 == "Sector C")
                                {
                                    pylons = _arena.getVehiclesInArea(8736, 1, 22064, 14368).Where(v => v._type.Id == _pylonVehID);
                                }
                                _rand = new Random(System.Environment.TickCount);
                                int rand = _rand.Next(0, pylons.Count());
                                home = pylons.ElementAt(rand);
                            }
                            if (playing >= 60)
                            {
                                pylons = _arena.getVehiclesInArea(1, 1, 22064, 14368).Where(v => v._type.Id == _pylonVehID);
                                _rand = new Random(System.Environment.TickCount);
                                int rand = _rand.Next(1, pylons.Count());
                                home = pylons.ElementAt(rand);
                            }

                            if (home._type.Id == _pylonVehID)
                            {
                                Team team = null;
                                if (_hqs[botTeam1] == null)
                                    team = botTeam1;
                                else if (_hqs[botTeam2] == null)
                                    team = botTeam2;
                                else if (_hqs[botTeam3] == null)
                                    team = botTeam3;
                                //else
                                // team = botTeam1;

                                //Find the pylon we are about to destroy and mark it as nonexistent
                                foreach (KeyValuePair<int, pylonObject> obj in _usedpylons)
                                    if (home._state.positionX == obj.Value.getX() && home._state.positionY == obj.Value.getY())
                                        obj.Value.setExists(false);

                                if (home._state.positionX == 512 && home._state.positionY == 480)
                                    _pylonLocation = 1;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 2736 && home._state.positionY == 5600)
                                    _pylonLocation = 2;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 4901 && home._state.positionY == 4740)
                                    _pylonLocation = 3;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 7445 && home._state.positionY == 2356)
                                    _pylonLocation = 4;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 5504 && home._state.positionY == 7040)
                                    _pylonLocation = 5;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 8304 && home._state.positionY == 11008)
                                    _pylonLocation = 6;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 6784 && home._state.positionY == 13808)
                                    _pylonLocation = 7;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 4117 && home._state.positionY == 9956)
                                    _pylonLocation = 8;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 13765 && home._state.positionY == 1236)
                                    _pylonLocation = 9;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 17093 && home._state.positionY == 5076)
                                    _pylonLocation = 10;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 12565 && home._state.positionY == 5252)
                                    _pylonLocation = 11;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 18117 && home._state.positionY == 3316)
                                    _pylonLocation = 12;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 20661 && home._state.positionY == 7924)
                                    _pylonLocation = 13;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 16981 && home._state.positionY == 10580)
                                    _pylonLocation = 14;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 18064 && home._state.positionY == 7584)
                                    _pylonLocation = 15;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;
                                if (home._state.positionX == 11957 && home._state.positionY == 13604)
                                    _pylonLocation = 16;
                                _botlocX = home._state.positionX;
                                _botlocY = home._state.positionY;

                                home.destroy(false);

                                #region bot turrets

                                //Create their HQ
                                createVehicle(469, 0, 0, team, home); //Build our HQ which will spawn our captain
                                _hqs.Create(team);
                                _hqs[team].Bounty = 10000;
                                _currentEngineers++;
                                engineerBots.Add(team);
                                _arena.sendArenaMessage("$A new Headquarters has been dispatched by Pioneer Station");
                                _rand = new Random();
                                int rand;
                                switch (_pylonLocation)
                                {
                                    case 1:
                                        //Build a rocket
                                        createVehicle(467, -133, 20, team, home);
                                        //Build two MGs
                                        createVehicle(457, -264, 84, team, home);
                                        createVehicle(457, 245, 84, team, home);
                                        //Build a sentry
                                        createVehicle(467, 0, 50, team, home);
                                        //Build two plasma
                                        createVehicle(460, 0, 132, team, home);
                                        createVehicle(460, 229, -156, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        // createVehicle(453, -35, 35, team, home);
                                        //createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 2:
                                        //Build a rocket
                                        createVehicle(467, 293, 4, team, home);
                                        //Build two MGs
                                        createVehicle(457, 168, -92, team, home);
                                        createVehicle(457, 168, 96, team, home);
                                        //Build a sentry
                                        createVehicle(466, 50, 0, team, home);
                                        //Build two plasma
                                        createVehicle(460, 90, 0, team, home);
                                        createVehicle(460, 195, -100, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        //createVehicle(453, -35, 35, team, home);
                                        //createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 3:
                                        //Build a rocket
                                        createVehicle(467, 320, -32, team, home);
                                        //Build two MGs
                                        createVehicle(457, 90, 0, team, home);
                                        createVehicle(457, 368, 0, team, home);
                                        //Build a sentry
                                        createVehicle(466, 0, -80, team, home);
                                        //Build two plasma
                                        createVehicle(460, 208, -64, team, home);
                                        createVehicle(460, -48, -32, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        //createVehicle(453, -35, 35, team, home);
                                        //createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 4:
                                        //Build a rocket
                                        createVehicle(467, -80, 0, team, home);
                                        //Build two MGs
                                        createVehicle(457, 0, -80, team, home);
                                        createVehicle(457, 0, 80, team, home);
                                        //Build a sentry
                                        createVehicle(466, -240, -90, team, home);
                                        //Build two plasma
                                        createVehicle(460, -195, -100, team, home);
                                        createVehicle(460, -195, 100, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        // createVehicle(453, -35, 35, team, home);
                                        //createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 5:
                                        createVehicle(467, -75, -316, team, home);
                                        //Build two MGs
                                        createVehicle(457, 229, -204, team, home);
                                        createVehicle(457, 181, -428, team, home);
                                        //Build a sentry
                                        createVehicle(466, 101, -108, team, home);
                                        //Build two plasma
                                        createVehicle(460, 69, 84, team, home);
                                        createVehicle(460, -91, -44, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        //createVehicle(453, -35, 35, team, home);
                                        // createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 6:
                                        //Build a rocket
                                        createVehicle(467, -75, -35, team, home);
                                        //Build two MGs
                                        createVehicle(457, -75, 50, team, home);
                                        createVehicle(457, 75, 150, team, home);
                                        //Build a sentry
                                        createVehicle(466, -45, 50, team, home);
                                        //Build two plasma
                                        createVehicle(460, 75, -50, team, home);
                                        createVehicle(460, 35, 80, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        //createVehicle(453, -35, 35, team, home);
                                        // createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 7:
                                        //Build a rocket
                                        createVehicle(467, -107, -332, team, home);
                                        //Build two MGs
                                        createVehicle(457, -411, -332, team, home);
                                        createVehicle(457, -155, 36, team, home);
                                        //Build a sentry
                                        createVehicle(466, 101, 36, team, home);
                                        //Build two plasma
                                        createVehicle(460, -155, -140, team, home);
                                        createVehicle(460, -59, 52, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        //createVehicle(453, -35, 35, team, home);
                                        //createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 8:
                                        //Build a rocket
                                        createVehicle(467, -80, -112, team, home);
                                        //Build two MGs
                                        createVehicle(457, -304, 64, team, home);
                                        createVehicle(457, 320, -64, team, home);
                                        //Build a sentry
                                        createVehicle(466, -128, 48, team, home);
                                        //Build two plasma
                                        createVehicle(460, -176, -48, team, home);
                                        createVehicle(460, 128, 48, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        // createVehicle(453, -35, 35, team, home);
                                        //createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 9:
                                        //Build a rocket
                                        createVehicle(467, -144, 64, team, home);
                                        //Build two MGs
                                        createVehicle(457, -160, -208, team, home);
                                        createVehicle(457, 80, -64, team, home);
                                        //Build a sentry
                                        createVehicle(466, 336, -16, team, home);
                                        //Build two plasma
                                        createVehicle(460, 176, 0, team, home);
                                        createVehicle(460, 64, 128, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        //createVehicle(453, -35, 35, team, home);
                                        // createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 10:
                                        //Build a rocket
                                        createVehicle(467, -96, -48, team, home);
                                        //Build two MGs
                                        createVehicle(457, -96, 176, team, home);
                                        createVehicle(457, -96, 528, team, home);
                                        //Build a sentry
                                        createVehicle(466, 96, -50, team, home);
                                        //Build two plasma
                                        createVehicle(460, -96, 400, team, home);
                                        createVehicle(460, 96, 112, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //createVehicle(453, 35, 35, team, home);
                                        //createVehicle(453, -35, -35, team, home);
                                        //createVehicle(453, -35, 35, team, home);
                                        //createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 11:
                                        //Build a rocket
                                        createVehicle(467, -96, -0, team, home);
                                        //Build two MGs
                                        createVehicle(457, 112, -128, team, home);
                                        createVehicle(457, 112, 128, team, home);
                                        //Build a sentry
                                        createVehicle(466, -96, -176, team, home);
                                        //Build two plasma
                                        createVehicle(460, -48, -144, team, home);
                                        createVehicle(460, 48, 144, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        // createVehicle(453, 35, 35, team, home);
                                        // createVehicle(453, -35, -35, team, home);
                                        // createVehicle(453, -35, 35, team, home);
                                        // createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 12:
                                        //Build a rocket
                                        createVehicle(467, 336, -16, team, home);
                                        //Build two MGs
                                        createVehicle(457, 336, 224, team, home);
                                        createVehicle(457, -48, -48, team, home);
                                        //Build a sentry
                                        createVehicle(466, -48, 80, team, home);
                                        //Build two plasma
                                        createVehicle(460, -80, 224, team, home);
                                        createVehicle(460, 112, 224, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //  createVehicle(453, 35, 35, team, home);
                                        //  createVehicle(453, -35, -35, team, home);
                                        // createVehicle(453, -35, 35, team, home);
                                        // createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 13:
                                        createVehicle(467, 112, 144, team, home);
                                        //Build two MGs
                                        createVehicle(457, 256, 112, team, home);
                                        createVehicle(457, 256, -112, team, home);
                                        //Build a sentry
                                        createVehicle(466, -64, 0, team, home);
                                        //Build two plasma
                                        createVehicle(460, -20, 112, team, home);
                                        createVehicle(460, -20, -112, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //  createVehicle(453, 35, 35, team, home);
                                        //  createVehicle(453, -35, -35, team, home);
                                        //  createVehicle(453, -35, 35, team, home);
                                        //  createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 14:
                                        //Build a rocket
                                        createVehicle(467, -384, 0, team, home);
                                        //Build two MGs
                                        createVehicle(457, -400, 144, team, home);
                                        createVehicle(457, 304, 16, team, home);
                                        //Build a sentry
                                        createVehicle(466, 48, -80, team, home);
                                        //Build two plasma
                                        createVehicle(460, -272, -50, team, home);
                                        createVehicle(460, 256, 144, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //   createVehicle(453, 35, 35, team, home);
                                        //   createVehicle(453, -35, -35, team, home);
                                        //  createVehicle(453, -35, 35, team, home);
                                        //   createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 15:
                                        //Build a rocket
                                        createVehicle(467, 117, 212, team, home);
                                        //Build two MGs
                                        createVehicle(457, 117, -60, team, home);
                                        createVehicle(457, -219, 132, team, home);
                                        //Build a sentry
                                        createVehicle(466, -395, 196, team, home);
                                        //Build two plasma
                                        createVehicle(460, 69, 308, team, home);
                                        createVehicle(460, -75, -76, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //   createVehicle(453, 35, 35, team, home);
                                        //   createVehicle(453, -35, -35, team, home);
                                        //   createVehicle(453, -35, 35, team, home);
                                        //   createVehicle(453, 35, -35, team, home);
                                        break;
                                    case 16:
                                        //Build a rocket
                                        createVehicle(467, -96, 16, team, home);
                                        //Build two MGs
                                        createVehicle(457, 192, -240, team, home);
                                        createVehicle(457, 192, -96, team, home);
                                        //Build a sentry
                                        createVehicle(466, 80, 64, team, home);
                                        //Build two plasma
                                        createVehicle(460, -64, 64, team, home);
                                        createVehicle(460, 352, -160, team, home);
                                        //walls
                                        //createVehicle(453, 35, 0, team, home);
                                        //createVehicle(453, 0, 35, team, home);
                                        //createVehicle(453, -35, 0, team, home);
                                        //createVehicle(453, 0, -35, team, home);
                                        //   createVehicle(453, 35, 35, team, home);
                                        //   createVehicle(453, -35, -35, team, home);
                                        //   createVehicle(453, -35, 35, team, home);
                                        //   createVehicle(453, 35, -35, team, home);
                                        break;

                                }
                                #endregion

                            }
                        }
                        else { addPylons(); }
                    }

                }
                _tickLastEngineer = now;
            }
            #endregion

            return true;
        }
        #endregion

        #region Change Sectors

        public void preNewSector()
        {
            _arena.sendArenaMessage("Radiation Wind Change Warning! New Sectors in 90 Seconds get safe, You will be sent to Pioneer Station before sector change", _config.flag.victoryWarningBong);
            _arena.setTicker(1, 1, 90 * 100, "Radiation Wind Change Warning! New Sectors in 90 Seconds: ",
            delegate ()
            {
                newSectorDSRecall();
                _bbetweengames = true;
            });
        }

        public void newSectorDSRecall()
        {
            Helpers.ObjectState warpPoint;
            foreach (Player player in _arena.PlayersIngame)
            {
                warpPoint = findOpenWarp(player, _arena, 27008, 3360, 300);
                if (warpPoint == null)
                {
                    Log.write(TLog.Normal, String.Format("Could not find open warp (Warp Blocked)", player._alias));
                    player.sendMessage(-1, "There was an issue sending you back to Pioneer Station, please contact the dev team about this issue.");
                }
                warp(player, warpPoint);
            }
            foreach (Player player in _arena.Players)
            {
                player.clearProjectiles();
            }

            _bbetweengames = true;
            foreach (Vehicle v in _arena.Vehicles)
                if (v._type.Type == VehInfo.Types.Computer)
                    //Destroy it!
                    v.destroy(true);
            _usedpylons.Clear();
            foreach (Bot bot in _bots)
                _condemnedBots.Add(bot);

            foreach (Bot bot in _condemnedBots)
                bot.destroy(true);

            _minX = 0;
            _maxX = 0;
            _minY = 0;
            _maxY = 0;

        }

        #endregion

        #region AddingBots
        //Creates a turrent, offsets are from HQ
        public void createVehicle(int id, int x_offset, int y_offset, Team botTeam, Vehicle homeloc)
        {
            if (homeloc != null && botTeam != null)
            {
                VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(id));
                Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
                newState.positionX = Convert.ToInt16(homeloc._state.positionX + x_offset);
                newState.positionY = Convert.ToInt16(homeloc._state.positionY + y_offset);
                newState.positionZ = homeloc._state.positionZ;
                newState.yaw = homeloc._state.yaw;

                _arena.newVehicle(vehicle, botTeam, null, newState);
            }
        }

        //Creates a turrent, offsets are from HQ
        public void createHPVehicle(int id, int x_offset, int y_offset, Team botTeam, Helpers.ObjectState homeloc)
        {
            if (homeloc != null && botTeam != null)
            {
                VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(id));

                _arena.newVehicle(vehicle, botTeam, null, homeloc);
            }
        }

        //Creates a turrent, offsets are from HQ
        public void createPDB(int id, int x_offset, int y_offset, Team botTeam, Helpers.ObjectState homeloc)
        {
            if (homeloc != null && botTeam != null)
            {
                VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(id));
                Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
                newState.positionX = Convert.ToInt16(homeloc.positionX + x_offset);
                newState.positionY = Convert.ToInt16(homeloc.positionY + y_offset);
                newState.positionZ = homeloc.positionZ;
                newState.yaw = homeloc.yaw;

                _arena.newVehicle(vehicle, botTeam, null, newState);
            }
        }

        public void spawnHorde(int now)
        {
            if (_bPvpHappyHour)
            {
                IEnumerable<Vehicle> hps = _arena.Vehicles.Where(v => v._type.Id == 468);

                if (hps.Count() == 0)
                {
                    try
                    {
                        int blockedAttempts = 30;

                        short pX;
                        short pY;
                        Helpers.ObjectState warpPointy = null;
                        while (true)
                        {
                            
                            Random r1 = new Random();
                            Random r2 = new Random();
                            pX = (short)r1.Next(_minX, _maxX);
                            pY = (short)r2.Next(_minY, _maxY);
                            CfgInfo.Terrain terrain = _arena.getTerrain(pX, pY);

                            Helpers.randomPositionInArea(_arena, 100, ref pX, ref pY);
                            if (_arena.getTile(pX, pY).Blocked && (terrain.message != "Roadway/Cement" || terrain.message != "Clear Terrain"))
                            {
                                blockedAttempts--;
                                if (blockedAttempts <= 0)
                                {
                                    //Consider the area to be blocked
                                    warpPointy = new Helpers.ObjectState();
                                    warpPointy = null;
                                }
                                else
                                {
                                    warpPointy = new Helpers.ObjectState();
                                    warpPointy.positionX = pX;
                                    warpPointy.positionY = pY;
                                    continue;
                                }
                            }

                        }
                        if (warpPointy != null)
                        {
                            Helpers.ObjectState openPoint = null;
                            openPoint = new Helpers.ObjectState();

                            openPoint = findOpenWarpBot(_arena, (warpPointy.positionX), (warpPointy.positionY), 10);

                            //Vehicle newloc = null;
                            //newloc._state.positionX = openPoint.positionX;
                            //newloc._state.positionY = openPoint.positionY;

                            Team teambot = botTeam4;
                            createHPVehicle(468, 0, 0, teambot, openPoint); //Build our Horde Point

                            Helpers.ObjectState PDB1 = null;
                            Helpers.ObjectState PDB2 = null;
                            Helpers.ObjectState PDB3 = null;

                            PDB1 = new Helpers.ObjectState();
                            PDB1 = findOpenWarpBot(_arena, (openPoint.positionX), (openPoint.positionY), 10);
                            PDB2 = new Helpers.ObjectState();
                            PDB2 = findOpenWarpBot(_arena, (openPoint.positionX), (openPoint.positionY), 10);
                            PDB3 = new Helpers.ObjectState();
                            PDB3 = findOpenWarpBot(_arena, (openPoint.positionX), (openPoint.positionY), 10);
                            //Build AA/AG PDB
                            createPDB(159, -233, 50, teambot, PDB1);
                            //Build AC PDB
                            createPDB(160, -264, 284, teambot, PDB2);
                            //Build ASC PDB
                            createPDB(161, -464, 484, teambot, PDB3);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.write(TLog.Exception, ex.Message);
                    }

                    IEnumerable<Vehicle> points = _arena.Vehicles.Where(v => v._type.Id == 468);


                    if (points.Count() >= 1)
                    {
                        Vehicle newhp = points.FirstOrDefault();
                        HordePoints hpoint = new HordePoints(_arena, newhp, this);
                        _activehPoints.Add(hpoint);
                    }
                    else
                    {
                        _activehPoints.Clear();
                    }

                }
                

                if (now - _tickLastRoamingCaptain > _checkRoamingCaptain && _arena._bGameRunning && hps.Count() >= 1)
                {

                    if (_bots == null)
                        _bots = new List<Bot>();
                    if (_currentRoamCaptains < _maxRoamCaptains)
                    {
                        int id = 0;

                        Helpers.ObjectState warpPoint = null;
                        Helpers.ObjectState openPoint = null;

                        short indexX = 0;
                        short indexY = 0;

                        Random r1 = new Random();
                        Random r2 = new Random();
                        indexX = (short)r1.Next(_minX, _maxX);
                        indexY = (short)r2.Next(_minY, _maxY);

                        warpPoint = new Helpers.ObjectState();
                        warpPoint.positionX = indexX;
                        warpPoint.positionY = indexY;

                        openPoint = new Helpers.ObjectState();

                        openPoint = findOpenWarpBot(_arena, (warpPoint.positionX), (warpPoint.positionY), 150);

                        Team team = botTeam4;

                        if (openPoint != null)
                        {
                            RoamingCaptain Flagger = _arena.newBot(typeof(RoamingCaptain), (ushort)142, team, null, openPoint, this) as RoamingCaptain;
                            _arena.sendArenaMessage("$A rogue Captain has arrived in Eol. Intent on killing all and capturing the seismic control unit.");
                            _bots.Add(Flagger);
                            _currentRoamCaptains++;
                            roamingCaptianBots.Add(team);
                            capRoamBots.Add(team, 0);
                            if (roamBots.ContainsKey(team))
                            { roamBots[team] = 0; }
                            else
                            { roamBots.Add(team, 0); }
                        }
                    }
                    _tickLastRoamingCaptain = now;
                }
            }
            else
            {
                Team team = botTeam4;

                foreach (Bot bot in _bots)
                {
                    switch (bot._type.Name)
                    {
                        case ("Bot Team - Eta"):
                            {
                                _condemnedBots.Add(bot);
                            }
                            break;
                    }
                }

                foreach (Bot bot in _condemnedBots)
                    bot.destroy(false);

                roamBots.Remove(team); //Signal to our captain we died
                roamingCaptianBots.Remove(team);
                capRoamBots.Remove(team);
                _currentRoamCaptains--;
            }
        }

        public void addBot(Player owner, Helpers.ObjectState state, Team team)
        {
            if (_bots == null)
                _bots = new List<Bot>();

            int id = 140;
            Helpers.ObjectState openPoint = new Helpers.ObjectState();

            openPoint = findOpenWarpBot(_arena, state.positionX, state.positionY, 150);

            Random coolbots = new Random();
            int _randombot = coolbots.Next(1, 5);

            switch (_randombot)
            {
                case 1:
                    id = 140;
                    break;
                case 2:
                    id = 141;
                    break;
                case 3:
                    id = 143;
                    break;
                case 4:
                    id = 156;
                    break;
                case 5:
                    id = 158;
                    break;
            }
            if (owner == null)
            {//This is a bot team
             //Spawn a random bot in their faction
                if (botCount.ContainsKey(team))
                    botCount[team]++;
                else
                    botCount.Add(team, 0);
                BasicDefense dBot = _arena.newBot(typeof(BasicDefense), (ushort)id, team, null, openPoint, this, null) as BasicDefense;
                _bots.Add(dBot);
            }
        }

        public void addBotRoam(Player owner, Helpers.ObjectState state, Team team)
        {

            if (_bots == null)
                _bots = new List<Bot>();

            Helpers.ObjectState openPoint = new Helpers.ObjectState();

            openPoint = findOpenWarpBot(_arena, state.positionX, state.positionY, 150);

            int id = 144;

            Random roamtype = new Random();
            int _randomroam = roamtype.Next(1, 5);

            switch (_randomroam)
            {
                case 1:
                    id = 144;
                    break;
                case 2:
                    id = 145;
                    break;
                case 3:
                    id = 146;
                    break;
                case 4:
                    id = 147;
                    break;
                case 5:
                    id = 157;
                    break;

            }
            if (owner == null)
            {//This is a bot team
             //Spawn a random bot in their faction
                if (roamBots.ContainsKey(team))
                    roamBots[team]++;
                else
                    roamBots.Add(team, 0);
                RoamingAttacker dBot = _arena.newBot(typeof(RoamingAttacker), (ushort)id, team, null, openPoint, this, null) as RoamingAttacker;
                _bots.Add(dBot);
            }
        }
        #endregion

        #region Pylons
        public void addPylons()
        {
            int playing = _arena.PlayersIngame.Count();
            _usedpylons = new Dictionary<int, pylonObject>();
            if (playing < 30) //30
            {
                _currentSector1 = _eol.sectUnder30;
                switch (_currentSector1)
                {
                    case "Sector A":
                        _usedpylons.Add(0, new pylonObject(512, 480)); // Sector A
                        _usedpylons.Add(1, new pylonObject(2736, 5600)); // Sector A
                        _usedpylons.Add(2, new pylonObject(4901, 4740)); // Sector A
                        _usedpylons.Add(3, new pylonObject(7445, 2356)); // Sector A
                        break;
                    case "Sector B":
                        _usedpylons.Add(0, new pylonObject(5504, 7040)); // Sector B
                        _usedpylons.Add(1, new pylonObject(8304, 11008));// Sector B
                        _usedpylons.Add(2, new pylonObject(6784, 13808));// Sector B
                        _usedpylons.Add(3, new pylonObject(4117, 9956));// Sector B

                        break;
                    case "Sector C":
                        _usedpylons.Add(0, new pylonObject(13765, 1236)); // Sector C
                        _usedpylons.Add(1, new pylonObject(17093, 5076)); // Sector C
                        _usedpylons.Add(2, new pylonObject(12565, 5252)); // Sector C
                        _usedpylons.Add(3, new pylonObject(18117, 3316)); // Sector C
                        break;
                    case "Sector D":
                        _usedpylons.Add(0, new pylonObject(20661, 7924)); // Sector D
                        _usedpylons.Add(1, new pylonObject(16981, 10580)); // Sector D
                        _usedpylons.Add(2, new pylonObject(18064, 7584)); // Sector D
                        _usedpylons.Add(3, new pylonObject(11957, 13604)); // Sector D
                        break;
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
                    _arena.newVehicle(vehicle, botTeam1, null, newState);

                    _bpylonsSpawned = true;
                }
            }
            if (playing >= 30 && playing < 60) //30
            {
                _currentSector1 = _eol.sectUnder30;
                _currentSector2 = _eol.sectUnder60;
                switch (_currentSector1)
                {
                    case "Sector A":
                        switch (_currentSector2)
                        {
                            case "Sector B":
                                _usedpylons.Add(0, new pylonObject(512, 480)); // Sector A
                                _usedpylons.Add(1, new pylonObject(2736, 5600)); // Sector A
                                _usedpylons.Add(2, new pylonObject(4901, 4740)); // Sector A
                                _usedpylons.Add(3, new pylonObject(7445, 2356)); // Sector A
                                _usedpylons.Add(4, new pylonObject(5504, 7040)); // Sector B
                                _usedpylons.Add(5, new pylonObject(8304, 11008));// Sector B
                                _usedpylons.Add(6, new pylonObject(6784, 13808));// Sector B
                                _usedpylons.Add(7, new pylonObject(4117, 9956));// Sector B
                                break;
                            case "Sector C":
                                _usedpylons.Add(0, new pylonObject(512, 480)); // Sector A
                                _usedpylons.Add(1, new pylonObject(2736, 5600)); // Sector A
                                _usedpylons.Add(2, new pylonObject(4901, 4740)); // Sector A
                                _usedpylons.Add(3, new pylonObject(7445, 2356)); // Sector A
                                _usedpylons.Add(4, new pylonObject(13765, 1236)); // Sector C
                                _usedpylons.Add(5, new pylonObject(17093, 5076)); // Sector C
                                _usedpylons.Add(6, new pylonObject(12565, 5252)); // Sector C
                                _usedpylons.Add(7, new pylonObject(18117, 3316)); // Sector C
                                break;
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
                            _arena.newVehicle(vehicle, botTeam1, null, newState);

                            _bpylonsSpawned = true;
                        }
                        break;
                    case "Sector B":
                        switch (_currentSector2)
                        {
                            case "Sector A":
                                _usedpylons.Add(0, new pylonObject(512, 480)); // Sector A
                                _usedpylons.Add(1, new pylonObject(2736, 5600)); // Sector A
                                _usedpylons.Add(2, new pylonObject(4901, 4740)); // Sector A
                                _usedpylons.Add(3, new pylonObject(7445, 2356)); // Sector A
                                _usedpylons.Add(4, new pylonObject(5504, 7040)); // Sector B
                                _usedpylons.Add(5, new pylonObject(8304, 11008));// Sector B
                                _usedpylons.Add(6, new pylonObject(6784, 13808));// Sector B
                                _usedpylons.Add(7, new pylonObject(4117, 9956));// Sector B
                                break;
                            case "Sector D":
                                _usedpylons.Add(0, new pylonObject(5504, 7040)); // Sector B
                                _usedpylons.Add(1, new pylonObject(8304, 11008));// Sector B
                                _usedpylons.Add(2, new pylonObject(6784, 13808));// Sector B
                                _usedpylons.Add(3, new pylonObject(4117, 9956));// Sector B
                                _usedpylons.Add(4, new pylonObject(20661, 7924)); // Sector D
                                _usedpylons.Add(5, new pylonObject(16981, 10580)); // Sector D
                                _usedpylons.Add(6, new pylonObject(18064, 7584)); // Sector D
                                _usedpylons.Add(7, new pylonObject(11957, 13604)); // Sector D
                                break;
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
                            _arena.newVehicle(vehicle, botTeam1, null, newState);

                            _bpylonsSpawned = true;
                        }
                        break;
                    case "Sector C":
                        switch (_currentSector2)
                        {
                            case "Sector A":
                                _usedpylons.Add(0, new pylonObject(512, 480)); // Sector A
                                _usedpylons.Add(1, new pylonObject(2736, 5600)); // Sector A
                                _usedpylons.Add(2, new pylonObject(4901, 4740)); // Sector A
                                _usedpylons.Add(3, new pylonObject(7445, 2356)); // Sector A
                                _usedpylons.Add(4, new pylonObject(13765, 1236)); // Sector C
                                _usedpylons.Add(5, new pylonObject(17093, 5076)); // Sector C
                                _usedpylons.Add(6, new pylonObject(12565, 5252)); // Sector C
                                _usedpylons.Add(7, new pylonObject(18117, 3316)); // Sector C
                                break;
                            case "Sector D":
                                _usedpylons.Add(0, new pylonObject(13765, 1236)); // Sector C
                                _usedpylons.Add(1, new pylonObject(17093, 5076)); // Sector C
                                _usedpylons.Add(2, new pylonObject(12565, 5252)); // Sector C
                                _usedpylons.Add(3, new pylonObject(18117, 3316)); // Sector C
                                _usedpylons.Add(4, new pylonObject(20661, 7924)); // Sector D
                                _usedpylons.Add(5, new pylonObject(16981, 10580)); // Sector D
                                _usedpylons.Add(6, new pylonObject(18064, 7584)); // Sector D
                                _usedpylons.Add(7, new pylonObject(11957, 13604)); // Sector D
                                break;
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
                            _arena.newVehicle(vehicle, botTeam1, null, newState);

                            _bpylonsSpawned = true;
                        }
                        break;
                    case "Sector D":
                        switch (_currentSector2)
                        {
                            case "Sector C":
                                _usedpylons.Add(0, new pylonObject(13765, 1236)); // Sector C
                                _usedpylons.Add(1, new pylonObject(17093, 5076)); // Sector C
                                _usedpylons.Add(2, new pylonObject(12565, 5252)); // Sector C
                                _usedpylons.Add(3, new pylonObject(18117, 3316)); // Sector C
                                _usedpylons.Add(4, new pylonObject(20661, 7924)); // Sector D
                                _usedpylons.Add(5, new pylonObject(16981, 10580)); // Sector D
                                _usedpylons.Add(6, new pylonObject(18064, 7584)); // Sector D
                                _usedpylons.Add(7, new pylonObject(11957, 13604)); // Sector D
                                break;
                            case "Sector B":
                                _usedpylons.Add(0, new pylonObject(5504, 7040)); // Sector B
                                _usedpylons.Add(1, new pylonObject(8304, 11008));// Sector B
                                _usedpylons.Add(2, new pylonObject(6784, 13808));// Sector B
                                _usedpylons.Add(3, new pylonObject(4117, 9956));// Sector B
                                _usedpylons.Add(4, new pylonObject(20661, 7924)); // Sector D
                                _usedpylons.Add(5, new pylonObject(16981, 10580)); // Sector D
                                _usedpylons.Add(6, new pylonObject(18064, 7584)); // Sector D
                                _usedpylons.Add(7, new pylonObject(11957, 13604)); // Sector D
                                break;
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
                            _arena.newVehicle(vehicle, botTeam1, null, newState);

                            _bpylonsSpawned = true;
                        }
                        break;
                }
            }
            _currentSector1 = _eol.sectUnder30;
            if (playing >= 60 && _currentSector1 == "All Sectors")
            {
                _usedpylons.Add(0, new pylonObject(512, 480)); // Sector A
                _usedpylons.Add(1, new pylonObject(2736, 5600)); // Sector A
                _usedpylons.Add(2, new pylonObject(4901, 4740)); // Sector A
                _usedpylons.Add(3, new pylonObject(7445, 2356)); // Sector A
                _usedpylons.Add(4, new pylonObject(5504, 7040)); // Sector B
                _usedpylons.Add(5, new pylonObject(8304, 11008));// Sector B
                _usedpylons.Add(6, new pylonObject(6784, 13808));// Sector B
                _usedpylons.Add(7, new pylonObject(4117, 9956));// Sector B
                _usedpylons.Add(8, new pylonObject(13765, 1236)); // Sector C
                _usedpylons.Add(9, new pylonObject(17093, 5076)); // Sector C
                _usedpylons.Add(10, new pylonObject(12565, 5252)); // Sector C
                _usedpylons.Add(11, new pylonObject(18117, 3316)); // Sector C
                _usedpylons.Add(12, new pylonObject(20661, 7924)); // Sector D
                _usedpylons.Add(13, new pylonObject(16981, 10580)); // Sector D
                _usedpylons.Add(14, new pylonObject(18064, 7584)); // Sector D
                _usedpylons.Add(15, new pylonObject(11957, 13604)); // Sector D

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
                    _arena.newVehicle(vehicle, botTeam1, null, newState);

                    _bpylonsSpawned = true;
                }
            }

        }
        #endregion

        #region KOTH
        /// <summary>
        /// Called when KOTH game has ended
        /// </summary>
        public void endKOTH()
        {
            _arena.sendArenaMessage("KOTH has ended");

            _tickKothGameStart = 0;
            _tickKOTHGameStarting = 0;
            _victoryKothTeam = null;
            _crownTeams = null;

            _arena.setTicker(1, 1, 0, "KOTH - Not Enough Players");

            //Remove all crowns and clear list of KOTH players
            Helpers.Player_Crowns(_arena, false, _arena.Players.ToList());
            _playerCrownStatus.Clear();
        }

        /// <summary>
        /// Called when KOTH game has been restarted
        /// </summary>
        public void resetKOTH()
        {//Game reset, perhaps start a new one
            _tickKothGameStart = 0;
            _tickKOTHGameStarting = 0;

            _victoryKothTeam = null;

            //Remove all crowns and clear list of KOTH players
            Helpers.Player_Crowns(_arena, false, _arena.Players.ToList());
            _playerCrownStatus.Clear();
        }

        /// <summary>
        /// Called when KOTH game has started
        /// </summary>
        public void startKOTH()
        {
            //We've started!
            _tickKothGameStart = Environment.TickCount;
            _tickKOTHGameStarting = 0;
            _playerCrownStatus.Clear();
            _kothGameRunning = true;

            //Let everyone know
            _arena.sendArenaMessage("A new KOTH Game has started!", 1);

            _crownTeams = new List<Team>();
            _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
            List<Player> crownPlayers = (_config.king.giveSpecsCrowns ? _arena.Players : _arena.PlayersIngame).ToList();

            foreach (var p in crownPlayers)
            {
                _playerCrownStatus[p] = new PlayerCrownStatus();
                giveCrown(p);
            }
            //Everybody is king!
            Helpers.Player_Crowns(_arena, true, crownPlayers);
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void kothVictory(Team victors)
        {   //Let everyone know          
            //Calculate the jackpot for each player
            foreach (Player p in victors.AllPlayers)
            {   //Spectating? 
                if (p.IsSpectator)
                    continue;

                //Obtain the respective rewards
                int cashReward = _config.king.cashReward * _arena.PlayerCount;
                int experienceReward = _config.king.experienceReward * _arena.PlayerCount;
                int pointReward = _config.king.pointReward * _arena.PlayerCount;

                p.sendMessage(0, String.Format("Your Personal Reward: Points={0} Cash={1} Experience={2}", pointReward, cashReward, experienceReward));

                //Prize winning team
                p.Cash += cashReward;
                p.Experience += experienceReward;
                p.BonusPoints += pointReward;
            }
            _victoryKothTeam = null;

            endKOTH();
        }


        /// <summary>
        /// Updates our tickers for KOTH
        /// </summary>
        public void updateTickers()
        {
            if (_arena.ActiveTeams.Count() > 1 && _kothGameRunning)
            {//Show players their crown timer using a ticker
                _arena.setTicker(1, 1, 0, delegate (Player p)
                {
                    if (_playerCrownStatus.ContainsKey(p) && _playerCrownStatus[p].crown)
                        return String.Format("Crown Timer: {0}", (_playerCrownStatus[p].expireTime - Environment.TickCount) / 1000);

                    else
                        return "";
                });
            }

            if (_points != null)
            {
                //Their teams points
                _arena.setTicker(0, 2, 0,
                    delegate (Player p)
                    {
                        //Update their ticker with current team points
                        if (!_arena.DesiredTeams.Contains(p._team) && _points != null)
                            return "";
                        return "Your Team: " + _points[p._team] + " points";
                    }
                );
                //Other teams points
                _arena.setTicker(0, 3, 0,
                    delegate (Player p)
                    {
                        //Update their ticker with every other teams points
                        List<string> otherTeams = new List<string>();
                        foreach (Team t in _arena.DesiredTeams)
                            if (t != p._team)
                                otherTeams.Add(t._name + ": " + _points[t] + " points");
                        if (otherTeams.Count == 0)
                            return "";
                        return String.Join(", ", otherTeams.ToArray());
                    }
                );
                //Point rewards
                //_arena.setTicker(0, 2, 0, "Kill rewards: " + _pointSmallChange + " points");
            }

        }

        /// <summary>
        /// Gives a crown to the specified player
        /// </summary>
        public void giveCrown(Player p)
        {//Give the player a crown and inform the arena
            var v = _playerCrownStatus[p];
            v.crown = true;
            v.crownDeaths = 0;
            v.crownKills = 0;
            List<Player> crowns = _activeCrowns;
            Helpers.Player_Crowns(_arena, true, crowns);
            updateCrownTime(p);
        }

        /// <summary>
        /// Updates the crown time for the specified player
        /// </summary>
        public void updateCrownTime(Player p)
        {   //Reset the player's counter
            _playerCrownStatus[p].expireTime = Environment.TickCount + (_config.king.expireTime * 1000);
        }

        #endregion

        #region CTF

        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void onFlagChange(Arena.FlagState flag)
        {   //Does this team now have all the flags?
            Team victoryTeam = flag.team;


            foreach (Arena.FlagState fs in _arena._flags.Values)
                if (fs.bActive && fs.team != victoryTeam)
                    victoryTeam = null;

            if (victoryTeam != null)
            {   //Yes! Victory for them!
                _arena.setTicker(1, 1, _config.flag.victoryHoldTime, "Victory in ");
                _tickNextVictoryNotice = _tickVictoryStart = Environment.TickCount;
                _victoryTeam = victoryTeam;
            }
            else
            {   //Aborted?
                if (_victoryTeam != null && !_gameWon)
                {
                    _tickVictoryStart = 0;
                    _tickNextVictoryNotice = 0;
                    _victoryTeam = null;
                    _victoryNotice = 0;

                    _arena.sendArenaMessage("Victory has been aborted.", _config.flag.victoryAbortedBong);
                    _arena.setTicker(1, 1, 0, "");
                }
            }
        }

        #endregion

        #region Warping
        /// <summary>
        /// Finds a specific point within a radius with no physics for a player to warp to
        /// </summary>
        /// <param name="arena"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public Helpers.ObjectState findOpenWarp(Player player, Arena _arena, short posX, short posY, int radius)
        {
            Helpers.ObjectState warpPoint = null;

            try
            {
                int blockedAttempts = 10;

                short pX;
                short pY;

                while (true)
                {
                    pX = posX;
                    pY = posY;
                    Helpers.randomPositionInArea(_arena, radius, ref pX, ref pY);
                    if (_arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the area to be blocked
                            return null;
                        else
                            continue;
                    }
                    warpPoint = new Helpers.ObjectState();
                    warpPoint.positionX = pX;
                    warpPoint.positionY = pY;
                    break;
                }
            }
            catch (Exception ex)
            {
                Log.write(TLog.Exception, ex.Message);
            }
            return warpPoint;
        }

        public Helpers.ObjectState findOpenWarpBot(Arena _arena, short posX, short posY, int radius)
        {
            Helpers.ObjectState warpPoint = null;

            try
            {
                int blockedAttempts = 10;

                short pX;
                short pY;

                while (true)
                {
                    pX = posX;
                    pY = posY;
                    Helpers.randomPositionInArea(_arena, radius, ref pX, ref pY);
                    if (_arena.getTile(pX, pY).Blocked)
                    {
                        blockedAttempts--;
                        if (blockedAttempts <= 0)
                            //Consider the area to be blocked
                            return null;
                        else
                            continue;
                    }
                    warpPoint = new Helpers.ObjectState();
                    warpPoint.positionX = pX;
                    warpPoint.positionY = pY;
                    break;
                }
            }
            catch (Exception ex)
            {
                Log.write(TLog.Exception, ex.Message);
            }
            return warpPoint;
        }

        public Helpers.ObjectState findFlagWarp(Team team, bool bBot)
        {
            Helpers.ObjectState warpPoint = null;
            List<Arena.FlagState> sortedFlags = new List<Arena.FlagState>();

            if (!team._name.Contains("Bot Team -"))
                sortedFlags = _arena._flags.Values.ToList();

            int count = sortedFlags.Count;

            //No flags for some reason?
            if (count == 0)
                return null;


            Random r = new Random();
            int _randFlag = r.Next(0, sortedFlags.Count);
            _flagchecking = _coolArray[_randFlag];

            warpPoint = new Helpers.ObjectState();
            warpPoint.positionX = sortedFlags[_randFlag].posX;
            warpPoint.positionY = sortedFlags[_randFlag].posY;

            short _randomDist = 0;
            Random rn = new Random();
            int _randomNo = rn.Next(50, 450);
            _randomDist = Convert.ToInt16(_randomNo);

            int spawnPos = 0;

            Random rnd = new Random();
            int _randPos = rnd.Next(0, 4);
            spawnPos = _randPos;

            switch (spawnPos)
            {
                case 1:
                    warpPoint.positionX = (short)(warpPoint.positionX - _randomDist);
                    break;
                case 2:
                    warpPoint.positionX = (short)(warpPoint.positionX + _randomDist);
                    break;
                case 3:
                    warpPoint.positionY = (short)(warpPoint.positionY - _randomDist);
                    break;
                case 4:
                    warpPoint.positionY = (short)(warpPoint.positionY + _randomDist);
                    break;
            }


            return warpPoint;
        }


        public void warp(Player player, Helpers.ObjectState warpTo)
        {
            player.warp(warpTo.positionX, warpTo.positionY);
        }

        #endregion

        #region HQ Levelling
        /// <summary>
        /// Triggered when an HQ levels up (or down?)
        /// </summary>
        public void onHQLevelModify(Team team)
        {
            //Let the team know they've leveled up
            if (_hqs[team].Level != _hqlevels.Count())
                team.sendArenaMessage("&Headquarters - Your HQ has reached level " + _hqs[team].Level + "! You need " + _hqlevels[_hqs[team].Level] + " bounty to reach the next level");

            //Lets notify everyone whenever an HQ reaches level 10!
            if (_hqs[team].Level == 10)
                _arena.sendArenaMessage("&Headquarters - " + team._name + " HQ has reached the max level of " + _hqlevels.Count() + "!");
        }
        #endregion

        #region Tickers and Updaters

        private void UpdateCTFTickers()
        {
            int playing = _arena.PlayerCount;
            List<Player> rankedPlayers = _arena.Players.ToList().OrderBy(player => (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.deaths)).OrderByDescending(
                player => (player.StatsCurrentGame == null ? 0 : player.StatsCurrentGame.kills)).ToList();
            int idx = 3;
            string format = "";
            foreach (Player p in rankedPlayers)
            {
                if (p.StatsCurrentGame == null)
                { continue; }
                if (idx-- == 0)
                {
                    break;
                }

                switch (idx)
                {
                    case 2:
                        format = string.Format("1st: {0}(K={1} D={2}) ", p._alias, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
                        break;
                    case 1:
                        format = (format + string.Format("2nd: {0}(K={1} D={2})", p._alias, p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths));
                        break;
                }
            }
            if (!string.IsNullOrWhiteSpace(format))
            { _arena.setTicker(1, 4, 0, format); }

            _arena.setTicker(2, 4, 0, delegate (Player p)
            {
                if (p.StatsCurrentGame == null)
                {
                    return "Personal Score: Kills=0 - Deaths=0";
                }
                return string.Format("Personal Score: Kills={0} - Deaths={1}", p.StatsCurrentGame.kills, p.StatsCurrentGame.deaths);
            });
        }

        /// <summary>
        /// Updates our players kill streak timer
        /// </summary>
        private void UpdateKillStreaks()
        {
            foreach (KeyValuePair<string, PlayerStreak> p in killStreaks)
            {
                if (p.Value.lastUsedWepTick == -1)
                    continue;

                if (Environment.TickCount - p.Value.lastUsedWepTick <= 0)
                    ResetWeaponTicker(p.Key);
            }
        }


        /// <summary>
        /// Updates the last killer
        /// </summary>
        private void ResetKiller(Player killer)
        {
            lastKiller = killer;
        }

        /// <summary>
        /// Resets the weapon ticker to default (Time Expired)
        /// </summary>
        private void ResetWeaponTicker(string targetAlias)
        {
            if (killStreaks.ContainsKey(targetAlias))
            {
                killStreaks[targetAlias].lastUsedWeap = null;
                killStreaks[targetAlias].lastUsedWepKillCount = 0;
                killStreaks[targetAlias].lastUsedWepTick = -1;
            }
        }

        /// <summary>
        /// Updates the killer and their kill counter
        /// </summary>
        private void UpdateKiller(Player killer)
        {
            if (killStreaks.ContainsKey(killer._alias))
            {
                killStreaks[killer._alias].lastKillerCount++;
                switch (killStreaks[killer._alias].lastKillerCount)
                {
                    case 6:
                        _arena.sendArenaMessage(string.Format("{0} is on fire!", killer._alias), 8);
                        break;
                    case 8:
                        _arena.sendArenaMessage(string.Format("Someone kill {0}!", killer._alias), 9);
                        break;
                }
            }
            //Is this first blood?
            if (lastKiller == null)
            {
                //It is, lets make the sound
                _arena.sendArenaMessage(string.Format("{0} has drawn first blood.", killer._alias), 7);
            }
            lastKiller = killer;
        }

        /// <summary>
        /// Updates the victim's kill streak and notifies the public
        /// </summary>
        private void UpdateDeath(Player victim, Player killer)
        {
            if (killStreaks.ContainsKey(victim._alias))
            {
                if (killStreaks[victim._alias].lastKillerCount >= 6)
                {
                    _arena.sendArenaMessage(string.Format("{0}", killer != null ? killer._alias + " has ended " + victim._alias + "'s kill streak." :
                        victim._alias + "'s kill streak has ended."), 6);
                }
                killStreaks[victim._alias].lastKillerCount = 0;
            }
        }

        /// <summary>
        /// Updates the last fired weapon and its ticker
        /// </summary>
        private void UpdateWeapon(Player from, ItemInfo.Projectile usedWep, int aliveTime)
        {
            if (killStreaks.ContainsKey(from._alias))
            {
                killStreaks[from._alias].lastUsedWeap = usedWep;
                killStreaks[from._alias].lastUsedWepTick = DateTime.Now.AddTicks(aliveTime).Ticks;
            }
        }

        /// <summary>
        /// Updates the last weapon used and kill count then announcing it to the public
        /// </summary>
        private void UpdateWeaponKill(Player from)
        {
            if (killStreaks.ContainsKey(from._alias))
            {
                if (killStreaks[from._alias].lastUsedWeap == null)
                    return;

                killStreaks[from._alias].lastUsedWepKillCount++;
                ItemInfo.Projectile lastUsedWep = killStreaks[from._alias].lastUsedWeap;
                switch (killStreaks[from._alias].lastUsedWepKillCount)
                {
                    case 2:
                        _arena.sendArenaMessage(string.Format("{0} just got a double {1} kill.", from._alias, lastUsedWep.name), 17);
                        break;
                    case 3:
                        _arena.sendArenaMessage(string.Format("{0} just got a triple {1} kill!", from._alias, lastUsedWep.name), 18);
                        break;
                    case 4:
                        _arena.sendArenaMessage(string.Format("A 4 {0} kill by {0}?!?", lastUsedWep.name, from._alias), 19);
                        break;
                    case 5:
                        _arena.sendArenaMessage(string.Format("Unbelievable! {0} with the 5 {1} kill?", from._alias, lastUsedWep.name), 20);
                        break;
                }
            }
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void gameVictory(Team victors)
        {

        }

        public void rewards()
        {
            foreach (Team rewardees in _arena.ActiveTeams)
            {
                int cashReward = 0;
                int expReward = 0;
                int pointReward = 0;

                foreach (Arena.FlagState fs in _arena._flags.Values)
                {
                    if (fs.team != rewardees)
                        continue;

                    LioInfo.Flag.FlagSettings flagSettings = fs.flag.FlagData;
                    //Periodic reward?
                    if (flagSettings.PeriodicCashReward == 0 && flagSettings.PeriodicExperienceReward == 0 && flagSettings.PeriodicPointsReward == 00)
                        continue;

                    //Cash
                    if (flagSettings.PeriodicCashReward < 0)
                        cashReward += (Math.Abs(flagSettings.PeriodicCashReward) * _arena.PlayersIngame.Count()) / 1000;
                    else
                        cashReward += Math.Abs(flagSettings.PeriodicCashReward);
                    //Experience
                    if (flagSettings.PeriodicExperienceReward < 0)
                        expReward += (Math.Abs(flagSettings.PeriodicExperienceReward) * _arena.PlayersIngame.Count()) / 1000;
                    else
                        expReward += Math.Abs(flagSettings.PeriodicExperienceReward);
                    //Points
                    if (flagSettings.PeriodicPointsReward < 0)
                        pointReward += (Math.Abs(flagSettings.PeriodicPointsReward) * _arena.PlayersIngame.Count()) / 1000;
                    else
                        pointReward += Math.Abs(flagSettings.PeriodicPointsReward);

                }

                if (cashReward == 0 && expReward == 0 && pointReward == 0)
                    continue;

                //Format the message
                string format = String.Format
                    ("&Reward (Cash={0} Experience={1} Points={2}) Flags have been reset, Next reward in {3} minutes.",
                    cashReward, expReward, pointReward, 10);

                //Send it.
                rewardees.sendArenaMessage(format, _arena._server._zoneConfig.flag.periodicBong);

                //Reward each player on the rewardees team
                foreach (Player player in rewardees.ActivePlayers)
                {
                    player.Cash += cashReward;
                    player.Experience += expReward;
                    player.syncState();
                }

                _arena.setTicker(0, 0, 60 * 1000, "Flags have been reset, Next flag reward in : ");

                _arena.flagReset();
                _arena.flagSpawn();
                _lastFlagReward = Environment.TickCount;
            }
        }

        static public void calculatePlayerKillRewards(Player victim, Player killer)
        {
            CfgInfo cfg = victim._server._zoneConfig;

            int killerBounty = 0;
            int killerBountyIncrease = 0;
            int victimBounty = 0;
            int killerCash = 0;
            int killerExp = 0;
            int killerPoints = 0;

            //Fake it to make it
            CS_VehicleDeath update = new CS_VehicleDeath(0, new byte[0], 0, 0);
            update.killedID = victim._id;
            update.killerPlayerID = killer._id;
            update.positionX = victim._state.positionX;
            update.positionY = victim._state.positionY;
            update.type = Helpers.KillType.Player;

            if (killer._team != victim._team)
            {
                killerBounty = Convert.ToInt32(((double)killer.Bounty / 100) * c_percentOfOwn);
                killerBountyIncrease = Convert.ToInt32(((double)killer.Bounty / 100) * c_percentOfOwnIncrease);
                victimBounty = Convert.ToInt32(((double)victim.Bounty / 100) * c_percentOfVictim);

                killerPoints = Convert.ToInt32((c_baseReward + killerBounty + victimBounty) * c_pointMultiplier);
                killerCash = Convert.ToInt32((c_baseReward + killerBounty + victimBounty) * c_cashMultiplier);
                killerExp = Convert.ToInt32((c_baseReward + killerBounty + victimBounty) * c_expMultiplier);

            }
            else
            {
                foreach (Player p in victim._arena.Players)
                    Helpers.Player_RouteKill(p, update, victim, 0, 0, 0, 0);
                return;
            }


            //Inform the killer
            Helpers.Player_RouteKill(killer, update, victim, killerCash, killerPoints, killerPoints, killerExp);

            //Update some statistics
            killerCash = addCash(killer, killerCash);
            killer.Experience += killerExp;
            killer.KillPoints += killerPoints;
            victim.DeathPoints += killerPoints;

            //Update his bounty
            killer.Bounty += (killerBountyIncrease + victimBounty);

            //Check for players in the share radius
            List<Player> sharedCash = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.cash.shareRadius).ToList();
            List<Player> sharedExp = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.experience.shareRadius).ToList();
            List<Player> sharedPoints = victim._arena.getPlayersInRange(update.positionX, update.positionY, cfg.point.shareRadius).ToList();
            Dictionary<int, int> cashRewards = new Dictionary<int, int>();
            Dictionary<int, int> expRewards = new Dictionary<int, int>();
            Dictionary<int, int> pointRewards = new Dictionary<int, int>();
            //Set up our shared math
            int CashShare = (int)((((float)killerCash) / 1000) * cfg.cash.sharePercent);
            int ExpShare = (int)((((float)killerExp) / 1000) * cfg.experience.sharePercent);
            int PointsShare = (int)((((float)killerPoints) / 1000) * cfg.point.sharePercent);
            int BtyShare = (int)((killerPoints * (((float)cfg.bounty.percentToAssistBounty) / 1000)));

            foreach (Player p in sharedCash)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                cashRewards[p._id] = CashShare;
                expRewards[p._id] = 0;
                pointRewards[p._id] = 0;
            }

            foreach (Player p in sharedExp)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                expRewards[p._id] = ExpShare;
                if (!cashRewards.ContainsKey(p._id))
                    cashRewards[p._id] = 0;
                if (!pointRewards.ContainsKey(p._id))
                    pointRewards[p._id] = 0;
            }

            foreach (Player p in sharedPoints)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                pointRewards[p._id] = PointsShare;
                if (!cashRewards.ContainsKey(p._id))
                    cashRewards[p._id] = 0;
                if (!expRewards.ContainsKey(p._id))
                    expRewards[p._id] = 0;

                //Share bounty within the experience radius, Dunno if there is a sharebounty radius?
                p.Bounty += BtyShare;
            }

            //Sent reward notices to our lucky witnesses
            List<int> sentTo = new List<int>();
            foreach (Player p in sharedCash)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                cashRewards[p._id] = addCash(p, cashRewards[p._id]);
                p.Experience += expRewards[p._id];
                p.AssistPoints += pointRewards[p._id];
                Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);
                sentTo.Add(p._id);
            }

            foreach (Player p in sharedExp)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                if (!sentTo.Contains(p._id))
                {
                    cashRewards[p._id] = addCash(p, cashRewards[p._id]);
                    p.Experience += expRewards[p._id];
                    p.AssistPoints += pointRewards[p._id];

                    Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);

                    sentTo.Add(p._id);
                }
            }

            foreach (Player p in sharedPoints)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                if (!sentTo.Contains(p._id))
                {   //Update the assist bounty
                    p.Bounty += BtyShare;

                    cashRewards[p._id] = addCash(p, cashRewards[p._id]);
                    p.Experience += expRewards[p._id];
                    p.AssistPoints += pointRewards[p._id];

                    Helpers.Player_RouteKill(p, update, victim, cashRewards[p._id], killerPoints, pointRewards[p._id], expRewards[p._id]);

                    sentTo.Add(p._id);
                }
            }

            //Shared kills anyone?
            Vehicle sharedveh = killer._occupiedVehicle;
            //are we in a vehicle?
            if (sharedveh != null)
            {
                //Was this a child vehicle? If so, re-route us to the parent
                if (sharedveh._parent != null)
                    sharedveh = sharedveh._parent;

                //Can we even share kills?
                if (sharedveh._type.SiblingKillsShared > 0)
                {   //Yep!
                    //Does this vehicle have any childs?
                    if (sharedveh._childs.Count > 0)
                    {
                        //Cycle through each child and reward them
                        foreach (Vehicle child in sharedveh._childs)
                        {
                            //Anyone home?
                            if (child._inhabitant == null)
                                continue;

                            //Can we share?
                            if (child._type.SiblingKillsShared == 0)
                                continue;

                            //Skip our killer
                            if (child._inhabitant == killer)
                                continue;

                            //Give them a kill!
                            child._inhabitant.Kills++;

                            //Show the message
                            child._inhabitant.triggerMessage(2, 500,
                                String.Format("Sibling Assist: Kills=1 (Points={0} Exp={1} Cash={2})",
                                CashShare, ExpShare, PointsShare));
                        }
                    }
                }
            }

            //Route the kill to the rest of the arena
            foreach (Player p in victim._arena.Players.ToList())
            {   //As long as we haven't already declared it, send
                if (p == null)
                    continue;

                if (p == killer)
                    continue;

                if (sentTo.Contains(p._id))
                    continue;

                Helpers.Player_RouteKill(p, update, victim, 0, killerPoints, 0, 0);
            }
        }

        static public int addCash(Player player, int quantity)
        {
            //Any rare cash item?
            double multiplier = 1.0;
            int cash = 0;

            if (player.getInventoryAmount(2019) > 0)
                multiplier += 0.10;

            cash = Convert.ToInt32(quantity * multiplier);

            player.Cash += cash;
            player.syncState();

            return cash;
        }

        public bool isHappyHour(TimeSpan start, TimeSpan end)
        {
            // convert datetime to a TimeSpan
            TimeSpan now = DateTime.Now.TimeOfDay;
            // see if start comes before end
            if (start < end)
                return start <= now && now <= end;
            // start is after end, so do the inverse comparison
            return !(end < now && now < start);
        }

        public TimeSpan timeTo(TimeSpan start)
        {
            TimeSpan remaining;

            DateTime nime = DateTime.Now;
            remaining = start - nime.TimeOfDay;

            if (remaining.TotalHours < 0)
            {
                remaining += TimeSpan.FromHours(24);
                remaining += TimeSpan.FromMinutes(1440);
            }

            return remaining;
        }

        public void setTime()
        {
            TimeSpan nowTime = DateTime.Now.TimeOfDay;
            if (_pvpHappyHourEnd > nowTime && nowTime < _pvpHappyHourStart)
            {
                DateTime nime = DateTime.Now;

                string cool = nime.TimeOfDay.Hours.ToString();

                int check = int.Parse(cool);

                _pvpHappyHourStart = TimeSpan.Parse(startTime[check]);
                _pvpHappyHourEnd = TimeSpan.Parse(endTime[check]);
            }
        }

        public void firstTime()
        {
            DateTime nowTime = DateTime.Now;

            string cool = nowTime.TimeOfDay.Hours.ToString();

            int check = int.Parse(cool);
            
            _pvpHappyHourStart = TimeSpan.Parse(startTime[check]);
            _pvpHappyHourEnd = TimeSpan.Parse(endTime[check]);
        }
        #endregion

        #region game start/end/reset
        /// <summary>
        /// Called when the game begins
        /// </summary>
        [Scripts.Event("Game.Start")]
        public bool gameStart()
        {   //We've started!
            _arena.flagReset();
            _arena.flagSpawn();
            _tickGameStart = Environment.TickCount;
            _tickKothGameStart = Environment.TickCount;
            _tickGameStarting = 0;
            _lastKOTHGameCheck = 0;
            _tickKOTHGameStarting = 0;
            _tickLastMinorPoll = Environment.TickCount;
            _tickGameLastTickerUpdate = Environment.TickCount;
            _tickLastEngineer = Environment.TickCount;
            _tickLastCaptain = Environment.TickCount;
            _tickLastRoamingCaptain = Environment.TickCount;
            _tickLastChief = Environment.TickCount;
            _arena.setTicker(0, 0, 60 * 1000, "Next flag reward in : ");
            _lastFlagReward = Environment.TickCount;
            _eol.gameStart();
            _lastPvpHappyHourAlert = 0;
            ResetKiller(null);
            killStreaks.Clear();
            _tickEolGameStart = Environment.TickCount;
            firstTime();
            _bPvpHappyHour = false;
            foreach (Player p in _arena.Players)
            {
                PlayerStreak temp = new PlayerStreak();
                temp.lastKillerCount = 0;
                temp.lastUsedWeap = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                killStreaks.Add(p._alias, temp);
            }
            //Let everyone know
            _arena.sendArenaMessage("A new game has started!", _config.flag.victoryWarningBong);
            //Start keeping track of healing
            _healingDone = new Dictionary<Player, int>();
            _bbetweengames = false;

            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {   //Game finished, perhaps start a new one
            _arena._bGameRunning = false;
            _kothGameRunning = false;
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickKothGameStart = 0;
            _lastKOTHGameCheck = 0;
            _tickKOTHGameStarting = 0;
            _maxRoamCaptains = 0;
            _maxEngineers = 0;
            _tickLastMinorPoll = 0;
            _tickLastEngineer = 0;
            _tickLastCaptain = 0;
            _tickLastRoamingCaptain = 0;
            _tickLastChief = 0;
            _tickGameLastTickerUpdate = 0;
            _lastFlagReward = 0;
            _currentRoamCaptains = 0;
            _currentRoamChief = 0;
            _lastPvpHappyHourAlert = 0;
            _healingDone = null;
            _eol.boundarygameEnd();
            _tickEolGameStart = 0;
            _bbetweengames = true;
            foreach (Vehicle v in _arena.Vehicles)
                if (v._type.Type == VehInfo.Types.Computer)
                    //Destroy it!
                    v.destroy(true);
            _usedpylons.Clear();
            _bbetweengames = true;
            foreach (Bot bot in _bots)
                _condemnedBots.Add(bot);

            foreach (Bot bot in _condemnedBots)
                bot.destroy(true);

            captainBots.Clear();
            capRoamBots.Clear();
            engineerBots.Clear();
            roamingCaptianBots.Clear();
            alienChiefBots.Clear();
            roamingAlienBots.Clear();

            _activehPoints.Clear();

            _minX = 0;
            _maxX = 0;
            _minY = 0;
            _maxY = 0;
            return true;
        }

        /// <summary>
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {   //Game reset, perhaps start a new one
            _arena.flagReset();
            _arena.flagSpawn();
            _arena._bGameRunning = false;
            _kothGameRunning = false;
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _tickKothGameStart = 0;
            _lastKOTHGameCheck = 0;
            _tickKOTHGameStarting = 0;
            _tickLastMinorPoll = 0;
            _maxRoamCaptains = 0;
            _maxEngineers = 0;
            _tickLastEngineer = 0;
            _tickLastCaptain = 0;
            _tickLastRoamingCaptain = 0;
            _tickLastChief = 0;
            _tickGameLastTickerUpdate = 0;
            _lastFlagReward = 0;
            _healingDone = null;
            _eol.boundarygameReset();
            _tickEolGameStart = 0;
            _lastPvpHappyHourAlert = 0;
            _bbetweengames = true;
            _bPvpHappyHour = false;
            firstTime();
            foreach (Vehicle v in _arena.Vehicles)
                if (v._type.Type == VehInfo.Types.Computer)
                    //Destroy it!
                    v.destroy(true);
            _usedpylons.Clear();
            _bbetweengames = true;
            foreach (Bot bot in _bots)
                _condemnedBots.Add(bot);

            foreach (Bot bot in _condemnedBots)
                bot.destroy(true);

            captainBots.Clear();
            capRoamBots.Clear();
            engineerBots.Clear();
            roamingCaptianBots.Clear();
            alienChiefBots.Clear();
            roamingAlienBots.Clear();
            _currentRoamCaptains = 0;
            _currentRoamChief = 0;

            _activehPoints.Clear();

            _minX = 0;
            _maxX = 0;
            _minY = 0;
            _maxY = 0;
            return true;
        }

        #endregion

        #region Player Events
        /// <summary>
        /// Calculates and rewards a players for a bot kill
        /// </summary>
        static public void calculateBotKillRewards(Bots.Bot victim, Player killer)
        {
            CfgInfo cfg = killer._server._zoneConfig;
            int killerCash = 0;
            int killerExp = 0;
            int killerPoints = 0;
            int killerBounty = 0;
            int victimBounty = 0;
            int killerBountyIncrease = 0;

            BotSettings settings = victim.Settings();
            if (settings != null)
            {
                killerBounty = Convert.ToInt32(((double)killer.Bounty / 100) * c_percentOfOwn);
                killerPoints = Convert.ToInt32((settings.Points) + (killerBounty * c_pointMultiplier));
                killerCash = Convert.ToInt32((settings.Cash) + (killerBounty * c_cashMultiplier));
                killerExp = Convert.ToInt32((settings.Experience) + (killerBounty * c_expMultiplier));
                victimBounty = settings.Bounty;
                killerBountyIncrease = 0;
            }
            else
            {
                killerBounty = Convert.ToInt32(((double)killer.Bounty / 100) * c_percentOfOwn);
                killerPoints = Convert.ToInt32(((int)cfg.bot.pointsKillReward) + (killerBounty * c_pointMultiplier));
                killerCash = Convert.ToInt32(((int)cfg.bot.cashKillReward) + (killerBounty * c_cashMultiplier));
                killerExp = Convert.ToInt32(((int)cfg.bot.expKillReward) + (killerBounty * c_expMultiplier));
                victimBounty = (int)cfg.bot.fixedBountyToKiller;
                killerBountyIncrease = 0;
            }

            //Update his stats
            killerCash = addCash(killer, killerCash);
            killer.Experience += killerExp;
            killer.KillPoints += killerPoints;
            //killer.Bounty += killerBounty;
            killer.Bounty += (killerBountyIncrease + victimBounty);

            //Inform the killer..
            killer.triggerMessage(1, 500,
                String.Format("{0} killed by {1} (Cash={2} Exp={3} Points={4})",
                victim._type.Name, killer._alias,
                killerCash, killerExp, killerPoints));

            //Sync his state
            killer.syncState();

            //Check for players in the share radius
            List<Player> sharedRewards = victim._arena.getPlayersInRange(victim._state.positionX, victim._state.positionY, cfg.bot.shareRadius);
            Dictionary<int, int> cashRewards = new Dictionary<int, int>();
            Dictionary<int, int> expRewards = new Dictionary<int, int>();
            Dictionary<int, int> pointRewards = new Dictionary<int, int>();

            foreach (Player p in sharedRewards)
            {
                if (p == killer || p._team != killer._team)
                    continue;

                cashRewards[p._id] = (int)((((float)killerCash) / 1000) * cfg.bot.sharePercent);
                expRewards[p._id] = (int)((((float)killerExp) / 1000) * cfg.bot.sharePercent);
                pointRewards[p._id] = (int)((((float)killerPoints) / 1000) * cfg.bot.sharePercent);
            }
        }

        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        [Scripts.Event("Bot.Death")]
        public bool botDeath(Bot dead, Player killer, int weaponID)
        {

            if (killer != null)
            {
                Helpers.Vehicle_RouteDeath(_arena.Players, killer, dead, null);
                if (killer != null && dead._team != killer._team)
                {//Don't allow rewards for team kills
                    calculateBotKillRewards(dead, killer);
                }

                killer.Kills++;

            }
            else
                Helpers.Vehicle_RouteDeath(_arena.Players, null, dead, null);

            //Check for players in the share radius
            List<Player> playersInRadius = _arena.getPlayersInRange(dead._state.positionX, dead._state.positionY, 600, false);

            //Killer is always added...
            if (!playersInRadius.Contains(killer))
                playersInRadius.Add(killer);

            return false;
        }

        /// <summary>
        /// Called when a teams points have been modified
        /// </summary>
        public void onPointModify(Team team, int points)
        {
            //Update the tickers
            updateTickers();

            //Check for game victory here
            if (points >= _points.MaxPoints)
                //They were the first team to reach max points!
                gameVictory(team);
        }

        /// <summary>
        /// Called when a player sends a chat command
        /// </summary>
        [Scripts.Event("Player.ChatCommand")]
        public bool playerChatCommand(Player player, Player recipient, string command, string payload)
        {
            if (command.ToLower().Equals("crown"))
            {   //Give them their crown time if KOTH is enabled
                if (_minPlayers <= 0)
                    player.sendMessage(0, "&KOTH is not enabled in this zone");

                else
                    if (_playerCrownStatus.ContainsKey(player))
                    player.sendMessage(0, "&Crown kills: " + _playerCrownStatus[player].crownKills);
            }

            if (command.ToLower().Equals("co"))
            {
                player.sendMessage(0, "X: " + player._state.positionX + " Y: " + player._state.positionY);
            }

            if (command.ToLower().Equals("hq"))
            {   //Give them some information on their HQ
                if (_hqs[player._team] == null)
                {
                    player.sendMessage(0, "&Headquarters - Your team has no headquarters");
                }
                else
                {
                    player.sendMessage(0, String.Format("&Headquarters - Level={0} Bounty={1}",
                        _hqs[player._team].Level,
                        _hqs[player._team].Bounty));
                }
            }

            if (command.ToLower().Equals("hqlist"))
            {   //Give them some information on all HQs present in the arena
                IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);
                if (hqs.Count().Equals(0))
                {
                    player.sendMessage(0, "&Headquarters - There are no headquarters present in the arena");
                }
                else
                {
                    player.sendMessage(0, "&Headquarters - Information");
                    foreach (Vehicle hq in hqs)
                    {
                        if (_hqs[hq._team] == null)
                            //We're not tracking this HQ for some reason... hm...
                            continue;
                        player.sendMessage(0, String.Format("*Headquarters - Team={0} Level={1} Bounty={2} Location={3}",
                            hq._team._name,
                            _hqs[hq._team].Level,
                            _hqs[hq._team].Bounty,
                            Helpers.posToLetterCoord(hq._state.positionX, hq._state.positionY)));
                    }
                }
            }
            if (command.ToLower().Equals("arena"))
            {
                player.sendMessage(0, "&No Private Arena are can be used in Eol: Pioneer Station");
                return false;
            }

            return true;
        }
        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnter(Player player)
        {
            //Send them the crowns if KOTH is enabled
            if (_minPlayers > 0)
                if (_playerCrownStatus == null)
                    _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();

            if (!_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player] = new PlayerCrownStatus(false);
                Helpers.Player_Crowns(_arena, true, _activeCrowns, player);
            }

            //Add them to the list if its not in it
            if (!killStreaks.ContainsKey(player._alias))
            {
                PlayerStreak temp = new PlayerStreak();
                temp.lastKillerCount = 0;
                temp.lastUsedWeap = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                killStreaks.Add(player._alias, temp);
            }

            player.sendMessage(4,
                      "#This zone is currently in Alpha. While it's under development, " +
                      "please leave any bug finds or feedback on our discord, in the Eol Channel. Thanks!");

            /*if (_bHorde)
                player.sendMessage(0, "&A Pirate horde is currently roaming Eol, Enjoy!");
            else
            {
                TimeSpan remaining = timeTo(_HordeStart);
                player.sendMessage(0, String.Format("&A Pirate horde will roam in {0} hours & {1} minutes", remaining.Hours, remaining.Minutes));
            }*/
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {//Find out if KOTH is enabled
            if (_kothGameRunning)
            {
                if (_playerCrownStatus.ContainsKey(player))
                {
                    _playerCrownStatus[player].crown = false;
                    Helpers.Player_Crowns(_arena, false, _noCrowns);
                }
            }
            //Destroy all vehicles belonging to him
            foreach (Vehicle v in _arena.Vehicles)
                if (v._type.Type == VehInfo.Types.Computer && v._creator == player)
                    //Destroy it!
                    v.destroy(true);

        }

        /// <summary>
        /// Handles a player's portal request
        /// </summary>
        [Scripts.Event("Player.Portal")]
        public bool playerPortal(Player player, LioInfo.Portal portal)
        {
            List<Arena.FlagState> carried = _arena._flags.Values.Where(flag => flag.carrier == player).ToList();

            foreach (Arena.FlagState carry in carried)
            {   //If the terrain number is 0-15

                int terrainNum = player._arena.getTerrainID(player._state.positionX, player._state.positionY);
                if (terrainNum >= 0 && terrainNum <= 15)
                {   //Check the FlagDroppableTerrains for that specific terrain id

                    if (carry.flag.FlagData.FlagDroppableTerrains[terrainNum] == 0)
                        _arena.flagResetPlayer(player);
                }
            }
            return _eol.playerPortal(player, portal);

            return true;
        }

        /// <summary>
        /// Triggered when a vehicle is created
        /// </summary>
        [Scripts.Event("Vehicle.Creation")]
        public bool vehicleCreation(Vehicle created, Team team, Player creator)
        {
            //Are they trying to create a headquarters?
            if (created._type.Id == _hqVehId)
            {
                if (_hqs[team] == null)
                {
                    _hqs.Create(team);
                    team.sendArenaMessage("&Headquarters - Your team has created a headquarters at " + Helpers.posToLetterCoord(created._state.positionX, created._state.positionY));
                }
                else
                {
                    if (creator != null)
                        creator.sendMessage(-1, "Your team already has a headquarters");
                    created.destroy(false, true);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Triggered when a vehicle dies
        /// </summary>
        [Scripts.Event("Vehicle.Death")]
        public bool vehicleDeath(Vehicle dead, Player killer)
        {

            //Did they just kill an HQ?!
            if (dead._type.Id == _hqVehId)
            {
                Team killers = killer._team;


                //Check if it was a team kill
                if (dead._team == killer._team)
                {   //Cheaters! Reward the last people to hurt the vehicle if it exists
                    IEnumerable<Player> attackers = dead._attackers;
                    attackers.Reverse();
                    foreach (Player p in attackers)
                        if (p._team != dead._team)
                            killers = p._team;

                    //Did we find a suitable killer?
                    if (killers == dead._team)
                    {   //Nope! Looks like nobody has ever hit their HQ... do nothing I guess.
                        _arena.sendArenaMessage("&Headquarters - " + killers._name + " killed their own HQ worth " + _hqs[dead._team].Bounty + " bounty... scum.");
                        _hqs.Destroy(dead._team);
                        return true;
                    }
                }

                if (dead._team != killer._team)
                {
                    foreach (Player p in killers.ActivePlayers)
                    {   //Calculate some rewards
                        int points = (int)(_basePointReward * 1.5 * _hqs[dead._team].Level) * 15;
                        int cash = (int)(_baseCashReward * 1.5 * _hqs[dead._team].Level) * 15;
                        int experience = (int)(_baseXPReward * 1.5 * _hqs[dead._team].Level) * 15;
                        p.BonusPoints += points;
                        p.Cash += cash;
                        p.Experience += experience;
                        p.sendMessage(0, "&Headquarters - Your team has destroyed " + dead._team._name + " HQ (" + _hqs[dead._team].Bounty + " bounty)! Cash=" + cash + " Experience=" + experience + " Points=" + points);
                    }
                }

                //Notify the rest of the arena
                foreach (Team t in _arena.Teams.Where(team => team != killers))
                    t.sendArenaMessage("&Headquarters - " + dead._team._name + " HQ worth " + _hqs[dead._team].Bounty + " bounty has been destroyed by " + killers._name + "!");

                //Stop tracking this HQ
                _hqs.Destroy(dead._team);

                if (dead._team == botTeam1 || dead._team == botTeam2 || dead._team == botTeam3)
                {
                    _currentEngineers--;
                    engineerBots.Remove(dead._team);
                }
            }
            return true;
        }

        /// <summary>
        /// Triggered when a player has died, by any means
        /// </summary>
        /// <remarks>killer may be null if it wasn't a player kill</remarks>
        [Scripts.Event("Player.Death")]
        public bool playerDeath(Player victim, Player killer, Helpers.KillType killType, CS_VehicleDeath update)
        {
            if (killer == null)
                return true;

            //Was it a player kill?
            if (killType == Helpers.KillType.Player)
            {   //No team killing!
                if (victim._team != killer._team)
                    //Does the killer have an HQ?
                    if (_hqs[killer._team] != null)
                        //Reward his HQ! (Victims bounty + half of own)
                        _hqs[killer._team].Bounty += victim.Bounty + (killer.Bounty / 2);
                if (_points != null)
                    _points[killer._team] += _pointSmallChange;
                //Reward the killers team!


                //Find out if KOTH is running
                if (_activeCrowns.Count == 0 || killer == null)
                    return true;

                //Handle crowns
                if (_playerCrownStatus[victim].crown)
                {   //Incr crownDeaths
                    _playerCrownStatus[victim].crownDeaths++;

                    if (_playerCrownStatus[victim].crownDeaths >= _config.king.deathCount)
                    {
                        //Take it away now
                        _playerCrownStatus[victim].crown = false;
                        _noCrowns.Remove(victim);
                        Helpers.Player_Crowns(_arena, false, _noCrowns);
                    }

                    if (!_playerCrownStatus[killer].crown)
                        _playerCrownStatus[killer].crownKills++;
                }

                //Reset their timer
                if (_playerCrownStatus[killer].crown)
                    updateCrownTime(killer);
                else if (_config.king.crownRecoverKills != 0)
                {   //Should they get a crown?
                    if (_playerCrownStatus[killer].crownKills >= _config.king.crownRecoverKills)
                    {
                        _playerCrownStatus[killer].crown = true;
                        giveCrown(killer);
                    }
                }
            }

            //Was it a computer kill?
            if (killType == Helpers.KillType.Computer)
            {
                //Let's find the vehicle!
                Computer cvehicle = victim._arena.Vehicles.FirstOrDefault(v => v._id == update.killerPlayerID) as Computer;
                Player vehKiller = cvehicle._creator;
                //Do they exist?
                if (cvehicle != null && vehKiller != null)
                {   //We'll take it from here...
                    update.type = Helpers.KillType.Player;
                    update.killerPlayerID = vehKiller._id;

                    //Don't reward for teamkills
                    if (vehKiller._team == victim._team)
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedTeam);
                    else
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedEnemy);

                    //Increase stats/HQ bounty and notify arena of the kill!
                    if (_hqs[vehKiller._team] != null)
                        //Reward his HQ! (Victims bounty + half of own)
                        _hqs[vehKiller._team].Bounty += victim.Bounty + (vehKiller.Bounty / 2);

                    vehKiller.Kills++;
                    victim.Deaths++;
                    Logic_Rewards.calculatePlayerKillRewards(victim, vehKiller, update);
                    return false;
                }
            }

            //Was it a player kill?
            if (killType == Helpers.KillType.Player)
            {   //No team killing!
                if (victim._team != killer._team)
                    //Reward the killers team!
                    if (_points != null)
                        _points[killer._team] += _pointSmallChange;
            }

            //Was it a computer kill?
            if (killType == Helpers.KillType.Computer)
            {
                //Let's find the vehicle!
                Computer cvehicle = victim._arena.Vehicles.FirstOrDefault(v => v._id == update.killerPlayerID) as Computer;
                Player vehKiller = cvehicle._creator;
                //Does it exist?
                if (cvehicle != null && vehKiller != null)
                {
                    //We'll take it from here...
                    update.type = Helpers.KillType.Player;
                    update.killerPlayerID = vehKiller._id;

                    //Don't reward for teamkills
                    if (vehKiller._team == victim._team)
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedTeam);
                    else
                        Logic_Assets.RunEvent(vehKiller, _arena._server._zoneConfig.EventInfo.killedEnemy);

                    //Increase stats/points and notify arena of the kill!
                    if (_points != null)
                        _points[vehKiller._team] += _pointSmallChange;
                    vehKiller.Kills++;
                    victim.Deaths++;
                    Logic_Rewards.calculatePlayerKillRewards(victim, vehKiller, update);
                    return false;
                }
            }
            //Update our kill counter
            UpdateDeath(victim, killer);
            return true;

        }
        /// <summary>
        /// Triggered when one player has killed another
        /// </summary>
        [Scripts.Event("Player.PlayerKill")]
        public bool playerPlayerKill(Player victim, Player killer)
        {

            if (killStreaks.ContainsKey(victim._alias))
            {
                long wepTick = killStreaks[victim._alias].lastUsedWepTick;
                if (wepTick != -1)
                    UpdateWeaponKill(killer);
            }
            if (killer != null && victim != null && victim._bounty >= 300)
                _arena.sendArenaMessage(String.Format("{0} has ended {1}'s bounty.", killer._alias, victim._alias), 5);

            //Don't reward for teamkills
            if (victim._team == killer._team)
                Logic_Assets.RunEvent(victim, _arena._server._zoneConfig.EventInfo.killedTeam);
            else
            {
                Logic_Assets.RunEvent(victim, _arena._server._zoneConfig.EventInfo.killedEnemy);
                //Calculate rewards
                calculatePlayerKillRewards(victim, killer);
            }

            //Update stats
            killer.Kills++;
            victim.Deaths++;
            //killer.ZoneStat1 = killer.Bounty;

            //Update our kill streak
            UpdateKiller(killer);
            UpdateDeath(victim, killer);

            //Check if a game is running
            if (_kothGameRunning && _activeCrowns.Count == 0)
                return true;

            if (_kothGameRunning)
            {
                if (_playerCrownStatus[victim].crown)
                {   //Incr crownDeaths
                    _playerCrownStatus[victim].crownDeaths++;

                    if (_playerCrownStatus[victim].crownDeaths >= _config.king.deathCount)
                    {
                        //Take it away now
                        _playerCrownStatus[victim].crown = false;
                        _noCrowns.Remove(victim);
                        Helpers.Player_Crowns(_arena, false, _noCrowns);
                    }

                    if (!_playerCrownStatus[killer].crown)
                        _playerCrownStatus[killer].crownKills++;
                }


                //Reset their timer
                if (_playerCrownStatus[killer].crown)
                    updateCrownTime(killer);
                else if (_config.king.crownRecoverKills != 0)
                {   //Should they get a crown?
                    if (_playerCrownStatus[killer].crownKills >= _config.king.crownRecoverKills)
                    {
                        _playerCrownStatus[killer].crown = true;
                        giveCrown(killer);
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool playerBreakdown(Player from, bool bCurrent)
        {   //Allows additional "custom" breakdown information
            //List the best healers by sorting them according to healingdone
            from.sendMessage(0, "#Healer Breakdown");
            if (_healingDone != null && _healingDone.Count > 0)
            {
                List<KeyValuePair<Player, int>> healers = _healingDone.ToList();
                healers.Sort((a, b) => { return a.Value.CompareTo(b.Value); });
                healers.Reverse();

                int i = 1;
                foreach (KeyValuePair<Player, int> healer in healers)
                {   //Display top 3 healers in arena
                    from.sendMessage(0, String.Format("!{0} (Healed={1}): {2}",
                        ScriptHelpers.toOrdinal(i), healer.Value, healer.Key._alias));
                    if (i++ > 3)
                        break;
                }
            }

            //List teams by most points
            from.sendMessage(0, "#Team Breakdown");
            if (_points != null)
            {
                List<Team> teams = _arena.Teams.Where(t => _points[t] != 0).ToList();
                teams.OrderByDescending(t => _points[t]);

                int j = 1;
                foreach (Team t in teams)
                {
                    from.sendMessage(0, String.Format("!{0} (Points={1} Kills={2} Deaths={3}): {4}",
                        ScriptHelpers.toOrdinal(j), _points[t], t._currentGameKills, t._currentGameDeaths, t._name));
                    j++;
                }
            }

            from.sendMessage(0, "#Player Breakdown");
            int k = 1;
            foreach (Player p in _arena.PlayersIngame.OrderByDescending(player => (bCurrent ? player.StatsCurrentGame.kills : player.StatsLastGame.kills)))
            {   //Display top 3 players
                from.sendMessage(0, String.Format("!{0} (K={1} D={2}): {3}",
                    ScriptHelpers.toOrdinal(k),
                    (bCurrent ? p.StatsCurrentGame.kills : p.StatsLastGame.kills),
                    (bCurrent ? p.StatsCurrentGame.deaths : p.StatsLastGame.deaths),
                    p._alias));
                if (k++ > 3)
                    break;
            }
            //Display his score
            from.sendMessage(0, String.Format("@Personal Score: (K={0} D={1})",
                (bCurrent ? from.StatsCurrentGame.kills : from.StatsLastGame.kills),
                (bCurrent ? from.StatsCurrentGame.deaths : from.StatsLastGame.deaths)));

            //return true to avoid another breakdown from showing
            return true;
        }


        /// <summary>
        /// Triggered when a player requests to pick up an item
        /// </summary>
        [Scripts.Event("Player.ItemPickup")]
        public bool playerItemPickup(Player player, Arena.ItemDrop drop, ushort quantity)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player requests to drop an item
        /// </summary>
        [Scripts.Event("Player.ItemDrop")]
        public bool playerItemDrop(Player player, ItemInfo item, ushort quantity)
        {
            return true;
        }

        /// <summary>
        /// Handles a player's produce request
        /// </summary>
        [Scripts.Event("Player.Produce")]
        public bool playerProduce(Player player, Computer computer, VehInfo.Computer.ComputerProduct product)
        {
            return true;
        }

        /// <summary>
        /// Handles a player's switch request
        /// </summary>
        [Scripts.Event("Player.Switch")]
        public bool playerSwitch(Player player, LioInfo.Switch swi)
        {
            return true;
        }

        /// <summary>
        /// Handles a player's flag request
        /// </summary>
        [Scripts.Event("Player.FlagAction")]
        public bool playerFlagAction(Player player, bool bPickup, bool bInPlace, LioInfo.Flag flag)
        {
            return true;
        }

        /// <summary>
        /// Handles the spawn of a player
        /// </summary>
        [Scripts.Event("Player.Spawn")]
        public bool playerSpawn(Player player, bool bDeath)
        {
            return true;
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnterArena(Player player)
        {
            //Add them to the list if its not in it
            if (!killStreaks.ContainsKey(player._alias))
            {
                PlayerStreak temp = new PlayerStreak();
                temp.lastKillerCount = 0;
                temp.lastUsedWeap = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                killStreaks.Add(player._alias, temp);
            }

            //Send them the crowns..
            if (!_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player] = new PlayerCrownStatus(false);
                Helpers.Player_Crowns(_arena, true, _activeCrowns, player);
            }

        }
        /// <summary>
        /// Triggered when a player wants to unspec and join the game
        /// </summary>
        [Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
            //Add them to the list if its not in it
            if (!killStreaks.ContainsKey(player._alias))
            {
                PlayerStreak temp = new PlayerStreak();
                temp.lastKillerCount = 0;
                temp.lastUsedWeap = null;
                temp.lastUsedWepKillCount = 0;
                temp.lastUsedWepTick = -1;
                killStreaks.Add(player._alias, temp);
            }

            //Find if player has more than 2 skills and remove them, giving back money spent and defaulting to conscript
            List<Player.SkillItem> removes = player._skills.Values.Where(skl => skl.skill.SkillId >= 0).ToList();
            if (removes.Count >= 2)
            {
                foreach (Player.SkillItem skl in removes)
                {
                    player._skills.Remove(skl.skill.SkillId);
                }
                player.Cash += 15000;
                player.sendMessage(0, String.Format("Reset skill to Conscript and refunded $15000 due to having multiple classes"));
                SkillInfo skill = _arena._server._assets.getSkillByName("Conscript");
                if (skill != null)
                    player.skillModify(skill, 1);
                player.syncState();
            }

            if (_kothGameRunning)
            {
                if (!_startTeams.ContainsKey(player))
                {
                    _startTeams[player] = null;
                }
            }

            return true;
        }

        /// <summary>
        /// Triggered when a player wants to spec and leave the game
        /// </summary>
        [Scripts.Event("Player.LeaveGame")]
        public bool playerLeaveGame(Player player)
        {
            if (_playerCrownStatus.ContainsKey(player))
            {
                _playerCrownStatus[player].crown = false;
                Helpers.Player_Crowns(_arena, false, _noCrowns);
            }
            return true;
        }
        /// <summary>
        /// Triggered when a player wants to enter a vehicle
        /// </summary>
        [Scripts.Event("Player.EnterVehicle")]
        public bool playerEnterVehicle(Player player, Vehicle vehicle)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player wants to leave a vehicle
        /// </summary>
        [Scripts.Event("Player.LeaveVehicle")]
        public bool playerLeaveVehicle(Player player, Vehicle vehicle)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player notifies the server of an explosion
        /// </summary>
        [Scripts.Event("Player.Explosion")]
        public bool playerExplosion(Player player, ItemInfo.Projectile weapon, short posX, short posY, short posZ)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a computer vehicle has killed a player
        /// </summary>
        [Scripts.Event("Player.ComputerKill")]
        public bool playerComputerKill(Player victim, Computer computer)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player attempts to use a warp item
        /// </summary>
        [Scripts.Event("Player.WarpItem")]
        public bool playerWarpItem(Player player, ItemInfo.WarpItem item, ushort targetPlayerID, short posX, short posY)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player attempts to use a warp item
        /// </summary>
        [Scripts.Event("Player.MakeVehicle")]
        public bool playerMakeVehicle(Player player, ItemInfo.VehicleMaker item, short posX, short posY)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player attempts to use a warp item
        /// </summary>
        [Scripts.Event("Player.MakeItem")]
        public bool playerMakeItem(Player player, ItemInfo.ItemMaker item, short posX, short posY)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player uses a repair item
        /// </summary>
        [Scripts.Event("Player.Repair")]
        public bool playerRepair(Player player, ItemInfo.RepairItem item, UInt16 targetVehicle, short posX, short posY)
        {
            int healamount = 0;
            //Let's try to credit him for the heal
            if (item.repairType == 0 || item.repairType == 2)
            {   //It's a player heal!
                if (item.repairDistance > 0)
                    //Credit him for a single heal
                    healamount = (item.repairAmount == 0) ? item.repairPercentage : item.repairAmount;
                else if (item.repairDistance < 0)
                    //Credit him for everybody he healed
                    healamount = (item.repairAmount == 0)
                        ? item.repairPercentage * _arena.getPlayersInRange(player._state.positionX, player._state.positionY, -item.repairDistance).Count
                        : item.repairAmount * _arena.getPlayersInRange(player._state.positionX, player._state.positionY, -item.repairDistance).Count;
            }

            //Keep track of it, mang
            if (_healingDone != null)
                if (_healingDone.ContainsKey(player))
                    _healingDone[player] += healamount;
                else
                    _healingDone.Add(player, healamount);
            return true;
        }

        /// <summary>
        /// Triggered when a player is buying an item from the shop
        /// </summary>
        [Scripts.Event("Shop.Buy")]
        public bool shopBuy(Player patron, ItemInfo item, int quantity)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player is selling an item to the shop
        /// </summary>
        [Scripts.Event("Shop.Sell")]
        public bool shopSell(Player patron, ItemInfo item, int quantity)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player requests a skill from skill screen (F11)
        /// </summary>
        [Scripts.Event("Shop.SkillRequest")]
        public bool shopSkillRequest(Player player, SkillInfo skill)
        {
            return true;
        }

        /// <summary>
        /// Triggered when a player successfully purchases a skill item from skill screen (F11)
        /// </summary>
        [Scripts.Event("Shop.SkillPurchase")]
        public void shopSkillPurchase(Player player, SkillInfo skill)
        {   //Is it a class or an attribute?
            if (skill.SkillId >= 0)
            {   //It's a class change, let's look for any computer vehicles he might have owned...
                foreach (Vehicle v in _arena.Vehicles)
                    if (v._type.Type == VehInfo.Types.Computer && v._creator == player)
                        //Destroy it!
                        v.destroy(true);
            }
        }

        /// <summary>
        /// Triggered when a bot has killed a player
        /// </summary>
        [Scripts.Event("Player.BotKill")]
        public bool playerBotKill(Player victim, Bot bot)
        {
            UpdateDeath(victim, null);
            //Update our base zone stats
            victim.Deaths++;

            return true;
        }

        public void pickTeam(Player player)
        {
            List<Team> potentialTeams = _arena.ActiveTeams.Where(t => t._name.StartsWith("Public") && t.ActivePlayerCount < 1).ToList();

            //Put him on a new Public Team
            if (potentialTeams.Count == 0)
            {
                Team newTeam = _arena.PublicTeams.First(t => t._name.StartsWith("Public") && t.ActivePlayerCount == 0);
                newTeam.addPlayer(player);
            }
            else
                potentialTeams.First().addPlayer(player);
        }

        #endregion
    }
}