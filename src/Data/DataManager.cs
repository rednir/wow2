using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using wow2.Verbose;
using wow2.Modules.Main;
using wow2.Modules.Keywords;
using wow2.Modules.Games;
using wow2.Modules.Voice;

namespace wow2.Data
{
    public static class DataManager
    {
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

        public static Dictionary<ulong, GuildData> DictionaryOfGuildData { get; set; } = new Dictionary<ulong, GuildData>();

        /// <summary>Creates required directories if necessary and loads all guild data.</summary>
        public static async Task InitializeAsync()
        {
            try
            {
                Directory.CreateDirectory(AppDataDirPath);
                AppDataDirInfo = Directory.CreateDirectory(GuildDataDirPath);
                Directory.CreateDirectory(LogsDirPath);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Logger.Log($"Could not initialize folders in {AppDataDirPath}, the program may lack sufficient privileges.", LogSeverity.Critical);
                Environment.Exit(-1);
            }

            await LoadGuildDataFromFileAsync();
        }

        /// <summary>Load all guild data from all files.</summary>
        public static async Task LoadGuildDataFromFileAsync()
        {
            foreach (FileInfo fileInfo in AppDataDirInfo.EnumerateFiles())
            {
                try
                {
                    ulong guildId = Convert.ToUInt64(Path.GetFileNameWithoutExtension(fileInfo.FullName));

                    string guildDataJson = await File.ReadAllTextAsync(fileInfo.FullName);
                    DictionaryOfGuildData[guildId] = JsonSerializer.Deserialize<GuildData>(guildDataJson);

                    Logger.Log($"Loaded guild data for {guildId} (mass load)", LogSeverity.Verbose);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to load from file {fileInfo.Name} due to: {ex.Message} (mass load)", LogSeverity.Warning);
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
                Logger.Log($"Failed to load for guild {specifiedGuildId} due to: {ex.Message} (specific load)", LogSeverity.Warning);
            }
        }

        /// <summary>Write all guild data to corresponding files.</summary>
        public static async Task SaveGuildDataToFileAsync()
        {
            foreach (ulong id in DictionaryOfGuildData.Keys)
            {
                await File.WriteAllTextAsync($"{GuildDataDirPath}/{id}.json", JsonSerializer.Serialize(DictionaryOfGuildData[id]));
                Logger.Log($"Saved guild data for {id} (mass save)", LogSeverity.Verbose);
            }
        }

        /// <summary>Write guild data to file for a specific guild.</summary>
        public static async Task SaveGuildDataToFileAsync(ulong guildId)
        {
            GuildData guildData = new GuildData();
            foreach (ulong id in DictionaryOfGuildData.Keys)
            {
                if (id == guildId)
                {
                    guildData = DictionaryOfGuildData[id];
                    break;
                }
            }
            await File.WriteAllTextAsync($"{GuildDataDirPath}/{guildId}.json", JsonSerializer.Serialize(guildData));
            Logger.Log($"Saved guild data for {guildId} (specific save)", LogSeverity.Verbose);
        }

        /// <summary>If the data file for the specified guild does not exist, one will be created and loaded. Otherwise this does nothing.</summary>
        public static async Task EnsureGuildDataFileExistsAsync(ulong guildId)
        {
            DictionaryOfGuildData.TryAdd(guildId, new GuildData());
            if (!File.Exists($"{GuildDataDirPath}/{guildId}.json"))
            {
                await SaveGuildDataToFileAsync(guildId);
                await LoadGuildDataFromFileAsync(guildId);
            }
        }

        public static MainModuleConfig GetMainConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Main;

        public static KeywordsModuleConfig GetKeywordsConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Keywords;

        public static GamesModuleConfig GetGamesConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Games;

        public static VoiceModuleConfig GetVoiceConfigForGuild(SocketGuild guild)
            => DataManager.DictionaryOfGuildData[guild.Id].Voice;
    }
}