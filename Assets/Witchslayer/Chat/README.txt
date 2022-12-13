
Thank you for purchasing my Chat system for FishNet using PlayFab!
If you need help or have questions or suggestions, email me at witchslayergames@gmail.com or on Discord at Witchslayer#1366.
Although this is targeted for and provides examples of PlayFab usage, all components are fully modular for your own logic.
Please read the rest of this doc to understand the usage and check out the prefabs included with the package.



== DEPENDENCIES ==
- TextMeshPro: should automatically ask you to import when opening the demo scene or creating any TMP object
- FishNet: https://github.com/FirstGearGames/FishNet
- PlayFab SDK and EdEx: https://docs.microsoft.com/en-us/gaming/playfab/sdks/unity3d/installing-unity3d-sdk



== IMPORTING PACKAGE AND DEMO SCENE ==
See the tutorial video on importing here: [COMING SOON TM]

Simply double click the package to import it. You can then navigate to "Assets > Witchslayer > Chat > Scenes" and open
the demo scene. If you are not using URP, you may need to click on the MainCamera and remove the invalid component.
You may also delete the camera and add your own (remember to tag it as MainCamera if necessary).

Assuming you have implemented all the dependency packages listed above, you can hit Play in the editor.
Start the server if it didn't automatically start. First use the Register tab to register a username/email.
After successful registration, you can click back on the Login tab and press the Login button.
The login menu should hide itself and spawn the default chat canvas as your player object and begin chatting!



== USING PLAYFAB ==
My package also adds a define (USE_PLAYFAB) since PlayFab has not yet included their own. This simply indicates to the assembly
that PlayFab is installed and should compile PlayFab related scripts if wrapped in this define.
Using the PlayFab editor extensions, you can login and set the API you want to use for the project, i.e. server API for server-side classes.
Keep in mind the server API should be excluded from your build, so if you want to use the server API for testing in editor, 
you should strip the defines when compiling as client.



== PLAYFAB AUTHENTICATOR ==
PlayFabAuthenticator is a custom authenticator for FishNet I've made just for logging in and getting authenticated using PlayFab.
You probably already have a NetworkManager in your scene. Make sure you add the ServerManager component to it,
add the authenticator component, then assign it. 

You can then call PlayFabAuthenticator.ClientPlayFabLogin to log in on the client, start a connection on success,
send the SessionTicket to the server, and have PlayFab's server API authenticate the connection server-side. 
Note this is totally optional, but provides ease of use.

On successful authentication, the client will assign the UserAccountInfo to the local client's conn.CustomData.
The authenticator also assigns the UserAccountInfo upon authentication to the connection.CustomData on the server.
The is used to get the necessary information for the various ServerChatProcessor components and chat commands.



== SERVER CHAT PROCESSOR ==
This object is created at runtime but without it's special components. I recommend you create a new game object in 
an startup scene or your game scene and add the ServerChatProcessor component to it. Similar to how you may already have
a NetworkManager in your scene.
Then you may add none, any, or all related components. These components are:

	- ProfanityFilter - Inherit from this class to implement our own logic for profanity filtering.
						Some examples are already included using an third-party asset and a web API.

	- ChannelMuteChecker - Inherit from this class to implement logic on how to check the mute status of a player
						   in a specific ChatChannel. An async PlayFab example is included.

	- WhisperMuteChecker - This class is responsible for checking mutes specifically set by players against another.
						   A PlayFab example is included.

	- ConnectionInfo - This class is responsible for getting information about a connection or a user, specifically a username.
					   The included PlayFab example caches the UserAccountInfo to connection.CustomData on the server upon authentication
					   and then this class uses that to get any PlayFab related data from that connection, and vice-versa.



== UI CHAT ==
This is the client-side UI panel for the chat system, fully featured with my custom TabPanel, input field, scrollable chat, etc.
There is a prefab included with the package that you would drop in as a child of your player's canvas.
You may optional use a ChannelMuteChecker as a sibling component to prevent sending unnecessary packets to the server.
A lot of the client customizable data, such as the tabs, colors, etc., are saved in PlayerPrefs.

Channel customization comes in the form of ChatChannelData scriptable objects, which are assigned to the UIChat component.
Here you may add visual data for a channel, such as the channel(s) they are for, icon for the dropdown menu, chat color, and hint.
Channel hints are typed by the player to quickly switch between channels rather than selecting it from the dropdown menu.

Default chat tabs are also scriptable objects with data that allows you to set the default tabs for a client.
A default is included and furthermore an internal default in case the reference goes missing.



== CHAT COMMANDS ==
These commands are typed by the player or admin to trigger a function or response. 
You can inherit from ChatCommandBase to create your own custom chat commands.
Several related commands are already included for PlayFab, such as muting, blocking, kicking, and banning.
These are automatically cached on application startup, but make sure to not use the same text commands multiple times.
This is only allowed within the same command class if you want to implement different server/client logic.
You should pass in a NetworkConnection parameter to these methods if you wish to run them as a server, and implement the logic as such.



== TROUBLESHOOTING ==
-	If you have a ton of errors saying something about PlayFab doesn't exist in this context, 
	make sure you have PlayFab and their editor extensions installed in your project, that you are signed in,
	and that you have "ENABLE SERVER API" enabled in their editor extensions window 
	(Window > PlayFab > Editor Extensions, Settings tab, API sub-tab).
	

