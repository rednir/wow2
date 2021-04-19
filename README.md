<p align="center"><img src="res/about/examples.png"></p>

# wow2
A Discord bot written in C# using the [Discord.NET](https://github.com/discord-net/Discord.Net) library.

## Using the bot
Link to invite the bot:
 - https://discord.com/api/oauth2/authorize?client_id=818156344594792451&permissions=37022788&scope=bot

Once the bot has joined, you can type `!wow help` in any text channel to view a list of commands.

## Hosting the bot yourself
Download and run the executable from the [releases page](https://github.com/rednir/wow2/releases/)

You'll be asked for a bot token. Make sure you've created an application [here](https://discord.com/developers/applications), and added a bot user to it with sufficient privileges.

### Dependencies
- .NET 5.0
- For voice commands: `libsodium`, `opus`, `ffmpeg`, `youtube-dl`
	- For Windows users, the necessary binaries are already included in releases.
	- Windows users should install [Microsoft Visual C++ 2010 Redistributable Package](https://www.microsoft.com/en-US/download/details.aspx?id=5555)
