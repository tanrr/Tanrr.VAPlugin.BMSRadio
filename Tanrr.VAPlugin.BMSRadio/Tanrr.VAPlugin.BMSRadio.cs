using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Runtime.CompilerServices;
using System.Dynamic;


namespace Tanrr.VAPlugin.BMSRadio
{
    public struct DirCmdInfo
    {
        // Holds menu key (menuFullID), and index of menuItem within that menu
        // Stored as values in s_DirCmdMap to map direct commands to their associated menu and menu item

        public int IndexMenuItem { get; }
        public string MenuFullID { get; }
        public DirCmdInfo(string menuFullID, int indexMenuItem)
        {
            this.MenuFullID = menuFullID;
            this.IndexMenuItem = indexMenuItem;
        }
    };

    public class Jeeves_BMS_Radio_Plugin 
    {
        // Jeeves BMS Radio VoiceAttack Plugin

        protected static string s_version = "v0.1.5";
        protected static string s_verPluginJSON = string.Empty;
        protected static string s_verBMSJSON = string.Empty;
        protected static string s_csVerPluginJSON = string.Empty;
        protected static string s_csVerBMSJSON = string.Empty;
        protected static Dictionary<string, MenuBMS> s_menusAll = null;       // Dictionary containing all BMS radio menus
        protected static Dictionary<string, DirCmdInfo> s_DirCmdMap = null;   // Map of direct command items to specific menus+items

        protected static MenuBMS s_curMenu = null;                       // Current menu being displayed, if any  
        protected static List<MenuBMS> s_menusToList = null;                // List of menus being stepped through if in List...Menus command
        protected static int s_indexMenuToList = -1;

        // VoiceAttack variables used/shared by plugin
        protected const string JBMS_JsonLog = ">JBMS_JSON_LOG";             // Boolean for logging of JSON parsing
        protected const string JBMS_MenuLog = ">JBMS_MENU_LOG";             // Boolean for logging of menu items(shows list of menu items in log)
        protected const string JBMS_StructLog = ">JBMS_STRUCT_LOG";         // Boolean for logging of data structures manipulation
        protected const string JBMS_VerboseLog = ">JBMS_VERBOSE_LOG";       // Boolean for more verbose general logging

        protected const string JBMS_MenuTgt = ">JBMS_MENU_TGT";             // Holds menuTarget when plugin called w/ context "JBMS_SHOW_MENU"
        protected const string JBMS_MenuName = ">JBMS_MENU_NAME";           // Holds menuName when calling plugin w/ context "JBMS_SHOW_MENU"
        protected const string JBMS_DirectCmd = ">JBMS_DIRECT_CMD";         // Holds directCommand when calling plugin w/ context "JBMS_DIRECT_CMD"

        protected const string JBMSI_NoSuchMenu = ">JBMSI_NO_SUCH_MENU";    // Checked & set by "JBMS Radio Menu Show" command
        protected const string JBMSI_MenuResponse = ">JBMSI_MENU_RESPONSE"; // (Checked & set only by "JBMS Wait For Menu Response" command)

        protected const string JBMSI_ListingMenus = ">JBMSI_LISTING_MENUS"; // Boolean set while listing multiple menus
        protected const string JBMSI_MenuUp = ">JBMSI_MENU_UP";             // Boolean set while menu is displayed and waiting for menu response
        protected const string JBMSI_Version = ">JBMSI_VERSION";            // Plugin Version


        protected const string JBMSI_CallsignsAWACS = ">>JBMSI_CALLSIGNS_AWACS";    // Possible AWACS Callsigns (no numbers)
        protected const string JBMSI_CallsignsJTAC =  ">>JBMSI_CALLSIGNS_JTAC";     // Possible JTAC callsigns (no numbers)
        protected const string JBMSI_CallsignsTankers=">>JBMSI_CALLSIGNS_TANKERS";  // Possible TANKER callsigns (no numbers)

        protected const string JBMSI_Callsign =       ">>JBMSI_CALLSIGN";           // Current callsign user has registered
        protected const string JBMSI_CallsignNum =    ">>JBMSI_CALLSIGN_NUM";       // Current callsign flight number (not position in flight) - can be empty
        protected const string JBMSI_CallsignPos =    ">>JBMSI_CALLSIGN_POS";       // Current callsign position in flight (1-4) - can be empty
        protected const string JBMSI_CSMatch =        ">>JBMSI_CS_MATCH";           // Phrase matching callsign with optional flight #: "[Fiend;Fiend 3]"
        protected const string JBMSI_CSMatchEmptyOK = ">>JBMSI_CS_MATCH_EMPTY_OK";  // Same as CSMatch, but allows matching nothing: "[Fiend;Fiend 3;]"


        protected const string JBMSI_CallsignList =   ">>JBMSI_CALLSIGN_LIST"; // Semicolon delimited list of all pilot callsigns we're allowed to match
        protected const string JBMSI_CallsignsLoaded= ">>JBMSI_CALLSIGNS_LOADED"; // Boolean set after plugin loads callsigns from files
        protected const string JBMSI_CallsignsInited= ">>JBMSI_CALLSIGNS_INITED"; // Boolean set AFTER profile RE-init refreshes command phrases using >>JBMSI_CALLSIGN or >>JBMSI_CALLSIGN_LIST
  
        protected const string JBMSI_Inited = ">>JBMSI_INITED";             // Plugin initialized - stays set when switching profiles, unset when VA quits


        // VoiceAttack command phrases to execute from plugin
        protected const string CmdJBMS_WaitForMenuResponse = "JBMS Wait For Menu Response";
        protected const string CmdJBMS_CloseMenu = "JBMS Close Menu";
        protected const string CmdJBMS_PressKeyComboList = "JBMS Press Key Combo List";
        protected const string CmdJBMS_KillWaitForMenuResponse = "JBMS Kill Command Wait For Menu Response";
        protected const string CmdJBMS_SetCallsign = "JBMS Set Callsign";

        protected const string JBMS_MenuTimeout = "_JBMS_MENU_TIMEOUT";     // Possible JBMSI_MenuResponse;

        // TODO - Forcing string comps to ignore case till decide lowercase vs mixed
        protected const StringComparison s_strComp = StringComparison.OrdinalIgnoreCase;

        protected static string s_awacsNames = string.Empty;   
        protected static string s_tankerNames = string.Empty;  
        protected static string s_jtacNames = string.Empty;    
        protected static string s_pilotCallsignsQuery = string.Empty;

    public static string VA_DisplayName()
            => $"Jeeves BMS Radio Plugin for VoiceAttack {s_version} Beta";  // Displayed in VA dropdowns and log

        public static string VA_DisplayInfo()
            => $"Jeeves BMS Radio Plugin for VoiceAttack {s_version} Beta 2023 for BMS 4.37.2";  // Extended info

        public static Guid VA_Id()
            => new Guid("{7e22363e-2cca-4b26-8aae-a292f73d2a53}");  // TANR JBMS G1

        public static void VA_StopCommand()  
        {
            // Called from VA when the 'stop all commands' button is pressed or a 'stop all commands' action is called
            // Only extended wait is actually async call into VA Profile CmdJBMS_WaitForMenuResponse
            // which has a "Wait for Spoken Response" command (with a timeout) - VA will stop that command on its own

            // TODO - Revisit this if major code or logic changes happen to make sure it's still not needed
        }

        protected static bool GetNonNullBool(dynamic vaProxy, string propName)
        {
            if (string.IsNullOrEmpty(propName)) { return false; }   
            return vaProxy.GetBoolean(propName) ?? false;
        }

        protected static bool GetNonNullText(dynamic vaProxy, string propName, out string textValue)
        {
            // Assumes non-null out variable for textValue - okay to throw exception if null
            textValue = vaProxy.GetText(propName);
            if (textValue == null) 
            { 
                textValue = string.Empty;
                return false; 
            }
            return true;
        }

        protected static bool VerifyMenuState(dynamic vaProxy, bool menuUp, bool noErrorsAllowed)
        {
            bool IsMenuUp = GetNonNullBool(vaProxy, JBMSI_MenuUp);
            bool HasErrors = GetNonNullBool(vaProxy, JBMSI_NoSuchMenu);

            return ((IsMenuUp == menuUp) && (!noErrorsAllowed || !HasErrors));
        }

        protected static void ResetMenuState(dynamic vaProxy, bool onlyUpAndErrors = false, bool killWaitForMenu = true)
        {
            // Resets our stored menu states, and cancels any pending Wait for Response in the VA profile
            // Clearing menu target and name can be overridden if needed

            Logger.VerboseWrite(vaProxy, "ResetMenuState");

            vaProxy.SetBoolean(JBMSI_NoSuchMenu, false);               // Set by plugin if it's called to load a target/menu pair that doesn't exist
            vaProxy.SetBoolean(JBMSI_MenuUp, false);                    // True only while menu is up (hopefully)
            // Kill "JBMS Wait For Menu Response" if its currently executing
            if (killWaitForMenu && vaProxy.Command.Active(CmdJBMS_WaitForMenuResponse))
            {
                vaProxy.Command.Execute(CmdJBMS_KillWaitForMenuResponse, WaitForReturn: true, AsSubcommand: true);
            }
            // Set in JBMS Wait For Menu Response - clear in case it got set just before JBMS Wait was terminated
            vaProxy.SetText(JBMSI_MenuResponse, string.Empty);

            if (onlyUpAndErrors) { return; }

            vaProxy.SetText(JBMS_MenuTgt, string.Empty);
            vaProxy.SetText(JBMS_MenuName, string.Empty);
        }

        protected static void CloseMenu(dynamic vaProxy, bool pressEscape = true, bool onlyUpAndErrors = false)
        {
            // Tell VA to execute close them menu with ESC, but NOT call back plugin with "JBMS_RESET_MENU_STATE"
            if (pressEscape)
            {   vaProxy.Command.Execute(CmdJBMS_CloseMenu, WaitForReturn: true, AsSubcommand: true);    }
            ResetMenuState(vaProxy, onlyUpAndErrors);
        }

        public static void VA_Init1(dynamic vaProxy)
        {
            // Called once on VA load, and it is called asynchronously.
            // Since this is called BEFORE our profile loads and calls us, will should wait till we get hit with VA_Invoke1 to do our initialization
        }

        public static void VA_Exit1(dynamic vaProxy)
        {
            // Called when VA is closing (normally) - not guaranteed
            // Garbage collection should handle everything - we don't hold onto file handles
        }

        protected static bool ReadFileIntoString(dynamic vaProxy, string pathFile, out string stringFromFile)
        {
            stringFromFile = string.Empty;
            try
            {   stringFromFile = File.ReadAllText(pathFile);    }
            catch
            {
                Logger.Error(vaProxy, "Cannot read file " + pathFile);
                return false;
            }
            if (String.IsNullOrEmpty(stringFromFile))
            {
                Logger.Error(vaProxy, "No data read from " + pathFile);
                return false;
            }
            return true;
        }

        protected static bool ValidateVersion(dynamic vaProxy, JObject versionInfo, string loadType, out string verPlugin, out string verBMS)
        {
            verPlugin = string.Empty;
            verBMS = string.Empty;

            if (string.IsNullOrEmpty(loadType) || versionInfo == null )
            {
                Logger.Error(vaProxy, "Failed to load version information from JSON");
                return false; 
            }

            try
            {
                verPlugin = (string)versionInfo["verPluginJSON"];
                verBMS = (string)versionInfo["verBMSJSON"];
                if (string.IsNullOrEmpty(verPlugin) || string.IsNullOrEmpty(verBMS))
                {
                    verPlugin = verBMS = string.Empty;
                    Logger.Error(vaProxy, $"Failed to load version information from {loadType} JSON");
                    return false;
                }
                Logger.JsonWrite(vaProxy, $"Loading JSON {loadType} file for plugin version {verPlugin} and BMS version {verBMS}");
                if (!s_version.Equals(verPlugin))
                { Logger.Warning(vaProxy, $"Plugin version {s_version} does not match {loadType} JSON version {verPlugin}"); }
            }
            catch (Exception e)
            {
                Logger.Error(vaProxy, $"Failed to load version information from {loadType} JSON - Exception thrown");
                Logger.Error(vaProxy, e.Message);
                return false;
            }
            return true;
        }

        // Convert JArray of string phrases, that must be in form "phrase1;phrase2;;phrase with spaces;foo" 
        // with phrase NOT including beginning or ending semicolons, or double semicolons - spaces allowed if specified
        // Puts pieces together separated by semicolons - does not add semicolons to the end
        protected static bool BuildMatchingPhraseFromJArrayPhrases(dynamic vaProxy, JArray phrases, bool spacesAllowed, out string builtMatchPhrase)
        {
            builtMatchPhrase = string.Empty;
            if (phrases == null || phrases.Count <= 0 )
            { return false; }

            for (int i = 0; i < phrases.Count; i++)
            {
                string phrase = phrases[i].ToString();
                if (phrase == null)
                {
                    Logger.Error(vaProxy, "Error building matching phrase lists");
                    return false;
                }
                else if (phrase == string.Empty)
                { continue; /* empty allowed - just doesn't do anything */ }
                else
                {
                    bool valid = true;
                    // Validate
                    if (phrase.Contains(";;"))
                    {
                        Logger.Error(vaProxy, "Double semicolon ;; not allowed in matching phrases");
                        valid = false;
                    }
                    if (phrase.StartsWith(";"))
                    {
                        Logger.Error(vaProxy, "Beginning semicolon ; not allowed in matching phrases");
                        valid = false;
                    }
                    if (phrase.EndsWith(";"))
                    {
                        Logger.Error(vaProxy, "Ending semicolon ; not allowed in matching phrases");
                        valid = false;
                    }
                    if (!spacesAllowed && phrase.Contains(" "))
                    {
                        Logger.Error(vaProxy, "Spaces not allowed in specific phrases");
                        valid = false;
                    }
                    if (!valid)
                    {
                        Logger.Error(vaProxy, $"Invalid phrase: {phrase}");
                        return false;
                    }
                    // Should be good to go
                    if (builtMatchPhrase.Length == 0)
                    {   builtMatchPhrase += phrase; }
                    else
                    {   builtMatchPhrase += ";" + phrase; }

                    continue;
                }
            }
            return true;
        }

        protected static bool OneTimeCallsignLoad(dynamic vaProxy)
        {
            // LOAD OUR CALLSIGN INFO
            // **DO NOT** set JBMSI_CALLSIGN_INITED - that should only be set by profile when it reinits to re-evaluate tokens in command phrases

            if (GetNonNullBool(vaProxy, JBMSI_CallsignsLoaded))
            {
                Logger.Warning(vaProxy, "OneTimeCallsignLoad() called more than once - this should not happen");
                // Just return success quietly since we should already be configured
                return true;
            }

            // TODO - Separate out loading files into strings, parsing into JArray or JObject, and validating against schema - dupe of OneTimeLoad below

            string appsDir = vaProxy.AppsDir;
            if (string.IsNullOrEmpty(appsDir))
            {
                Logger.Error(vaProxy, "Invalid AppsDir - Cannot load menu info");
                return false;
            }
            string csJsonPath = appsDir + "\\Tanrr.VAPlugin.BMSRadio\\Tanrr.VAPlugin.BMSRadio.Callsigns.json";
            string csJsonSchemaPath = appsDir + "\\Tanrr.VAPlugin.BMSRadio\\Tanrr.VAPlugin.BMSRadio.Callsigns.Schema.json";
            if (!ReadFileIntoString(vaProxy, csJsonPath, out string csJsonRead))
            {
                Logger.Error(vaProxy, "No json data read from " + csJsonPath);
                return false;
            }
            if (!ReadFileIntoString(vaProxy, csJsonSchemaPath, out string csSchemaRead))
            {
                Logger.Error(vaProxy, "No schema data read from " + csJsonSchemaPath);
                return false;
            }

            // Parse top level menu as array - If this fails, json isn't usable          
            JObject csDeserialized = new JObject();
            try
            { csDeserialized = JObject.Parse(csJsonRead); }
            catch
            {
                Logger.Error(vaProxy, "Failed to parse json " + csJsonPath + " to JObject");
                return false;
            }
            if (Object.ReferenceEquals(csDeserialized, null))
            {
                Logger.Error(vaProxy, "Failed to parse json " + csJsonPath + " to JObject");
                return false;
            }

            // Parse schema & verify menu json
            JSchema csSchemaDeserialized = new JSchema();
            try
            { csSchemaDeserialized = JSchema.Parse(csSchemaRead); }
            catch
            {
                Logger.Error(vaProxy, "Failed to parse schema " + csJsonSchemaPath);
                return false;
            }
            if (Object.ReferenceEquals(csDeserialized, null))
            {
                Logger.Error(vaProxy, "Failed to parse schema " + csJsonSchemaPath);
                return false;
            }

            IList<string> errorMsgs = new List<string>();
            if (!csDeserialized.IsValid(csSchemaDeserialized, out errorMsgs) || csDeserialized.Count <= 1)
            {
                Logger.Error(vaProxy, "" + csJsonPath + " failed schema validation against " + csJsonSchemaPath + "or didn't contain menus");
                if (errorMsgs != null)
                {
                    foreach (string msg in errorMsgs) { Logger.Error(vaProxy, "Schema Error: " + msg); }
                }
                return false;
            }
            Logger.JsonWrite(vaProxy, "Verified callsign JSON " + csJsonPath + "\n against JSON schema " + csJsonSchemaPath);

            // Pull out the objects

            // Verify and store version info from the JSON 
            if (!ValidateVersion(vaProxy, (JObject)csDeserialized["version"], "callsign", out s_csVerPluginJSON, out s_csVerBMSJSON))
            { return false; }

            // Load flight info - callsign, number, position
            try
            {
                JObject flightInfo = (JObject)csDeserialized["flightInfo"];
                if (flightInfo != null)
                {
                    string callsignFlight = (string)flightInfo["callsignFlight"];
                    string numberFlight = (string)flightInfo["numberFlight"];
                    string posInFlight = (string)flightInfo["posInFlight"];
                    if (string.IsNullOrEmpty(callsignFlight) || numberFlight == null || posInFlight == null)
                    {
                        Logger.Error(vaProxy, "Callsign JSON file doesn't have correct flightInfo section");
                        return false;
                    }
                    vaProxy.SetText(JBMSI_Callsign, callsignFlight);
                    if (numberFlight != string.Empty) { vaProxy.SetText(JBMSI_CallsignNum, numberFlight); }
                    if (posInFlight != string.Empty) { vaProxy.SetText(JBMSI_CallsignPos, posInFlight); }
                }
                else
                {
                    Logger.Error(vaProxy, "Callsign JSON file doesn't contain flightInfo section");
                    return false;
                }

                // Load all the other names/callsigns
                // TODO - Add a duplicate checker for these - probably built into the BuildMatching
                if ( !BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["awacsNames"], spacesAllowed: false, builtMatchPhrase: out s_awacsNames) )
                {   Logger.Error(vaProxy, "Failed to load AWACS callsigns from callsign JSON"); return false; }
                if (!BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["tankerNames"], spacesAllowed: false, builtMatchPhrase: out s_tankerNames))
                {   Logger.Error(vaProxy, "Failed to load TANKER callsigns from callsign JSON"); return false; }
                if (!BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["jtacNames"], spacesAllowed: false, builtMatchPhrase: out s_jtacNames))
                {   Logger.Error(vaProxy, "Failed to load JTAC callsigns from callsign JSON"); return false; }
                if (!BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["pilotNames"], spacesAllowed: false, builtMatchPhrase: out s_pilotCallsignsQuery))
                { Logger.Error(vaProxy, "Failed to load PILOT callsigns from callsign JSON"); return false; }

                // Target callsign groups can include duplicates callsigns from other target callsign groups
                // For example, no JTAC callsign can match a TANKER callsign (or command phrases could have ambiguous matches)
                // So check for duplicates between them
                HashSet<string> phraseBucket = new HashSet<string>();
                string csDupeTest = s_awacsNames + ";" + s_tankerNames + ";" + s_jtacNames;
                // split string for delimiter char then check all odd-numbered entries (since it can't have a ';' at either end)
                string [] phrasesSplit = csDupeTest.Split(';');
                for (int iPhrase = 0; iPhrase < phrasesSplit.Length; iPhrase++)
                {
                    // Add all phrases as lower case since "Dupe" should match "dupe"
                    if (iPhrase % 2 == 1 && !phraseBucket.Add(phrasesSplit[iPhrase].ToLowerInvariant()) )
                    {
                        Logger.Error(vaProxy, $"Duplicate callsign \"{phrasesSplit[iPhrase]}\" between AWACS, Tanker, and JTAC callsigns - ambiguous match - remove duplicate"); 
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(vaProxy, "Exception working with callsign JSON file");
                Logger.Error(vaProxy,e.Message);
                return false;
            }

            vaProxy.SetText(JBMSI_CallsignList, s_pilotCallsignsQuery);
            vaProxy.SetText(JBMSI_CallsignsAWACS, s_awacsNames);
            vaProxy.SetText(JBMSI_CallsignsJTAC, s_jtacNames);
            vaProxy.SetText(JBMSI_CallsignsTankers, s_tankerNames);

            vaProxy.SetBoolean(JBMSI_CallsignsLoaded, true);

            return true;
        }

        protected static bool OneTimeMenuDataLoad(dynamic vaProxy)
        {
            if (GetNonNullBool(vaProxy, JBMSI_Inited))
            {
                Logger.Warning(vaProxy, "OneTimeMenuDataLoad() called more than once - possible when switching betweeen profiles.");
                // Don't try to reinitialize - just return success quietly since we should already be configured
                return true;
            }

            // Configure logger
            Logger.Prefix = "JeevesBMSRadio: ";
            Logger.WarningPrefix = "JeevesBMSRadio: WARNING: ";
            Logger.ErrorPrefix = "JeevesBMSRadio: ERROR: ";
            Logger.Json = GetNonNullBool(vaProxy, JBMS_JsonLog);
            Logger.MenuItems = GetNonNullBool(vaProxy, JBMS_MenuLog);
            Logger.Structures = GetNonNullBool(vaProxy, JBMS_StructLog);
            Logger.Verbose = GetNonNullBool(vaProxy, JBMS_MenuLog);
            Logger.Write(vaProxy, "OneTimeMenuDataLoad()");

            string appsDir = vaProxy.AppsDir;
            if (string.IsNullOrEmpty(appsDir))
            {
                Logger.Error(vaProxy, "Invalid AppsDir - Cannot load menu info");
                return false;
            }

            if ( !GetNonNullText(vaProxy, JBMSI_Version, out string versionProfile) )
            {
                Logger.Error(vaProxy, "No version information available from VA Profile - Aborting load");
                return false;
            }
            else if (!versionProfile.Equals(s_version, s_strComp))
            {   Logger.Warning(vaProxy, $"Plugin version {s_version} does not equal profile version {versionProfile}");   }

            // Look for our menu info json file and schema and load each into a string
            string menuJsonPath = appsDir + "\\Tanrr.VAPlugin.BMSRadio\\Tanrr.VAPlugin.BMSRadio.Menus.json";
            string menuJsonSchemaPath = appsDir + "\\Tanrr.VAPlugin.BMSRadio\\Tanrr.VAPlugin.BMSRadio.Menus.Schema.json";
            if (!ReadFileIntoString(vaProxy, menuJsonPath, out string menusJsonRead))
            {
                Logger.Error(vaProxy, "No json data read from " + menuJsonPath);
                return false;
            }
            if (!ReadFileIntoString(vaProxy, menuJsonSchemaPath, out string menuSchemaRead))
            {
                Logger.Error(vaProxy, "No schema data read from " + menuJsonSchemaPath);
                return false;
            }

            // Parse top level menu as array - If this fails, json isn't usable          
            JArray menusDeserialized = new JArray();
            try
            {   menusDeserialized = JArray.Parse(menusJsonRead);    }
            catch
            {
                Logger.Error(vaProxy, "Failed to parse json " + menuJsonPath + " to JArray");
                return false;
            }
            if (Object.ReferenceEquals(menusDeserialized, null))
            {
                Logger.Error(vaProxy, "Failed to parse json " + menuJsonPath + " to JArray");
                return false;
            }

            // Parse schema & verify menu json
            JSchema menuSchemaDeserialized = new JSchema();
            try
            {   menuSchemaDeserialized = JSchema.Parse(menuSchemaRead); }
            catch
            {
                Logger.Error(vaProxy, "Failed to parse schema " + menuJsonSchemaPath);
                return false;
            }
            if (Object.ReferenceEquals(menusDeserialized, null))
            {
                Logger.Error(vaProxy, "Failed to parse schema " + menuJsonSchemaPath);
                return false;
            }
            IList<string> errorMsgs = new List<string>();
            if (!menusDeserialized.IsValid(menuSchemaDeserialized, out errorMsgs) || menusDeserialized.Count <= 1)
            {
                Logger.Error(vaProxy, "" + menuJsonPath + " failed schema validation against " + menuJsonSchemaPath + "or didn't contain menus");
                if (errorMsgs != null)
                {
                    foreach (string msg in errorMsgs) { Logger.Error(vaProxy, "Schema Error: " + msg); }
                }
                return false;
            }
            Logger.JsonWrite(vaProxy, "Verified menu JSON " + menuJsonPath + "\n against JSON schema " + menuJsonSchemaPath);

            // Verify and store version info from the JSON 
            if (!ValidateVersion(vaProxy, (JObject)menusDeserialized[0], "menus", out s_verPluginJSON, out s_verBMSJSON))
            { return false; }
            // Remove the first element of the JArray since it's not part of the menu
            int cTest = menusDeserialized.Count;
            menusDeserialized.RemoveAt(0);
            if (cTest - 1 != menusDeserialized.Count)
            {
                Logger.Error(vaProxy, "Unexpected failure removing head item (ver info) from menusDeserialized");
                return false;
            }
            Logger.JsonWrite(vaProxy, $"Loading JSON menu file for plugin version {s_verPluginJSON} and BMS version {s_verBMSJSON}");
            if (!s_version.Equals(s_verPluginJSON))
            {   Logger.Warning(vaProxy, $"Plugin version {s_version} does not match JSON version {s_verPluginJSON}"); }

            // Read through each menu and add details to our list
            s_menusAll = new Dictionary<string, MenuBMS>();
            s_DirCmdMap = new Dictionary<string, DirCmdInfo>();
            foreach (JObject menuJson in menusDeserialized) // <-- Note that here we used JObject instead of usual JProperty
            {
                JArray menuItemsJSON = (JArray)menuJson["menuItems"];
                int countMenuItems = menuItemsJSON.Count;
                MenuItemBMS[] menuItemsBMS = new MenuItemBMS[countMenuItems];
                for (int i = 0; i < countMenuItems; i++)
                {
                    JArray menuItemArray = (JArray)menuItemsJSON[i];
                    int countMenuArray = menuItemArray.Count;
                    if (countMenuArray < 2 || countMenuArray > 3)
                    {
                        Logger.Error(vaProxy, "Invalid number of items for a line-item menu");
                        return false;
                    }
                    MenuItemBMS menuItem = new MenuItemBMS(vaProxy, (string)menuItemArray[0], (string)menuItemArray[1], countMenuArray == 3 ? (string)menuItemArray[2] : null);
                    menuItemsBMS[i] = menuItem;
                }

                // Build menuShow from its json array into a semicolon delimited quoted element string
                JArray menuShowParts = (JArray)menuJson["menuShow"];
                string menuShow = string.Empty;
                if ( menuShowParts == null || menuShowParts.Count <= 0 || string.IsNullOrEmpty((string)menuShowParts[0]) )
                {
                    Logger.Error(vaProxy, "Invalid menuShow in JSON");
                    return false;
                }
                for (int iShow = 0; iShow < menuShowParts.Count; iShow++ )
                {
                    // A menuShow segment containing '[' will be assumed to be something to pass to "Variable Keypress" since it may have [LALT] etc.
                    // Otherwise it is assumed to be (multiple) simple keystrokes to pass to "Quick Input"
                    // HOWEVER, "Quick Input" assumes capital letters or shifted keys like '$' mean for it to hold the SHIFT key down
                    string menuShowPart = (string)menuShowParts[iShow];
                    if (!menuShowPart.Contains("["))
                    {
                        // Warn users if their "simple" text commands will actually generate SHIFT presses, but allow it through
                        string menuShowPartLower = menuShowPart.ToLower();
                        if (!menuShowPart.Equals(menuShowPartLower))
                        {
                            JToken tgtTok, menTok;  // Direct (string)menuJson["menuName"] returns null here - don't see why, but working around it
                            if (menuJson.TryGetValue("menuTarget", s_strComp, out tgtTok) && menuJson.TryGetValue("menuName", s_strComp, out menTok) )
                            {   Logger.Warning(vaProxy, "menuShow in JSON for " + tgtTok.ToString() + "_" + menTok.ToString() + " contains shifted characters"); }
                            else
                            { Logger.Warning(vaProxy, "menuShow in JSON contains shifted characters"); }
                        }
                    }
                    menuShow += "\"" + menuShowPart + "\"";
                    if (iShow <= menuShowParts.Count)
                    {   menuShow += ";";  }
                }

                MenuBMS menu = new MenuBMS
                (vaProxy, (string)menuJson["menuTarget"], (string)menuJson["targetPhrases"], (string)menuJson["menuName"], (string)menuJson["menuNamePhrases"], menuShow, menuItemsBMS);

                // Store menu in Dictionary with MenuFullID as key
                try
                {   s_menusAll.Add(menu.MenuFullID, menu);    }
                catch (System.ArgumentException e)
                {
                    Logger.Error(vaProxy, "Failed to add a menu with menuTarget_menuName of " + menu.MenuFullID);
                    Logger.Error(vaProxy, e.Message);
                    return false;
                }

                // Now that menu is fully validated, record map of its direct commmands to their matching dictionary and menuItem
                // TODO: Move this into a container for the list of menus?
                for (int i = 0; i < countMenuItems; i++)
                {
                    if (menuItemsBMS[i].HasDirectCmd())
                    {
                        string dirCmd = menuItemsBMS[i].MenuItemDirectCmd;
                        try
                        {   s_DirCmdMap.Add(dirCmd, new DirCmdInfo(menu.MenuFullID, i));  }
                        catch (System.ArgumentException e)
                        {
                            Logger.Warning(vaProxy, "Failed to add Direct Menu Cmd \"" + dirCmd + "\" probably due to duplicates");
                            Logger.Warning(vaProxy, e.Message);
                            // Don't fail for this - rest of system should be working
                        }
                    }
                }
            }

            if (!OneTimeCallsignLoad(vaProxy))
            {
                Logger.Error(vaProxy, "Error loading callsign data");
                return false;
            }

            // Flag set while listing out possible menus to show user
            vaProxy.SetBoolean(JBMSI_ListingMenus, false);

            Logger.Write(vaProxy, "OneTimeMenuDataLoad completed successfully");
            vaProxy.SetBoolean(JBMSI_Inited, true);
            return true;
         } // OneTimeMenuDataLoad()


        protected static MenuBMS GetMenuBMS(dynamic vaProxy, string menuFullID)
        {
            MenuBMS menu = null;
            if (string.IsNullOrEmpty(menuFullID))
            {
                Logger.Error(vaProxy, "Invalid menuFullID passed to GetMenuBMS");
                return null;
            }
            else if (!s_menusAll.TryGetValue(menuFullID, out menu))
            {
                Logger.Warning(vaProxy, "Unknown menuFullID passed to GetMenuBMS");
            }
            return menu;
        }

        protected static MenuBMS GetSetCurMenuBMS(dynamic vaProxy, string menuFullID)
        {
            // Returns the current menu if it matches, else sets the current menu to the menu for this FullID
            if (string.IsNullOrEmpty(menuFullID))
            {
                Logger.Error(vaProxy, "Invalid menuFullID passed to GetSetCurMenuBMS");
                return null;
            }

            // If we already have a current menu object, check if it's the same one we're looking for
            if (s_curMenu != null)
            {
                if (s_curMenu.MenuFullID == menuFullID) { return s_curMenu; }
                else { s_curMenu = null; }
            }

            s_menusAll.TryGetValue(menuFullID, out s_curMenu);
            // s_curMenu was already set to null, so return it whether this succeeded or not
            return s_curMenu;
        }

        protected static MenuBMS GetSetCurMenuBMS(dynamic vaProxy, string menuTarget, string menuName)
        {
            // Returns the current menu if it matches, else sets current menu to the menu for menuTarget+menuName
            if (string.IsNullOrEmpty(menuTarget) || string.IsNullOrEmpty(menuName))
            {
                Logger.Error(vaProxy, "Invalid menuTarget or menuName passed to GetSetCurMenuBMS");
                return null;
            }
            return GetSetCurMenuBMS(vaProxy, MenuBMS.MakeFullID(menuTarget, menuName));
        }

        protected static MenuBMS GetSetCurMenuBMS(dynamic vaProxy)
        {
            // Returns current menu if it matches JBMS_MenuTgt and JBMS_MenuName, else sets current menu to match and returns it
            string MenuTarget = vaProxy.GetText(JBMS_MenuTgt);
            string MenuName = vaProxy.GetText(JBMS_MenuName);
            if (string.IsNullOrEmpty(MenuTarget) || string.IsNullOrEmpty(MenuName))
            {
                Logger.Error(vaProxy, "GetSetCurMenuBMS without valid >JBMS_MENU_TGT or >JBMS_MENU_NAME");
                return null;
            }
            return GetSetCurMenuBMS(vaProxy, MenuTarget, MenuName);
        }

        protected static bool ExecuteCmdOnly(dynamic vaProxy, string cmdExecute, bool waitForReturn, bool asSubCommand = true)
        {
            // Executes the passed cmdString as a VA command phrase (if such a command exists)
            if (    !string.IsNullOrEmpty(cmdExecute)
                &&  vaProxy.CommandExists(cmdExecute))
            {
                // Tell VA to execute cmdOrKeys as a Command in the profile
                vaProxy.Command.Execute(cmdExecute, WaitForReturn: waitForReturn, AsSubcommand: asSubCommand);
                return true;
            }
            return false;
        }

        protected static bool PressKeyComboSingle(dynamic vaProxy, string keyComboSingle, bool waitForReturn, bool asSubCommand = true)
        {
            // Like PressKeyComboList, but for a single unquoted string that is not comma delimited - can handle modifiers or multiple keys but not both
            // TODO: May need to change this to handle Unicode or language variations
            // TODO: May need to change this to allow keys with modifiers to be sent: ie LCTRL+LSHIFT+T followed by CMD+C etc.
            if (!string.IsNullOrEmpty(keyComboSingle))
            {
                vaProxy.Command.Execute(CmdJBMS_PressKeyComboList, WaitForReturn: waitForReturn, AsSubcommand: asSubCommand, PassedText: $@"""{keyComboSingle}""");
                return true;
            }
            return false;
        }

        protected static bool PressKeyComboList(dynamic vaProxy, string keyComboList, bool waitForReturn, bool asSubCommand = true)
        {
            // Sends the passed keyComboList (semicolon delimited quoated strings) to VA to press those keys in sequence
            // TODO: May need to change this to handle Unicode or language variations
            // TODO: May need to change this to allow keys with modifiers to be sent: ie LCTRL+LSHIFT+T followed by CMD+C etc.
            if (!string.IsNullOrEmpty(keyComboList))
            {
                vaProxy.Command.Execute(CmdJBMS_PressKeyComboList, WaitForReturn: waitForReturn, AsSubcommand: asSubCommand, PassedText: keyComboList);
                return true;
            }
            return false;
        }

        protected static bool ExecuteCmdOrKeys(dynamic vaProxy, string cmdOrKeys, bool waitForReturn, bool asSubCommand = true)
        {
            // Executes as VA command phrase if one matches, else passes to VA to press the chars in the string as keystrokes
            if (string.IsNullOrEmpty(cmdOrKeys)) { return false; }

            if (vaProxy.CommandExists(cmdOrKeys))
            {   return ExecuteCmdOnly(vaProxy, cmdOrKeys, waitForReturn, asSubCommand);   }
            else
            {   return PressKeyComboSingle(vaProxy, cmdOrKeys, waitForReturn, asSubCommand);    }
        }

        protected static void ResetListMenus(dynamic vaProxy)
        {
            // Clear our "Listing Menus" state, but doesn't bring down the current menu
            vaProxy.SetBoolean(JBMSI_ListingMenus, false);
            s_menusToList = null;
            s_indexMenuToList = -1;
        }
        protected static void ListMenus(dynamic vaProxy)
        {
            // Called (possibly) multiple times to iterate through a list of menus as requested by user

            bool MenuUp = GetNonNullBool(vaProxy, JBMSI_MenuUp);

            if (s_menusToList == null || !GetNonNullBool(vaProxy, JBMSI_ListingMenus))
            {
                Logger.Warning(vaProxy, "ListMenus called when not in proper listing state");
                ResetListMenus(vaProxy); ;
                return;
            }

            if ( s_indexMenuToList >= s_menusToList.Count)
            {
                Logger.VerboseWrite(vaProxy, "Done listing menus");
                // Current menu *SHOULD* have been closed, but should verify it here
                if (MenuUp)
                {
                    Logger.Warning(vaProxy, "ListMenus at end of list, but last menu not closed properly");
                }
                ResetListMenus(vaProxy); ;
                return;
            }

            MenuBMS menu = s_menusToList.ElementAt(s_indexMenuToList++);
            if (menu != null)
            {
                // TODO - Maybe extend GetSetCurMenuBMS() to not make assumptons about these variables so can just do direct?
                vaProxy.SetText(JBMS_MenuTgt, menu.MenuTarget);
                vaProxy.SetText(JBMS_MenuName, menu.MenuName);
                ShowMenu(vaProxy, menuUp: MenuUp, listingMenus: true);
            }
            else
            {
                Logger.Error(vaProxy, "JBMS_LIST_MENUS failed to get correct menu info");
                ResetListMenus(vaProxy); ;
                return;
            }

        }

        protected static bool ShowMenu(dynamic vaProxy, bool menuUp = false, bool listingMenus = false)
        {
            // Brings down any currently menu (based on passed menuUp flag) and kills its "Wait For Response" listening command
            // Then displays the menu matching our curent stored settings
            // Then calls VA to listen for responses to the new menu

            MenuBMS Menu = GetSetCurMenuBMS(vaProxy);
            if (Menu == null)
            {
                Logger.Error(vaProxy, "JBMS_SHOW_MENU called without valid or matching MenuTarget or MenuName");
                // Set the menu failure state so VoiceAttack can notify user
                vaProxy.SetBoolean(JBMSI_NoSuchMenu, true);
                return false;
            }

            // Close the current menu regardless of if we have a menu up that is the same as the menu requested
            if (menuUp)
            {
                // Tell VA to kill the listening command then close them menu with ESC, but NOT call back plugin with "JBMS_RESET_MENU_STATE"
                Logger.Write(vaProxy, "JBMS_SHOW_MENU called when menu already up, cancelling listening and closing current menu");
                vaProxy.Command.Execute(CmdJBMS_KillWaitForMenuResponse, WaitForReturn: true, AsSubcommand: true);
                vaProxy.Command.Execute(CmdJBMS_CloseMenu, WaitForReturn: true, AsSubcommand: true);
                // Reset just the menu state related to it being up or having errors - already killed the "Wait For Menu Response"
                ResetMenuState(vaProxy, onlyUpAndErrors: true, killWaitForMenu: false);
            }

            PressKeyComboList(vaProxy, Menu.MenuShow, /* waitForReturn */ true);
            // Assume Success
            vaProxy.SetBoolean(JBMSI_MenuUp, true);

            // Only do the work to log available menu items if our flag is set
            if (Logger.MenuItems)
            {
                // We're showing the log in VoiceAttack, which *usually* inserts at the top so reverse it given the way it scrolls
                Stack<string> stackMenuDisplay = new Stack<string>();
                stackMenuDisplay.Push("    [[" + Menu.MenuTarget + "]] [[" + Menu.MenuName + "]]");

                foreach (MenuItemBMS item in Menu.MenuItemsBMS)
                {
                    stackMenuDisplay.Push("[" + item.MenuItemExecute + "]   " + item.MenuItemPhrases);
                }
                // If user reversed the log to run top to bottom reverse ourselves
                if (vaProxy.LogReversed) { stackMenuDisplay.Reverse(); }
                while (stackMenuDisplay.Count > 0)
                {
                    Logger.MenuWrite(vaProxy, stackMenuDisplay.Pop());
                }
            }

            // Async non-blocking wait & listen for phrases that match menu item choices (w/ timeout)
            // Plugin will be invoked with "JBMS_HANDLE_MENU_RESPONSE" if matching response heard, or when times out
            // Calling with AsSubcommand=false to allow calling back into ourselves as done while listing out multiple menus
            string AllMenuItemPhrases = Menu.AllMenuItemPhrases;
            vaProxy.Command.Execute(CmdJBMS_WaitForMenuResponse, WaitForReturn: false, AsSubcommand: false, PassedText: $@"""{AllMenuItemPhrases}""");

            return true;
        }

        public static void VA_Invoke1(dynamic vaProxy)
        {
            // Main method VoiceAttack profile calls into, with vaProxy.Context set to the command to do

            // Init if this is our first call
            if (!GetNonNullBool(vaProxy, JBMSI_Inited))
            {
                try
                {
                    OneTimeMenuDataLoad(vaProxy);
                    // If we failed to init, it should already be logged, so just return
                    if (!GetNonNullBool(vaProxy, JBMSI_Inited))
                        return;
                }
                catch (Exception e)
                {
                    // If an exception was thrown we might not even have Logger() inited, so log directly through vaProxy
                    // Most likely cause is not having the correct version of Newtonsoft.Json.dll as needed by Newtonsoft.Json.Schema.dll
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: ", "Red");
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: Did you forget to copy Newtonsoft.Json.dll to the VoiceAttack\\Shared\\Assemblies folder?", "Red");
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: ", "Red");
                    vaProxy.WriteToLog("EXCEPTION: " + e.Message, "Red");
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: ", "Red");
                    return;
                }
            }

            Logger.VerboseWrite(vaProxy, "Invoked with context " + vaProxy.Context);

            bool MenuUp = GetNonNullBool(vaProxy, JBMSI_MenuUp);

            // Note that we can only allow certain contexts while listing out multiple menus for user
            // See examples of how this is handled in JBMS_RESET_MENU_STATE and JBMS_SHOW_MENU
            // If adding new commands, states, or funcionality, consider if need to stop the listing first

            switch (vaProxy.Context)
            {
                case "JBMS_DO_INIT":
                    break;  // We init automatically on first call, so nothing to do

                case "JBMS_RESET_MENU_STATE":
                    // Should only be called when ESC has been pressed or current menu has already been closed
                    // NOTE: VA Profile currently calls this for ESC and for ANY number key, even if it doesn't match the menu items
                    // TODO: Test and verify if we need to filter these calls for only ones that match menu items?
                    Logger.VerboseWrite(vaProxy, "JBMS_RESET_MENU_STATE - ESC should have been pressed already");
                    // If we're in the process of listing menus, reset the listing so we won't continue showing them
                    if ( GetNonNullBool(vaProxy, JBMSI_ListingMenus) )
                    {   ResetListMenus(vaProxy);    }
                    // ResetMenuState will kill the current Wait For Menu Response, if it is running
                    ResetMenuState(vaProxy, onlyUpAndErrors: false, killWaitForMenu: true);
                    break;

                case "JBMS_RELOAD_LOG_SETTINGS":
                    // User or profile initiated reload of logging parameters
                    Logger.Write(vaProxy, "Reloading logging settings");
                    Logger.Json = GetNonNullBool(vaProxy, JBMS_JsonLog);
                    Logger.MenuItems = GetNonNullBool(vaProxy, JBMS_MenuLog);
                    Logger.Structures = GetNonNullBool(vaProxy, JBMS_StructLog);
                    Logger.Verbose = GetNonNullBool(vaProxy, JBMS_VerboseLog);
                    if (Logger.Verbose)
                    {
                        Logger.VerboseWrite(vaProxy, "JSON LOGGING: " + (Logger.Json ? "ON" : "OFF"));
                        Logger.VerboseWrite(vaProxy, "MENU LOGGING: " + (Logger.MenuItems ? "ON" : "OFF"));
                        Logger.VerboseWrite(vaProxy, "STRUCT LOGGING: " + (Logger.Structures ? "ON" : "OFF"));
                        Logger.VerboseWrite(vaProxy, "VERBOSE LOGGING: " + (Logger.Verbose ? "ON" : "OFF"));
                    }
                    break;

                case "JBMS_DIRECT_CMD":
                    // Plugin is asking us to execute a specific menu+menuItem without leaving the menu up
                    // No special check of JBMSI_ListingMenus needed since if listing has a menu up this will exit(break)
                    string dirCmd = vaProxy.GetText(JBMS_DirectCmd);
                    Logger.Write(vaProxy, "JBMS_DIRECT_CMD for " + dirCmd);
                    vaProxy.SetText(JBMS_DirectCmd, string.Empty);
                    if (string.IsNullOrEmpty(dirCmd))
                    {
                        Logger.Error(vaProxy, "JBMS_DIRECT_CMD received with empty >JBMS_DIRECT_CMD variable");
                        break;
                    }
                    Logger.VerboseWrite(vaProxy, "JBMS_DIRECT_CMD received for \"" + dirCmd + "\"");
                    if (MenuUp)
                    {
                        Logger.Write(vaProxy, "JBMS_DIRECT_CMD received for \"" + dirCmd + "\" while radio menu was up, discarding since menu choices have priority");
                        break;
                    }
                    try
                    {
                        // Find the matching menu and menuItem
                        DirCmdInfo dirCmdInfo = s_DirCmdMap[dirCmd];
                        MenuBMS menuForDC = s_menusAll[dirCmdInfo.MenuFullID];
                        MenuItemBMS[] menuItemsForDC = menuForDC.MenuItemsBMS;
                        MenuItemBMS menuItemForDC = menuItemsForDC[dirCmdInfo.IndexMenuItem];

                        bool ExecOK = false;
                        // Now we know the specific menu and which menu item, so we can bring up the menu 
                        ExecOK = ExecuteCmdOrKeys(vaProxy, menuForDC.MenuShow, waitForReturn: true);
                        if (!ExecOK) 
                        {
                            Logger.Error(vaProxy, "JBMS_DIRECT_CMD for \"" + dirCmd + "\" failed to bring up associated menu");
                            break;
                        }
                        // Don't set menu up state since we immediately run the menu item which should bring the menu down
                        ExecOK = ExecuteCmdOrKeys(vaProxy, menuItemForDC.MenuItemExecute, /* waitForReturn */ false);
                        if (!ExecOK)
                        {
                            Logger.Error(vaProxy, "JBMS_DIRECT_CMD for \"" + dirCmd + "\" brought up menu, but failed to execute menu item command");
                            break;
                        }
                    }
                    catch (KeyNotFoundException e) 
                    {
                        Logger.Warning(vaProxy, "JBMS_DIRECT_CMD received for \"" + dirCmd + "\" but failed to retrieve DirCmdMap or associated menu");
                        Logger.Warning(vaProxy, "Exception " + e.Message);
                        break;
                    }
                    break;

                case "JBMS_HANDLE_MENU_RESPONSE":
                    // Handle a valid, invalid, or timeout response from VA profile's CmdJBMS_WaitForMenuResponse
                    // Supports JBMSI_ListingMenus case internally (either continuing or cancelling)
                    string MenuResponse = vaProxy.GetText(JBMSI_MenuResponse);
                    if ( MenuUp && (s_curMenu != null) ) 
                    {
                        if (string.IsNullOrEmpty(MenuResponse))
                        {   Logger.VerboseWrite(vaProxy, "JBMS_HANDLE_MENU_RESPONSE received empty MenuResponse");  }
                        else if (MenuResponse == JBMS_MenuTimeout)
                        {
                            Logger.VerboseWrite(vaProxy, "JBMS_HANDLE_MENU_RESPONSE received _JBMS_MENU_TIMEOUT");
                            // Note that Menu is still up - it will get closed at the end of this handler
                        }
                        else
                        {
                            string MenuName = s_curMenu.MenuName;
                            string MenuItemExecute = s_curMenu.MenuItemExecuteFromPhrase(vaProxy, MenuResponse);
                            if (!string.IsNullOrEmpty(MenuItemExecute))
                            {
                                Logger.VerboseWrite(vaProxy, $"JBMS_HANDLE_MENU_RESPONSE with Menu: \"{MenuName}\"; Phrase: \"{MenuResponse}\"; Execute: \"{MenuItemExecute}\"");

                                if (GetNonNullBool(vaProxy, JBMSI_ListingMenus))
                                {
                                    // Since we're actually handling a command, stop listing out menus
                                    Logger.VerboseWrite(vaProxy, "Stopping listing menus since command received"); 
                                    ResetListMenus(vaProxy);
                                }

                                // NOTE: If MenuItemExecute is NOT just a keystroke that will close the menu, but is instead a VA command phrase
                                // THE VA command phrase ** NEEDS TO CLOSE THE RADIO MENU ITSELF **
                                // **NOT** by calling back into plugin, but with an appropriate generated keystroke
                                ExecuteCmdOrKeys(vaProxy, MenuItemExecute, /* waitForReturn */ true);

                                // Passing key/cmd should have brought down menu, so only reset our menu state - don't press escape
                                // Since we're being passed the menu response by CmdJBMS_WaitForMenuResponse we don't need to kill it
                                ResetMenuState(vaProxy, onlyUpAndErrors: false, killWaitForMenu: false);
                                MenuUp = false;
                            }
                            else
                            {   Logger.Write(vaProxy, "JBMS_HANDLE_MENU_RESPONSE found no match for MenuResponse \"" + MenuResponse + "\"");    }
                        }
                    }
                    else 
                    {
                        string MenuResponseSafe = string.IsNullOrEmpty(MenuResponse) ? string.Empty : MenuResponse;
                        Logger.Write(vaProxy, $"JBMS_HANDLE_MENU_RESPONSE recieved response \"{MenuResponseSafe}\" but no menu was still up");
                    }

                    if (MenuUp)
                    {
                        Logger.Write(vaProxy, "Closing menu after JBMS_HANDLE_MENU_RESPONSE without a match");
                        CloseMenu(vaProxy);
                    }
                    else
                    {
                        // Sanity check that our menu state is reset and we don't need to call ResetMenuState(vaProxy);
                        if (!VerifyMenuState(vaProxy, menuUp: false, noErrorsAllowed: true))
                        {
                            Logger.Warning(vaProxy, "ERROR: Expected menu to be in non-error closed state after JBMS_HANDLE_MENU_RESPONSE");
                            ResetMenuState(vaProxy);
                        }
                    }

                    // If the timeout was for a menu up while we were listing menus, we need to call ListMenus() again (its walking the list)
                    if ((MenuResponse == JBMS_MenuTimeout) && GetNonNullBool(vaProxy, JBMSI_ListingMenus))
                    {
                        Logger.VerboseWrite(vaProxy, "We are still listing out menus matching the users request, so put up the next one, if any");
                        vaProxy.SetText(JBMSI_MenuResponse, string.Empty);
                        // Note that current menu should have already been closed for us
                        ListMenus(vaProxy);
                        break;
                    }

                    break;
                
                case "JBMS_SHOW_MENU":
                    // TEMP TEST TEMP TEST
                    // PressKeyComboList(vaProxy, "\"Ok\";\"[LALT]F\";\"A\";\"?\"", waitForReturn: false);
                    // break;

                    // If user asks for a different menu while listing menus, we need to cancel the listing first
                    if (GetNonNullBool(vaProxy, JBMSI_ListingMenus))
                    {
                        ResetListMenus(vaProxy);
                    }
                    // ShowMenu will dismiss the current menu and kill the Wait For Menu Response handler
                    ShowMenu(vaProxy, menuUp: MenuUp, listingMenus: false);
                    break;

                case "JBMS_SET_CALLSIGN":
                    string callsign = vaProxy.Command.After();
                    if (string.IsNullOrEmpty(callsign)) 
                    {
                        Logger.Warning(vaProxy, "JBMS_SET_CALLSIGN called without a suffix wildcard for callsign");
                        return;
                    }
                    Logger.Write(vaProxy, $"Changing Callsign to: \"{callsign}\"");
                    vaProxy.SetText( JBMSI_Callsign, callsign );
                    // Note that changing callsign reloads profile so the callsign token can be re-evaluated into command phrases
                    ExecuteCmdOnly(vaProxy, CmdJBMS_SetCallsign, waitForReturn: false, asSubCommand: true);
                    break;

                case "JBMS_LIST_MENUS":
                    // Will build a list of menus matching the users phrase and iterate through them, showing one at a time
                    // User can stop the listing while menus are up by:
                    // - Waying a matching menuItemPhrase
                    // - Saying a phrase to bring up some other menu
                    // - Saying "Cancel", or "Reset Menu", or similar
                    // - Pressing a key such as ESC or a number key that brings down the current menu

                    if (GetNonNullBool(vaProxy, JBMSI_ListingMenus))
                    {
                        Logger.Error(vaProxy, "JBMS_LIST_MENUS called when already listing menus");
                        // TODO - Reset menu state?  Difficult to terminate
                        // Maybe do after provide a method to stop the listing...
                        break;
                    }
                    if (s_menusToList != null)
                    {
                        Logger.Error(vaProxy, "JBMS_LIST_MENUS called when list of menus to view already created?");
                        break;
                    }

                    string menuTargetPhrase = string.Empty; 
                    string menuNamePhrase = string.Empty;
                    string menuTargetNorm = string.Empty;
                    string menuNameNorm = string.Empty;
                    if (!GetNonNullText(vaProxy, JBMS_MenuTgt, out menuTargetPhrase) || !GetNonNullText(vaProxy, JBMS_MenuName, out menuNamePhrase) )
                    {
                        Logger.Warning(vaProxy, $"JBMS_LIST_MENUS called with invalid menu target phrase \"{menuTargetPhrase}\" or menu name phrase \"{menuNamePhrase}\"");
                        break;
                    }
                    // Now that we've cached the menuTargetPhrase and menuNamePhrase, bring down any menu that's currently up
                    if (MenuUp)
                    {
                        // Close the menu that is up - this will call ResetMenu to kill the current "Wait For Menu Response"
                        CloseMenu(vaProxy, pressEscape: true, onlyUpAndErrors: false);
                    }
                    MenuUp = false;
                    // All checks assume lowercase
                    menuTargetPhrase = menuTargetPhrase.ToLower();
                    menuNamePhrase = menuNamePhrase.ToLower();

                    bool targetAll, foundTarget;
                    bool menuAll, foundMenuName;
                    targetAll = foundTarget = menuTargetPhrase.Equals("all", s_strComp);
                    menuAll = foundMenuName = menuNamePhrase.Equals("all", s_strComp);

                    if (targetAll) { menuTargetNorm = menuTargetPhrase; }
                    if (menuAll) { menuNameNorm = menuNamePhrase; }

                    Logger.MenuWrite(vaProxy, $"JBMS_LIST_MENUS for menuTargetPhrase \"{menuTargetPhrase}\" and menuNamePhrase \"{menuNamePhrase}\"");

                    // Try to match the passed "normalized" menuTargetPhrase and menuNamePhrase ("Combat", not "Combat 1")
                    // to the actual normalized target and menuName.  For example, menuTargetPhrase "A Wax" matches to menuTargetNorm "awacs"
                    // Note don't want to iterate dictionary with ElementAt() since O(n^2)
                    foreach ( KeyValuePair<string, MenuBMS> kvp in s_menusAll )
                    {
                        MenuBMS menu = kvp.Value;
                        if ( !foundTarget && (menu.ContainsNormMenuTargetPhrase(menuTargetPhrase) || menuTargetPhrase.Equals(menu.MenuTargetNorm, s_strComp)) )
                        {
                            menuTargetNorm = menu.MenuTargetNorm;
                            foundTarget = true;
                        }
                        if (!foundMenuName && (menu.ContainsNormMenuNamePhrase(menuNamePhrase) || menuNamePhrase.Equals(menu.MenuNameNorm, s_strComp)) )
                        {
                            menuNameNorm = menu.MenuNameNorm;
                            foundMenuName = true;
                        }
                        if (foundTarget && foundMenuName) { break; }
                    }
                    if (!foundTarget || !foundMenuName)
                    {
                        Logger.Warning(vaProxy, $"No menuTarget found for \"{menuTargetPhrase}\" or menuName found for \"{menuNamePhrase}\"");
                        break;
                    }
                    Logger.MenuWrite(vaProxy, $"JBMS_LIST_MENUS matched to menuTargetNorm \"{menuTargetNorm}\" and menuNameNorm \"{menuNameNorm}\"");

                    // Now we have the normalized menuTarget and menuName for all the menus we should list out
                    // Store the list of matching menus
                    s_menusToList = new List<MenuBMS>();
                    foreach (KeyValuePair<string, MenuBMS> kvp in s_menusAll)
                    {
                        MenuBMS menu = kvp.Value;
                        if (    (targetAll || menuTargetNorm.Equals(menu.MenuTargetNorm, s_strComp))
                            &&  (menuAll || menuNameNorm.Equals(menu.MenuNameNorm, s_strComp)))
                        {
                            Logger.MenuWrite(vaProxy, $"Adding menu tgt=\"{menu.MenuTarget}\" name=\"{menu.MenuName}\" to list to display");
                            s_menusToList.Add(menu);
                        }
                    }
                    if (s_menusToList.Count <= 0)
                    {
                        Logger.Warning(vaProxy, $"No menus with normalized name \"{menuNameNorm}\" found for menuTargets \"{menuTargetNorm}\"");
                        s_menusToList = null;
                        break;
                    }

                    // TODO - Consider sorting list by menuTarget then menuName, or by menuShow number of letters for same letters

                    vaProxy.SetBoolean(JBMSI_ListingMenus, true);
                    s_indexMenuToList = 0;
                    // Start the first menu in the list showing
                    ListMenus(vaProxy);
                    break;

                default:
                    Logger.Error(vaProxy, "Called with unknown Context: " + vaProxy.Context);
                    throw new ArgumentException("Unknown vaProxy.Context");
            }
        }
    }
} // end Tanrr.VAPlugin.BMSRadio Namespace