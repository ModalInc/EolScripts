﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text.RegularExpressions;

using InfServer.Logic;
using InfServer.Game;
using InfServer.Scripting;
using InfServer.Bots;
using InfServer.Protocol;

using Assets;

namespace InfServer.Script.GameType_Eol
{
    public class BotSettings
    {
        public int Cash;
        public int Experience;
        public int Points;
        public int Bounty;

        public BotSettings(int cash, int experience, int points, int bounty)
        {
            Cash = cash;
            Experience = experience;
            Points = points;
            Bounty = bounty;
        }

    }
    public static class BotHelpers
    {

        public static BotSettings Settings(this Bot bot)
        {
            string description = bot._type.Description;
            int cash = 0;
            int experience = 0;
            int points = 0;
            int bounty = 0;

            //Read in any sub-settings within the description
            if (description.Length >= 4 && description.Substring(0, 9).ToLower().Equals("settings="))
            {
                string[] lootparams;
                lootparams = Regex.Split(description.Substring(9, description.Length - 9), ",(?=(?:[^\']*\'[^\']*\')*(?![^\']*\'))");


                foreach (string lootparam in lootparams)
                {
                    if (!lootparam.Contains(':'))
                        continue;

                    string paramname = lootparam.Split(':').ElementAt(0).ToLower();
                    string paramvalue = lootparam.Split(':').ElementAt(1).ToLower();
                    switch (paramname)
                    {
                        case "cash":
                            {
                                string input = paramvalue.Replace("'", "");
                                cash = Convert.ToInt16(input);
                            }
                            break;
                        case "exp":
                            {
                                string input = paramvalue.Replace("'", "");
                                experience = Convert.ToInt16(input);
                            }
                            break;
                        case "points":
                            {
                                string input = paramvalue.Replace("'", "");
                                points = Convert.ToInt16(input);
                            }
                            break;
                        case "bounty":
                            {
                                string input = paramvalue.Replace("'", "");
                                bounty = Convert.ToInt16(input);
                            }
                            break;
                    }
                }
                return new BotSettings(cash, experience, points, bounty);
            }
            else
                return null;
        }
    }
}
