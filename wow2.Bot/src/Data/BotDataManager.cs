using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using wow2.Bot.Verbose;

namespace wow2.Bot.Data
{
    public class BotDataManager
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
        };

        public BotDataManager(BotService botService, string guildDataDirPath)
        {
            BotService = botService;
            GuildDataDirPath = guildDataDirPath;
        }

        public BotService BotService { get; }

        public string GuildDataDirPath { get; }

        public Dictionary<ulong, GuildData> AllGuildData { get; set; } = new Dictionary<ulong, GuildData>();

        /// <summary>Creates required directories if necessary and loads all guild data.</summary>
        public async Task InitializeAsync()
        {
            try
            {
                Directory.CreateDirectory(GuildDataDirPath);
            }
            catch (Exception ex)
            {
                BotService.LogException(ex, $"Could not initialize folder {GuildDataDirPath}");
                Environment.Exit(-1);
            }

            await LoadGuildDataFromFileAsync();
        }

        /// <summary>Load all guild data from all files, excluding the guilds the client is not in.</summary>
        public async Task LoadGuildDataFromFileAsync()
        {
            BotService.Log("About to load all guild data.", LogSeverity.Verbose);
            foreach (FileInfo fileInfo in new DirectoryInfo(GuildDataDirPath).EnumerateFiles())
            {
                try
                {
                    ulong guildId = Convert.ToUInt64(Path.GetFileNameWithoutExtension(fileInfo.FullName));
                    if (!BotService.Client.Guilds.Select(g => g.Id).Contains(guildId))
                    {
                        BotService.Log($"Not loading guild data for {guildId}, as the bot is not connected to it.", LogSeverity.Info);
                        continue;
                    }

                    await LoadGuildDataFromFileAsync(guildId);
                }
                catch (Exception ex)
                {
                    BotService.LogException(ex, $"Failed to load from file {fileInfo.Name} during mass load.");
                }
            }
        }

        /// <summary>Load guild data from the corresponding file.</summary>
        public async Task LoadGuildDataFromFileAsync(ulong guildId)
        {
            try
            {
                string guildDataJson = await File.ReadAllTextAsync(GuildDataDirPath + $"/{guildId}.json");
                AllGuildData[guildId] = JsonSerializer.Deserialize<GuildData>(guildDataJson, SerializerOptions);
                BotService.Log($"Loaded guild data for {AllGuildData[guildId].NameOfGuild} ({guildId})", LogSeverity.Verbose);
            }
            catch (Exception ex)
            {
                BotService.LogException(ex, $"Failed to load guild data for {guildId}");
            }
        }

        /// <summary>Write all guild data to corresponding files.</summary>
        public async Task SaveGuildDataToFileAsync()
        {
            BotService.Log("About to save all guild data.", LogSeverity.Verbose);
            foreach (ulong guildId in AllGuildData.Keys)
            {
                try
                {
                    await SaveGuildDataToFileAsync(guildId);
                }
                catch (Exception ex)
                {
                    BotService.LogException(ex, $"Error thrown when saving guild data for {guildId}");
                }
            }
        }

        /// <summary>Write guild data to file for a specific guild.</summary>
        public async Task SaveGuildDataToFileAsync(ulong guildId)
        {
            if (!AllGuildData.TryGetValue(guildId, out GuildData guildData))
                throw new KeyNotFoundException($"The guild ID {guildId} was not found in the dictionary");

            EnsureGuildNameExists(guildId);
            await File.WriteAllTextAsync(
                $"{GuildDataDirPath}/{guildId}.json", JsonSerializer.Serialize(guildData, SerializerOptions));
            BotService.Log($"Saved guild data for {AllGuildData[guildId]?.NameOfGuild} ({guildId})", LogSeverity.Verbose);
        }

        /// <summary>If the GuildData for the specified guild does not exist, one will be created.</summary>
        /// <returns>The GuildData for the guild.</returns>
        public async Task<GuildData> EnsureGuildDataExistsAsync(ulong guildId)
        {
            if (File.Exists($"{GuildDataDirPath}/{guildId}.json"))
            {
                // If not already loaded in memory, do so.
                if (!AllGuildData.ContainsKey(guildId))
                    await LoadGuildDataFromFileAsync(guildId);
            }
            else
            {
                // Ensure guild data file exists.
                AllGuildData.TryAdd(guildId, new GuildData());
                await SaveGuildDataToFileAsync(guildId);
            }

            return AllGuildData[guildId];
        }

        public async Task UnloadGuildDataAsync(ulong guildId)
        {
            await SaveGuildDataToFileAsync(guildId);
            AllGuildData.Remove(guildId);
            BotService.Log($"Unloaded guild data for {guildId}", LogSeverity.Verbose);
        }

        public void EnsureGuildNameExists(ulong guildId)
        {
            var guildData = AllGuildData[guildId];
            if (guildData?.NameOfGuild == null)
            {
                try
                {
                    guildData.NameOfGuild = BotService.Client.GetGuild(guildId).Name;
                }
                catch
                {
                    BotService.Log($"Could not get name of guild {guildId}", LogSeverity.Warning);
                }
            }
        }
    }
}