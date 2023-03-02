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


namespace Tanrr.VAPlugin.BMSRadio
{
    // JBMS Generated GUIDS:
    // G1 7e22363e-2cca-4b26-8aae-a292f73d2a53
    // G2 16223078-853e-41fb-a741-3e9cacfe94d9
    // G3 da2b13d9-f5fd-47e3-93af-b5c9e089b5a8

    public struct DirCmdIndexes
    {
        public int IndexMenu { get; }
        public int IndexMenuItem { get; }
        public DirCmdIndexes(int indexMenu, int indexMenuItem)
        {
            this.IndexMenu = indexMenu; 
            this.IndexMenuItem = indexMenuItem;
        }
    };

    public class Jeeves_BMS_Radio_Plugin 
    {
        protected static List<MenuBMS> s_menusBMS = null;
        protected static MenuBMS s_curMenuBMS = null;

        protected static Dictionary<string, DirCmdIndexes> s_DirCmdMap = new Dictionary<string, DirCmdIndexes>();
       
        public static MenuBMS CurMenuBMS 
        { 
            get => s_curMenuBMS;
            set => s_curMenuBMS = value; 
        }

        public static string VA_DisplayName()
            => "Tanrr VA BMS Radio Plugin v0.5";  // Displayed in dropdowns and log as plugin name

        public static string VA_DisplayInfo()
            => "Tanrr BMS Radio Plugin for VoiceAttack\r\n\r\nInitial POC.\r\n\r\n2023";  // Extended info

        public static Guid VA_Id()
            => new Guid("{7e22363e-2cca-4b26-8aae-a292f73d2a53}");  // TANR - DONE as G1

        //this function is called from VoiceAttack when the 'stop all commands' button is pressed or a, 'stop all commands' action is called.  this will help you know if processing needs to stop if you have long-running code
        public static void VA_StopCommand()  
        {
            // TODO - Do Something Here??
        }

        public static bool GetNonNullBool(dynamic vaProxy, string propName)
        {
            if (string.IsNullOrEmpty(propName))
                return false;

            return vaProxy.GetBoolean(propName) ?? false;
        }

        public static void SetSharedVarsDefaults(dynamic vaProxy)
        {
            vaProxy.SetBoolean(">JBMS_INITED", false);
            ResetMenuState(vaProxy);
        }
        public static bool VerifyMenuState(dynamic vaProxy, bool menuUp, bool noErrorsAllowed)
        {
            bool IsMenuUp = GetNonNullBool(vaProxy, ">JBMS_MENU_UP");
            bool HasErrors = GetNonNullBool(vaProxy, ">JBMS_NO_SUCH_MENU");

            if (noErrorsAllowed)
            {
                return ((IsMenuUp == menuUp) && !HasErrors);
            }
            else
            {
                return ((IsMenuUp == menuUp));
            }
        }

        public static void ResetMenuState(dynamic vaProxy, bool onlyUpAndErrors = false)
        {
            Logger.Write(vaProxy, "ResetMenuState");

            vaProxy.SetBoolean(">JBMS_NO_SUCH_MENU", false);            // This is set by plugin if its called to load a target/menu pair that doesn't exist
            vaProxy.SetBoolean(">JBMS_MENU_UP", false);                 // True only while menu is up (hopefully)
            if (onlyUpAndErrors)
                return;

            vaProxy.SetText(">JBMS_MENU_TGT", "");
            vaProxy.SetText(">JBMS_MENU_NAME", "");
        }

        public static void CloseMenu(dynamic vaProxy, bool pressEscape = true, bool onlyUpAndErrors = false)
        {
            // If we currently have a menu up, bring it down

            // Tell VA to execute close them menu with ESC, but NOT call back plugin with "JBMS_RESET_MENU_STATE"
            if (pressEscape)
            {
                vaProxy.Command.Execute("JBMS Close Menu", WaitForReturn: true);
            }
            
            ResetMenuState(vaProxy, onlyUpAndErrors);
        }


        //note that in this version of the plugin interface, there is only a single dynamic parameter.  All the functionality of the previous parameters can be found in vaProxy
        public static void VA_Init1(dynamic vaProxy)
        {
            //this is where you can set up whatever session information that you want.  this will only be called once on voiceattack load, and it is called asynchronously.
            //the SessionState property is a local copy of the state held on to by VoiceAttack.  In this case, the state will be a dictionary with zero items.  You can add as many items to hold on to as you want.
            //note that in this version, you can get and set the VoiceAttack variables directly.

            // Since this is called BEFORE our profile loads and calls us, will should wait till we get hit with VA_Invoke1 to do our initialization

            // DO NOTHING
        }
        
        public static void VA_Exit1(dynamic vaProxy)
        {
            //this function gets called when VoiceAttack is closing (normally).  You would put your cleanup code in here, but be aware that your code must be robust enough to not absolutely depend on this function being called
            if (vaProxy.SessionState.ContainsKey("myStateValue"))  //the sessionstate property is a dictionary of (string, object)
            {
                //do some kind of file cleanup or whatever at this point
            }
        }

        public static bool ReadFileIntoString(dynamic vaProxy, string pathFile, out string stringFromFile)
        {
            stringFromFile = string.Empty;
            try
            {
                stringFromFile = File.ReadAllText(pathFile);
            }
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

        public static bool OneTimeMenuDataLoad(dynamic vaProxy)
        {
            if (GetNonNullBool(vaProxy, ">JBMS_INITED"))
            {
                // Already initialized
                Logger.Warning(vaProxy, "OneTimeMenuDataLoad() called more than once - possible when switching betweeen profiles.");
                // Don't try to reinitialize - just return success quietly since we should already be configured
                return true;
            }

            Logger.Write(vaProxy, "OneTimeMenuDataLoad()");

            // Look for our menu info json file and schema and load each into a string
            string appsDir = vaProxy.AppsDir;
            if (String.IsNullOrEmpty(appsDir))
            {
                Logger.Error(vaProxy, "Invalid AppsDir - Cannot load menu info");
                return false;
            }
            string menuJsonPath = appsDir + "\\Tanrr.VAPlugin.BMSRadio\\Tanrr.VAPlugin.Radio.Menus.json";
            string menuJsonSchemaPath = appsDir + "\\Tanrr.VAPlugin.BMSRadio\\Tanrr.VAPlugin.Radio.Schema.json";

            string menusJsonRead = string.Empty;
            string menuSchemaRead = string.Empty;
            if (!ReadFileIntoString(vaProxy, menuJsonPath, out menusJsonRead))
            {
                Logger.Error(vaProxy, "No json data read from " + menuJsonPath);
                return false;
            }
            if (!ReadFileIntoString(vaProxy, menuJsonSchemaPath, out menuSchemaRead))
            {
                Logger.Error(vaProxy, "No schema data read from " + menuJsonSchemaPath);
                return false;
            }

            // Parse top level menu as array            
            JArray menusDeserialized = new JArray();
            try
            {
                //menusDeserialized = JsonConvert.DeserializeObject<dynamic>(menuJsonRead);
                // menusTest = JsonConvert.DeserializeObject<JArray>(menuJsonRead);
                menusDeserialized = JArray.Parse(menusJsonRead);
            }
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


            
            // SCHEMA SCHEMA SCHEMA
            // Parse schema & verify menu json
            JSchema menuSchemaDeserialized = new JSchema();
            try
            {
                menuSchemaDeserialized = JSchema.Parse(menuSchemaRead);
            }
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
            if (!menusDeserialized.IsValid(menuSchemaDeserialized, out errorMsgs))
            {
                Logger.Error(vaProxy, "" + menuJsonPath + " failed schema validation against " + menuJsonSchemaPath);
                if (errorMsgs != null)
                {
                    foreach (string msg in errorMsgs)
                    {
                        Logger.Error(vaProxy, "Schema Error: " + msg);
                    }
                }
                return false;
            }
            // SCHEMA SCHEMA SCHEMA
            



            // Read through each menu and add details to our list
            s_menusBMS = new List<MenuBMS>();
            int indexMenu = 0;
            foreach (JObject menuJson in menusDeserialized) // <-- Note that here we used JObject instead of usual JProperty
            {
                JArray menuItems = (JArray)menuJson["menuItems"];
                int countMenuItems = menuItems.Count;
                MenuItemBMS[] menuItemsBMS = new MenuItemBMS[countMenuItems];
                for (int i = 0; i < countMenuItems; i++)
                {
                    JArray menuItemArray = (JArray)menuItems[i];
                    int countMenuArray = menuItemArray.Count;
                    if (countMenuArray < 2 || countMenuArray > 3)
                    {
                        Logger.Error(vaProxy, "Invalid number of items for a line-item menu");
                        return false;
                    }
                    MenuItemBMS menuItemBMS = new MenuItemBMS(vaProxy, (string)menuItemArray[0], (string)menuItemArray[1], countMenuArray == 3 ? (string)menuItemArray[2] : null);
                    menuItemsBMS[i] = menuItemBMS;
                }

                MenuBMS menuBMS = new MenuBMS
                (vaProxy, (string)menuJson["menuTarget"], (string)menuJson["targetPhrases"], (string)menuJson["menuName"], (string)menuJson["menuNamePhrases"], (string)menuJson["menuShow"], menuItemsBMS);

                // Store the index to this menu for quicker access later
                try
                {
                    s_menusBMS.Add(menuBMS);
                    // Don't increase the index yet as we refer to it when caching info for menuItem directCommands
                    vaProxy.SessionState.Add(menuBMS.MenuFullID, indexMenu);
                }
                catch (System.ArgumentException e)
                {
                    Logger.Error(vaProxy, "Failed to add a menu with menuTarget_menuName of " + menuBMS.MenuFullID);
                    Logger.Error(vaProxy, e.Message);
                    return false;
                }

                // For each menu fully added, add any of it's direct commands to a dictionary mapping to the specific menu and menuitem
                // We're repeating the for loop, but couldn't do this safely before in case creation and adding the full menu failed
                // TODO: Move this into a container for the list of menus
                for (int i = 0; i < countMenuItems; i++)
                {
                    if (menuItemsBMS[i].HasDirectCmd())
                    {
                        // Cache the string in case we need to show it in the fail message
                        string dirCmd = menuItemsBMS[i].MenuItemDirectCmd;
                        // Save dictionary entry of DirCmd to this specific menu index and menu item index
                        try
                        {
                            s_DirCmdMap.Add(dirCmd, new DirCmdIndexes(indexMenu, i));
                        }
                        catch (System.ArgumentException e)
                        {
                            Logger.Warning(vaProxy, "Failed to add Direct Menu Cmd \"" + dirCmd + "\" probably due to duplicates");
                            Logger.Warning(vaProxy, e.Message);
                            // Don't fail for this - rest of system should be working
                        }
                    }
                }

                indexMenu++;
            }

            Logger.Write(vaProxy, "OneTimeMenuDataLoad completed successfully");

            vaProxy.SetBoolean(">JBMS_INITED", true);
            return true;

         }

        public static MenuBMS GetMenuBMS(dynamic vaProxy, string menuFullID)
        {
            if (string.IsNullOrEmpty(menuFullID))
            {
                Logger.Error(vaProxy, "Invalid menuFullID passed to GetMenuBMS");
                return null;
            }

            foreach (MenuBMS MenuTest in s_menusBMS)
            {
                if (MenuTest.MenuFullID == menuFullID)
                {
                    return MenuTest;
                }
            }

            return null;
        }

        public static MenuBMS GetSetCurMenuBMS(dynamic vaProxy, string menuFullID)
        {
            if (string.IsNullOrEmpty(menuFullID))
            {
                Logger.Error(vaProxy, "Invalid menuFullID passed to GetSetCurMenuBMS");
                return null;
            }

            // If we already have a current menu object, check if it's the same one we're looking for
            if (s_curMenuBMS != null)
            {
                if (s_curMenuBMS.MenuFullID == menuFullID) { return s_curMenuBMS; }
                else { s_curMenuBMS = null; }
            }

            return s_curMenuBMS = GetMenuBMS(vaProxy, menuFullID);
        }

        public static MenuBMS GetSetCurMenuBMS(dynamic vaProxy, string MenuTarget, string MenuName)
        {
            if (string.IsNullOrEmpty(MenuTarget) || string.IsNullOrEmpty(MenuName))
            {
                Logger.Error(vaProxy, "Invalid MenuTarget or MenuName passed to GetSetCurMenuBMS");
                return null;
            }
            return GetSetCurMenuBMS(vaProxy, MenuBMS.MakeFullID(MenuTarget, MenuName));
        }

        public static MenuBMS GetSetCurMenuBMS(dynamic vaProxy)
        {
            string MenuTarget = vaProxy.GetText(">JBMS_MENU_TGT");
            string MenuName = vaProxy.GetText(">JBMS_MENU_NAME");
            if (string.IsNullOrEmpty(MenuTarget) || string.IsNullOrEmpty(MenuName))
            {
                Logger.Error(vaProxy, "GetSetCurMenuBMS without valid >JBMS_MENU_TGT or >JBMS_MENU_NAME");
                return null;
            }
            return GetSetCurMenuBMS(vaProxy, MenuTarget, MenuName);
        }

        public static bool ExecuteCmdOnly(dynamic vaProxy, string cmdExecute, bool waitForReturn)
        {
            if (    !string.IsNullOrEmpty(cmdExecute)
                &&  vaProxy.CommandExists(cmdExecute))
            {
                // Tell VA to execute cmdOrKeys as a Command in the profile
                vaProxy.Command.Execute(cmdExecute, WaitForReturn: waitForReturn);
                return true;
            }
            return false;
        }

        public static bool PressKeysOnly(dynamic vaProxy, string keysToPress, bool waitForReturn)
        {
            if (!string.IsNullOrEmpty(keysToPress))
            {
                // Tell VA to press the chars in cmdOrKeys
                vaProxy.Command.Execute("JBMS Press Keys", WaitForReturn: waitForReturn, PassedText: $@"""{keysToPress}""");
                return true;
            }
            return false;
        }

        public static bool ExecuteCmdOrKeys(dynamic vaProxy, string cmdOrKeys, bool waitForReturn)
        {
            if (string.IsNullOrEmpty(cmdOrKeys)) { return false; }

            if (vaProxy.CommandExists(cmdOrKeys))
            {
                return ExecuteCmdOnly(vaProxy, cmdOrKeys, waitForReturn);
            }
            else
            {
                return PressKeysOnly(vaProxy, cmdOrKeys, waitForReturn);
            }
        }

        public static void VA_Invoke1(dynamic vaProxy)
        {
            // Configure logger
            Logger.Prefix = "JeevesBMSRadio: ";
            Logger.WarningPrefix = "JeevesBMSRadio: WARNING: ";
            Logger.ErrorPrefix = "JeevesBMSRadio: ERROR: ";
            Logger.Json = GetNonNullBool(vaProxy, ">JBMS_JSON_LOGGING");
            Logger.Structures = GetNonNullBool(vaProxy, ">JBMS_STRUCTURES_LOGGING");
            Logger.Verbose = GetNonNullBool(vaProxy, ">JBMS_VERBOSE_LOGGING");

            // DEVELOPMENT TEMP - HARDCODED:
            Logger.Verbose = false;
            Logger.Json = false;
            Logger.Structures = false;

            string StartMessage = "Invoked with context " + vaProxy.Context;
            Logger.VerboseWrite(vaProxy, StartMessage);

            // Init if this is our first call
            if (!GetNonNullBool(vaProxy, ">JBMS_INITED"))
            {
                OneTimeMenuDataLoad(vaProxy);
                // If we failed to init, it will already be logged, so just return
                if (!GetNonNullBool(vaProxy, ">JBMS_INITED"))
                    return;
            }

            bool MenuUp = GetNonNullBool(vaProxy, ">JBMS_MENU_UP");

            switch (vaProxy.Context)
            {
                case "JBMS_DO_INIT":
                    // We init automatically on first call, so nothing to do
                    break;

                case "JBMS_RESET_MENU_STATE":
                    // Should only be called when ESC has been pressed so current menu has already been closed
                    Logger.VerboseWrite(vaProxy, "JBMS_RESET_MENU_STATE - ESC should have been pressed already");
                    ResetMenuState(vaProxy);
                    break;

                case "JBMS_DIRECT_CMD":
                    string dirCmd = vaProxy.GetText(">JBMS_DIRECT_CMD");
                    vaProxy.SetText(">JBMS_DIRECT_CMD", string.Empty);
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
                        DirCmdIndexes dirCmdIndexes = s_DirCmdMap[dirCmd];
                        if (dirCmdIndexes.IndexMenu < 0 || dirCmdIndexes.IndexMenu > s_menusBMS.Count)
                        {
                            Logger.Error(vaProxy, "JBMS_DIRECT_CMD received for \"" + dirCmd + "\" but no such direct command found in DirCmdMap");
                            break;
                        }
                        MenuBMS menuForDC = s_menusBMS[dirCmdIndexes.IndexMenu];
                        MenuItemBMS[] menuItemsForDC = menuForDC.MenuItemsBMS;
                        if (dirCmdIndexes.IndexMenuItem < 0 || dirCmdIndexes.IndexMenuItem >= menuItemsForDC.Length )
                        {
                            Logger.Error(vaProxy, "JBMS_DIRECT_CMD received for \"" + dirCmd + "\" but invalid menu item index stored in DirCmdMap");
                            break;
                        }
                        MenuItemBMS menuItemForDC = menuItemsForDC[dirCmdIndexes.IndexMenuItem];

                        bool ExecOK = false;
                        // Now we know the specific menu and which menu item, so we can bring up the menu 
                        ExecOK = ExecuteCmdOrKeys(vaProxy, menuForDC.MenuShow, /* waitForReturn */ true);
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
                        Logger.Warning(vaProxy, "JBMS_DIRECT_CMD received for \"" + dirCmd + "\" but no such direct command found in DirCmdMap");
                        Logger.Warning(vaProxy, "Exception " + e.Message);
                        break;
                    }

                    break;

                case "JBMS_HANDLE_MENU_RESPONSE":
                    string MenuResponse = vaProxy.GetText(">JBMS_MENU_RESPONSE");
                    if ( MenuUp && (s_curMenuBMS != null) )
                    {
                        if (string.IsNullOrEmpty(MenuResponse))
                        {
                            Logger.VerboseWrite(vaProxy, "JBMS_HANDLE_MENU_RESPONSE received empty MenuResponse");
                        }
                        else if (MenuResponse == "_JBMS_MENU_TIMEOUT")
                        {
                            Logger.VerboseWrite(vaProxy, "JBMS_HANDLE_MENU_RESPONSE received _JBMS_MENU_TIMEOUT");
                            // Note that Menu is still up - it will get closed at the end of this handler
                        }
                        else
                        {
                            string MenuName = s_curMenuBMS.MenuName;
                            string MenuItemExecute = s_curMenuBMS.MenuItemExecuteFromPhrase(vaProxy, MenuResponse);
                            if (!string.IsNullOrEmpty(MenuItemExecute))
                            {
                                Logger.VerboseWrite(vaProxy, $"JBMS_HANDLE_MENU_RESPONSE with Menu: \"{MenuName}\"; Phrase: \"{MenuResponse}\"; Execute: \"{MenuItemExecute}\"");

                                // DOCUMENTATION: Any non-menu closing keystrokes (a VA command phrase) NEEDS TO CLOSE THE RADIO MENU ITSELF NOW (*NOT* by calling back into plugin, but with an "ESC" key if that's what does it
                                ExecuteCmdOrKeys(vaProxy, MenuItemExecute, /* waitForReturn */ true);

                                // Passing key/cmd should have brought down menu, so only reset our menu state - don't press escape
                                ResetMenuState(vaProxy);
                                MenuUp = false;
                            }
                            else
                            {
                                Logger.Write(vaProxy, "JBMS_HANDLE_MENU_RESPONSE found no match for MenuResponse \"" + MenuResponse + "\"");
                            }
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

                    break;
                
                case "JBMS_SHOW_MENU":
                    MenuBMS Menu = GetSetCurMenuBMS(vaProxy);
                    if ( Menu == null)
                    {
                        Logger.Error(vaProxy, "JBMS_SHOW_MENU called without valid MenuTarget or MenuName");
                        // TODO - SET ERROR VALUE?
                        return;
                    }

                    if (MenuUp)
                    {
                        // If we currently have a menu up, bring it down
                        if (vaProxy.CommandExists("JBMS Close Menu"))
                        {
                            // Tell VA to execute close them menu with ESC, but NOT call back plugin with "JBMS_RESET_MENU_STATE"
                            Logger.Write(vaProxy, "JBMS_SHOW_MENU called when menu already up, closing current menu");
                            vaProxy.Command.Execute("JBMS Close Menu", WaitForReturn: true);
                            // Reset just the menu state related to it being up or having errors
                            ResetMenuState(vaProxy, onlyUpAndErrors: true);
                        }
                        else
                        {
                            Logger.Write(vaProxy, "JBMS_SHOW_MENU called when menu already up, but couldn't execute \"JBMS Close Menu\"");
                            return;
                        }
                    }

                    string MenuShow = Menu.MenuShow;
                    ExecuteCmdOrKeys(vaProxy, MenuShow, /* waitForReturn */ true);
                    // Assume Success
                    vaProxy.SetBoolean(">JBMS_MENU_UP", true);

                    // Async non-blocking wait & listen for phrases that match menu item choices (with timeout)
                    // Plugin invoked with "JBMS_HANDLE_MENU_RESPONSE" if matching response heard
                    string AllMenuItemPhrases = Menu.AllMenuItemPhrases;
                    vaProxy.Command.Execute("JBMS Wait For Menu Response", WaitForReturn: false, PassedText: $@"""{AllMenuItemPhrases}""");

                    break;

                default:
                    Logger.Error(vaProxy, "Called with unknown Context: " + vaProxy.Context);
                    throw new ArgumentException("", "context");
            }
        }
    }
} // end Tanrr.VAPlugin.BMSRadio Namespace