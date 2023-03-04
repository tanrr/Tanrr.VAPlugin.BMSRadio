using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanrr.VAPlugin.BMSRadio
{
    public static class Logger
    {
        static private bool s_Json = false;
        static private bool s_MenuItems = false;
        static private bool s_Structures = false;
        static private bool s_Verbose = false;

        static private string s_Prefix = string.Empty;
        static private string s_WarningPrefix = "WARNING: ";
        static private string s_ErrorPrefix = "ERROR: ";

        static public bool Json { get => s_Json; set => s_Json = value; }
        static public bool MenuItems {  get => s_MenuItems; set => s_MenuItems = value; } 
        static public bool Structures{ get => s_Structures; set => s_Structures = value; }
        static public bool Verbose{ get => s_Verbose; set => s_Verbose = value; }

        static public string Prefix { get => s_Prefix; set => s_Prefix = value;  }
        static public string WarningPrefix { get => s_WarningPrefix; set => s_WarningPrefix = value; }
        static public string ErrorPrefix { get => s_ErrorPrefix; set => s_ErrorPrefix = value; }

        // Default logging we always do
        public static void Write(dynamic vaProxy, string msg)
        {
            vaProxy.WriteToLog(s_Prefix + msg, "Purple");
        }

        // Logging only if s_Json, but ALSO do if s_Verbose
        public static void JsonWrite(dynamic vaProxy, string msg)
        {
            if (s_Json || s_Verbose)
            {
                vaProxy.WriteToLog(s_Prefix + msg, "Blue");
            }
        }

        public static void MenuItemsWrite(dynamic vaProxy, string msg)
        {
            if (s_MenuItems || s_Verbose)
            {
                vaProxy.WriteToLog(s_Prefix + msg, "Blue");
            }
        }

        // Logging only if s_Strucutres
        public static void StructuresWrite(dynamic vaProxy, string msg)
        {
            if (s_Structures)
            {
                vaProxy.WriteToLog(s_Prefix + msg, "Gray");
            }
        }

        // Logging only if s_Verbose
        public static void VerboseWrite(dynamic vaProxy, string msg)
        {
            if (s_Verbose)
            {
                vaProxy.WriteToLog(s_Prefix + msg, "Gray");
            }
        }

        // Warning Logging
        public static void Warning(dynamic vaProxy, string msg)
        {
            vaProxy.WriteToLog(s_WarningPrefix + msg, "Orange");
        }

        // Error Logging
        public static void Error(dynamic vaProxy, string msg)
        {
            vaProxy.WriteToLog(s_ErrorPrefix + msg, "Red");
        }
    }

}
