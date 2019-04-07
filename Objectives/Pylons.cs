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
        
        public Pylons(Arena arena, Script_Eol baseScript)
        {
            _arena = arena;
            _config = arena._server._zoneConfig;
            _baseScript = baseScript;
            _bpylonsSpawned = false;
            _gameBegun = false;
            
        }

        public bool Poll(int now)
        {
            int playing = _arena.PlayerCount;

        }

        /// <summary>
        /// Called when adding a new bot to a bot team in game
        /// </summary>
       
        public void gameStart()
        {	//We've started!
            _gameBegun = true;
        }

        public bool gamesEnd()
        {
            _bpylonsSpawned = false;
            _gameBegun = false;
            return true;
        }

        public bool gameReset()
        {	
            _bpylonsSpawned = false;
            _gameBegun = false;
            return true;
        }
    }
}
