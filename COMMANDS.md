# List of commands (91 total)

## Main (8)
Stuff to do with the bot and other random stuff.

|Command|Summary|
|---|---|
|`!wow about`|Shows some infomation about the bot.|
|`!wow help [optional:MODULE] [optional:PAGE]`|Displays a list of modules or commands in a specific module.|
|`!wow alias [NAME] [DEFINITION]`|Sets an alias. Typing the NAME of an alias will execute '!wow DEFINITION' as a command. Set the DEFINITION of an alias to blank to remove it.|
|`!wow alias-list`|Displays a list of aliases.|
|`!wow ping`|Checks the latency between the message that executes a command, and the response that the bot sends.|
|`!wow say [MESSAGE]`|Sends a message. That's it.|
|`!wow set-command-prefix [PREFIX]`|Change the prefix used to identify commands. '!wow' is the default.|
|`!wow upload-raw-data`|Uploads a file containing all the data the bot stores about this server.|

## YouTube (5)
Integrations with YouTube, like getting notified for new videos.

|Command|Summary|
|---|---|
|`!wow yt channel [CHANNEL]`|Shows some basic data about a channel.|
|`!wow yt subscribe [CHANNEL]`|Toggle whether your server will get notified when CHANNEL uploads a new video.|
|`!wow yt list-subs [optional:PAGE]`|Lists the channels your server will get notified about.|
|`!wow yt set-announcements-channel [CHANNEL]`|Sets the channel where notifications about new videos will be sent.|
|`!wow yt test-poll`|Check for new videos.|

## Voice (19)
Play YouTube or Twitch audio in a voice channel.

|Command|Summary|
|---|---|
|`!wow vc list [optional:PAGE]`|Show the song request queue.|
|`!wow vc clear`|Clears the song request queue.|
|`!wow vc add [REQUEST]`|Adds REQUEST to the song request queue. REQUEST can be a video URL or a youtube search term.|
|`!wow vc remove [NUMBER]`|Removes a song request from the queue at the given index.|
|`!wow vc remove-many [START] [END]`|Removes all song requests from START to END inclusive.|
|`!wow vc skip`|Stops the currently playing request and starts the next request if it exists.|
|`!wow vc join`|Joins the voice channel of the person that executed the command.|
|`!wow vc leave`|Leaves the voice channel.|
|`!wow vc shuffle`|Randomly shuffles the song request queue.|
|`!wow vc np`|Shows details about the currently playing song request.|
|`!wow vc save-queue [NAME]`|Saves the current song request queue with a name for later use.|
|`!wow vc list-saved [NAME] [optional:PAGE]`|Shows a list of songs in a saved queue.|
|`!wow vc list-saved`|Shows a list of saved queues.|
|`!wow vc pop-queue [NAME]`|Replaces the current song request queue with a saved queue. The saved queue will also be deleted.|
|`!wow vc load-queue [NAME]`|Replaces the current song request queue with a saved queue. The saved queue will also be deleted.|
|`!wow vc toggle-loop`|Toggles whether the current song request will keep looping.|
|`!wow vc toggle-auto-np`|Toggles whether the np command will be executed everytime a new song is playing.|
|`!wow vc toggle-auto-join`|Toggles whether the bot will try join when a new song is added to the queue.|
|`!wow vc set-vote-skips-needed [NUMBER]`|Sets the number of votes needed to skip a song request to NUMBER.|

## Timers (2)
Create and manage timers and reminders.

|Command|Summary|
|---|---|
|`!wow timer start [optional:MESSAGE]`|Starts a timer that will send a message when elapsed.|
|`!wow timer stop`|Stops the most recently created timer.|

## Text (3)
Change and manipulate text.

|Command|Summary|
|---|---|
|`!wow text quote [QUOTE] [optional:AUTHOR]`|Creates a fake quote of a famous person. If you want to use a specific person, set AUTHOR to their name.|
|`!wow text replace [OLDVALUE] [NEWVALUE] [TEXT]`|Replaces all instances of OLDVALUE with NEWVALUE within TEXT.|
|`!wow text emojify [TEXT]`|Adds emojis to some text because its funny haha.|

## Server Icon (4)
Manage the icon of this server.

|Command|Summary|
|---|---|
|`!wow icon toggle-icon-rotate`|Toggles whether this server's icon will rotate periodically. You can configure what images are in the rotation.|
|`!wow icon add [IMAGEURL]`|Adds an image for server icon rotation. IMAGEURL must contain an image only.|
|`!wow icon remove [ID]`|Removes an image from the server icon rotation from ID.|
|`!wow icon list [optional:PAGE]`|Lists all the server icons in rotation.|

## Reddit (4)
View Reddit posts and other content.

|Command|Summary|
|---|---|
|`!wow reddit top [SUBREDDIT]`|Gets the top post of all time from a given subreddit.|
|`!wow reddit new [SUBREDDIT]`|Gets the newest post from a given subreddit.|
|`!wow reddit hot [SUBREDDIT]`|Gets the first post in hot from a given subreddit.|
|`!wow reddit cont [SUBREDDIT]`|Gets the most controversial of all time from given subreddit.|

## osu! (7)
Integrations with the osu!api

|Command|Summary|
|---|---|
|`!wow osu user [USER] [optional:MODE]`|Get some infomation about a user.|
|`!wow osu score [ID] [optional:MODE]`|Show some infomation about a score.|
|`!wow osu last [USER] [optional:MODE]`|Shows the most recent score set by a player.|
|`!wow osu subscribe [USER] [optional:MODE]`|Toggle whether your server will get notified about USER.|
|`!wow osu list-subs [optional:PAGE]`|Lists the users your server will get notified about.|
|`!wow osu set-announcements-channel [CHANNEL]`|Sets the channel where notifications about users will be sent.|
|`!wow osu test-poll`|Check for new user milestones.|

## Moderator (5)
Use tools to manage the server. This is still very rudimentary and unfinished.  Requires the 'Ban Members' permission.

|Command|Summary|
|---|---|
|`!wow mod warn [MENTION] [optional:MESSAGE]`|Sends a warning to a user with an optional message.|
|`!wow mod mute [MENTION] [optional:TIME] [optional:MESSAGE]`|Temporarily disables a user's permission to speak. (WIP)|
|`!wow mod user-record [MENTION]`|Gets a user record.|
|`!wow mod set-warnings-until-ban [NUMBER]`|Sets the number of warnings required before a user is automatically banned. Set NUMBER to -1 to disable this.|
|`!wow mod toggle-auto-mod`|Toggles whether the bot give warnings to users, for example if spam is detected.|

## Keywords (11)
Automatically respond to keywords in user messages.

|Command|Summary|
|---|---|
|`!wow keywords add [KEYWORD] [VALUE]`|Adds value(s) to a keyword, creating a new keyword if it doesn't exist.|
|`!wow keywords remove [KEYWORD] [optional:VALUE]`|Removes value(s) from a keyword, or if none are specified, removes all values and the keyword.|
|`!wow keywords rename [OLDKEYWORD] [NEWKEYWORD]`|Renames a keyword, leaving its values unchanged.|
|`!wow keywords list [optional:SORT] [optional:PAGE]`|Shows a list of all keywords, and a preview of their values. SORT can be date/likes/deletions/values|
|`!wow keywords values [KEYWORD] [optional:PAGE]`|Shows a list of values for a keyword.|
|`!wow keywords gallery [KEYWORD]`|Shows all values in a keyword as a message you can scroll through (useful for values with images or videos)|
|`!wow keywords restore [KEYWORD]`|Restores a previously deleted keyword from its name.|
|`!wow keywords set-chance [PERCENTAGE]`|Sets the chance of the bot replying to a keyword with a response.|
|`!wow keywords toggle-delete-reaction`|Toggles whether bot responses to keywords should have a wastebasket reaction, allowing a user to delete the message.|
|`!wow keywords toggle-like-reaction`|Toggles whether bot responses to keywords should have a thumbs up reaction.|
|`!wow keywords toggle-responses`|Toggles whether the bot will respond to keywords.|

## Games (4)
For having a bit of fun.

|Command|Summary|
|---|---|
|`!wow games counting [optional:INCREMENT]`|Start counting in a text channel. INCREMENT is the number that will be added each time.|
|`!wow games counting-leaderboard [optional:PAGE]`|Shows the leaderboard for the counting game.|
|`!wow games verbal-memory`|Try remember as many words as you can.|
|`!wow games verbal-memory-leaderboard [optional:PAGE]`|Shows the leaderboard for the counting game.|

## Developer (17)
Boring stuff for developers.

|Command|Summary|
|---|---|
|`!wow dev load-guild-data [optional:ID]`|Loads guild data from file to memory, discarding any unsaved changes.|
|`!wow dev save-guild-data [optional:ID]`|Save guild data from memory to file, optionally stopping the bot.|
|`!wow dev guild-list`|Displays a list of guilds the bot is currently in.|
|`!wow dev get-guild-data [optional:ID]`|Gets the json file for the specified guild, default current guild.|
|`!wow dev set-status [MESSAGE] [STATUS]`|Sets the 'playing' text and the status of the bot user.|
|`!wow dev run-test [GROUPS]`|Runs a list of commands.|
|`!wow dev commands-list`|Creates a COMMANDS.md file with a list of all commands.|
|`!wow dev get-logs`|Sends the log file for this session.|
|`!wow dev panic`|Disables the bot for non-owner and changes the bot's Discord status.|
|`!wow dev unpanic`|Enables the bot for all.|
|`!wow dev poll-start [NAME]`|Enables the bot for all.|
|`!wow dev poll-stop [NAME]`|Stops a polling service.|
|`!wow dev poll-list`|Lists all polling services.|
|`!wow dev reconnect`|Disconnect and reconnect the bot.|
|`!wow dev execute-manual-script`|Run the file at the environment variable WOW2_MANUAL_SCRIPT.|
|`!wow dev stop-program [optional:EXIT]`|Prepares to stop the program.|
|`!wow dev throw`|Throws an unhandled exception.|

## Attachment Voting (2)
Enable users to vote on attachments in messages.

|Command|Summary|
|---|---|
|`!wow attachment toggle [CHANNEL]`|Toggles whether the specified text channel will have thumbs up/down reactions for each new message with attachment posted there.|
|`!wow attachment list [optional:SORT] [optional:PAGE]`|Lists all attachments with voting enabled. SORT can be points/users/date/likes/deletions/values, default is likes.|

