# Jeeves BMS Radio Menus (Tanrr.VAPlugin.BMSRadio) 

version 0.0.7

Jeeves BMS Radio Menus is a simple but powerful plugin for VoiceAttack to work with BMS radio menus. 

It is a flexible solution with more features than simpler VA profiles, while being relatively fast and light weight.

Users can easily call up radio menus by saying a menuTarget such as "2" or "Flight" followed by an appropriate menuName, such as "Combat 3."  The menu is displayed and the plugin will listen for phrases that match the current menu's menu items.  This lets users who are not familiar with the menus look through them to make their choices, and will even warn them when they ask for a menu doesn't exist such as "Flight, Combat 3."


The system is designed for flexibility while remaining reasonably simple.  It is easy to update JBMS to match updates to the game, and to change the recognition phrases for menuTargets, menuNames, and menuItems to work for you.  Additional ease of use features include adding direct commands that don't leave menus up, as well as the ability to have menu items call your own methods in the VoiceAttack profile. 

### **The extended functionality is what makes this useful:**

- *All the menus and their data about key combinations are in a human readable JSON file, so you can change any and all recognition phrases.  You can easily update the JSON to match changes to the games menu items, or even add new menus as needed.  The VoiceAttack profile is relatively simple to modify to your needs.*

- *The menu items can be configured to do keypresses or to call your own named command in VA.* 

- *A particular menu item can be given a custom "callable" name, so you can add your own VA phrase to directly run that menu item without waiting (say for "Bogey Dope").*

- *Since the list of menuItem phrases in the JSON file are not directly part of the VA profile, your phrases can duplicate menuItem phrases without it being a problem. For example, you can match the direct phrase "Attack My Target" to execute the menu item for "Wingman, Combat Management 1" without it causing problems for the "Attack My Target" menu items for Wingman, Element, or Flight.*

- *A JSON schema is provided to validate the menu JSON file.  This is done automatically on program load, or you can manually verify your changes match the schema as you edit the menu file by using a JSON schema validator like https://jsonmate.com/.*

- *When the plugin loads the menu JSON it will check your phrases to make sure you haven't generated duplicate or empty phrases within each menu.*

## **INSTALLATION**:

*This plugin uses Newtonsoft.Json.dll version 13.0.2, and Newtonsoft.Json.Schema.dll version 3.0.14.  They are included, or you can download your own versions. Note that STEP 3 copies the Newtonsoft.Json.dll file to a different directory.*

1. Make sure that *"Enable Plugin Support"* is enabled (VoiceAttack Settings (Wrench icon) under "General"), then shut down VoiceAttack
2. Move the **Tanrr.VAPlugin.BMSRadio** folder into the folder **..\VoiceAttack\Apps**.  This should leave the dlls and related files under **..\VoiceAttack\Apps\Tanrr.VAPlugin.BMSRadio**.
3. **IMPORTANT**: From the **Tanrr.VAPlugin.BMSRadio** folder, *copy* the **Newtonsoft.Json.dll** file into the **..\VoiceAttack\Shared\Assemblies** folder.
4. Launch VoiceAttack and import **Jeeves BMS Radio Menus Profile.vap**
5. *HOTKEYS:* The VoiceAttack profile provided only sets a hotkey of F24 (hold to listen) for VoiceAttack.  You will want to change the hotkeys to whatever keyboard keys or game controller buttons you use.  Hotkeys can be set globally (VoiceAttack Settings, Hotkeys) or just for the profile (Edit Profile, Options, Profile Hotkeys).

\*\* *See recomended VoiceAttack settings at the end of this document* \*\*

## DETAILS:

### BMS Menus

Details of the BMS Radio menus can be found **BMS-Comms-Nav-Book.pdf** in which is in your BMS folder under **Docs\00 BMS Manuals**. The relevant sections are:

- 1.1 AIRBASE COMM PLAN: (ATC) Just after the 1.1.7 Contingencies section
- 2.1.2 Your AI Wingmen/Element/Flight: The Wingman/Element & Flight radio menu
- 2.2.1 Tactical Net: (AWACS)
- 2.2.2 Tanker
- 2.2.3 JTAC

### Limitations

- The plugin uses the default keys for BMS menus (T for ATC; W/E/R for Wingman/Element/Flight, Q for AWACS, Y for Tanker/JTAC).  If you have changed these shortcuts you should edit the JSON to use your shortcuts.
- The plugin's menu data is for BMS version 4.37.1 menus.  If you have a newer version of BMS or have modified the menu layout by changing the BMS ##Data/Art/CkptArt/menu.dat## file you should update the JSON file to match your changes.
- The plugin's JSON files must be in UTF-8 format and DO NOT support UTF-16 or UNICODE
- menuItemExecute strings currently only support visiible characters, do not support modifiers such as SHIFT, CTRL, or ALT, and do not support separate numpad characters. Support for these may be added to later versions.


### VoiceAttack Variables

**\>JBMS** variables can be changed by the VA profile, but should only be used in the same way (to pass info to the plugin, or check return states from the plugin).  Feel free to set the Boolean **_LOG** variables (initialized in VA command *"JBMS Initial Load Init"*) to true for additinal logging, with slightly slower performance.

**\>JBMSI** internal variables should NOT be changed by the VA profile, EXCEPT for the provided methods provided that use them. Changes to them could easily break the plugin.

- **\>JBMS_JSON_LOG**	-       Boolean to add logging of JSON parsing
- **\>JBMS_MENUITEMS_LOG**	- Boolean to add logging of menu items (shows list of menu items in log)
- **\>JBMS_STRUCTURES_LOG** - Boolean to add logging of data structures manipulation
- **\>JBMS_VERBOSE_LOG** -   Boolean to add more verbose general logging

- **\>JBMS_MENU_TGT**	-      Change ONLY to set menuTarget before calling plugin with context *"JBMS_SHOW_MENU"*
- **\>JBMS_MENU_NAME**	-      Change ONLY to set menuName before calling plugin with context *"JBMS_SHOW_MENU"*
- **\>JBMS_DIRECT_CMD**	-    Change ONLY to set directCommand before calling plugin with context *"JBMS_DIRECT_CMD"*

- **\>JBMSI_NO_SUCH_MENU** -	READ-ONLY - (Checked and set only by VA *"JBMS Radio Menu Show"* command)
- **\>JBMSI_MENU_RESPONSE** -	READ-ONLY - (Checked and set only by VA *"JBMS Wait For Menu Response"* command)

- **\>JBMSI_MENU_UP** -		    INTERNAL, do not use
- **\>\>JBMSI_INITED** -         INTERNAL, do not use


### JSON FORMAT:

The JSON menu file **Tanrr.VAPlugin.Radio.Menus.json** is a top level array of menus. Each menu has the format shown below.

**EXAMPLE JSON MENU:**

	{
		"menuTarget": "Flight",
		"targetPhrases": "Flight",
		"menuName": "Miscellaneous 1",
		"menuNamePhrases": "[Miscellaneous;Misk] [1;]",
		"menuShow": "RRRRRRR",

		"menuItems": [
			[ "Fence In", "1", "JBMS-FX1-FENCE-IN" ],
			[ "Fence Out", "2" ],
			[ "Lights [S O P;Ess Oh Pea]", "3" ],
			[ "Music On", "4" ],
			[ "Music Off", "5" ],
			[ "[Turn;] Smoke On", "6" ],
			[ "[Turn;] Smoke Off", "7" ],
			[ "[Switch;Push] Flight Uniform", "DC Push Flight Uniform" ],
			[ "[Switch;Push] Flight Victor", "9" ],
			[ "Set Bingo", "0" ]
		]
	},

**REQUIRED:**
- All values are text strings in *"Quotes"*
- The combination of menuTarget+menuName must be unique for all menus in the JSON
- The menuShow text/command must be unique for all menus in the JSON
- All named items must be non-empty
- menuItems Array line includes multiple menuItem arrays:
  - First string: *menuItemPhrases* to match that menu item
  - Second string: *menuItemExecute* keys/command to execute for that menu item
  - Optional third string: *menuItemDirectCmd* identifier for the menu item so a VA command can call it directly
  
**NOTES:**
  - *targetPhrases* and *menuNamePhrases* are not currently used, but should match the VA profile's prefix & suffix phrases for this menu as they are planned to be used later.  If you update the phrases in the VoiceAttack profile, please update the phrases in the JSON as well.

**WORKING WITH MENU JSON and VOICEATTACK PROFILE:**

The menu JSON menu file and schema file should have been installed to **..\VoiceAttack\Apps\Tanrr.VAPlugin.BMSRadio**. 
You can use an online JSON schema validator, like https://jsonmate.com/ to validate the menu JSON against the schema while you work on it.

Menu JSON: **Tanrr.VAPlugin.Radio.Menus.json**

Schema JSON: **Tanrr.VAPlugin.Radio.Schema.json**

PHRASES TO SELECT MENU ITEMS:

*MenuItemPhrases* (1st text field in each array within *menuItems*) are the VoiceAttack command phrases the plugin will listen for while this menu is up.  They are the same format as VoiceAttack commands: multiple options, seprated by semi-colons, sub-sections specified by \[brackets\].  Note that sub-sections can match to nothing as well by having a ; with nothing after it.

For example: *"\[Switch;Push\] \[Flight;\] Uniform"* matches all of the following phrases:
- "Switch Flight Uniform"
- "Push Flight Uniform"
- "Switch Uniform"
- "Push Uniform"

The provided VA profile already includes the command "Press \[0..9\]"" which will press the number keys for you, and these work even if the menu is not up.  DO NOT add the menu number as part of the recognition phrases.  It could interfere with other phrases - for example *"2"* and *"3"* are used as prefix *menuTargets*.  

KEYSTROKES FOR MENU ITEMS:

*MenuItemExecute* (2nd text field in each array within *menuItems*) are normally set to the keystrokes to press when that menu item is chosen. These keys will be passed to the the VA profile *"JBMS Press Keys"* command which will simulate the keypresses.  (Unless they match a VA command as described later.) 

You can change the *"JBMS Press Keys"* command in the VA profile by adjusting the keypress and pause times.  Those times are set conservatively long to work on all systems, so if you change the times make sure you test that they work for busy multiplayer servers and your own system.

See the *"DC Push Flight Uniform"* command in the VA profile as an example of using MenuItemExecute as a command.

MAPPING VOICEATTACK PHRASES DIRECTLY TO MENU ITEMS:

Direct Command Calls (to execute a menu item directly *from* a VA command phrase without leaving a menu up and waiting for a menu selection) are made possible by adding a third *menuItemDirectCmd* string to the array for that menu item in *menuItems*.  These should be unique and preferably identify the *menuTarget*, *menuName*, and *menuItem* they're for, as described in the *menuTarget/menuName* abbreviations section later on.

To use this functionality, add your own command phrase to the VA profile, and have it set the **\>JBMS_DIRECT_CMD** variable to the command name before calling the plugin with context set to *JBMS_DIRECT_CMD*.

The provided VA profile has some examples of this:

- "Bogey Dope" mapped to "JBMS-QV1-BOGEY-DOPE" (Shortcut for "Awacs - Vectors", "Bogey Dope")
- "Awacs Declare" mapped to "JBMS-QV1-BOGEY-DOPE" (Shortcut for "Awacs - Tactical", "Declare")
- "Fence In" mapped to the "JBMS-FX1-FENCE-IN" (Shortcut for "Flight - Miscellaneous", "Fence In")
- "Attack My Target" mapped to "JBMS-WC1-ATTACK-TGT" (Shortcut for "2 - Combat 1", "Attack My Target")

Note that it is OKAY for your VA profile to use phrases that duplicate the phrases for the various menu items, because (1) the menu item phrases are only listened for when the specific menu is up, and (2) duplicate phrases that call JBMS_DIRECT_CMD while a menu is up will be dropped as the menu phrases have priority.

MAPPING MENU ITEMS TO YOUR OWN CUSTOM COMMANDS:

If you set *menuItemExecute* (2nd text field in each array within *menuItems*) to match the name of a **unique** command within the VA profile, the plugin will execute that command, instead of pressing keys matching the letters in the string. It is strongly recommended that you make such commands NOT callable by voice to avoid conflicting menu states.  Your implementation of such a VA command should NOT call back into the plugin with a "JBMS_DIRECT_COMMAND" for the same menu item as this would create a never-ending loop.

Your *menuItemExecute* VA command (if not keypresses) should not be long running as the plugin will wait for it to return. Your command can look at the **\>JBMS_MENU_TGT** and **\>JBMS_MENU_NAME** variables which will still be valid till your command returns. Your command is responsible for making sure the menu is dismissed, either by pressing a key to choose a menu item, or by executing *"Dismiss Menu Only"* command in the VA profile. Your command SHOULD NOT call the plugin with context *JBMS_RESET_MENU_STATE* as that will be done by the plugin after your command finishes.

**MENUTARGET & MENUNAME ABBREVIATIONS (NOT REQUIRED BUT HELPFUL):**

menuTarget abbreviations:
  W-ingman
  E-lement
  F-light
  T-ATC
  Q-AWACS
  Y-Tanker/JTAC

menuName abbreviations (followed by 1 or 2/3 etc. for multiples)
  C-ombat
  M-ission
  F-ormation
  X-Misc
  R-unway
  
  G-round
  T-ower
  A-pproach
  D-eparture
  N-Common
  C-arrior & LSO
  B-Abort

  Q-Awacts Tactical (so QQ)
  V-Awacs Vectors   (so QV)

  T-Tanker (so YT)
  J-JTAC (so YJ)
  
For example, Wingman, Combat 1 menu for Attack My Target 
would be "JBMS-WC1-ATTACK-TGT" or something similar.


## VOICEATTACK BASICS & RECOMMENDED SETTINGS

**BASICS:**
- *Only have VoiceAttack listen when you are holding down a key - don't leave it listening all the time.*
- Turn on "Show Confidence Level" in VA Settings (wrench), Recognition tab to see how well/poorly VA is understanding you.

**RECOMMENDED SETTINGS:**

*VoiceAttack Settings, Recognition tab: See VA docs under "Recognition Tab" for deeper descriptions.*

- *Recognized Speech Delay:* 10-30 - Helps VA differentiate between "Awacs" vs "Awacs Vectors" and combination phrases.

- *Unrecognized Speech Delay:* 0

- *Command Weight:* 80-95 - Higher values here tell VA to try to make whatever you are saying fit to one of the phrases in the profile.  If you set this to 100 it will ALWAYS pick the closest match it can, so be careful.

- *Minimum Confidence Level:* 30-70

   The minimum confidence level needed for a match. VA might recognize "Combat 1" but have a low confidence because you didn't say it clearly.  Run VA with "Show Confidence Level" enabled to see if how you should adjust this. I have mine at 35.
   
   **NOTE 1:** This can also be set in a profiles options, or for a specific command.
   
   **NOTE 2:** *"JBMS Wait For Menu Response"* VA Command specifically sets this very low, as it is extremely likely, since you just called up a menu, that your next words will be items in the menu.

- *Min Unrecognized Confidence Level:* 30-60 - Higher numbers throw out more "unrecognized" sounds.  Only need to turn this up if you're in a noisy environment.

- Disable Adaptive Recognition: OFF (unless VA seems to become unresponsive often)
- Disable Acoustic Echo Cancellation: ON (but can effect other apps, and OFF is usually ok)
- Reject Pending Speech: OFF

