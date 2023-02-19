using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.Cryptography;
using System.Text;


namespace Tanrr.VAPlugin.BMSRadio
{
    public static class DebugLogger
    {
        static private bool s_debugLogging = false;

        public static void EnableDebugLog(bool enable)
        { s_debugLogging = enable; }

        public static void Write(dynamic vaProxy, string msg, string color)
        {
            if (s_debugLogging)
                vaProxy.WriteToLog("JeevesBMSRadio DEBUG: " + msg, color);
        }
    }
    public class MenuItemBMS
    {
        public string MenuItemPhrases { get; set; }              // Possibly multiple phrases
        public string MenuItemExecute { get; set; }              // Having Command as string instead of number means it could be any char (or a combo of chars)
        public string[] ExtractedMenuItemPhrases { get; set; }   // Array of each of all possible versions of the phrases, extracted separately 

        public MenuItemBMS(dynamic vaProxy, string itemPhrases, string itemExec)
        {

            if (itemPhrases == null || itemExec == null)
            {
                SetEmpty();
                return;
            }

            MenuItemPhrases = RemoveEmptyMatches( itemPhrases.ToLower() );
            MenuItemExecute = itemExec;
            if ( MenuItemPhrases.Length == 0 || MenuItemExecute.Length == 0 )
            {
                SetEmpty();
                return;
            }

            ExtractedMenuItemPhrases = vaProxy.Utility.ExtractPhrases(MenuItemPhrases);
            if ( ExtractedMenuItemPhrases.Length == 0 )
            {
                SetEmpty();
                return;
            }
        }

        void SetEmpty()
        {
            // TODO: Cleanup first if non-null
                
            MenuItemPhrases = string.Empty;
            MenuItemExecute = string.Empty;
            ExtractedMenuItemPhrases = new string[] { string.Empty };
        }

        // Clear out starting, ending, or double ";" in strings - needed to avoid empty matches
        // Note that we DO allow internal null matches, but not for a full match - "Attack [Air;AA;];Get Them" is fine, but "Hello ;; Attack [Air;AA;];Get Them;" is not
        private string RemoveEmptyMatches(string phrases) 
        {
            if (phrases == null) return string.Empty;

            phrases = phrases.Trim(';');  // Remove leading or trailing ; so that phrases can't match "" which matches all

            if (phrases.Length == 0) return string.Empty;

            return phrases.Replace(";;", ";");
        }
    }
    public class MenuBMS
    {
        protected Dictionary<string, string> _extractedMenuItems;
        public string MenuFullID { get; set; }        // MenuTarget_MenuName for easy ident, generated on creation
        public string MenuTarget { get; set; }        // "Wingman"   // Caller must pass THIS string when trying to bring up a menu, not whatever user said
        public string TargetPhrases { get; set; }     // "2;Wingman;Heya"
        public string MenuName { get; set; }          // "Combat 3"  // Caller must pass THIS string when trying to bring up a menu, not whatever user said
        public string MenuNamePhrases { get; set; }   // "Combat 3;Combat Management 3"
        public string MenuShow { get; set; }          // If this matches a VA phrase in the profile, execute that command, else presses chars passed

        public MenuItemBMS[] MenuItemsBMS;
        public string AllMenuItemPhrases;

        static public string MakeFullID(string menuTarget, string menuName)
        {
            if (string.IsNullOrEmpty(menuTarget) || string.IsNullOrEmpty(menuName) )
                return string.Empty;
            return menuTarget + "_" + menuName;
        }

        public string MenuItemExecuteFromPhrase(dynamic vaProxy, string simplePhrase)
        {
            if (string.IsNullOrEmpty(simplePhrase)) 
                return null;    

            if (_extractedMenuItems.ContainsKey(simplePhrase))
                return _extractedMenuItems[simplePhrase];
            else
                return null;
        }

        public MenuBMS(dynamic vaProxy, string tgt, string tgtPhrases, string name, string namePhrases, string show, MenuItemBMS[] items  ) 
        {
            // vaProxy.WriteToLog("JeevesBMSRadio: In MenuBMS Constructor", "Purple");

            _extractedMenuItems = new Dictionary<string, string>();

            MenuTarget = tgt;
            TargetPhrases = tgtPhrases;
            MenuName = name;
            MenuNamePhrases= namePhrases;
            MenuShow = show;

            MenuItemsBMS = items;

            MenuFullID = string.Empty; // Starts empty
            AllMenuItemPhrases = string.Empty;

            // MakeFullID has its own error checks and will return string.Empty on error
            if ( string.IsNullOrEmpty(MenuTarget) || string.IsNullOrEmpty(TargetPhrases) 
                || string.IsNullOrEmpty(MenuName) || string.IsNullOrEmpty(MenuNamePhrases) 
                || string.IsNullOrEmpty(MenuShow) 
                || items == null || items.Length == 0)
            {
                vaProxy.WriteToLog("JeevesBMSRadio: Unexpected failure creating MenuBMS - Invalid Parameters passed to constructor", "Red");
                return;
            }

            // DebugLogger.Write(vaProxy, "In MenuBMS Constructor", "Purple");

            // Make MenuFullID out of MenuTarget and MenuName
            MenuFullID = MakeFullID(MenuTarget, MenuName);
            if ( string.IsNullOrEmpty(MenuFullID) )
            { 
                MenuFullID = string.Empty;
                // error
                vaProxy.WriteToLog("JeevesBMSRadio: Unexpected failure creating MenuBMS MenuFullID", "Red");
                return;
            }

            // DebugLogger.Write(vaProxy, "MenuBMS MakeFullID Finished", "Purple");

            // Create a string with all commands, ; delimeted, to pass to Get User Input - Wait for Spoken Response 
            // AND Fill in _extractedMenuItems dictionary with all possble phrases, mapped to their MenuItemExecute values
            // ie "Weapons Free [Air;A A;]" separated into "Weapons Free Air", "Weapons Free A A" and "Weapons Free"
            foreach (MenuItemBMS menuItem in MenuItemsBMS)
            {
                // DebugLogger.Write(vaProxy, "menuItem " + menuItem.MenuItemPhrases + " : " + menuItem.MenuItemExecute, "Purple");

                AllMenuItemPhrases = AllMenuItemPhrases.Insert(AllMenuItemPhrases.Length, menuItem.MenuItemPhrases + ";");
                string[] MenuItemPhrases = menuItem.ExtractedMenuItemPhrases;
                string MenuItemExecute = menuItem.MenuItemExecute;

                // DebugLogger.Write(vaProxy, "    Set MenuItemPhrases array of extracted phrases and MenuItemExecute", "Purple");

                foreach (string MenuItemPhrase in MenuItemPhrases)
                {
                    if (_extractedMenuItems.ContainsKey(MenuItemPhrase))
                    {
                        // This is a duplicate - skip it, but log
                        string DupedExecute = _extractedMenuItems[MenuItemPhrase];
                        string AddWarningMessage = "JeevesBMSRadio: Duplicate Extracted Phrase " + MenuItemPhrase + " in Menu " + MenuName + "\n Leaving version with Menu Item Key " + DupedExecute + "\n Dropping version with Menu Item Key " + MenuItemExecute;
                        vaProxy.WriteToLog(AddWarningMessage, "Red");
                        continue;
                    }
                    else 
                    { 
                        try
                        {
                            _extractedMenuItems.Add(MenuItemPhrase, MenuItemExecute);
                        }
                        catch (ArgumentException)
                        {
                            vaProxy.WriteToLog("JeevesBMSRadio: Unexpected failure to add an extracted menu item phrase to dictionary.", "Red");
                            string AddErrorMessage = " MenuItemPhrase = " + MenuItemPhrase + "\n MenuItemExecute = " + MenuItemExecute;
                            vaProxy.WriteToLog(AddErrorMessage, "Red");
                            // Letting the loop continue here, in case this was just a duplicate we failed to catch
                        }
                    }
                }
            }
            // Get rid of that last ";" so we don't match an empty string
            AllMenuItemPhrases = AllMenuItemPhrases.TrimEnd(';');
        }

    };

    // JBMS Generated GUIDS:
    // G1 7e22363e-2cca-4b26-8aae-a292f73d2a53
    // G2 16223078-853e-41fb-a741-3e9cacfe94d9
    // G3 da2b13d9-f5fd-47e3-93af-b5c9e089b5a8

    public class Jeeves_BMS_Radio_Plugin 
    {
        protected static List<MenuBMS> s_menusBMS = null;
        protected static MenuBMS s_curMenuBMS = null;
        
        public static MenuBMS CurMenuBMS 
        { 
            get => s_curMenuBMS;
            set => s_curMenuBMS = value; 
        }

        public static string VA_DisplayName()
            => "Jeeves BMS Radio Plugin v0.5";  // Displayed in dropdowns and log as plugin name

        public static string VA_DisplayInfo()
            => "Jeeves BMS Radio Plugin for VoiceAttack\r\n\r\nInitial POC.\r\n\r\n2023";  // Extended info

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
            vaProxy.Setbool(">JBMS_INITED", false);
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
            vaProxy.WriteToLog("JeevesBMSRadio: ResetMenuState", "Purple");

            vaProxy.Setbool(">JBMS_NO_SUCH_MENU", false);            // This is set by plugin if its called to load a target/menu pair that doesn't exist
            vaProxy.Setbool(">JBMS_MENU_UP", false);                 // True only while menu is up (hopefully)
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

        public static bool OneTimeMenuDataLoad(dynamic vaProxy)
        {
            vaProxy.WriteToLog("JeevesBMSRadio: OneTimeMenuDataLoad()", "Purple");

            if (GetNonNullBool(vaProxy, ">JBMS_INITED"))
            {
                // Already initialized
                vaProxy.WriteToLog("JeevesBMSRadio: OneTimeMenuDataLoad() called more than once - possible when switching betweeen profiles.", "Red");
                // Don't try to reinitialize - just return success quietly since we should already be configured
                return true;
            }

            s_menusBMS = new List<MenuBMS>();

            // Combat 1 menu items same for Flight, Element, and Wingman Menu Targets
            MenuItemBMS[] menuItemsREWC1 = new MenuItemBMS[9]
                {
                    /*                        *Target Phrases*          *MenuItemExecute* */
                    new MenuItemBMS( vaProxy, "Attack My Target",             "1" ),
                    new MenuItemBMS( vaProxy, "Buddy Spike",                  "2" ),
                    new MenuItemBMS( vaProxy, "Ray Gun;Raygun",               "3" ),
                    new MenuItemBMS( vaProxy, "Weapons Free [Air;A A]",       "4" ),
                    new MenuItemBMS( vaProxy, "Weapons Free [Ground;A G]",    "5" ),
                    new MenuItemBMS( vaProxy, "Weapons Hold",                 "6" ),
                    new MenuItemBMS( vaProxy, "Check Your Six",               "7" ),
                    new MenuItemBMS( vaProxy, "Check My Six;Watch My Six",    "8" ),
                    new MenuItemBMS( vaProxy, "Attack Targets",               "9" )
                };

            // Combat 2 menu items for Wingman Menu Target
            MenuItemBMS[] menuItemsWC2 = new MenuItemBMS[7]
                {
                    /*                        *Target Phrases*          *MenuItemExecute* */
                    new MenuItemBMS( vaProxy, "Rejoin",                         "1" ),
                    new MenuItemBMS( vaProxy, "Split Wing",                     "2" ),
                    new MenuItemBMS( vaProxy, "Glue Wing",                      "3" ),
                    new MenuItemBMS( vaProxy, "Drop Stores;Jettison Stores;Emergency Jettison",       "4" ),
                    new MenuItemBMS( vaProxy, "Datalink [Ground;] Target",      "5" ),
                    new MenuItemBMS( vaProxy, "Go Shooter",                     "6" ),
                    new MenuItemBMS( vaProxy, "Go Cover",                       "7" )
                };

            // Combat 2 menu items for Flight and Element Menu Targets are the same
            MenuItemBMS[] menuItemsREC2 = new MenuItemBMS[3]
                {
                    /*                        *Target Phrases*          *MenuItemExecute* */
                    new MenuItemBMS( vaProxy, "Rejoin",                         "1" ),
                    new MenuItemBMS( vaProxy, "Drop Stores;Jettison Stores;Emergency Jettison",       "2" ),
                    new MenuItemBMS( vaProxy, "Datalink [Ground;] Target",      "3" )
                };

            // Combat 3 menu only for Element, and Wingman Menu Targets
            MenuItemBMS[] menuItemsEWC3 = new MenuItemBMS[8]
                {
                    /*                        *Target Phrases*          *MenuItemExecute* */
                    new MenuItemBMS( vaProxy, "Offensive Pursuit",              "1" ),
                    new MenuItemBMS( vaProxy, "Offensive Split",                "2" ),
                    new MenuItemBMS( vaProxy, "Beam Deploy",                    "3" ),
                    new MenuItemBMS( vaProxy, "Grinder",                        "4" ),
                    new MenuItemBMS( vaProxy, "Wide Azimuth]",                  "5" ),
                    new MenuItemBMS( vaProxy, "Short Azimuth",                  "6" ),
                    new MenuItemBMS( vaProxy, "Sweep",                          "7" ),
                    new MenuItemBMS( vaProxy, "Defensive",                      "8" )
                };

            // vaProxy.WriteToLog("JeevesBMSRadio: MenuItemBMS Arrays Created", "Purple");

            MenuBMS menuWC1 = new MenuBMS
            ( vaProxy,  "Wingman", "Wingman;2",     "Combat 1", "Combat [Management;] 1",   "W",    menuItemsREWC1 );
            // Store the index to this menu for quicker access later
            s_menusBMS.Add(menuWC1);
            vaProxy.SessionState.Add(menuWC1.MenuFullID, 0);

            MenuBMS menuEC1 = new MenuBMS
            ( vaProxy,  "Element", "Element;3",     "Combat 1", "Combat [Management;] 1",   "E",  menuItemsREWC1 );
            // Store the index to this menu for quicker access later
            s_menusBMS.Add(menuEC1);
            vaProxy.SessionState.Add(menuEC1.MenuFullID, 1);

            MenuBMS menuRC1 = new MenuBMS
            ( vaProxy,  "Flight", "Flight",     "Combat 1", "Combat [Management;] 1",   "R",    menuItemsREWC1 );
            // Store the index to this menu for quicker access later
            s_menusBMS.Add(menuRC1);
            vaProxy.SessionState.Add(menuRC1.MenuFullID, 2);

            MenuBMS menuWC2 = new MenuBMS
            (vaProxy, "Wingman", "Wingman;2", "Combat 2", "Combat [Management;] 2", "WW", menuItemsWC2);
            // Store the index to this menu for quicker access later
            s_menusBMS.Add(menuWC2);
            vaProxy.SessionState.Add(menuWC2.MenuFullID, 3);

            MenuBMS menuEC2 = new MenuBMS
            (vaProxy, "Element", "Element;3", "Combat 2", "Combat [Management;] 2", "EE", menuItemsREC2);
            // Store the index to this menu for quicker access later
            s_menusBMS.Add(menuEC2);
            vaProxy.SessionState.Add(menuEC2.MenuFullID, 4);

            MenuBMS menuRC2 = new MenuBMS
            (vaProxy, "Flight", "Flight", "Combat 2", "Combat [Management;] 2", "RR", menuItemsREC2);
            // Store the index to this menu for quicker access later
            s_menusBMS.Add(menuRC2);
            vaProxy.SessionState.Add(menuRC2.MenuFullID, 5);

            MenuBMS menuWC3 = new MenuBMS
            (vaProxy, "Wingman", "Wingman;2", "Combat 3", "Combat [Management;] 3", "WWW", menuItemsEWC3);
            // Store the index to this menu for quicker access later
            s_menusBMS.Add(menuWC3);
            vaProxy.SessionState.Add(menuWC3.MenuFullID, 6);

            MenuBMS menuEC3 = new MenuBMS
            (vaProxy, "Element", "Element;3", "Combat 3", "Combat [Management;] 3", "EEE", menuItemsEWC3);
            // Store the index to this menu for quicker access later
            s_menusBMS.Add(menuEC3);
            vaProxy.SessionState.Add(menuEC3.MenuFullID, 7);


            vaProxy.WriteToLog("JeevesBMSRadio: OneTimeMenuDatLoad completed successfully", "Purple");

            vaProxy.Setbool(">JBMS_INITED", true);
            return true;
        }

        public static MenuBMS GetMenuBMS(dynamic vaProxy, string menuFullID)
        {
            if (string.IsNullOrEmpty(menuFullID))
            {
                vaProxy.WriteToLog("Invalid menuFullID passed to GetMenuBMS", "Red");
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
                vaProxy.WriteToLog("Invalid menuFullID passed to GetSetCurMenuBMS", "Red");
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
                vaProxy.WriteToLog("Invalid MenuTarget or MenuName passed to GetSetCurMenuBMS", "Red");
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
                vaProxy.WriteToLog("JeevesBMSRadio: GetSetCurMenuBMS without valid >JBMS_MENU_TGT or >JBMS_MENU_NAME", "Red");
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
            //This function is where you will do all of your work.  When VoiceAttack encounters an, 'Execute External Plugin Function' action, the plugin indicated will be called.
            //in previous versions, you were presented with a long list of parameters you could use.  The parameters have been consolidated in to one dynamic, 'vaProxy' parameter.

            //vaProxy.Context - a string that can be anything you want it to be.  this is passed in from the command action.  this was added to allow you to just pass a value into the plugin in a simple fashion (without having to set conditions/text values beforehand).  Convert the string to whatever type you need to.

            //vaProxy.SessionState - all values from the state maintained by VoiceAttack for this plugin.  the state allows you to maintain kind of a, 'session' within VoiceAttack.  this value is not persisted to disk and will be erased on restart. other plugins do not have access to this state (private to the plugin)

            //the SessionState dictionary is the complete state.  you can manipulate it however you want, the whole thing will be copied back and replace what VoiceAttack is holding on to


            //the following get and set the various types of variables in VoiceAttack.  note that any of these are nullable (can be null and can be set to null).  in previous versions of this interface, these were represented by a series of dictionaries

            //vaProxy.SetSmallInt and vaProxy.GetSmallInt - use to access short integer values (used to be called, 'conditions')
            //vaProxy.SetText and vaProxy.GetText - access text variable values
            //vaProxy.SetInt and vaProxy.GetInt - access integer variable values
            //vaProxy.SetDecimal and vaProxy.GetDecimal - access decimal variable values
            //vaProxy.Setbool and vaProxy.Getbool - access boolean variable values
            //vaProxy.SetDate and vaProxy.GetDate - access date/time variable values

            //to indicate to VoiceAttack that you would like a variable removed, simply set it to null.  all variables set here can be used within VoiceAttack.
            //note that the variables are global (for now) and can be accessed by anyone, so be mindful of that while naming

            //if the, 'Execute External Plugin Function' command action has the, 'wait for return' flag set, VoiceAttack will wait until this function completes so that you may check condition values and
            //have VoiceAttack react accordingly.  otherwise, VoiceAttack fires and forgets and doesn't hang out for extra processing.

            // Debug Logging on by default for now
            DebugLogger.EnableDebugLog(true);

            string StartMessage = "JeevesBMSRadio: Invoked with context " + vaProxy.Context;
            vaProxy.WriteToLog(StartMessage, "Purple");

            // Init if this is our first call
            if (!GetNonNullBool(vaProxy, ">JBMS_INITED"))
            {
                OneTimeMenuDataLoad(vaProxy);
                // If we failed to init, it will already be logged, so just return
                if (!GetNonNullBool(vaProxy, ">JBMS_INITED"))
                    return;
            }

            switch (vaProxy.Context)
            {
                case "JBMS_DO_INIT":
                    // We init automatically on first call, so nothing to do
                    break;

                case "JBMS_RESET_MENU_STATE":
                    // Should only be called when ESC has been pressed so current menu has already been closed
                    vaProxy.WriteToLog("JeevesBMSRadio: JBMS_RESET_MENU_STATE - ESC should have been pressed already", "Purple");
                    ResetMenuState(vaProxy);
                    break;

                case "JBMS_HANDLE_MENU_RESPONSE":
                    bool MenuUp = GetNonNullBool(vaProxy, ">JBMS_MENU_UP");
                    string MenuResponse = vaProxy.GetText(">JBMS_MENU_RESPONSE");
                    if ( MenuUp && (s_curMenuBMS != null) )
                    {
                        if (string.IsNullOrEmpty(MenuResponse))
                        {
                            vaProxy.WriteToLog("JeevesBMSRadio: JBMS_HANDLE_MENU_RESPONSE received empty MenuResponse", "Purple");
                        }
                        else if (MenuResponse == "_JBMS_MENU_TIMEOUT")
                        {
                            vaProxy.WriteToLog("JeevesBMSRadio: JBMS_HANDLE_MENU_RESPONSE received _JBMS_MENU_TIMEOUT", "Purple");
                            // Note that Menu is still up - it will get closed at the end of this handler
                        }
                        else
                        {
                            string MenuName = s_curMenuBMS.MenuName;
                            string MenuItemExecute = s_curMenuBMS.MenuItemExecuteFromPhrase(vaProxy, MenuResponse);
                            if (!string.IsNullOrEmpty(MenuItemExecute))
                            {
                                vaProxy.WriteToLog($"JeevesBMSRadio: JBMS_HANDLE_MENU_RESPONSE with Menu: \"{MenuName}\"; Phrase: \"{MenuResponse}\"; Execute: \"{MenuItemExecute}\"", "Purple");
                                ExecuteCmdOrKeys(vaProxy, MenuItemExecute, /* waitForReturn */ true);
                                // Passing key/cmd should have brought down menu, so only reset our menu state - don't press escape
                                ResetMenuState(vaProxy);
                                MenuUp = false;
                            }
                            else
                            {
                                vaProxy.WriteToLog("JeevesBMSRadio: JBMS_HANDLE_MENU_RESPONSE found no match for MenuResponse \"" + MenuResponse + "\"", "Purple");
                            }
                        }
                    }
                    else 
                    {
                        string MenuResponseSafe = string.IsNullOrEmpty(MenuResponse) ? string.Empty : MenuResponse;
                        vaProxy.WriteToLog($"JeevesBMSRadio: JBMS_HANDLE_MENU_RESPONSE recieved response \"{MenuResponseSafe}\" but no menu was still up", "Purple");
                    }

                    if (MenuUp)
                    {
                        vaProxy.WriteToLog("JeevesBMSRadio: Closing menu after JBMS_HANDLE_MENU_RESPONSE without a match", "Purple");
                        CloseMenu(vaProxy);
                    }
                    else
                    {
                        // Sanity check that our menu state is reset and we don't need to call ResetMenuState(vaProxy);
                        if (!VerifyMenuState(vaProxy, menuUp: false, noErrorsAllowed: true))
                        {
                            vaProxy.WriteToLog("JeevesBMSRadio: ERROR: Expected menu to be in non-error closed state after JBMS_HANDLE_MENU_RESPONSE", "Red");
                            ResetMenuState(vaProxy);
                        }
                    }

                    break;
                
                case "JBMS_SHOW_MENU":
                    MenuBMS Menu = GetSetCurMenuBMS(vaProxy);
                    if ( Menu == null)
                    {
                        vaProxy.WriteToLog("JeevesBMSRadio: JBMS_SHOW_MENU called without valid MenuTarget or MenuName", "Red");
                        // TODO - SET ERROR VALUE?
                        return;
                    }

                    if (GetNonNullBool(vaProxy, ">JBMS_MENU_UP"))
                    {
                        // If we currently have a menu up, bring it down
                        if (vaProxy.CommandExists("JBMS Close Menu"))
                        {
                            // Tell VA to execute close them menu with ESC, but NOT call back plugin with "JBMS_RESET_MENU_STATE"
                            vaProxy.WriteToLog("JeevesBMSRadio: JBMS_SHOW_MENU called when menu already up, closing current menu", "Purple");
                            vaProxy.Command.Execute("JBMS Close Menu", WaitForReturn: true);
                            // Reset just the menu state related to it being up or having errors
                            ResetMenuState(vaProxy, onlyUpAndErrors: true);
                        }
                        else
                        {
                            vaProxy.WriteToLog("JeevesBMSRadio: JBMS_SHOW_MENU called when menu already up, but couldn't execute \"JBMS Close Menu\"", "Red");
                            return;
                        }
                    }

                    string MenuShow = Menu.MenuShow;
                    ExecuteCmdOrKeys(vaProxy, MenuShow, /* waitForReturn */ true);
                    // Assume Success
                    vaProxy.Setbool(">JBMS_MENU_UP", true);

                    // Async non-blocking wait & listen for phrases that match menu item choices (with timeout)
                    // Plugin invoked with "JBMS_HANDLE_MENU_RESPONSE" if matching response heard
                    string AllMenuItemPhrases = Menu.AllMenuItemPhrases;
                    vaProxy.Command.Execute("JBMS Wait For Menu Response", WaitForReturn: false, PassedText: $@"""{AllMenuItemPhrases}""");

                    break;

                default:
                    vaProxy.WriteToLog( "Called with unknown Context: " + vaProxy.Context, "Red");
                    throw new ArgumentException("", "context");
            }
        }
    }
} // end Tanrr.VAPlugin.BMSRadio Namespace