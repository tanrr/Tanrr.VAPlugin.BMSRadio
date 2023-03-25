# Jeeves BMS Radio Menus (Tanrr.VAPlugin.BMSRadio) 

v0.1.6 - tested with Falcon BMS 4.37.2 - (for BMS 4.37.1 use plugin v0.1.2)

Jeeves BMS Radio Menus is a simple but powerful plugin for VoiceAttack 
to work with Falcon BMS radio menus. 

It is a flexible solution with more features than simpler VA profiles, 
while being relatively fast and light weight.

Users can easily call up radio menus by saying a menuTarget 
such as *"2"* or *"Flight"* followed by an appropriate menuName, 
such as *"Combat 3"*.  While the menu is displayed the plugin 
listens for phrases that match its menu items.
Thus users who are not familiar with the menus can look through 
them to make their choices.

Calls outside your flight are similar, but may not need the menu name. 
For example, *"Tanker"* or *"Tower"* will just bring up the tanker or tower menu.

As of v0.1.5 **you can now use over 150 callsigns for AWACS, tankers, JTAC and your own flight!** 
Calls like *"Arco, Viper 2 - Request Flight Refueling"* just work. 
You can change your own callsign on the fly by voice command.

The system is designed for flexibility while remaining reasonably simple.
It is easy to update JBMS to match updates to the game, 
to change the recognition phrases for menuTargets, menuNames, 
and menuItems to work for you.  Additional ease of use features include 
adding direct commands that don't leave menus up, as well as the ability 
to have menu items call your own methods in the VoiceAttack profile. 

This is a personal project, so use at your own risk.
With that said, I hope to keep this up to date with new versions of 
Falcon BMS and welcome feedback through GitHub's Issue reporting.
The current GitHub project is at 
https://github.com/tanrr/Tanrr.VAPlugin.BMSRadio.

### **The extended functionality is what makes this useful:**

- *Menus and their key combinations are in a human readable JSON file, 
so you can change recognition phrases or keystrokes. 
It's easy to update to match changes to menu items, 
or add new menus as needed. 
The VoiceAttack profile is relatively simple to modify.*

- *Menu items can be configured to do keypresses or to call your 
own named command in VA.* 

- *A menu item can be given a custom "callable" name, 
so you can add your own VA phrase to directly run that menu item 
without waiting (say for "Bogey Dope").*

- *Your profile command phrases can duplicate menuItem phrases without a conflict. 
For example, the VA profile can match the direct phrase "Attack My Target" 
to execute the menu item for "Wingman, Combat Management 1" without 
it causing problems for the "Attack My Target" menu items for Element or Flight.*

- *MULTIPLE CALLSIGNS for pilots, AWACS, JTAC, and tankers are included 
in a human readable JSON file. Any AWACS, JTAC, or tanker callsign will 
match automatically and can be used with numbers (but not position number), 
though you can still just say "Tanker" or "JTAC". 
The default pilot callsign, flight number, and position in flight is set 
and will be matched, but is not required. **You can change your callsign, 
flight number, and position on the fly by voice** picking from the list of
valid callsigns. An example of use is below:*
```
    Arco 3, Viper 1 - Request Flight Refueling
    Update Callsign Hunter 6
        <<Say your flight position in Hunter 6 or say 'Skip'>>
    Three
        <<New Callsign Hunter 6 3, Reinitializing Profile>>
    Tower, Hunter 6 3 - Emergency!
    Axeman 2, Hunter 6 - Check In
```

- *JSON schemas are provided to validate the menu and callsign JSON files. 
This is done automatically on program load.  A better option, though,
is to manually verify your changes against the schema as you edit by 
using a JSON schema validator like https://jsonmate.com/.*

- *When the plugin loads the menu JSON it will check your phrases 
to make sure you haven't generated duplicate or empty phrases 
within each menu.  The callsign JSON is similarly checked.*

- *To help learn the menus, ask the plugin to list menus by target or name.
"List Wingman Combat Menus", "List ATC Menus", "List All Formation Menus", etc. 
Note that to list all menus for a menuTarget you must say something in the format 
"List Wingman Menus" ("List All Wingman Menus" won't be understood).
The amount of time each menu is displayed when listing menus is adjustable in the profile.*

- *To help with radio tuning, VA commands for 
"Push [Uniform;Victor] [1..20]" are included, to allow you to 
quickly change between UHF and VHF presets.  You can also say 
"Press [0..9]" to generate a keystroke for a number, or "Press Escape."*

- *Logging levels can be set on the fly by voice with "Enable/Disable Menu/JSON/Struct/Verbose Logging"*

## **INSTALLATION**:

*This plugin uses Newtonsoft.Json.dll version 13.0.2, 
and Newtonsoft.Json.Schema.dll version 3.0.14.  
They are included, or you can download your own versions. 
Note that STEP 3 copies the Newtonsoft.Json.dll file to a different directory.*

### Remove Any Previous Install

- Run VoiceAttack and delete or rename "Jeeves BMS Radio Profile"  
Rename if you customized the profile so you can copy command phrases to the new profile
- If renaming, change the active profile so your old profile won't 
initialize with the newer version of the plugin
- Shut down VoiceAttack
- Save a copy of the **Tanrr.VAPlugin.Radio.Menus.json** file if you customized it
- Delete the **Tanrr.VAPlugin.BMSRadio** folder in **..VoiceAttack\Apps**

### Install New Version

1. Make sure that *"Enable Plugin Support"* is enabled 
(VoiceAttack Settings (Wrench icon) under "General"), then shut down VoiceAttack
2. Move the **Tanrr.VAPlugin.BMSRadio** folder into the folder 
**..\VoiceAttack\Apps**.  This should leave the dlls and related files 
under **..\VoiceAttack\Apps\Tanrr.VAPlugin.BMSRadio**.
3. **IMPORTANT**: From the **Tanrr.VAPlugin.BMSRadio** folder, 
**COPY** the **Newtonsoft.Json.dll** file into the 
**..\VoiceAttack\Shared\Assemblies** folder.  
If you have different versions of **Newtonsoft.Json.dll** installed, 
you may need to also copy **VoiceAttac.exe.config** from the plugin 
folder to the folder that **VoiceAttack.exe** is in.
4. Launch VoiceAttack and import **Jeeves BMS Radio Menus Profile.vap**
5. Shut down, then relaunch VoiceAttack, so the updated profile 
can initialize the updated plugin
6. If you had custom phrases in the JSON file, merge them by hand (usually easy to do).  
If you had custom phrases in the VA profile, copy them to the new profile. 
7. *HOTKEYS:* The VoiceAttack profile provided only sets a hotkey 
of F24 (hold to listen) for VoiceAttack. You will want to change 
the hotkeys to whatever keyboard keys or game controller buttons you use. 
Hotkeys can be set globally (VoiceAttack Settings, Hotkeys) 
or just for the profile (Edit Profile, Options, Profile Hotkeys).
8. Optionally restrict plugin from generating keystrokes if BMS does not have focus. 
See *Restricting Plugin when BMS Not Active*

\*\* *See recommended VoiceAttack settings at the end of this document* \*\*

## DETAILS:

### Restricting Plugin when BMS Not Active

The plugin can listen for application focus changes so if you alt+tab away 
from BMS it can stop sending keystrokes until BMS has focus again. To allow 
the plugin to catch app focus changes, Select **"Enable Auto Profile Switching"** 
in VoiceAttack Settings, General Tab and set **>JBMS_KEYS_ONLY_TO_BMS** to true 
in *JBMS Initial Load Init* in VA profile. You will need to restart VoiceAttack
after this for the plugin to get focus change events.

You can change enable or disable the focus requirement with the voice command 
*"Enable/Disable B M S Focus Only"*.

Note that this isn't a perfect solution, but it should avoid most problems.
If a menu was up, or you were listing menus, when you alt-tabbed away 
the plugin will stop listening for menu items or iterating menus. 
You can still say *"Press [0..9]"* to choose a menu item for a menu that is up, 
or just say *"Cancel"* or *"Reset Menu"* to bring it down.

### BMS Menus

Details of the BMS Radio menus can be found **BMS-Comms-Nav-Book.pdf** 
in which is in your BMS folder under **Docs\00 BMS Manuals**. 
The relevant sections are:

- 1.1 AIRBASE COMM PLAN: (ATC) Just after the 1.1.7 Contingencies section
- 2.1.2 Your AI Wingmen/Element/Flight: The Wingman/Element & Flight radio menu
- 2.2.1 Tactical Net: (AWACS) ; - * 2.2.2 Tanker ; - * 2.2.3 JTAC

### Limitations

- Current menu data is for BMS 4.37.2 menus (use plugin v0.1.2 for BMS 4.37.1)
  For newer versions of BMS, or if you have modified the menu 
  by changing the BMS **Data/Art/CkptArt/menu.dat** file, 
  update the JSON file to match your changes.
- No callsigns for ATC/airports. A future possibility. 
- Though flight/unit numbers are supported for JTACs, AWACS and tankers, position numbers 
  are not. *"Arco 2"* is okay but *"Arco 2-1"* is not.
- BMS menus, when disimissed, briefly display the first menu 
  for their group before closing. This appears to be a BMS bug.
- Plugin does not (yet) support comms menus being disabled with g_bDisableCommsMenu 1,
  nor does it support switching off the comms menu with the new chatline dot command 
  **".commsmenu 0"**.  Support may be added later.
- Plugin uses the default keys for BMS menus 
  (T for ATC; W/E/R for Wingman/Element/Flight, Q for AWACS, 
  Y for Tanker/JTAC).  If you have changed these shortcuts you should 
  edit the JSON to use your shortcuts.
- Plugin's JSON files DO NOT support UNICODE
- The plugin manages state without calling into BMS or locking data. 
  It does not get instant notification of window focus changes. 
  This greatly simplifies the plugin, but it is possible for it to get out
  of sync. If it is incorrectly displaying a menu, or listing menus, 
  just say *"Reset Menu"*, *"Cancel Menu*" or even just *"Press Escape"* to reset.

### VoiceAttack Variables

**\>JBMS** variables can be changed by the VA profile **CAREFULLY**, 
but should only be used in the same way (to pass info to the plugin, 
or check return states from the plugin). 
You can set the Boolean **_LOG** variables 
(initialized in *"JBMS Initial Load Init"*) to true 
for additional logging, or you can enable/disable them by voice.

**\>JBMSI** internal variables should NOT be changed by the VA profile, 
**EXCEPT** for the provided methods provided that use them. 
Changes to them could easily break the plugin.

A partial list of variables:
```
\>JBMS_JSON_LOG       Boolean for logging of JSON parsing
\>JBMS_MENU_LOG       Boolean for logging of menu items (shows list of menu items in log)
\>JBMS_STRUCT_LOG     Boolean for logging of data structures manipulation
\>JBMS_VERBOSE_LOG    Boolean for more verbose general logging

\>JBMS_KEYS_ONLY_TO_BMS  User set boolean to restrict keystrokes if BMS does not have focus
\>JBMSI_FOCUS_BMS        Boolean usually set to true if BMS has focus

>JBMS_MENU_TGT	   ONLY to set menuTarget before calling plugin w/ context "JBMS_SHOW_MENU"
>JBMS_MENU_NAME	   ONLY to set menuName before calling plugin w/ context "JBMS_SHOW_MENU"
>JBMS_DIRECT_CMD   ONLY to set directCommand before calling plugin w/ context "JBMS_DIRECT_CMD"

>JBMSI_NO_SUCH_MENU   READ-ONLY - (Checked & set only by "JBMS Radio Menu Show" command)
>JBMSI_MENU_RESPONSE  READ-ONLY - (Checked & set only by "JBMS Wait For Menu Response" command)

>>JBMSI_CS_MATCH"           READ-ONLY - (Phrase matching callsign with optional flight #: "[Fiend;Fiend 3]")
>>JBMSI_CS_MATCH_EMPTY_OK"  READ-ONLY;  (Allows matching callsign or nothing: "[Fiend;Fiend 3;]"
```

### JSON FORMAT:

The JSON menu file **Tanrr.VAPlugin.BMSRadio.Menus.json** is a top level 
array of menus. Each menu has the format shown below.

**EXAMPLE JSON MENU:**

	{
		"menuTarget": "Flight",
		"targetPhrases": "Flight",
		"menuName": "Miscellaneous 1",
		"menuNamePhrases": "[Miscellaneous;Misk] [1;]",
		"menuShow": [ "rrrrrrr" ],

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
- The menuShow array of keystrokes should use lowercase (details further on)
- All named items must be non-empty
- menuItems Array line includes multiple menuItem arrays:
  - First string: *menuItemPhrases* to match that menu item
  - Second string: *menuItemExecute* keys/command to execute for that menu item
  - Optional third string: *menuItemDirectCmd* identifier for the menu item so a VA command can call it directly
  
**NOTES:**
  - *targetPhrases* and *menuNamePhrases* are used internally to list menus for 
  the VA *"List Wingman/ATC/All Combat/Formation Menus"* command.
  Make sure they match the VA profile's prefix & suffix phrases for menus. 
  If you update the phrases in the VA profile, please update the matching 
  phrases in the JSON and vs. vs.

**CALLSIGN JSON:** 
The callsign JSON holds arrays of strings for AWACS, Tanker, JTAC, and pilot callsigns. 
Just follow the format of the config file. Strings containing multiple callsigns 
(no numbers or spaces allowed) are delimited by semicolons.  No semicolons allowed 
at beginning or end of strings and no double semicolons. 
Callsigns should not be duplicated between any groups. 

Put your desired default callsignFlight, numberFlight, and posInFlight in the "flightInfo" section. 
"callsignFlight" can not be an empty string nor can it contain spaces or semicolons. 
numberFlight and PosInFlight can be empty strings, or can be a string holding a single digit, 
1-9 for number and 1-4 for position.
You can change all these on the fly with a voice command "Update Callsign Plasma 6" or similar.


**EDITING JSON FILES:**

The menu menu and callsign JSON and schema files are installed in 
**..\VoiceAttack\Apps\Tanrr.VAPlugin.BMSRadio**. You can use an online 
JSON schema validator, like https://jsonmate.com/ to validate changes you make to the 
JSON menu/callsign files against their matching schema while you work on it.

Menu JSON: **Tanrr.VAPlugin.BMSRadio.Menus.json**  
Menu Schema JSON: **Tanrr.VAPlugin.BMSRadio.Menus.Schema.json**  
Callsign JSON: **Tanrr.VAPlugin.BMSRadio.Callsigns.json**  
Callsign Schema JSON: **Tanrr.VAPlugin.BMSRadio.Callsigns.Schema.json** 

KEYSTROKES TO SHOW EACH MENU:

*menuShow* is an array of (possibly) multiple strings that contain 
the keystrokes to bring up a menu.  The list of keystrokes is sent to 
the command *"JBMS Press Key Combo List"* in the VA profile. 
This gives good flexibility but has some restrictions.  

- Multiple keypresses without modifiers or special keys can in a single string like 
`"menuShow": [ "rrrrrrr" ],` but can **NOT** include capital letters
or any key that requires SHIFT to be held down. 
These keystrokes are sent to VoiceAttack's "Quick Input" command.
- Keypresses that include modifiers like \[CTRL\], \[RSHIFT\], \[LWIN\], or \[ALT\] or that use 
"special" keys like \[NUMENTER\] (anything in brackets) must only include
a **SINGLE** non-modifier keystroke.  Multiple keystrokes will **NOT** work as expected. 
So, `[ "[LSHIFT][LALT]r" ]` works, but `[ "[LALT]rrr" ]` does not. 
These strings are passed to VoiceAttack's "Variable Input" command. 

A silly example would be if you ran the below *menuShow* with an empty Notepad active.  
It would type *Hello* then make the text larger and smaller before saying *bye*.

`"menuShow": [ "[SHIFT]H", "ello", "[LCTRL]=", "[RCTRL]=", "[CTRL]-", "[CTRL]-", "[LCTRL]A", "bye" ],`


PHRASES TO SELECT MENU ITEMS:

*MenuItemPhrases* (1st text field in each array within *menuItems*) are 
the VoiceAttack command phrases the plugin will listen for while this 
menu is up.  They are the same format as VoiceAttack commands: 
multiple options, separated by semi-colons, sub-sections specified by 
\[brackets\].  Note that sub-sections can match to nothing as well by 
having a ; with nothing after it.

For example: *"\[Switch;Push\] \[Flight;\] Uniform"* matches all these phrases:
- "Switch Flight Uniform"
- "Push Flight Uniform"
- "Switch Uniform"
- "Push Uniform"

The provided VA profile already includes the command "Press \[0..9\]"" 
which will press the number keys for you, and these work even if the 
menu is not up. DO NOT add the menu number as part of the recognition 
phrases.  It could interfere with other phrases - for example *"2"* 
and *"3"* are used as prefix *menuTargets*.  

KEYSTROKES FOR MENU ITEMS:

*MenuItemExecute* (2nd text field in each array within *menuItems*) are 
normally set to the keystroke(s) to press when that menu item is chosen. 
These keys will be passed to the the VA profile *"JBMS Press Key Combo List"* 
command and are handled like a single string of the *menuShow* field. 
This means you can have multiple simple non-shifted keys like *"aaa"* 
or a single keystroke or special key with modifiers, like *"[LCTRL]a"*. 
(Unless they match a VA command as described later.) 

You can change the *"JBMS Press Key Combo List"* command in the VA profile to 
adjust keypress time and pause times - see the *"Press variable keys"* and *"Quick Input"* 
calls within the command. Those times are set somewhat conservatively 
long to work on all systems, so if you change the times make sure you 
test that they work for busy multiplayer servers and your own system.

See the *"DC Push Flight Uniform"* command in the VA profile as an 
example of using MenuItemExecute as a command.

MAPPING VOICEATTACK PHRASES DIRECTLY TO MENU ITEMS:

Direct Command Calls (to execute a menu item directly *from* a VA command 
phrase without leaving a menu up and waiting for a menu selection) are 
made possible by adding a third *menuItemDirectCmd* string to the array 
for that menu item in *menuItems*. These should be unique and preferably 
identify the *menuTarget*, *menuName*, and *menuItem* they're for, 
as described in the *menuTarget/menuName* abbreviations section later on.

To use this functionality, add your own command phrase to the VA profile, 
and have it set the **\>JBMS_DIRECT_CMD** variable to the command name 
before calling the plugin with context set to *JBMS_DIRECT_CMD*.

The provided VA profile has some examples of this:

- "Bogey Dope" mapped to "JBMS-QV1-BOGEY-DOPE" (for "AWACS - Vectors", "Bogey Dope")
- "AWACS Declare" mapped to "JBMS-QV1-BOGEY-DOPE" (for "AWACS - Tactical", "Declare")
- "Fence In" mapped to the "JBMS-FX1-FENCE-IN" (for "Flight - Miscellaneous", "Fence In")
- "Attack My Target" mapped to "JBMS-WC1-ATTACK-TGT" (for "2 - Combat 1", "Attack My Target")

Note that it is OKAY for your VA profile to use phrases that duplicate the phrases 
for the various menu items, because (1) the menu item phrases are only listened 
for when the specific menu is up, and (2) duplicate phrases that call 
JBMS_DIRECT_CMD while a menu is up will be dropped as the menu phrases have priority.

MAPPING MENU ITEMS TO YOUR OWN CUSTOM COMMANDS:

If you set *menuItemExecute* (2nd text field in each array within *menuItems*) 
to match the name of a **unique** command within the VA profile, the plugin will 
execute that command, instead of pressing keys matching the letters in the string. 
It is strongly recommended that you make such commands NOT callable by voice to 
avoid conflicting menu states.  Your implementation of such a VA command should 
NOT call back into the plugin with a "JBMS_DIRECT_COMMAND" for the same menu item 
as this would create a never-ending loop.

Your *menuItemExecute* VA command (if not keypresses) should not be long running 
as the plugin will wait for it to return. Your command can look at the 
**\>JBMS_MENU_TGT** and **\>JBMS_MENU_NAME** variables which will still be valid 
till your command returns. Your command is responsible for making sure the menu 
is dismissed, either by pressing a key to choose a menu item, or by executing 
*"Dismiss Menu Only"* command in the VA profile. Your command SHOULD NOT call 
the plugin with context *JBMS_RESET_MENU_STATE* as that will be done by the 
plugin after your command finishes.

CUSTOMIZING THE VOICEATTACK PROFILE

The profile command *"JBMS Initial Load Init"* sets default logging options.  
You can change these to have more logging. You can also change log settings 
on the fly with the voice command 
*"\[Enable;Disable\] \[Jason;Menu;Struct;Struture;Verbose\] Logging"*.

The amount of time the profile waits for responses for a menu before dismissing 
it is set in *"JBMS Wait For Menu Response"* command, within the 
"Wait for spoken response" call. It is currently left at 15 seconds, which is pretty long. 
Change it to whatever works for you.

As mentioned earlier, you can change the timing for keypresses and pauses in the 
*"JBMS Press Key Combo List"* command.  Make sure you verify changes work for your system 
and the servers you play on.

Though you can add your own commands directly to this JBMS Radio profile, 
it is probably better to make your own separate profile and include it from this profile. 
This can be done by editing the JBMS Radio profile, clicking OPTIONS, going to 
the *General* tab, and adding your profile to the *"Include Commands from Other Profiles" section. 
NOTE: This can strongly effect the accuracy of the VA profile by adding many or conflicting 
commands, so make sure your additional profile is lightweight and does not include tons of commands.

Note that Voice Attack commands that call the *"Internal Reset Menu" command need 
to have *"Allow Other Commands to Execute While This One is Running" checked.


## VOICEATTACK BASICS & RECOMMENDED SETTINGS

**BASICS:**
- *Only have VoiceAttack listen when you are holding down a key - 
don't leave it listening all the time.*
- Turn on "Show Confidence Level" in VA Settings (wrench), Recognition tab to see 
how well/poorly VA is understanding you.  Turn this off once things are working well.
- It is *VERY IMPORTANT* that you only have the spoken commands you actually 
USE enabled for speech.  All commands that you only call into from other commands 
but don't say directly should have speech disabled.  Similarly, try to limit the 
number of phrases you add to VA profiles.  Otherwise you are making it much more 
difficult for VoiceAttack (and my plugin) to understand you.


**RECOMMENDED SETTINGS:**

*VoiceAttack Settings, Recognition tab: See VA docs under "Recognition Tab" for 
deeper descriptions.*

- *Recognized Speech Delay:* 10-30 - Helps VA differentiate between "AWACS" vs 
"AWACS Vectors" and combination phrases.

- *Unrecognized Speech Delay:* 0

- *Command Weight:* 80-98 - Higher values here tell VA to try to make whatever 
you are saying fit to one of the phrases in the profile.  If you set this 
to 100 it will ALWAYS pick the closest match it can, so be careful. 
I have mine set very high, at 98, due to fan noise and a quiet voice. 

- *Minimum Confidence Level:* 30-70

   The minimum confidence level needed for a match. VA might recognize "Combat 1" 
   but have a low confidence because you didn't say it clearly. Run VA with "Show Confidence Level" enabled to see if how you should adjust this. I have mine at 35.
   
   **NOTE 1:** Minimum confidence level can also be set in a profiles options, or for a specific command
   
   **NOTE 2:** *"JBMS Wait For Menu Response"* VA Command specifically sets this very low, 
   as it is extremely likely, since you just called up a menu, that your next words will 
   be items in the menu.

- *Min Unrecognized Confidence Level:* 30-60 - Higher numbers throw out more 
"unrecognized" sounds.  Only need to turn this up if you're in a noisy environment.

- Disable Adaptive Recognition: OFF (unless VA seems to become unresponsive)
- Disable Acoustic Echo Cancellation: ON (but can effect other apps, and OFF is usually ok)
- Reject Pending Speech: OFF


**MENUTARGET & MENUNAME ABBREVIATIONS USED FOR DIRECT COMMANDS (NOT REQUIRED BUT HELPFUL):**
```
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
```  
For example, Wingman, Combat 1 menu for Attack My Target 
would be "JBMS-WC1-ATTACK-TGT" or something similar.
