using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Discord;
using wow2.Verbose;

namespace wow2.Data
{
    public static class DataManager
    {
        public static readonly string AppDataDirPath = Environment.GetEnvironmentVariable("WOW2_APPDATA_FOLDER") ?? $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/wow2";
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
        };

        public static Dictionary<ulong, GuildData> AllGuildData { get; set; } = new Dictionary<ulong, GuildData>();
        public static Secrets Secrets { get; set; } = new Secrets();

        public static string GuildDataDirPath => $"{AppDataDirPath}/GuildData";
        public static string LogsDirPath => $"{AppDataDirPath}/Logs";

        /// <summary>Creates required directories if necessary and loads all guild data.</summary>
        public static async Task InitializeAsync()
        {
            try
            {
                Directory.CreateDirectory(AppDataDirPath);

                Directory.CreateDirectory(GuildDataDirPath);
                Directory.CreateDirectory(LogsDirPath);
                await LoadSecretsFromFileAsync();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Logger.Log($"Could not initialize folders in {AppDataDirPath}, the program may lack sufficient privileges.", LogSeverity.Critical);
                Environment.Exit(-1);
            }

            await LoadGuildDataFromFileAsync();
        }

        /// <summary>Deserializes the secrets.json file into the Secrets property. If it doesn't exist, creates one and stops the program.</summary>
        public static async Task LoadSecretsFromFileAsync()
        {
            string fullPath = AppDataDirPath + "/secrets.json";
            if (!File.Exists(fullPath))
            {
                Logger.Log($"Couldn't find a secrets file at {fullPath}, so one was created. You MUST have your Discord bot token at the very least in the secrets file in order to run this program.", LogSeverity.Warning);
                await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(Secrets, SerializerOptions));
                Console.Read();
                Environment.Exit(-1);
            }

            Secrets = JsonSerializer.Deserialize<Secrets>(File.ReadAllText(fullPath), SerializerOptions);

            // Always rewrite file, just in case there are new properties.
            await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(Secrets, SerializerOptions));
        }

        /// <summary>Load all guild data from all files, excluding the guilds the client is not in.</summary>
        public static async Task LoadGuildDataFromFileAsync()
        {
            Logger.Log("About to load all guild data.", LogSeverity.Verbose);
            foreach (FileInfo fileInfo in new DirectoryInfo(GuildDataDirPath).EnumerateFiles())
            {
                try
                {
                    ulong guildId = Convert.ToUInt64(Path.GetFileNameWithoutExtension(fileInfo.FullName));
                    if (!Bot.Client.Guilds.Select(g => g.Id).Contains(guildId))
                    {
                        Logger.Log($"Not loading guild data for {guildId}, as the bot is not connected to it.", LogSeverity.Info);
                        continue;
                    }

                    await LoadGuildDataFromFileAsync(guildId);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Failed to load from file {fileInfo.Name} during mass load.");
                }
            }
        }

        /// <summary>Load guild data from the corresponding file.</summary>
        public static async Task LoadGuildDataFromFileAsync(ulong guildId)
        {
            try
            {
                string guildDataJson = await File.ReadAllTextAsync($"{AppDataDirPath}/GuildData/{guildId}.json");
                AllGuildData[guildId] = JsonSerializer.Deserialize<GuildData>(guildDataJson, SerializerOptions);
                Logger.Log($"Loaded guild data for {AllGuildData[guildId].NameOfGuild} ({guildId})", LogSeverity.Verbose);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, $"Failed to load guild data for {guildId}");
            }
        }

        /// <summary>Write all guild data to corresponding files.</summary>
        public static async Task SaveGuildDataToFileAsync()
        {
            Logger.Log("About to save all guild data.", LogSeverity.Verbose);
            foreach (ulong guildId in AllGuildData.Keys)
            {
                try
                {
                    await SaveGuildDataToFileAsync(guildId);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Error thrown when saving guild data for {guildId}");
                }
            }
        }

        /// <summary>Write guild data to file for a specific guild.</summary>
        public static async Task SaveGuildDataToFileAsync(ulong guildId)
        {
            if (!AllGuildData.TryGetValue(guildId, out GuildData guildData))
                throw new KeyNotFoundException($"The guild ID {guildId} was not found in the dictionary");

            EnsureGuildNameExists(guildId);
            await File.WriteAllTextAsync(
                $"{GuildDataDirPath}/{guildId}.json", JsonSerializer.Serialize(guildData, SerializerOptions));
            Logger.Log($"Saved guild data for {AllGuildData[guildId]?.NameOfGuild} ({guildId})", LogSeverity.Verbose);
        }

        /// <summary>If the GuildData for the specified guild does not exist, one will be created.</summary>
        /// <returns>The GuildData for the guild.</returns>
        public static async Task<GuildData> EnsureGuildDataExistsAsync(ulong guildId)
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

            var guildData = AllGuildData[guildId];
            return guildData;
        }

        public static async Task UnloadGuildDataAsync(ulong guildId)
        {
            await SaveGuildDataToFileAsync(guildId);
            AllGuildData.Remove(guildId);
            Logger.Log($"Unloaded guild data for {guildId}", LogSeverity.Verbose);
        }

        public static void EnsureGuildNameExists(ulong guildId)
        {
            var guildData = AllGuildData[guildId];
            if (guildData?.NameOfGuild == null)
            {
                try
                {
                    guildData.NameOfGuild = Bot.Client.GetGuild(guildId).Name;
                }
                catch
                {
                    Logger.Log($"Could not get name of guild {guildId}", LogSeverity.Warning);
                }
            }
        }
    }
}