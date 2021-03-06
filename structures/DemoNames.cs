﻿using DemoCleaner3.DemoParser.parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DemoCleaner3.structures
{
    public class DemoNames
    {
        static string defaultName = "UnnamedPlayer";

        public string dfName = null;        //name in params - df_name
        public string uName = null;         //name in the game
        public string oName = null;         //name from console line (online name)
        public string fName = null;         //name from the filename

        public void setNamesByPlayerInfo(Dictionary<string, string> playerInfo) {
            if (playerInfo != null) {
                dfName = Ext.GetOrNull(playerInfo, "df_name");
                uName = normalizeName(RawInfo.removeColors(Ext.GetOrNull(playerInfo, "name")));
            }
        }

        public void setOnlineName(string onlineName) {
            oName = normalizeName(RawInfo.removeColors(onlineName));
        }

        public void setBracketsName(string bracketsName) {
            fName = bracketsName;
        }

        public string chooseNormalName() {
            return chooseName(dfName, uName, oName, fName); 
        }

        //selection of the first non-empty string from parameters
        private static string chooseName(params string[] names) {
            for (int i = 0; i < names.Length - 1; i++) {
                if (!string.IsNullOrEmpty(names[i]) && names[i] != defaultName) {
                    return names[i];
                }
            }
            return defaultName;
        }



        //name that can be used in the file name
        public static string normalizeName(string name) {
            return string.IsNullOrEmpty(name)
                ? name
                : Regex.Replace(name, "[^a-zA-Z0-9\\!\\#\\$\\%\\&\\'\\(\\)\\+\\,\\-\\.\\;\\=\\[\\]\\^_\\{\\}]", "");
        }
    }
}
