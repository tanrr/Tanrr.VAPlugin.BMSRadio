using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanrr.VAPlugin.BMSRadio
{
    public class MenuItemBMS
    {
        public string MenuItemPhrases { get; set; }              // Possibly multiple phrases
        public string MenuItemExecute { get; set; }              // Having Command as string instead of number means it could be any char (or a combo of chars)
        public string MenuItemDirectCmd { get; set; }              // Having Command as string instead of number means it could be any char (or a combo of chars)
        public string[] ExtractedMenuItemPhrases { get; set; }   // Array of each of all possible versions of the phrases, extracted separately 

        public bool HasDirectCmd() { return !string.IsNullOrEmpty(MenuItemDirectCmd); }  
        public MenuItemBMS(dynamic vaProxy, string itemPhrases, string itemExec, string directCmd = null)
        {

            if (itemPhrases == null || itemExec == null)
            {
                SetEmpty();
                return;
            }

            MenuItemPhrases = RemoveEmptyMatches(itemPhrases.ToLower());
            MenuItemExecute = itemExec;
            if (MenuItemPhrases.Length == 0 || MenuItemExecute.Length == 0)
            {
                SetEmpty();
                return;
            }

            ExtractedMenuItemPhrases = vaProxy.Utility.ExtractPhrases(MenuItemPhrases);
            if (ExtractedMenuItemPhrases.Length == 0)
            {
                SetEmpty();
                return;
            }

            if (!string.IsNullOrEmpty(directCmd))
            {
                MenuItemDirectCmd = directCmd;
            }
        }

        void SetEmpty()
        {
            // TODO: Cleanup first if non-null

            MenuItemPhrases = string.Empty;
            MenuItemExecute = string.Empty;
            MenuItemDirectCmd = string.Empty;
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
        protected static char[] s_normTrimChars = {' ', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

        protected Dictionary<string, string> _extractedMenuItems;

        protected HashSet<string> _extractedNormTargetPhrases;
        protected HashSet<string> _extractedNormMenuNamePhrases;

        // TODO - Make these get only, rename as protected members with _

        public string MenuFullID { get; set; }        // MenuTarget_MenuName for easy ident, generated on creation
        public string MenuTarget { get; set; }        // "Wingman"   // Caller must pass THIS string when trying to bring up a menu, not whatever user said

        protected string _menuTargetNorm;
        public string MenuTargetNorm { get => _menuTargetNorm; }

        public string TargetPhrases { get; set; }     // "2;Wingman;Heya"
        public string MenuName { get; set; }          // "Combat 3"  // Caller must pass THIS string when trying to bring up a menu, not whatever user said

        protected string _menuNameNorm;
        public string MenuNameNorm { get => _menuNameNorm; }
        public string MenuNamePhrases { get; set; }   // "Combat [Management;] 3"
        public string MenuShow { get; set; }          // If this matches a VA phrase in the profile, execute that command, else presses chars passed

        public MenuItemBMS[] MenuItemsBMS;
        public string AllMenuItemPhrases;

        static public string MakeFullID(string menuTarget, string menuName)
        {
            if (string.IsNullOrEmpty(menuTarget) || string.IsNullOrEmpty(menuName))
                return string.Empty;
            return menuTarget.ToLower() + "_" + menuName.ToLower();
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

        static public bool NormalizeTarget(string target, out string targetNorm)
        {   return NormalizeName(target, out targetNorm); }
       static public bool NormalizeMenuName(string menuName, out string menuNameNorm)
        {   return NormalizeName(menuName, out menuNameNorm); }
        static protected bool NormalizeName(string name, out string nameNorm)
        {
            nameNorm = string.Empty;
            if (string.IsNullOrEmpty(name)) { return false; }

            nameNorm = name.TrimEnd(s_normTrimChars);
            nameNorm = nameNorm.ToLower();
            return true;
        }

        public bool ContainsNormMenuTargetPhrase(string menuTargetPhrase)
        {
            if (string.IsNullOrEmpty(menuTargetPhrase))
            {   return false; }   

            return _extractedNormTargetPhrases.Contains(menuTargetPhrase);
        }

        public bool ContainsNormMenuNamePhrase(string menuNamePhrase)
        {
            if (string.IsNullOrEmpty(menuNamePhrase))
            { return false; }

            return _extractedNormMenuNamePhrases.Contains(menuNamePhrase);
        }

        public MenuBMS(dynamic vaProxy, string tgt, string tgtPhrases, string name, string namePhrases, string show, MenuItemBMS[] items)
        {
            Logger.StructuresWrite(vaProxy, "In MenuBMS Constructor");

            _extractedMenuItems = new Dictionary<string, string>();
            _extractedNormTargetPhrases = new HashSet<string>();
            _extractedNormMenuNamePhrases= new HashSet<string>();

            MenuTarget = tgt;
            TargetPhrases = tgtPhrases;
            MenuName = name;
            MenuNamePhrases = namePhrases;
            MenuShow = show;

            MenuItemsBMS = items;

            MenuFullID = string.Empty; // Starts empty
            AllMenuItemPhrases = string.Empty;

            // MakeFullID has its own error checks and will return string.Empty on error
            if (string.IsNullOrEmpty(MenuTarget) || string.IsNullOrEmpty(TargetPhrases)
                || string.IsNullOrEmpty(MenuName) || string.IsNullOrEmpty(MenuNamePhrases)
                || string.IsNullOrEmpty(MenuShow)
                || items == null || items.Length == 0)
            {
                Logger.Error(vaProxy, "Unexpected failure creating MenuBMS - Invalid Parameters passed to constructor");
                return;
            }

            // Keep everything lowercase
            MenuTarget = MenuTarget.ToLower();
            TargetPhrases = TargetPhrases.ToLower();
            MenuName = MenuName.ToLower();
            MenuNamePhrases = MenuNamePhrases.ToLower();
            MenuShow = MenuShow.ToLower();

            // Make MenuFullID out of MenuTarget and MenuName
            MenuFullID = MakeFullID(MenuTarget, MenuName);
            if (string.IsNullOrEmpty(MenuFullID))
            {
                MenuFullID = string.Empty;
                Logger.Error(vaProxy, "Unexpected failure creating MenuBMS MenuFullID");
                return;
            }

            MenuBMS.NormalizeTarget(MenuTarget, out _menuTargetNorm);
            MenuBMS.NormalizeMenuName(MenuName, out _menuNameNorm);

            string[] allTargetPhrases = vaProxy.Utility.ExtractPhrases(TargetPhrases);
            if (allTargetPhrases.Length == 0)
            {
                Logger.Warning(vaProxy, $"Unexpected failure extracting discrete MenuTargetPhrases for {MenuFullID}");
                // Continue anyway - this only blocks "List ... Menus" commands
            }
            else
            {
                foreach ( string tgtPhrase in allTargetPhrases )
                {
                    string tgtNorm = string.Empty;
                    if (MenuBMS.NormalizeTarget(tgtPhrase, out tgtNorm) ) 
                    {   _extractedNormTargetPhrases.Add(tgtNorm); }
                }
            }
            string[] allMenuNamePhrases = vaProxy.Utility.ExtractPhrases(MenuNamePhrases);
            if (allMenuNamePhrases.Length == 0)
            {
                Logger.Warning(vaProxy, $"Unexpected failure extracting discrete MenuNamePhrases for {MenuFullID}");
                // Continue anyway - this only blocks "List ... Menus" commands
            }
            else
            {
                foreach (string menuPhrase in allMenuNamePhrases)
                { 
                    string menuNorm = string.Empty;
                    if (MenuBMS.NormalizeMenuName(menuPhrase, out menuNorm))
                    {   _extractedNormMenuNamePhrases.Add(menuPhrase); }
                }
            }

            Logger.StructuresWrite(vaProxy, "MenuBMS MakeFullID Finished");

            // Create a string with all commands, ; delimeted, to pass to Get User Input - Wait for Spoken Response 
            // AND Fill in _extractedMenuItems dictionary with all possble phrases, mapped to their MenuItemExecute values
            // ie "Weapons Free [Air;A A;]" separated into "Weapons Free Air", "Weapons Free A A" and "Weapons Free"
            foreach (MenuItemBMS menuItem in MenuItemsBMS)
            {
                Logger.StructuresWrite(vaProxy, "menuItem " + menuItem.MenuItemPhrases + " : " + menuItem.MenuItemExecute);

                AllMenuItemPhrases = AllMenuItemPhrases.Insert(AllMenuItemPhrases.Length, menuItem.MenuItemPhrases + ";");
                string[] MenuItemPhrases = menuItem.ExtractedMenuItemPhrases;
                string MenuItemExecute = menuItem.MenuItemExecute;

                Logger.StructuresWrite(vaProxy, "    Set MenuItemPhrases array of extracted phrases and MenuItemExecute");

                foreach (string MenuItemPhrase in MenuItemPhrases)
                {
                    if (_extractedMenuItems.ContainsKey(MenuItemPhrase))
                    {
                        // This is a duplicate - skip it, but log
                        string DupedExecute = _extractedMenuItems[MenuItemPhrase];
                        string AddWarningMessage = "Duplicate Extracted Phrase " + MenuItemPhrase + " in Menu " + MenuName + "\n Leaving version with Menu Item Key " + DupedExecute + "\n Dropping version with Menu Item Key " + MenuItemExecute;
                        Logger.Warning(vaProxy, AddWarningMessage);
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
                            Logger.Error(vaProxy, "Unexpected failure to add an extracted menu item phrase to dictionary.");
                            string AddErrorMessage = " MenuItemPhrase = " + MenuItemPhrase + "\n MenuItemExecute = " + MenuItemExecute;
                            Logger.Error(vaProxy, AddErrorMessage);
                            // Letting the loop continue here, in case this was just a duplicate we failed to catch
                        }
                    }
                }
            }
            // Get rid of that last ";" so we don't match an empty string
            AllMenuItemPhrases = AllMenuItemPhrases.TrimEnd(';');
        }

    };

}
