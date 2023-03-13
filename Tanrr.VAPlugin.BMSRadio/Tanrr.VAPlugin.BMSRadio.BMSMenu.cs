using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanrr.VAPlugin.BMSRadio
{
    public class MenuItemBMS
    {
        // Single menu line item that has the phrases menuItem matches to, the keys/command to execute if selected
        // and an optional direct command that should cause this menuItem to be executed if called from the

        public string _menuItemPhrases;              // Possibly multiple phrases
        public string _menuItemExecute { get; set; }              // Execute as string can be chars to press or a named VA command to execute
        public string _menuItemDirectCmd { get; set; }            // Has to be unique string, used by VA profile to call plugin to execute this menuItem
        public string[] _extractedMenuItemPhrases { get; set; }   // Array of all possible versions of the phrases, extracted separately, lowercase

        public string MenuItemPhrases 
        { get => _menuItemPhrases; set => _menuItemPhrases = value; }              
        public string MenuItemExecute 
        { get => _menuItemExecute; set => _menuItemExecute = value; }              
        public string MenuItemDirectCmd 
        { get => _menuItemDirectCmd; set => _menuItemDirectCmd = value; }            
        public string[] ExtractedMenuItemPhrases 
        { get => _extractedMenuItemPhrases; set => _extractedMenuItemPhrases = value; }

        public bool HasDirectCmd() { return !string.IsNullOrEmpty(MenuItemDirectCmd); }  
        public MenuItemBMS(dynamic vaProxy, string itemPhrases, string itemExec, string directCmd = null)
        {

            if (itemPhrases == null || itemExec == null)
            {
                SetEmpty();
                return;
            }

            _menuItemPhrases = RemoveEmptyMatches(itemPhrases.ToLower());
            _menuItemExecute = itemExec;
            if (_menuItemPhrases.Length == 0 || _menuItemExecute.Length == 0)
            {
                SetEmpty();
                return;
            }

            _extractedMenuItemPhrases = vaProxy.Utility.ExtractPhrases(_menuItemPhrases);
            if (_extractedMenuItemPhrases.Length == 0)
            {
                SetEmpty();
                return;
            }

            if (!string.IsNullOrEmpty(directCmd))
            {
                _menuItemDirectCmd = directCmd;
            }
        }

        void SetEmpty()
        {
            _menuItemPhrases = string.Empty;
            _menuItemExecute = string.Empty;
            _menuItemDirectCmd = string.Empty;
            _extractedMenuItemPhrases = new string[] { string.Empty };
        }

        // Clear out starting, ending, or double ";" in strings - needed to avoid empty matches
        // Internal null matches allowed, but not a full match - "Attack [Air;AA;];Get Them" is fine, but "Attack [Air;AA;];Get Them;" is not
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
        // Main data class containing a single menu (menuTarget+menuName) and a list of that menu's menuItems

        protected static char[] s_normTrimChars = {' ', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

        protected Dictionary<string, string> _extractedMenuItems;

        protected HashSet<string> _extractedNormTargetPhrases;
        protected HashSet<string> _extractedNormMenuNamePhrases;

        protected string _menuFullID;       // MenuTarget_MenuName for easy ident, generated on creation

        public string    _menuTarget;       // "Wingman"   // Caller must pass THIS string when trying to bring up a menu, not whatever user said
        protected string _menuTargetNorm;   // "wingman"
        protected string _targetPhrases;    // "2;Wingman;Heya"

        protected string _menuName;         // "Combat 3"  // Caller must pass THIS string when trying to bring up a menu, not whatever user said
        protected string _menuNameNorm;     // "combat"
        protected string _menuNamePhrases;  // "Combat [Management;] 3"
        protected string _menuShow;         // If this matches a VA phrase in the profile, execute that command, else presses chars passed

        protected string _allMenuItemPhrases;

        public MenuItemBMS[] MenuItemsBMS;

        // TODO - Make these get only, rename as protected members with _

        public string MenuFullID { get => _menuFullID; set => _menuFullID = value; }

        public string MenuTarget { get => _menuTarget; set => _menuTarget = value; }
        public string MenuTargetNorm { get => _menuTargetNorm; }
        public string TargetPhrases { get => _targetPhrases; set => _targetPhrases = value; }
        
        public string MenuName { get => _menuName; set => _menuName = value; }
        public string MenuNameNorm { get => _menuNameNorm; }
        public string MenuNamePhrases { get => _menuNamePhrases; set => _menuNamePhrases = value; }
        public string MenuShow { get => _menuShow; set => _menuShow = value; }

        public string AllMenuItemPhrases { get => _allMenuItemPhrases; set=> _allMenuItemPhrases = value; }

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

            _menuTarget = tgt;
            _targetPhrases = tgtPhrases;
            _menuName = name;
            _menuNameNorm = namePhrases;
            _menuShow = show;

            MenuItemsBMS = items;

            _menuFullID = string.Empty; // Starts empty
            _allMenuItemPhrases = string.Empty;

            // MakeFullID has its own error checks and will return string.Empty on error
            if (string.IsNullOrEmpty(_menuTarget) || string.IsNullOrEmpty(_targetPhrases)
                || string.IsNullOrEmpty(_menuName) || string.IsNullOrEmpty(_menuNameNorm)
                || string.IsNullOrEmpty(_menuShow)
                || items == null || items.Length == 0)
            {
                Logger.Error(vaProxy, "Unexpected failure creating MenuBMS - Invalid Parameters passed to constructor");
                return;
            }

            // Keep everything lowercase
            // TODO: Maybe keep FullID, Target, and MenuName mixed case for readability?  Slows matching/dictionary though
            _menuTarget = _menuTarget.ToLower();
            _targetPhrases = _targetPhrases.ToLower();
            _menuName = _menuName.ToLower();
            _menuNameNorm = _menuNameNorm.ToLower();
            _menuShow = _menuShow.ToLower();

            // Make MenuFullID out of MenuTarget and MenuName
            _menuFullID = MakeFullID(_menuTarget, _menuName);
            if (string.IsNullOrEmpty(_menuFullID))
            {
                _menuFullID = string.Empty;
                Logger.Error(vaProxy, "Unexpected failure creating MenuBMS MenuFullID");
                return;
            }

            MenuBMS.NormalizeTarget(_menuTarget, out _menuTargetNorm);
            MenuBMS.NormalizeMenuName(_menuName, out _menuNameNorm);

            string[] allTargetPhrases = vaProxy.Utility.ExtractPhrases(_targetPhrases);
            if (allTargetPhrases.Length == 0)
            {
                Logger.Warning(vaProxy, $"Unexpected failure extracting discrete MenuTargetPhrases for {_menuFullID}");
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
            string[] allMenuNamePhrases = vaProxy.Utility.ExtractPhrases(_menuNameNorm);
            if (allMenuNamePhrases.Length == 0)
            {
                Logger.Warning(vaProxy, $"Unexpected failure extracting discrete MenuNamePhrases for {_menuFullID}");
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

            // Create a string with all lowercase commands, ; delimeted, to pass to Get User Input - Wait for Spoken Response 
            // AND Fill in _extractedMenuItems dictionary with all possble phrases, mapped to their MenuItemExecute values
            // ie "Weapons Free [Air;A A;]" separated into "weapons free air", "weapons free a a" and "weapons free"
            foreach (MenuItemBMS menuItem in MenuItemsBMS)
            {
                Logger.StructuresWrite(vaProxy, "menuItem " + menuItem.MenuItemPhrases + " : " + menuItem.MenuItemExecute);

                _allMenuItemPhrases = _allMenuItemPhrases.Insert(_allMenuItemPhrases.Length, menuItem.MenuItemPhrases + ";");
                string[] MenuItemPhrases = menuItem.ExtractedMenuItemPhrases;
                string MenuItemExecute = menuItem.MenuItemExecute;

                Logger.StructuresWrite(vaProxy, "    Set MenuItemPhrases array of extracted phrases and MenuItemExecute");

                foreach (string MenuItemPhrase in MenuItemPhrases)
                {
                    if (_extractedMenuItems.ContainsKey(MenuItemPhrase))
                    {
                        // This is a duplicate - skip it, but log
                        string DupedExecute = _extractedMenuItems[MenuItemPhrase];
                        string AddWarningMessage = "Duplicate Extracted Phrase " + MenuItemPhrase + " in Menu " + _menuName + "\n Leaving version with Menu Item Key " + DupedExecute + "\n Dropping version with Menu Item Key " + MenuItemExecute;
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
            // Get rid of that last ";" so we don't allow matching an empty string
            _allMenuItemPhrases = _allMenuItemPhrases.TrimEnd(';');
        }

    };

}
