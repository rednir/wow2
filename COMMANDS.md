# List of commands

## Main
 - `!wow about`
     - Shows some infomation about the bot.

 - `!wow help [optional:MODULE] [optional:PAGE]`
     - Displays a list of modules or commands in a specific module.

 - `!wow alias [NAME] [DEFINITION]`
     - Sets an alias. Typing the NAME of an alias will execute '!wow DEFINITION' as a command. Set the DEFINITION of an alias to blank to remove it.

 - `!wow alias-list`
     - Displays a list of aliases.

 - `!wow ping`
     - Checks the latency between the message that executes a command, and the response that the bot sends.

 - `!wow upload-raw-data`
     - Uploads a file containing all the data the bot stores about this server.

 - `!wow set-command-prefix [PREFIX]`
     - Change the prefix used to identify commands. '!wow' is the default.

## YouTube
 - `!wow yt channel [CHANNEL]`
     - Shows some basic data about a channel.

 - `!wow yt subscribe [CHANNEL]`
     - Toggle whether your server will get notified when CHANNEL uploads a new video.

 - `!wow yt list-subs [optional:PAGE]`
     - Lists the channels your server will get notified about.

 - `!wow yt set-announcements-channel [CHANNEL]`
     - Sets the channel where notifications about new videos will be sent.

 - `!wow yt test-poll`
     - Check for new videos.

## Voice
 - `!wow vc list [optional:PAGE]`
     - Show the song request queue.

 - `!wow vc clear`
     - Clears the song request queue.

 - `!wow vc add [REQUEST]`
     - Adds REQUEST to the song request queue. REQUEST can be a video URL or a youtube search term.

 - `!wow vc skip`
     - Stops the currently playing request and starts the next request if it exists.

 - `!wow vc join`
     - Joins the voice channel of the person that executed the command.

 - `!wow vc leave`
     - Leaves the voice channel.

 - `!wow vc np`
     - Shows details about the currently playing song request.

 - `!wow vc toggle-loop`
     - Toggles whether the current song request will keep looping.

 - `!wow vc toggle-auto-np`
     - Toggles whether the np command will be executed everytime a new song is playing.

 - `!wow vc toggle-auto-join`
     - Toggles whether the bot will try join when a new song is added to the queue.

 - `!wow vc set-vote-skips-needed [NUMBER]`
     - Sets the number of votes needed to skip a song request to NUMBER.

## Text
 - `!wow text quote [QUOTE] [optional:AUTHOR]`
     - Creates a fake quote of a famous person. If you want to use a specific person, set AUTHOR to their name.

 - `!wow text replace [OLDVALUE] [NEWVALUE] [TEXT]`
     - Replaces all instances of OLDVALUE with NEWVALUE within TEXT.

 - `!wow text emojify [TEXT]`
     - Adds emojis to some text because its funny haha.

## osu!
 - `!wow osu user [USER]`
     - Get some infomation about a user.

 - `!wow osu subscribe [USER]`
     - Toggle whether your server will get notified about USER.

 - `!wow osu list-subs [optional:PAGE]`
     - Lists the users your server will get notified about.

 - `!wow osu set-announcements-channel [CHANNEL]`
     - Sets the channel where notifications about users will be sent.

 - `!wow osu test-poll`
     - Check for new user milestones.

## Moderator
 - `!wow mod warn [MENTION] [optional:MESSAGE]`
     - Sends a warning to a user with an optional message.

 - `!wow mod mute [MENTION] [optional:TIME] [optional:MESSAGE]`
     - Temporarily disables a user's permission to speak. (WIP)

 - `!wow mod user-record [MENTION]`
     - Gets a user record.

 - `!wow mod set-warnings-until-ban [NUMBER]`
     - Sets the number of warnings required before a user is automatically banned. Set NUMBER to -1 to disable this.

 - `!wow mod toggle-auto-mod`
     - Toggles whether the bot give warnings to users, for example if spam is detected.

## Keywords
 - `!wow keywords add [KEYWORD] [VALUE]`
     - Adds value(s) to a keyword, creating a new keyword if it doesn't exist.

 - `!wow keywords remove [KEYWORD] [optional:VALUE]`
     - Removes value(s) from a keyword, or if none are specified, removes all values and the keyword.

 - `!wow keywords rename [OLDKEYWORD] [NEWKEYWORD]`
     - Renames a keyword, leaving its values unchanged.

 - `!wow keywords list [optional:PAGE]`
     - Shows a list of all keywords, and a preview of their values.

 - `!wow keywords values [KEYWORD] [optional:PAGE]`
     - Shows a list of values for a keyword.

 - `!wow keywords toggle-delete-reaction`
     - Toggles whether bot responses to keywords should have a wastebasket reaction, allowing a user to delete the message.

 - `!wow keywords toggle-like-reaction`
     - Toggles whether bot responses to keywords should have a thumbs up reaction.

## Games
 - `!wow games counting [optional:INCREMENT]`
     - Start counting in a text channel. INCREMENT is the number that will be added each time.

 - `!wow games verbal-memory`
     - Try remember as many words as you can.

 - `!wow games number-memory`
     - Try remember the largest number possible.

## Developer
 - `!wow dev load-guild-data`
     - Loads guild data from file to memory, discarding any unsaved changes.

 - `!wow dev save-guild-data [optional:ALSOEXIT]`
     - Save guild data from memory to file, optionally stopping the bot.

 - `!wow dev set-status [MESSAGE] [STATUS]`
     - Sets the 'playing' text and the status of the bot user.

 - `!wow dev run-test [optional:GROUP]`
     - Runs a list of commands.

 - `!wow dev commands-list`
     - Creates a COMMANDS.md file with a list of all commands.

 - `!wow dev get-logs`
     - Sends the log file for this session.

 - `!wow dev throw`
     - Throws an unhandled exception.
