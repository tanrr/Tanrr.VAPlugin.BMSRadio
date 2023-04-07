using System;
using System.Reflection;
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
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;


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

        protected static string s_version = "v0.2.0";
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
        protected const string JBMS_JsonLog =       ">JBMS_JSON_LOG";       // Boolean for logging of JSON parsing
        protected const string JBMS_MenuLog =       ">JBMS_MENU_LOG";       // Boolean for logging of menu items(shows list of menu items in log)
        protected const string JBMS_StructLog =     ">JBMS_STRUCT_LOG";     // Boolean for logging of data structures manipulation
        protected const string JBMS_VerboseLog =    ">JBMS_VERBOSE_LOG";    // Boolean for more verbose general logging

        protected const string JBMS_AudioFeedback = ">JBMS_AUDIO_FEEDBACK"; // Allows audible feedback to user

        protected const string JBMS_MenuTgt = ">JBMS_MENU_TGT";             // Holds menuTarget when plugin called w/ context "JBMS_SHOW_MENU"
        protected const string JBMS_MenuName = ">JBMS_MENU_NAME";           // Holds menuName when calling plugin w/ context "JBMS_SHOW_MENU"
        protected const string JBMS_DirectCmd = ">JBMS_DIRECT_CMD";         // Holds directCommand when calling plugin w/ context "JBMS_DIRECT_CMD"

        protected const string JBMSI_NoSuchMenu = ">JBMSI_NO_SUCH_MENU";    // Checked & set by "JBMS Radio Menu Show" command
        protected const string JBMSI_MenuResponse = ">JBMSI_MENU_RESPONSE"; // (Checked & set only by "JBMS Wait For Menu Response" command)

        protected const string JBMSI_ListingMenus = ">JBMSI_LISTING_MENUS"; // Boolean set while listing multiple menus
        protected const string JBMSI_MenuUp =       ">JBMSI_MENU_UP";       // Boolean set while menu is displayed and waiting for menu response
        protected const string JBMSI_Listening =    ">JBMSI_LISTENING";     // Boolean set while listening for menu items (regardless of menu being up or not)
        protected const string JBMSI_Version =      ">JBMSI_VERSION";       // Plugin Version


        protected const string JBMSI_CallsignsAWACS = ">>JBMSI_CALLSIGNS_AWACS";    // Possible AWACS Callsigns (no numbers)
        protected const string JBMSI_CallsignsJTAC =  ">>JBMSI_CALLSIGNS_JTAC";     // Possible JTAC callsigns (no numbers)
        protected const string JBMSI_CallsignsTankers=">>JBMSI_CALLSIGNS_TANKERS";  // Possible TANKER callsigns (no numbers)

        protected const string JBMSI_Callsign =       ">>JBMSI_CALLSIGN";           // Current callsign user has registered
        protected const string JBMSI_CallsignNum =    ">>JBMSI_CALLSIGN_NUM";       // Current callsign flight number (not position in flight) - can be empty
        protected const string JBMSI_CallsignPos =    ">>JBMSI_CALLSIGN_POS";       // Current callsign position in flight (1-4) - can be empty
        protected const string JBMSI_CSMatch =        ">>JBMSI_CS_MATCH";           // Phrase matching callsign with optional flight #: "[Fiend;Fiend 3]"
        protected const string JBMSI_CSMatchEmptyOK = ">>JBMSI_CS_MATCH_EMPTY_OK";  // Same as CSMatch, but allows matching nothing: "[Fiend;Fiend 3;]"
        protected const string JBMSI_ACType =         ">>JBMSI_AC_TYPE";            // "ef 16" or "ef 18" etc. - Read from callsign JSON

        protected const string JBMSI_ACTypeList =     ">>JBMSI_AC_TYPE_LIST";       // Semicolon delimited list of all possible aircraft types pilots can fly
        protected const string JBMSI_CallsignList =   ">>JBMSI_CALLSIGN_LIST";      // Semicolon delimited list of all pilot callsigns we're allowed to match
        protected const string JBMSI_CallsignsLoaded= ">>JBMSI_CALLSIGNS_LOADED";   // Boolean set after plugin loads callsigns from files
        protected const string JBMSI_CallsignsInited= ">>JBMSI_CALLSIGNS_INITED";   // Boolean set AFTER profile RE-init refreshes command phrases using >>JBMSI_CALLSIGN or >>JBMSI_CALLSIGN_LIST

        protected const string JBMSI_Init_Error = ">>JBMSI_INIT_ERROR";     // Failure during init, Don't try to reload again
        protected const string JBMSI_Inited = ">>JBMSI_INITED";             // Plugin initialized - stays set when switching profiles, unset when VA quits


        // VoiceAttack command phrases to execute from plugin
        protected const string CmdJBMS_WaitForMenuResponse =    "JBMS Wait For Menu Response";
        protected const string CmdJBMS_DirectListenStart =      "JBMS Direct Listen Start";
        protected const string CmdJBMS_CloseMenu =              "JBMS Close Menu";
        protected const string CmdJBMS_CloseMenuAlwaysTgtBMS =  "JBMS Close Menu Always Target BMS";
        protected const string CmdJBMS_PressKeyComboList =      "JBMS Press Key Combo List";
        protected const string CmdJBMS_KillWaitForMenuResponse= "JBMS Kill Command Wait For Menu Response";
        protected const string CmdJBMS_SetCallsign =            "JBMS Set Callsign";

        protected const string JBMS_MenuTimeout = "_JBMS_MENU_TIMEOUT";     // Possible JBMSI_MenuResponse;

        // Contexts passed to plugin's Invoke method
        protected const string JBMS_ContextDoInit = "JBMS_DO_INIT";
        protected const string JBMS_ContextReloadLogSettings =  "JBMS_RELOAD_LOG_SETTINGS";
        protected const string JBMS_ContextShowMenu = "JBMS_SHOW_MENU";
        protected const string JBMS_ContextListMenus = "JBMS_LIST_MENUS";

        // TODO - Forcing string comps to ignore case till decide lowercase vs mixed
        protected const StringComparison s_strComp = StringComparison.OrdinalIgnoreCase;

        protected static string s_aircraftTypes = string.Empty;
        protected static string s_awacsNames = string.Empty;   
        protected static string s_tankerNames = string.Empty;  
        protected static string s_jtacNames = string.Empty;    
        protected static string s_pilotCallsignsQuery = string.Empty;

        protected const string JBMS_KeysOnlyToBMS = ">JBMS_KEYS_ONLY_TO_BMS";    // User changeable value for whether keystrokes are not sent if Falcon BMS does not have focus
        protected const string JBMSI_FocusBMS = ">JBMSI_FOCUS_BMS";                // Set to TRUE by event handlers or init when BMS has focus

        protected const string JBMS_ProcNameFalconBMS = "Falcon BMS";
        private static dynamic s_proxy = null;   // cached reference to the proxy object for event handlers

        protected static Assembly s_assemblyJson = null;
        protected static Assembly s_assemblyJsonSchema = null;


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

            if (s_proxy != null)
            {
                // Stop any current LISTING of menus (resets static variables so iteration will stop)
                ResetListMenusNOKEYS(s_proxy);
            }
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

        protected static void KillWaitForMenuResponse(dynamic vaProxy, bool waitForReturn = true, bool asSubCommand = true)
        {
            // Tell VA to kill the listening command 
            vaProxy.Command.Execute(CmdJBMS_KillWaitForMenuResponse, waitForReturn, asSubCommand);
            vaProxy.SetBoolean(JBMSI_Listening, false);
        }

        // Note that (per the method name) this method should NOT cause any keypresses, as it may be called when BMS does not have focus
        protected static void ResetMenuStateNOKEYS(dynamic vaProxy, bool onlyUpAndErrors = false, bool killWaitForMenu = true)
        {
            // Resets our stored menu states, and cancels any pending Wait for Response in the VA profile
            // Clearing menu target and name can be overridden if needed

            Logger.VerboseWrite(vaProxy, "ResetMenuState");

            vaProxy.SetBoolean(JBMSI_NoSuchMenu, false);               // Set by plugin if it's called to load a target/menu pair that doesn't exist
            vaProxy.SetBoolean(JBMSI_MenuUp, false);                    // True only while menu is up (hopefully)
            // Kill "JBMS Wait For Menu Response" if its currently executing - 
            if (killWaitForMenu && vaProxy.Command.Active(CmdJBMS_WaitForMenuResponse))
            {
                // Kill the listening and resets JBMSI_Listening to false
                KillWaitForMenuResponse(vaProxy, waitForReturn: true, asSubCommand: true);
            }
            else
            {
                // If caller didn't set killWaitForMenu it means it's not listening anymore, so reset the associated variable
                vaProxy.SetBoolean(JBMSI_Listening, false);
            }
            // Set in JBMS Wait For Menu Response - clear in case it got set just before JBMS Wait was terminated
            vaProxy.SetText(JBMSI_MenuResponse, string.Empty);

            if (onlyUpAndErrors) { return; }

            vaProxy.SetText(JBMS_MenuTgt, string.Empty);
            vaProxy.SetText(JBMS_MenuName, string.Empty);
        }

        protected static void CloseMenu(dynamic vaProxy, bool pressEscape = true, bool onlyUpAndErrors = false)
        {
            // Tell VA to kill the listening command -  also sets JBMSI_Listening to false
            KillWaitForMenuResponse(vaProxy, waitForReturn: true, asSubCommand: true);
            // Tell VA to execute close the menu with ESC, but NOT call back plugin with "JBMS_RESET_MENU_STATE"
            if (pressEscape)
            {   vaProxy.Command.Execute(CmdJBMS_CloseMenu, WaitForReturn: true, AsSubcommand: true);    }
            // ResetMenuState - Don't need to kill WaitForMenu listening since did at beginning of this method
            ResetMenuStateNOKEYS(vaProxy, onlyUpAndErrors, killWaitForMenu: false);
        }

        public static void VA_Init1(dynamic vaProxy)
        {
            // Called once on VA load, and it is called asynchronously.
            // Since this is called BEFORE our profile loads and calls us, will should wait till we get hit with VA_Invoke1 to do our initialization
            // But do cache the proxy and set up any event handlers here
            s_proxy = vaProxy;
            s_proxy.ApplicationFocusChanged += new Action<System.Diagnostics.Process, String>(AppFocusChanged);
        }

        protected static void SafeCleanupWithoutFocus(dynamic vaProxy)
        {
            // Called only when BMS does NOT have focus, but previously might have had focus
            // Can only do "safe" things like changing variables, not keypresses or similar

            // If Listing Menus, reset the variables that keep us iterating through menus
            if (GetNonNullBool(vaProxy, JBMSI_ListingMenus))
            { ResetListMenusNOKEYS(vaProxy); }

            // If we currently have a menu up, clean up what we can
            if (GetNonNullBool(vaProxy, JBMSI_MenuUp))
            {
                // DON'T Send ESC to BMS since that could change focus during a focus change, causing this to be called again, etc.
                // Just reset menu state, which will kill Wait for Menu Response Listening - not optimal as leaves the menu up, but ok
                // NOTE: This doesn't appear to be safe to call (with killWaitForMenu) from the event handler
                ResetMenuStateNOKEYS(vaProxy, onlyUpAndErrors: false, killWaitForMenu: true);
            }
        }

        // Called up to 0.25 seconds AFTER there is a change of focus
        protected static void AppFocusChanged(System.Diagnostics.Process pFocus, String windowTitle)
        {
            // Bail if no valid proxy, or no focus, or if we haven't finished being initialized
            if (s_proxy == null || pFocus == null)
            { return; }

            string procActive = pFocus.ProcessName;
            if (!string.IsNullOrEmpty(procActive))
            {
                bool prevActiveBMS = GetNonNullBool(s_proxy, JBMSI_FocusBMS);
                bool curActiveBMS = procActive.Equals(JBMS_ProcNameFalconBMS, s_strComp);
                s_proxy.SetBoolean(JBMSI_FocusBMS, curActiveBMS);

                // TODO - Though only setting variables and stopping listen command, this can cause VA to hang
                // So, don't do anything else here.  If we get invoked, we'll call SafeCleanup then
                /*
                // If we haven't been fully initialized, don't do anything with our member functions
                if (!GetNonNullBool(s_proxy, JBMSI_Inited))
                { return; }

                if (prevActiveBMS && !curActiveBMS)
                {
                    SafeCleanupWithoutFocus(s_proxy);
                }
                */
            }
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

            // Load flight info - callsign, number, position, aircraft type
            try
            {
                JObject flightInfo = (JObject)csDeserialized["flightInfo"];
                if (flightInfo != null)
                {
                    string callsignFlight = (string)flightInfo["callsignFlight"];
                    string numberFlight = (string)flightInfo["numberFlight"];
                    string posInFlight = (string)flightInfo["posInFlight"];
                    string aircraftType = (string)flightInfo["aircraftType"];
                    if (string.IsNullOrEmpty(callsignFlight) || string.IsNullOrEmpty(aircraftType) || numberFlight == null || posInFlight == null)
                    {
                        Logger.Error(vaProxy, "Callsign JSON file doesn't have correct flightInfo section");
                        return false;
                    }
                    if ( callsignFlight.Contains(";") || callsignFlight.Contains(" "))
                    {
                        Logger.Error(vaProxy, $"callsignFlight \"{callsignFlight}\" contains a semicolon or space which makes it invalid");
                        return false;
                    }
                    vaProxy.SetText(JBMSI_Callsign, callsignFlight);
                    vaProxy.SetText(JBMSI_ACType, aircraftType);
                    try
                    {
                        if (numberFlight != string.Empty)
                        {
                            int num = int.Parse(numberFlight);
                            if (numberFlight.Length != 1 || num < 1 || num > 9)
                            {
                                Logger.Error(vaProxy, $"numbmerFlight \"{numberFlight}\" is not a valid string with a single digit 1-9");
                                return false;
                            }
                            vaProxy.SetText(JBMSI_CallsignNum, numberFlight);
                        }
                        if (posInFlight != string.Empty)
                        {
                            int pos = int.Parse(posInFlight);
                            if (numberFlight.Length != 1 || pos < 1 || pos > 4)
                            {
                                Logger.Error(vaProxy, $"posInFlight \"{posInFlight}\" is not a valid string with a single digit 1-4");
                                return false;
                            }
                            vaProxy.SetText(JBMSI_CallsignPos, posInFlight);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(vaProxy, "Exception working with callsign JSON file");
                        Logger.Error(vaProxy, e.Message);
                        Logger.Error(vaProxy, "Callsign JSON file doesn't contain a valid numberFlight or posInFlight - they must be strings containing a single digit");
                        return false;
                    }
                }
                else
                {
                    Logger.Error(vaProxy, "Callsign JSON file doesn't contain flightInfo section");
                    return false;
                }

                // Load all the other names/callsigns
                if (!BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["aircraftTypes"], spacesAllowed: true, builtMatchPhrase: out s_aircraftTypes))
                {   Logger.Error(vaProxy, "Failed to load aircrafty types from callsign JSON"); return false; }
                if ( !BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["awacsNames"], spacesAllowed: false, builtMatchPhrase: out s_awacsNames) )
                {   Logger.Error(vaProxy, "Failed to load AWACS callsigns from callsign JSON"); return false; }
                if (!BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["tankerNames"], spacesAllowed: false, builtMatchPhrase: out s_tankerNames))
                {   Logger.Error(vaProxy, "Failed to load TANKER callsigns from callsign JSON"); return false; }
                if (!BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["jtacNames"], spacesAllowed: false, builtMatchPhrase: out s_jtacNames))
                {   Logger.Error(vaProxy, "Failed to load JTAC callsigns from callsign JSON"); return false; }
                if (!BuildMatchingPhraseFromJArrayPhrases(vaProxy, (JArray)csDeserialized["pilotNames"], spacesAllowed: false, builtMatchPhrase: out s_pilotCallsignsQuery))
                { Logger.Error(vaProxy, "Failed to load PILOT callsigns from callsign JSON"); return false; }

                // Target callsign groups can't include duplicates callsigns from other target callsign groups
                // For example, no JTAC callsign can match a TANKER callsign (or command phrases could have ambiguous matches)
                // So check for duplicates between them
                HashSet<string> phraseBucket = new HashSet<string>();
                string csDupeTest = s_awacsNames + ";" + s_tankerNames + ";" + s_jtacNames;
                // split string for delimiter char then check all odd-numbered entries (since it won't have a ';' at either end)
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

            vaProxy.SetText(JBMSI_ACTypeList, s_aircraftTypes);
            vaProxy.SetText(JBMSI_CallsignList, s_pilotCallsignsQuery);
            vaProxy.SetText(JBMSI_CallsignsAWACS, s_awacsNames);
            vaProxy.SetText(JBMSI_CallsignsJTAC, s_jtacNames);
            vaProxy.SetText(JBMSI_CallsignsTankers, s_tankerNames);

            vaProxy.SetBoolean(JBMSI_CallsignsLoaded, true);

            return true;
        } // OneTimeCallsignLoad()

        protected static void ListNewtonsoftAssemblies(dynamic vaProxy, string note = null)
        {
            if (!string.IsNullOrEmpty(note))
            {   vaProxy.WriteToLog(note, "Purple");}

            // This should give us the actual assemblies loaded
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (loadedAssemblies == null) { return; }

            for (int i=0; i<loadedAssemblies.Length; i++)
            {
                Assembly assembly = loadedAssemblies[i];

                string assName = assembly.FullName;
                if (assName.Contains("Newtonsoft"))
                {
                    string assPath = assembly.Location;
                    vaProxy.WriteToLog($"LOADED: {assName} from {assPath}", "Purple");
                }
            }
        }

        protected static bool WarnIfOverCountMenu(dynamic vaProxy, MenuBMS menu, string headerMsg = "" )
        {
            int countMenuExtractedPhrases = menu.CountAllExtractedMenuItemPhrases;
            if (countMenuExtractedPhrases > 500)
            { 
                Logger.Error(vaProxy, $"{headerMsg} MENU {menu.MenuFullID} HAS PHRASE COUNT {countMenuExtractedPhrases} - OVER 500 LIMIT; SOME MENU ITEMS WONT BE RECOGNIZED!"); 
            }
            else if (countMenuExtractedPhrases >= 465)
            { 
                Logger.Warning(vaProxy, $"{headerMsg} MENU {menu.MenuFullID} HAS PHRASE COUNT {countMenuExtractedPhrases} - APPROACHING 500 LIMIT!"); 
            }
            else
            { 
                Logger.MenuWrite(vaProxy, $"{headerMsg} MENU {menu.MenuFullID} loaded - Extracted Phrase Count: {countMenuExtractedPhrases}"); 
            }
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

                string menuTarget = (string)menuJson["menuTarget"];
                string menuName = (string)menuJson["menuName"];
                string menuFullID = MenuBMS.MakeFullID(menuTarget, menuName);

                // Build menuShow from its json array into a semicolon delimited quoted element string
                JArray menuShowParts = (JArray)menuJson["menuShow"];
                string menuShow = string.Empty;
                if (menuShowParts == null || menuShowParts.Count <= 0 || string.IsNullOrEmpty((string)menuShowParts[0]))
                {
                    Logger.Error(vaProxy, "Invalid menuShow in JSON");
                    return false;
                }
                for (int iShow = 0; iShow < menuShowParts.Count; iShow++)
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
                            if (menuJson.TryGetValue("menuTarget", s_strComp, out tgtTok) && menuJson.TryGetValue("menuName", s_strComp, out menTok))
                            { Logger.Warning(vaProxy, "menuShow in JSON for " + tgtTok.ToString() + "_" + menTok.ToString() + " contains shifted characters"); }
                            else
                            { Logger.Warning(vaProxy, "menuShow in JSON contains shifted characters"); }
                        }
                    }
                    menuShow += "\"" + menuShowPart + "\"";
                    if (iShow <= menuShowParts.Count)
                    { menuShow += ";"; }
                }

                JArray menuItemsJSON = (JArray)menuJson["menuItems"];
                int countMenuItems = menuItemsJSON.Count;
                List<MenuItemBMS> menuItemsBMS = new List<MenuItemBMS>(countMenuItems);
                for (int i = 0; i < countMenuItems; i++)
                {
                    JArray menuItemArray = (JArray)menuItemsJSON[i];
                    int countMenuArray = menuItemArray.Count;
                    if (countMenuArray < 2 || countMenuArray > 3)
                    {
                        Logger.Error(vaProxy, "Invalid number of items for a line-item menu");
                        return false;
                    }
                    MenuItemBMS menuItem = new MenuItemBMS
                                                (vaProxy, 
                                                (string)menuItemArray[0], 
                                                (string)menuItemArray[1], 
                                                directCmd: countMenuArray == 3 ? (string)menuItemArray[2] : null,
                                                containgMenuFullID: menuFullID,
                                                containingMenuShow: menuShow);
                    menuItemsBMS.Add(menuItem);
                }

                MenuBMS menu = new MenuBMS
                    (vaProxy, 
                    menuTarget, 
                    (string)menuJson["targetPhrases"], 
                    menuName, 
                    (string)menuJson["menuNamePhrases"], 
                    menuShow, 
                    menuItemsBMS,
                    isDirectMenu: (bool)menuJson["isDirectMenu"],
                    isListingMenu: true,
                    isGroupMenu: false,
                    directMenuGroup: (string)menuJson["directMenuGroup"]);

                // Store menu in Dictionary with MenuFullID as key
                try
                {   
                    // Add the menu and verify its details
                    s_menusAll.Add(menu.MenuFullID, menu);
                    WarnIfOverCountMenu(vaProxy, menu);

                    // If this menu is part of a directMenuGroup, store a COPY of it too
                    if (!string.IsNullOrEmpty(menu.DirectMenuGroup))
                    {
                        try
                        {
                            MenuBMS menuGroup = null;
                            // Need to look up the GROUP version of the FullID (example: "group_awacs")
                            if (s_menusAll.ContainsKey(menu.DirectMenuGroupFullID()))
                            {
                                // We have an existing group already, so just add our additional menuItems to it
                                menuGroup = s_menusAll[menu.DirectMenuGroupFullID()];
                                if (!menuGroup.AddMenuItemsToGroupMenu(vaProxy, menu))
                                {
                                    Logger.Error(vaProxy, $"Error appending to existing DirectMenuGroup {menu.DirectMenuGroup} from menu {menu.MenuFullID}");
                                }
                            }
                            else
                            {
                                // We don't have a group menu for this menuGroup yet, so make one
                                // Note that MenuBMS ctor will duplicate the menuItemsBMS list (shallow copy) since unlike other menus it can add more menuItems later
                                menuGroup = new MenuBMS
                                    (vaProxy,
                                    "group",
                                    "group",
                                    menu.DirectMenuGroup,
                                    menu.DirectMenuGroup,
                                    show: "",                   // No Show command for group menus 
                                    items: menuItemsBMS,        // Copy over our current list of menu items
                                    isDirectMenu: true,
                                    isListingMenu: false,
                                    isGroupMenu: true,
                                    directMenuGroup: menu.DirectMenuGroup);
                                s_menusAll.Add(menu.DirectMenuGroupFullID(), menuGroup);
                            }
                            if (null != menuGroup)
                            { WarnIfOverCountMenu(vaProxy, menuGroup, "GROUP"); }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(vaProxy, $"Exception adding or adding to DirectMenuGroup {menu.DirectMenuGroup} from menu {menu.MenuFullID}");
                            Logger.Error(vaProxy, $"EXCEPTION MESSAGE: {e.Message}");
                        }
                    }
                }
                catch (System.ArgumentException e)
                {
                    Logger.Error(vaProxy, "Failed to add a menu with menuTarget_menuName of " + menu.MenuFullID);
                    Logger.Error(vaProxy, e.Message);
                    return false;
                }

                // TODO: Move this into a container for the list of menus?
                // Now that menu is fully validated, check it for direct commands
                for (int i = 0; i < countMenuItems; i++)
                {
                    // Record map of matching dictionary and menuItem to any direct command
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

        // Note (per method name) this method should NOT cause any keypresses
        protected static void ResetListMenusNOKEYS(dynamic vaProxy)
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
                ResetListMenusNOKEYS(vaProxy); ;
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
                ResetListMenusNOKEYS(vaProxy); ;
                return;
            }

            MenuBMS menu = s_menusToList.ElementAt(s_indexMenuToList++);
            if (menu != null)
            {
                // TODO - Maybe extend GetSetCurMenuBMS() to not make assumptons about these variables so can just do direct?
                vaProxy.SetText(JBMS_MenuTgt, menu.MenuTarget);
                vaProxy.SetText(JBMS_MenuName, menu.MenuName);
                ShowMenu(vaProxy, listingMenus: true, directMenuExpected: false);
            }
            else
            {
                Logger.Error(vaProxy, $"{JBMS_ContextListMenus} failed to get correct menu info");
                ResetListMenusNOKEYS(vaProxy); ;
                return;
            }

        }

        protected static bool ShowMenu(dynamic vaProxy, bool listingMenus = false, bool directMenuExpected = false)
        {
            // Brings down any currently menu (based on passed menuUp flag)
            // If any Listening is going on, kills its "Wait For Response" listening command
            // Then displays the menu matching our curent stored settings
            // Then calls VA to listen for responses to the new menu

            bool menuUp = GetNonNullBool(vaProxy, JBMSI_MenuUp);
            bool listening = GetNonNullBool(vaProxy, JBMSI_Listening);

            MenuBMS Menu = GetSetCurMenuBMS(vaProxy);
            if (Menu == null)
            {
                Logger.Error(vaProxy, "JBMS_SHOW_MENU called without valid or matching MenuTarget or MenuName");
                // Set the menu failure state so VoiceAttack can notify user
                vaProxy.SetBoolean(JBMSI_NoSuchMenu, true);
                return false;
            }

            // Close any menu that's currently up or being listened for, even if it is the same as the menu as requested
            if (menuUp || listening)
            {
                Logger.VerboseWrite(vaProxy, "ShowMenu() called when menu already up or listening, cancelling listening and closing any menu up");
                // Note that this is passed with onlyUpAndErrors true, so will leave >JMBSI_MENU_TGT and >JMBSI_MENU_NAME - those hold the new menu to bring up
                CloseMenu(vaProxy, pressEscape: menuUp, onlyUpAndErrors: true);
            }

            // Direct menus are only shown if we're listing menus and they're listable
            if (!Menu.IsDirectMenu || (listingMenus && Menu.IsListingMenu))
            {
                if (!Menu.IsDirectMenu && directMenuExpected)
                {   Logger.Warning(vaProxy, $"ShowMenu() called for {Menu.MenuFullID} as a Direct Menu, but this menu is not a Direct Menu"); }
                // else Getting a direct Menu without expecting it is ok

                // Don't wait for all keypresses since want to listen for menu items as soon as possible (and user speech will take long enough)
                PressKeyComboList(vaProxy, Menu.MenuShow, /* waitForReturn */ false);
                // Assume Success
                vaProxy.SetBoolean(JBMSI_MenuUp, true);
            }

            // Only do the work to log available menu items if our flag is set
            if (Logger.MenuItems)
            {
                // We're showing the log in VoiceAttack, which *usually* inserts at the top so reverse it given the way it scrolls
                Stack<string> stackMenuDisplay = new Stack<string>();
                if (Menu.IsDirectMenu)
                {
                    if (Menu.MenuTarget.Equals("group"))
                    {   stackMenuDisplay.Push($"    GROUP: [[{Menu.DirectMenuGroup}]]"); }
                    else
                    {   stackMenuDisplay.Push($"DIRECT: [[{Menu.MenuTarget}]] [[{Menu.MenuName}]]"); }
                }
                else
                {   
                    stackMenuDisplay.Push($"    [[{Menu.MenuTarget}]] [[{Menu.MenuName}]]"); 
                }

                foreach (MenuItemBMS item in Menu.MenuItemsBMS)
                {
                    stackMenuDisplay.Push($"{item.ContainingMenuFullID} : [{item.MenuItemExecute}]   {item.MenuItemPhrases}");
                }
                // If user reversed the log to run top to bottom reverse ourselves
                if (vaProxy.LogReversed) { stackMenuDisplay.Reverse(); }
                while (stackMenuDisplay.Count > 0)
                {
                    Logger.MenuWrite(vaProxy, stackMenuDisplay.Pop());
                }
            }

            // Async non-blocking wait & listen for phrases that match menu item choices (w/ timeout)
            // Plugin will be invoked with "JBMS_HANDLE_MENU_RESPONSE" if matching response received, or when times out
            // Calling with AsSubcommand=false to allow calling back into ourselves as done while listing out multiple menus
            vaProxy.SetBoolean(JBMSI_Listening, true);
            vaProxy.Command.Execute(CmdJBMS_WaitForMenuResponse, WaitForReturn: false, AsSubcommand: false, PassedText: $@"""{Menu.AllMenuItemPhrases}""");

            return true;
        } // ShowMenu

        public static void VA_Invoke1(dynamic vaProxy)
        {
            // Main method VoiceAttack profile calls into, with vaProxy.Context set to the command to do

            // Don't allow invoke if we failed during init already
            if (GetNonNullBool(vaProxy, JBMSI_Init_Error))
            {   return; }

            // Init if this is our first call
            if (!GetNonNullBool(vaProxy, JBMSI_Inited))
            {
                // Log which Newtonsoft assemblies are loaded  - Do before OneTimeMenuDataLoad() which could fail with mismatched JSON/Schema versions, 
                // Note that we may get more than one version of Newtonsoft.Json.dll and this will help track it down
                ListNewtonsoftAssemblies(vaProxy, "JBMS Newtonsoft Assemblies:");

                try
                {
                    OneTimeMenuDataLoad(vaProxy);
                    // If we failed to init, it should already be logged, so set >>JBMSI_INIT_ERROR so profile won't keep trying to load us and return
                    if (!GetNonNullBool(vaProxy, JBMSI_Inited))
                    {
                        vaProxy.SetBoolean(JBMSI_Init_Error, true);
                        return;
                    }
                }
                catch (Exception e)
                {
                    // If an exception was thrown we might not even have Logger() inited, so log directly through vaProxy
                    // Most likely cause is not having the correct version of Newtonsoft.Json.dll as needed by Newtonsoft.Json.Schema.dll

                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: ", "Red");
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: If Newtonsoft.Json.dll already copied, try copying VoiceAttack.exe.config from plugin folder to VoiceAttack.exe's folder", "Red");
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: or if error involves Newtonsoft.Json.Schema.SchemaExtensions.IsValid() might be older version of Newtonsoft.Json.dll installed", "Red");
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: Did you forget to copy Newtonsoft.Json.dll to the VoiceAttack\\Shared\\Assemblies folder?", "Red");
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: ", "Red");
                    vaProxy.WriteToLog("EXCEPTION: " + e.Message, "Red");
                    vaProxy.WriteToLog("JeevesBMSRadio: ERROR INIT: ", "Red");
                    vaProxy.SetBoolean(JBMSI_Init_Error, true);
                    return;
                }
            }

            // Our AppFocusChanged() is called by VA when active window changes, but it only polls every 1/4 second
            // AND if the user doesn't have VoiceAttack's "Auto Profile Switching" enabled no event polling is done
            // Thus better to do a focus check each time the plugin is called from the profile
            bool keysOnlyToBMS = GetNonNullBool(vaProxy, JBMS_KeysOnlyToBMS);
            bool procActiveBMS = GetNonNullBool(vaProxy, JBMSI_FocusBMS);
            if (keysOnlyToBMS || vaProxy.Context == "JBMS_CHECK_BMS_FOCUS")
            {
                string procActive = vaProxy.Utility.ActiveWindowProcessName();
                if (!string.IsNullOrEmpty(procActive))
                {
                    procActiveBMS = procActive.Equals(JBMS_ProcNameFalconBMS, s_strComp);
                    vaProxy.SetBoolean(JBMSI_FocusBMS, procActiveBMS);

                    if (vaProxy.Context == "JBMS_CHECK_BMS_FOCUS")
                    {
                        // We were only called to check if BMS had the focus, so return
                        return;
                    }

                    if (!procActiveBMS)
                    {
                        // Leave the duplicate check of !procActiveBMS below, as we might add other cases
                        if (!procActiveBMS && keysOnlyToBMS)
                        {
                            // Init is done on first call into invoke, above, so nothing to do and don't need to log a warning
                            if (vaProxy.Context == JBMS_ContextDoInit)
                            { return; }

                            // If BMS had focus but has now lost focus cleanup menu state and stop any listening
                            // Note this could possibly muck up valid calls to the plugin,
                            // but erring on side of cleaning up vs leaving plugin listening while other app has focus
                            Logger.VerboseWrite(vaProxy, $"Plugin invoked with context=\"{vaProxy.Context}\" when Falcon BMS does not have focus and >JBMSI_KEYS_ONLY_TO_BMS true");
                            Logger.VerboseWrite(vaProxy, "Cleaning up any left over menu state and listing menu state and exiting");
                            SafeCleanupWithoutFocus(vaProxy);
                            // Bail out, unless user is changing log settings (since user might be changing settings TO allow keystrokes outside BMS)
                            if (vaProxy.Context != JBMS_ContextReloadLogSettings)
                                return;
                        }
                    }
                }
            }

            Logger.VerboseWrite(vaProxy, "Invoked with context " + vaProxy.Context);

            bool MenuUp = GetNonNullBool(vaProxy, JBMSI_MenuUp);
            bool Listening = GetNonNullBool(vaProxy, JBMSI_Listening);
            bool directMenuExpected = false; // Only set to TRUE for SHOW_MENU_DIRECT
            if (MenuUp && !Listening)
            {
                Logger.Warning(vaProxy, $"Plugin invoked with context=\"{vaProxy.Context}\" but invalid state of MenuUp but NOT Listening");
            }

            // Note that we can only allow certain contexts while listing out multiple menus for user
            // See examples of how this is handled in JBMS_RESET_MENU_STATE and JBMS_SHOW_MENU
            // If adding new commands, states, or funcionality, consider if need to stop the listing first

            switch (vaProxy.Context)
            {
                case JBMS_ContextDoInit:
                    break;  // We init automatically on first call, so nothing to do

                case "JBMS_RESET_MENU_STATE":
                    // Should only be called when ESC has been pressed or current menu has already been closed
                    // NOTE: VA Profile currently calls this for ESC and for ANY number key, even if it doesn't match the menu items
                    // TODO: Have the profile pass the key pressed so we can check it against the current menu items?
                    Logger.VerboseWrite(vaProxy, "JBMS_RESET_MENU_STATE - ESC should have been pressed already");
                    // If we're in the process of listing menus, reset the listing so we won't continue showing them
                    if ( GetNonNullBool(vaProxy, JBMSI_ListingMenus) )
                    {   ResetListMenusNOKEYS(vaProxy);    }
                    // ResetMenuState will kill the current Wait For Menu Response, if it is running
                    ResetMenuStateNOKEYS(vaProxy, onlyUpAndErrors: false, killWaitForMenu: true);
                    break;

                case JBMS_ContextReloadLogSettings:
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
                    Logger.MenuWrite(vaProxy, "JBMS_DIRECT_CMD for " + dirCmd);

                    if (!procActiveBMS && keysOnlyToBMS)
                    {
                        Logger.VerboseWrite(vaProxy, "Exiting since BMS does not have focus and >JBMS_KEYS_ONLY_TO_BMS = true");
                        break;
                    }

                    vaProxy.SetText(JBMS_DirectCmd, string.Empty);
                    if (string.IsNullOrEmpty(dirCmd))
                    {
                        Logger.Error(vaProxy, "JBMS_DIRECT_CMD received with empty >JBMS_DIRECT_CMD variable");
                        break;
                    }
                    Logger.VerboseWrite(vaProxy, "JBMS_DIRECT_CMD received for \"" + dirCmd + "\"");
                    if (MenuUp || Listening)
                    {
                        Logger.MenuWrite(vaProxy, "JBMS_DIRECT_CMD received for \"" + dirCmd + "\" while radio menu up or listening for DirectMenu, discarding since menu choices have priority");
                        break;
                    }
                    try
                    {
                        // Find the matching menu and menuItem
                        DirCmdInfo dirCmdInfo = s_DirCmdMap[dirCmd];
                        MenuBMS menuForDC = s_menusAll[dirCmdInfo.MenuFullID];
                        List<MenuItemBMS> menuItemsForDC = menuForDC.MenuItemsBMS;
                        MenuItemBMS menuItemForDC = menuItemsForDC[dirCmdInfo.IndexMenuItem];

                        bool ExecOK = false;
                        // Now we know the specific menu and which menu item, so we can bring up the menu 
                        ExecOK = PressKeyComboList(vaProxy, menuForDC.MenuShow, /* waitForReturn */ true);
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

                    if (!procActiveBMS && keysOnlyToBMS)
                    {
                        Logger.Warning(vaProxy, "JBMS_HANDLE_MENU_RESPONSE received while BMS does not have focus and >JBMS_KEYS_ONLY_TO_BMS = true");
                        Logger.Warning(vaProxy, "Unexpected since menu listening is stopped when BMS loses focus");
                        Logger.Warning(vaProxy, "- Possible user does not have VoiceAttack Option \"Auto Profile Switching\" Enabled?");
                        break;
                    }

                    // Handle a valid, invalid, or timeout response from VA profile's CmdJBMS_WaitForMenuResponse
                    // Supports JBMSI_ListingMenus case internally (either continuing or cancelling)
                    // Supports responses while listening for isDirectMenu menus (listening but no menu up)
                    string MenuResponse = vaProxy.GetText(JBMSI_MenuResponse);
                    if ( (MenuUp || Listening) && (s_curMenu != null) ) 
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
                            MenuItemShort menuItemShort = s_curMenu.MenuItemShortFromPhrase(vaProxy, MenuResponse);
                            if (!string.IsNullOrEmpty(menuItemShort.MenuItemExecute))
                            {
                                Logger.VerboseWrite(vaProxy, $"JBMS_HANDLE_MENU_RESPONSE with Menu: \"{MenuName}\"; Phrase: \"{MenuResponse}\"; Execute: \"{menuItemShort.MenuItemExecute}\"");

                                if (GetNonNullBool(vaProxy, JBMSI_ListingMenus))
                                {
                                    // Since we're actually handling a command, stop listing out menus
                                    Logger.VerboseWrite(vaProxy, "Stopping listing menus since command received"); 
                                    ResetListMenusNOKEYS(vaProxy);
                                }

                                if (!MenuUp && Listening)
                                {
                                    if (!s_curMenu.IsDirectMenu)
                                    {   Logger.Warning(vaProxy, "Unexpected JBMS_HANDLE_MENU_RESPONSE without MenuUp and without being a DirectMenu");  }
                                    // Put UP menu this menuItem matches to so the following MenuItemExecute works on the appropriate menu
                                    // Note that we have to wait for this to finish otherwise the correct menu might not be up when the menuItem is keyed
                                    PressKeyComboList(vaProxy, menuItemShort.ContainingMenuShow, /* waitForReturn */ true);
                                    // Don't set menu up state, since the following command should bring the menu down
                                }

                                // NOTE: If MenuItemExecute is NOT just a keystroke that will close the menu, but is instead a VA command phrase
                                // THE VA command phrase ** NEEDS TO CLOSE THE RADIO MENU ITSELF **
                                // **NOT** by calling back into plugin, but with an appropriate generated keystroke
                                ExecuteCmdOrKeys(vaProxy, menuItemShort.MenuItemExecute, /* waitForReturn */ true);

                                // Passing key/cmd should have brought down menu, so only reset our menu state - don't press escape
                                // Since we're being passed the menu response by CmdJBMS_WaitForMenuResponse we don't need to kill it,
                                // This will set JBMSI_Listening to false, but need to set our own MenuUp and Listening to false as well
                                ResetMenuStateNOKEYS(vaProxy, onlyUpAndErrors: false, killWaitForMenu: false);
                                vaProxy.SetBoolean(JBMSI_Listening, false);
                                MenuUp = Listening = false;
                            }
                            else
                            {   Logger.Write(vaProxy, "JBMS_HANDLE_MENU_RESPONSE found no match for MenuResponse \"" + MenuResponse + "\"");    }
                        }
                    }
                    else 
                    {
                        string MenuResponseSafe = string.IsNullOrEmpty(MenuResponse) ? string.Empty : MenuResponse;
                        Logger.Write(vaProxy, $"JBMS_HANDLE_MENU_RESPONSE recieved response \"{MenuResponseSafe}\" but no menu was still being listened for");
                    }

                    if (MenuUp || Listening)
                    {
                        Logger.Write(vaProxy, "Closing menu after JBMS_HANDLE_MENU_RESPONSE without a match");
                        // Only send an ESC if we actually had a menu up, vs just listening for a direct/group menu
                        CloseMenu(vaProxy, pressEscape: MenuUp, onlyUpAndErrors: false);
                        MenuUp = Listening = false;
                    }
                    else
                    {
                        // Sanity check that our menu state is reset and we don't need to call ResetMenuState(vaProxy);
                        if (!VerifyMenuState(vaProxy, menuUp: false, noErrorsAllowed: true))
                        {
                            Logger.Warning(vaProxy, "ERROR: Expected menu to be in non-error closed state after JBMS_HANDLE_MENU_RESPONSE");
                            ResetMenuStateNOKEYS(vaProxy);
                            MenuUp = Listening = false;
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

                case "JBMS_DIRECT_MENU":

                    // DIRECT_MENU is just normal SHOW_MENU for a menu that *should* have _isDirectMenu == true
                    directMenuExpected = true;
                    goto case JBMS_ContextShowMenu;

                case JBMS_ContextShowMenu:
                    if (!procActiveBMS && keysOnlyToBMS)
                    {
                        Logger.VerboseWrite(vaProxy, $"Exiting {vaProxy.Context} since BMS does not have focus and >JBMS_KEYS_ONLY_TO_BMS = true");
                        break;
                    }
                    // If user asks for a different menu while listing menus, we need to cancel the listing first
                    if (GetNonNullBool(vaProxy, JBMSI_ListingMenus))
                    {
                        ResetListMenusNOKEYS(vaProxy);
                    }
                    // ShowMenu will dismiss the current menu and kill the Wait For Menu Response handler
                    ShowMenu(vaProxy, listingMenus: false, directMenuExpected: directMenuExpected) ;
                    break;

                case JBMS_ContextListMenus:
                    // Will build a list of menus matching the users phrase and iterate through them, showing one at a time
                    // User can stop the listing while menus are up by:
                    // - Waying a matching menuItemPhrase
                    // - Saying a phrase to bring up some other menu
                    // - Saying "Cancel", or "Reset Menu", or similar
                    // - Pressing a key such as ESC or a number key that brings down the current menu

                    if (!procActiveBMS && keysOnlyToBMS)
                    {
                        Logger.Warning(vaProxy, $"{JBMS_ContextListMenus} received - Exiting since BMS does not have focus and >JBMS_KEYS_ONLY_TO_BMS = true");
                        break;
                    }

                    if (GetNonNullBool(vaProxy, JBMSI_ListingMenus))
                    {
                        Logger.Error(vaProxy, $"{JBMS_ContextListMenus} called when already listing menus");
                        // TODO - Reset menu state?  Difficult to terminate
                        // Maybe do after provide a method to stop the listing...
                        break;
                    }
                    if (s_menusToList != null)
                    {
                        Logger.Error(vaProxy, $"{JBMS_ContextListMenus} called when list of menus to view already created?");
                        break;
                    }

                    string menuTargetPhrase = string.Empty; 
                    string menuNamePhrase = string.Empty;
                    string menuTargetNorm = string.Empty;
                    string menuNameNorm = string.Empty;
                    if (!GetNonNullText(vaProxy, JBMS_MenuTgt, out menuTargetPhrase) || !GetNonNullText(vaProxy, JBMS_MenuName, out menuNamePhrase) )
                    {
                        Logger.Warning(vaProxy, $"{JBMS_ContextListMenus} called with invalid menu target phrase \"{menuTargetPhrase}\" or menu name phrase \"{menuNamePhrase}\"");
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

                    Logger.MenuWrite(vaProxy, $"{JBMS_ContextListMenus} for menuTargetPhrase \"{menuTargetPhrase}\" and menuNamePhrase \"{menuNamePhrase}\"");

                    // Try to match the passed "normalized" menuTargetPhrase and menuNamePhrase ("Combat", not "Combat 1")
                    // to the actual normalized target and menuName.  For example, menuTargetPhrase "A Wax" matches to menuTargetNorm "awacs"
                    // Note don't want to iterate dictionary with ElementAt() since O(n^2)
                    foreach ( KeyValuePair<string, MenuBMS> kvp in s_menusAll )
                    {
                        MenuBMS menu = kvp.Value;
                        // Skip menus that don't want to be listed
                        if (!menu.IsListingMenu) { continue; }

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
                    Logger.MenuWrite(vaProxy, $"{JBMS_ContextListMenus} matched to menuTargetNorm \"{menuTargetNorm}\" and menuNameNorm \"{menuNameNorm}\"");

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