using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using Discord;
using wow2.Verbose;

namespace wow2.Data
{
    public static class DataManager
    {
        public static Dictionary<ulong, GuildData> DictionaryOfGuildData { get; set; } = new Dictionary<ulong, GuildData>();
        public static Secrets Secrets { get; set; } = new Secrets();

        public static readonly string ResDirPath = Directory.Exists($"{Program.RuntimeDirectory}/res") ? $"{Program.RuntimeDirectory}/res" : "res";
        public static readonly string AppDataDirPath = Environment.GetEnvironmentVariable("WOW2_APPDATA_FOLDER") ?? $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/wow2";
        public static DirectoryInfo AppDataDirInfo { get; set; }
        public static string GuildDataDirPath
        {
            get { return $"{AppDataDirPath}/GuildData"; }
        }
        public static string LogsDirPath
        {
            get { return $"{AppDataDirPath}/Logs"; }
        }

        private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

        /// <summary>Creates required directories if necessary and loads all guild data.</summary>
        public static async Task InitializeAsync()
        {
            try
            {
                Directory.CreateDirectory(AppDataDirPath);

                AppDataDirInfo = Directory.CreateDirectory(GuildDataDirPath);
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

        public static async Task LoadSecretsFromFileAsync()
        {
            string fullPath = AppDataDirPath + "/secrets.json";
            if (File.Exists(fullPath))
                Secrets = JsonSerializer.Deserialize<Secrets>(File.ReadAllText(fullPath));

            // Always rewrite file, just in case there are new properties.
            await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(Secrets, SerializerOptions));
        }

        /// <summary>Load all guild data from all files, excluding the guilds the client is not in.</summary>
        public static async Task LoadGuildDataFromFileAsync()
        {
            foreach (FileInfo fileInfo in AppDataDirInfo.EnumerateFiles())
            {
                try
                {
                    ulong guildId = Convert.ToUInt64(Path.GetFileNameWithoutExtension(fileInfo.FullName));

                    if (!Program.Client.Guilds.Select(g => g.Id).Contains(guildId))
                    {
                        Logger.Log($"Not loading guild data for {guildId}, as the bot is not connected to it. (mass load)", LogSeverity.Verbose);
                        continue;
                    }

                    string guildDataJson = await File.ReadAllTextAsync(fileInfo.FullName);
                    DictionaryOfGuildData[guildId] = JsonSerializer.Deserialize<GuildData>(guildDataJson);

                    Logger.Log($"Loaded guild data for {guildId} (mass load)", LogSeverity.Verbose);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, $"Failed to load from file {fileInfo.Name}");
                }
            }
        }

        /// <summary>Load guild data from the corresponding file.</summary>
        public static async Task LoadGuildDataFromFileAsync(ulong specifiedGuildId)
        {
            try
            {
                string guildDataJson = await File.ReadAllTextAsync($"{AppDataDirPath}/GuildData/{specifiedGuildId}.json");
                DictionaryOfGuildData[specifiedGuildId] = JsonSerializer.Deserialize<GuildData>(guildDataJson);

                Logger.Log($"Loaded guild data for {specifiedGuildId} (specific load)", LogSeverity.Verbose);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load for guild {specifiedGuildId} due to: {ex.Message} (specific load)", LogSeverity.Error);
            }
        }

        /// <summary>Write all guild data to corresponding files.</summary>
        public static async Task SaveGuildDataToFileAsync()
        {
            foreach (ulong id in DictionaryOfGuildData.Keys)
            {
                EnsureGuildNameExists(id);
                await File.WriteAllTextAsync(
                    $"{GuildDataDirPath}/{id}.json", JsonSerializer.Serialize(DictionaryOfGuildData[id], SerializerOptions));
                Logger.Log($"Saved guild data for {id} (mass save)", LogSeverity.Verbose);
            }
        }

        /// <summary>Write guild data to file for a specific guild.</summary>
        public static async Task SaveGuildDataToFileAsync(ulong guildId)
        {
            if (!DictionaryOfGuildData.TryGetValue(guildId, out GuildData guildData))
                throw new KeyNotFoundException($"Failed to load for guild {guildId} as the guild ID was not found in the dictionary (specific save)");

            EnsureGuildNameExists(guildId);
            await File.WriteAllTextAsync(
                $"{GuildDataDirPath}/{guildId}.json", JsonSerializer.Serialize(guildData, SerializerOptions));
            Logger.Log($"Saved guild data for {guildId} (specific save)", LogSeverity.Verbose);
        }

        /// <summary>If the GuildData for the specified guild does not exist, one will be created.</summary>
        /// <returns>The GuildData for the guild.</returns>
        public static async Task<GuildData> EnsureGuildDataExistsAsync(ulong guildId)
        {
            if (File.Exists($"{GuildDataDirPath}/{guildId}.json"))
            {
                // If not already loaded in memory, do so.
                if (!DictionaryOfGuildData.ContainsKey(guildId))
                    await LoadGuildDataFromFileAsync(guildId);
            }
            else
            {
                // Ensure guild data file exists.
                DictionaryOfGuildData.TryAdd(guildId, new GuildData());
                await SaveGuildDataToFileAsync(guildId);
            }

            var guildData = DictionaryOfGuildData[guildId];
            return guildData;
        }

        public static async Task UnloadGuildDataAsync(ulong guildId)
        {
            await SaveGuildDataToFileAsync(guildId);
            DictionaryOfGuildData.Remove(guildId);
            Logger.Log($"Unloaded guild data for {guildId}", LogSeverity.Verbose);
        }

        public static void EnsureGuildNameExists(ulong guildId)
        {
            var guildData = DictionaryOfGuildData[guildId];
            if (guildData.NameOfGuild == null)
            {
                try
                {
                    guildData.NameOfGuild = Program.Client.GetGuild(guildId).Name;
                }
                catch
                {
                    Logger.Log($"Could not get name of guild {guildId}", LogSeverity.Warning);
                }
            }
        }
    }
}