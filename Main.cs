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

namespace InfServer.Script.GameType_Eol
{	// Script Class
    /// Provides the interface between the script and arena
    ///////////////////////////////////////////////////////
    public partial class Script_Eol : Scripts.IScript
    {	///////////////////////////////////////////////////
        // Member Variables
        ///////////////////////////////////////////////////
        private Arena _arena;					//Pointer to our arena class
        private CfgInfo _config;				//The zone config
        private Points _points;                 //Our points

        //Headquarters
        public Headquarters _hqs;               //Our headquarter tracker
        private int[] _hqlevels;                //Bounty required to level up HQs
        public int _hqVehId;                    //The vehicle ID of our HQs
        private int _baseXPReward;              //Base XP reward for HQs
        private int _baseCashReward;            //Base Cash reward for HQs
        private int _basePointReward;           //Base Point reward for HQs
        private int _rewardInterval;            //The interval at which we reward for HQs

        public EolBoundaries _eol;

        //Bots
        private int _lastBotCheck;

        private int _lastGameCheck;				//The tick at which we last checked for game viability
        private int _lastHQReward;              //The tick at which we last checked for HQ rewards

        //KOTH
        private Team _victoryKothTeam;			//The team currently winning!
        private int _tickGameLastTickerUpdate;  //The tick at which the ticker was last updated
        private int _tickGameStarting;			//The tick at which the game began starting (0 == not initiated)
        private int _tickGameStart;				//The tick at which the game started (0 == stopped)
        private int _minPlayers;                //The minimum amount of players needed for a KOTH game

        //CTF

        private int _lastFlagCheck;

        //Settings
        private int _pointSmallChange;                  //The small change to # of points (ex: kills, turret kills, etc)
        private int _pointPeriodicChange;               //The change to # of points based on periodic flag rewards
        

        //Scores
        private Dictionary<Player, int> _healingDone;   //Keep track of healing done by players

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
        private List<Player> _activeCrowns //List of people with a crown
        {
            get { return _playerCrownStatus.Where(p => p.Value.crown).Select(p => p.Key).ToList(); }
        }
        private List<Player> _noCrowns //List of people with no crowns
        {
            get { return _playerCrownStatus.Where(p => !p.Value.crown).Select(p => p.Key).ToList(); }
        }
        private List<Team> _crownTeams;

        

        //Bots
        //Perimeter defense Bots
        public const float c_defenseInitialAmountPP = 0.5f;		//The amount of defense bots per player initially spawned (minimum of 1)
        public const int c_defenseAddTimerGrowth = 8;			//The amount of seconds to add to the new bot timer for each person missing from the team
        public const int c_defenseAddTimer = 36;			    //The amount of seconds between allowing new defense bots
        public const int c_defenseRespawnTimeGrowth = 400;		//The amount of time to add to the respawn timer for each missing player
        public const int c_defenseRespawnTime = 600;		    //The amount of ms between spawning new zombies
        public const int c_defenseMinRespawnDist = 900;			//The minimum distance bot can be spawned from the players
        public const int c_defenseMaxRespawnDist = 1500;		//The maximum distance bot can be spawned from the players
        public const int c_defenseMaxPath = 350;				//The maximum path length before a bot will request a respawn
        public const int c_defensePathUpdateInterval = 1000;	//The amount of ticks before a bot will renew it's path
        public const int c_defenseDistanceLeeway = 500;			//The maximum distance leeway a bot can be from the team before it is respawned
        public const int _checkCaptain = 50000;                 //The tick at which we check for a captain
        public const int _checkEngineer = 70000;                //The tick at which we check for an engineer
        protected int _tickLastEngineer = 0;                    //Last time we checked for an engineer
        protected int _tickLastCaptain = 0;                     //Last time we checked for a captain
        protected int _lastHqCheck = 0;                         //Last time we check for bot hq's to build

        public const int c_CaptainPathUpdateInterval = 5000;	//The amount of ticks before an engineer's combat bot updates it's path

        public Dictionary<Team, int> botCount;
        public Dictionary<Team, int> captainBots;
        public List<Team> engineerBots;
        
        

        private class bothqsObject
        {
            short x;      //X coordinate of bot hq's
            short y;      //Y coordinate of bot hq's
            bool exists; //Tells us if the bot hq exists on the map

            public bothqsObject(short xPos, short yPos)
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
        private Dictionary<int, bothqsObject> _bothqs;

        public const int _maxEngineers = 3;                      //Maximum amount of engineer bots that will spawn in game
        public int _currentEngineers = 0;                        //Current amount of engineer bots playing in the game
        public int[] _lastbothqs;                                //Array of all bot hq's that are being used
        public const int _bothqVehID = 622;                      //The vehicle ID of our bot hq points

        //Bot teams
        Team botTeam1;
        Team botTeam2;
        Team botTeam3;

        Random _rand;

        public string botTeamName1 = "Bot Team - Titan Rebels";
        public string botTeamName2 = "Bot Team - Deeks Bandits";
        public string botTeamName3 = "Bot Team - NewJack Raiders";

        private List<CurrentSectors> _currentSectors;
        private class CurrentSectors
        {
            short xx;      //XX coordinate of sector
            short xy;      //XY coordinate of sector
            short yx;      //YX coordinate of sector
            short yy;      //YY coordinate of sector
            bool usingit; //Tells us if the sector is in use on the map

            public CurrentSectors(short xxPos, short xyPos, short yxPos, short yyPos)
            {
                xx = xxPos;
                xy = xyPos;
                yx = yxPos;
                yy = yyPos;
            }

            public short getXX()
            { return xx; }
            public short getXY()
            { return xy; }
            public short getYX()
            { return yx; }
            public short getYY()
            { return yy; }
        }
        


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

            //Load up our gametype handlers
            _eol = new EolBoundaries(_arena, this);

            foreach (Arena.FlagState fs in _arena._flags.Values)
            {   //Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
                    _minPlayers = fs.flag.FlagData.MinPlayerCount;

                //Register our flag change events
                fs.TeamChange += onFlagChange;
            }

            if (_minPlayers == Int32.MaxValue)
                //No flags? Run blank games
                _minPlayers = 1;

            return true;
            //Headquarters stuff!
            _hqlevels = new int[] { 500, 1000, 2500, 5000, 10000, 15000, 20000, 25000, 30000, 35000 };
            _hqVehId = 620;
            _baseXPReward = 25;
            _baseCashReward = 150;
            _basePointReward = 10;
            _rewardInterval = 90 * 1000; // 90 seconds
            _hqs = new Headquarters(_hqlevels);
            _hqs.LevelModify += onHQLevelModify;

            //Handle bots
            captainBots = new Dictionary<Team, int>(); //Keeps track of captain bots
            botCount = new Dictionary<Team, int>(); //Counts of all defense bots and their teams
            engineerBots = new List<Team>();
            _currentEngineers = 0;  //The number of engineers currently alive
            _bothqs = new Dictionary<int, bothqsObject>();
            _bothqs.Add(0, new bothqsObject(4901, 4708)); // Sector A
            _bothqs.Add(1, new bothqsObject(7461, 2276)); // Sector A
            _bothqs.Add(0, new bothqsObject(4181, 9956)); // Sector B
            _bothqs.Add(1, new bothqsObject(9573, 11332));// Sector B
            _bothqs.Add(0, new bothqsObject(13765, 1236)); // Sector C
            _bothqs.Add(1, new bothqsObject(17093, 5076)); // Sector C
            _bothqs.Add(0, new bothqsObject(12549, 6708)); // Sector D
            _bothqs.Add(1, new bothqsObject(16981, 10580)); // Sector D

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


            _lastbothqs = null;
            _minPlayers = Int32.MaxValue;

            foreach (Arena.FlagState fs in _arena._flags.Values)
            {   //Determine the minimum number of players
                if (fs.flag.FlagData.MinPlayerCount < _minPlayers)
                    _minPlayers = fs.flag.FlagData.MinPlayerCount;

                //Register our flag change events
                fs.TeamChange += onFlagChange;
            }

            if (_minPlayers == Int32.MaxValue)
                //No flags? Run blank games
                _minPlayers = 1;

            return true;
        }

            
        /// Allows the script to maintain itself
        /// </summary>
        public bool poll()
        {	//Should we check game state yet?
            int now = Environment.TickCount;

            //Do we have enough people to start a game of KOTH?
            int playing = _arena.PlayerCount;
            if (now - _lastGameCheck <= Arena.gameCheckInterval)
                return true;
            _lastGameCheck = now;

            //Should we spawn some bot hqs? Only check once an hour
            if (now - _lastBotCheck > 36000000 && playing > 0)
            {
                foreach (KeyValuePair<int, bothqsObject> obj in _bothqs)
                {
                    if (obj.Value.bExists())
                        continue;

                    VehInfo vehicle = _arena._server._assets.getVehicleByID(Convert.ToInt32(_bothqVehID));
                    Helpers.ObjectState newState = new Protocol.Helpers.ObjectState();
                    newState.positionX = obj.Value.getX();
                    newState.positionY = obj.Value.getY();
                    newState.positionZ = 0;
                    newState.yaw = 0;

                    obj.Value.setExists(true);

                    //Put them all on one bot team since it doesn't matter who owns the bot hq points
                    _arena.newVehicle(
                                vehicle,
                                botTeam1, null,
                                newState);
                }

                _lastBotCheck = now;
            }

            if (_arena._bGameRunning)
            {
                _eol.Poll(now);
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

            if (now - _tickLastCaptain > _checkCaptain)
            {
                IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);

                Player owner = null;
                if (hqs != null)
                {
                    foreach (Vehicle hq in hqs)
                    {//Handle the captains
                        Captain captain = null;

                        if (captain == null)
                        {//They don't have a captain   

                            //Pick a random faction out of two                   
                            Random rand = new Random(System.Environment.TickCount);
                            int id = 0;
                            //See if they have a captain for their HQ, if not spawn one
                            if (owner == null && captainBots != null && !captainBots.ContainsKey(botTeam1) && hq._team == botTeam1)
                            {//It's a bot team
                                id = 437;
                                captain = _arena.newBot(typeof(Captain), (ushort)id, botTeam1, null, hq._state, this, null) as Captain;
                                captainBots.Add(botTeam1, 1);
                                if (botCount.ContainsKey(botTeam1))
                                    botCount[botTeam1] = 0;
                                else
                                    botCount.Add(botTeam1, 0);
                            }
                            else if (owner == null && captainBots != null && !captainBots.ContainsKey(botTeam2) && hq._team == botTeam2)
                            {//It's a bot team
                                id = 415;
                                captain = _arena.newBot(typeof(Captain), (ushort)id, botTeam2, null, hq._state, this, null) as Captain;
                                captainBots.Add(botTeam2, 2);
                                if (botCount.ContainsKey(botTeam2))
                                    botCount[botTeam2] = 0;
                                else
                                    botCount.Add(botTeam2, 0);
                            }
                            else if (owner == null && captainBots != null && !captainBots.ContainsKey(botTeam3) && hq._team == botTeam3)
                            {//It's a bot team
                                id = 443;
                                captain = _arena.newBot(typeof(Captain), (ushort)id, botTeam3, null, hq._state, this, null) as Captain;
                                captainBots.Add(botTeam3, 3);
                                if (botCount.ContainsKey(botTeam3))
                                    botCount[botTeam3] = 0;
                                else
                                    botCount.Add(botTeam3, 0);
                            }
                        }
                    }
                }

                _tickLastCaptain = now;
            }


            if (now - _tickLastEngineer > _checkEngineer)
            {
                //Should we spawn a bot engineer to go base somewhere?
                if (_currentEngineers < _maxEngineers)
                {//Yes
                    IEnumerable<Vehicle> hqs = _arena.Vehicles.Where(v => v._type.Id == _hqVehId);
                    Vehicle home = null;

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
                        //Find a random bot hq point to make our new home
                        IEnumerable<Vehicle> bothqs = _arena.Vehicles.Where(v => v._type.Id == _bothqVehID);
                        if (bothqs.Count() != 0)
                        {
                            _rand = new Random(System.Environment.TickCount);
                            int rand = _rand.Next(0, bothqs.Count());

                            home = bothqs.ElementAt(rand);
                        }
                    }
                    //Just in case there are no bot hq points
                    if (home != null)
                    {
                        if (home._type.Id == _bothqVehID)
                        {
                            Team team = null;
                            _arena.sendArenaMessage("An engineer has been deployed to from Pioneer Station.");
                            if (_hqs[botTeam1] == null)
                                team = botTeam1;
                            else if (_hqs[botTeam2] == null)
                                team = botTeam2;
                            else if (_hqs[botTeam3] == null)
                                team = botTeam3;

                            Engineer George = _arena.newBot(typeof(Engineer), (ushort)300, team, null, home._state, this) as Engineer;

                            //Find the pylon we are about to destroy and mark it as nonexistent
                            foreach (KeyValuePair<int, bothqsObject> obj in _bothqs)
                                if (home._state.positionX == obj.Value.getX() && home._state.positionY == obj.Value.getY())
                                    obj.Value.setExists(false);

                            //Destroy our hq point because we will use our hq to respawn and we dont want any other engineers grabbing this one
                            home.destroy(false);

                            //Keep track of the engineers
                            _currentEngineers++;
                            engineerBots.Add(team);
                        }

                        if (home._type.Id == _hqVehId)
                        {
                            Team team = null;
                            if (home._team == botTeam1)
                                team = botTeam1;
                            else if (home._team == botTeam2)
                                team = botTeam2;
                            else if (home._team == botTeam3)
                                team = botTeam3;

                            Engineer Filbert = _arena.newBot(typeof(Engineer), (ushort)300, team, null, home._state, this) as Engineer;

                            _currentEngineers++;
                            engineerBots.Add(team);
                        }

                    }
                }
                _tickLastEngineer = now;

            }

            //Find out if we will be running KOTH games and if we have enough players
            _minPlayers = _config.king.minimumPlayers;
            if (_minPlayers > 0)
            {
                _playerCrownStatus = new Dictionary<Player, PlayerCrownStatus>();
                _crownTeams = new List<Team>();
            }

            return true;

            //Do we have enough players ingame?
            
            if ((_tickGameStart == 0 || _tickGameStarting == 0) && playing < 1)
            {   //Stop the game!
                _arena.setTicker(1, 1, 0, "Not Enough Players for CTF");
                _arena.gameReset();
            }
            //Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= 1)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, _config.flag.startDelay * 100, "Next CTF game: ",
                    delegate ()
                    {	//Trigger the game start
                        _arena.gameStart();
                    }
                );
            }
            //Maybe the game is in progress...
            else
            {   //It is!
                //The change to small points changes needs to be updated based on players in game constantly
                _pointSmallChange = (int)Math.Ceiling((double)25 / _arena.PlayersIngame.Count());
                _pointPeriodicChange = 1;

                //Let's update some points!
                int flagdelay = 1000; //1000 = 1 second
                if (now - _lastFlagCheck >= flagdelay)
                {   //It's time for a flag update

                    //Loop through every flag
                    foreach (Arena.FlagState fs in _arena._flags.Values)
                        //Add points for every flag they own
                        foreach (Team t in _arena.Teams)
                            if (t == fs.team && _points != null)
                                _points[t] += _pointPeriodicChange;

                    //Update our tick
                    _lastFlagCheck = now;
                }
            }
            return true;
            //Check for expiring crowners
            if (_tickGameStart > 0)
            {
                foreach (var p in _playerCrownStatus)
                {
                    if ((now > p.Value.expireTime || _victoryKothTeam != null) && p.Value.crown)
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

                if (_crownTeams.Count == 1 || _activeCrowns.Count == 1)
                {//We have a winning team
                    _victoryKothTeam = _activeCrowns.First()._team;
                    _arena.sendArenaMessage("Team " + _victoryKothTeam._name + " is the winner of KOTH!");
                    kothVictory(_victoryKothTeam);
                    return true;
                }
                else if (_activeCrowns.Count == 0)
                {//There was a tie
                    _arena.sendArenaMessage("There was no winner");
                    resetKOTH();
                    return true;
                }
            }

            //Update our tickers
            if (_tickGameStart > 0 && now - _arena._tickGameStarted > 2000)
            {
                if (now - _tickGameLastTickerUpdate > 1000)
                {
                    updateTickers();
                    _tickGameLastTickerUpdate = now;
                }
            }
            //Do we have enough players to start a game of KOTH?
            if ((_tickGameStart == 0 || _tickGameStarting == 0) && _minPlayers > 0 && playing < _minPlayers)
            {	//Stop the game!
                _arena.setTicker(1, 1, 0, "Not Enough Players");
                resetKOTH();
            }

             //Do we have enough players to start a game of KOTH?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, _config.king.startDelay * 100, "Next game: ",
                    delegate()
                    {	//Trigger the game start
                        startKOTH();
                    }
                );
            }

            return true;

            if ((_tickGameStart == 0 || _tickGameStarting == 0) && playing < _minPlayers)
            {   //Stop the game!
                _arena.setTicker(1, 1, 0, "Not Enough Players");
                _arena.gameReset();
            }
            //Do we have enough players to start a game?
            else if (_tickGameStart == 0 && _tickGameStarting == 0 && playing >= _minPlayers)
            {	//Great! Get going
                _tickGameStarting = now;
                _arena.setTicker(1, 1, _config.flag.startDelay * 100, "Next game: ",
                    delegate ()
                    {	//Trigger the game start
                        _arena.gameStart();
                    }
                );
            }
            //Maybe the game is in progress...
            else
            {   //It is!
                //The change to small points changes needs to be updated based on players in game constantly
                _pointSmallChange = (int)Math.Ceiling((double)25 / _arena.PlayersIngame.Count());
                _pointPeriodicChange = 1;

                //Let's update some points!
                int flagdelay = 1000; //1000 = 1 second
                if (now - _lastFlagCheck >= flagdelay)
                {   //It's time for a flag update

                    //Loop through every flag
                    foreach (Arena.FlagState fs in _arena._flags.Values)
                        //Add points for every flag they own
                        foreach (Team t in _arena.Teams)
                            if (t == fs.team && _points != null)
                                _points[t] += _pointPeriodicChange;

                    //Update our tick
                    _lastFlagCheck = now;
                }
            }
            return true;
        }
        /// <summary>
        /// Called when adding a new bot to a bot team in game
        /// </summary>
        public void addBot(Player owner, Helpers.ObjectState state, Team team)
        {
            int id = 0;

            if (owner == null)
            {//This is a bot team
                //Find out what bot team this is
                switch (captainBots[team])
                {
                    case 1: id = 437; break; //BotTeam1
                    case 2: id = 415; break; //BotTeam2
                    case 3: id = 443; break; //BotTeam3
                }
                //Spawn a random bot in their faction
                if (botCount.ContainsKey(team))
                    botCount[team]++;
                else
                    botCount.Add(team, 0);
                BasicDefense dBot = _arena.newBot(typeof(BasicDefense), (ushort)id, team, null, state, this, null) as BasicDefense;
            }
        }
        
        

        /// <summary>
        /// Called when KOTH game has ended
        /// </summary>
        public void endKOTH()
        {
            _arena.sendArenaMessage("Game has ended");

            _tickGameStart = 0;
            _tickGameStarting = 0;
            _victoryKothTeam = null;
            _crownTeams = null;

            //Remove all crowns and clear list of KOTH players
            Helpers.Player_Crowns(_arena, false, _arena.Players.ToList());
            _playerCrownStatus.Clear();
        }

        /// <summary>
        /// Called when KOTH game has been restarted
        /// </summary>
        public void resetKOTH()
        {//Game reset, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;

            _victoryKothTeam = null;
        }

        /// <summary>
        /// Called when KOTH game has started
        /// </summary>
        public void startKOTH()
        {
            //We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;
            _playerCrownStatus.Clear();

            //Let everyone know
            _arena.sendArenaMessage("Game has started!", 1);

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
        {	//Let everyone know          
            //Calculate the jackpot for each player
            foreach (Player p in victors.AllPlayers)
            {	//Spectating? 
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
            if (_arena.ActiveTeams.Count() > 1)
            {//Show players their crown timer using a ticker
                _arena.setTicker(1, 0, 0, delegate(Player p)
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
                _arena.setTicker(0, 1, 0,
                    delegate (Player p)
                    {
                        //Update their ticker with current team points
                        if (!_arena.DesiredTeams.Contains(p._team) && _points != null)
                            return "";
                        return "Your Team: " + _points[p._team] + " points";
                    }
                );
                //Other teams points
                _arena.setTicker(0, 2, 0,
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
                _arena.setTicker(0, 3, 0, "Kill rewards: " + _pointSmallChange + " points");
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
        /// <summary>
        /// Called when a flag changes team
        /// </summary>
        public void onFlagChange(Arena.FlagState flag)
        {	//Does this team now have all the flags?
            bool allFlags = true;

            if (flag.team == null || _arena._flags == null || _arena._flags.Count == 0)
                return;
            foreach (Arena.FlagState fs in _arena._flags.Values)
                if (fs.flag != null)
                    if (fs.team != flag.team)
                        allFlags = false;

            if (allFlags && _arena.DesiredTeams.Contains(flag.team))
                _arena.sendArenaMessage(flag.team._name + " controls all the flags!", 20);
            else
                _arena.sendArenaMessage(flag.team._name + " has captured " + flag.flag.GeneralData.Name + "!", 21);
        }

        /// <summary>
        /// Called when the specified team have won
        /// </summary>
        public void gameVictory(Team victors)
        {	//Game is over.
            _arena.sendArenaMessage(victors._name + " has reached " + _points[victors] + " points!", 13);

            //Clear out all tickers we use in updateTickers (1,2,3)
            for (int i = 1; i <= 3; i++)
                _arena.setTicker(0, i, 0, "");

            //Lets reward the teams and the MVP!
            int rpoints = _arena._server._zoneConfig.flag.pointReward * _arena.PlayersIngame.Count();
            int rcash = _arena._server._zoneConfig.flag.cashReward * _arena.PlayersIngame.Count();
            int rexperience = _arena._server._zoneConfig.flag.experienceReward * _arena.PlayersIngame.Count();

            //Give higher reward the more points they have
            foreach (Team t in _arena.ActiveTeams)
            {
                foreach (Player p in t.ActivePlayers)
                {   //Reward each player based on his teams performance
                    int points = _points[t];
                    double modifier = ((points / _points.MaxPoints) * 2) + 1;
                    rpoints = Convert.ToInt32(rpoints * modifier);
                    rcash = Convert.ToInt32(rcash * modifier);
                    rexperience = Convert.ToInt32(rexperience * modifier);
                    p.BonusPoints += rpoints;
                    p.Cash += rcash;
                    p.Experience += rexperience;
                    p.sendMessage(0, String.Format("Personal Reward: Points={0} Cash={1} Experience={2}",
                        rpoints, rcash, rexperience));
                    p.syncState();
                }
            }
            //Stop the game
            _arena.gameEnd();
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

            

            return true;
        }

        /// <summary>
        /// Called when a player enters the arena
        /// </summary>
        [Scripts.Event("Player.EnterArena")]
        public void playerEnter(Player player)
        {   //We always run blank games, try to start a game in whatever arena the player is in
            if (!_arena._bGameRunning)
            {
                _arena.gameStart();
                _arena.flagSpawn();
            }

            //Send them the crowns if KOTH is enabled
            if (_minPlayers > 0)
                if (!_playerCrownStatus.ContainsKey(player))
                {
                    _playerCrownStatus[player] = new PlayerCrownStatus(false);
                    Helpers.Player_Crowns(_arena, true, _activeCrowns, player);
                }
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        [Scripts.Event("Player.Leave")]
        public void playerLeave(Player player)
        {//Find out if KOTH is enabled
            if (_minPlayers > 0)
                if (_playerCrownStatus.ContainsKey(player))
                {//Remove their crown and tell everyone
                    _playerCrownStatus[player].crown = false;
                    Helpers.Player_Crowns(_arena, false, _noCrowns);
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

                //Notify the rest of the arena
                foreach (Team t in _arena.Teams.Where(team => team != killers))
                    t.sendArenaMessage("&Headquarters - " + dead._team._name + " HQ worth " + _hqs[dead._team].Bounty + " bounty has been destroyed by " + killers._name + "!");

                //Stop tracking this HQ
                _hqs.Destroy(dead._team);
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
            return true;
            
        }
        /// <summary>
		/// Called when the game begins
		/// </summary>
		[Scripts.Event("Game.Start")]
        public bool gameStart()
        {   //We've started!
            _tickGameStart = Environment.TickCount;
            _tickGameStarting = 0;

            //Scramble the teams!
            ScriptHelpers.scrambleTeams(_arena, 2, true);

            //Spawn our flags!
            _arena.flagSpawn();

            //Create some points and subscribe to our point modification event
            _points = new Points(_arena.ActiveTeams, 0, 1000);
            _points.PointModify += onPointModify;

            //Start keeping track of healing
            _healingDone = new Dictionary<Player, int>();

            //Let everyone know
            _arena.sendArenaMessage("Game has started! First team to " + _points.MaxPoints + " points wins!", _config.flag.resetBong);
            updateTickers();

            return true;
        }

        /// <summary>
        /// Called when the game ends
        /// </summary>
        [Scripts.Event("Game.End")]
        public bool gameEnd()
        {   //Game finished, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;
            _healingDone = null;
            return true;
        }

        /// <summary>
        /// Called when the statistical breakdown is displayed
        /// </summary>
        [Scripts.Event("Player.Breakdown")]
        public bool playerBreakdown(Player from, bool bCurrent)
        {	//Allows additional "custom" breakdown information
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
        /// Called to reset the game state
        /// </summary>
        [Scripts.Event("Game.Reset")]
        public bool gameReset()
        {   //Game reset, perhaps start a new one
            _tickGameStart = 0;
            _tickGameStarting = 0;

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
		/// Triggered when a player wants to unspec and join the game
		/// </summary>
		[Scripts.Event("Player.JoinGame")]
        public bool playerJoinGame(Player player)
        {
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
            return true;
        }
    }
}