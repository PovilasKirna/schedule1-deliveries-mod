using System;
using System.IO;
using Newtonsoft.Json;
using DeliveriesProMax.Core;

namespace DeliveriesProMax.Data
{
    /// <summary>
    /// Handles loading and saving mod data to JSON files.
    /// Data is stored per-save-slot in the UserData/DeliveriesProMax/ directory.
    /// </summary>
    public class SaveDataManager
    {
        private const string ModFolder = "UserData/DeliveriesProMax";
        private string? _currentSaveSlot;
        private ModSaveData _data = new();

        public ModSaveData Data => _data;

        public SaveDataManager()
        {
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Sets the current save slot and loads data for it.
        /// Call this when a game save is loaded.
        /// </summary>
        public void SetSaveSlot(string saveSlotName)
        {
            // Save current data if switching slots
            if (_currentSaveSlot != null)
            {
                SaveAll();
            }

            _currentSaveSlot = SanitizeFileName(saveSlotName);
            LoadData();
            Mod.Logger.Msg($"Save data loaded for slot: {_currentSaveSlot}");
        }

        /// <summary>
        /// Saves all current data to disk.
        /// </summary>
        public void SaveAll()
        {
            if (_currentSaveSlot == null)
            {
                Mod.Logger.Warning("Cannot save: no save slot set.");
                return;
            }

            try
            {
                var filePath = GetSaveFilePath();
                var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"Failed to save mod data: {ex.Message}");
            }
        }

        private void LoadData()
        {
            var filePath = GetSaveFilePath();

            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    _data = JsonConvert.DeserializeObject<ModSaveData>(json) ?? new ModSaveData();
                }
                catch (Exception ex)
                {
                    Mod.Logger.Error($"Failed to load mod data: {ex.Message}");
                    _data = new ModSaveData();
                }
            }
            else
            {
                _data = new ModSaveData();
            }
        }

        private string GetSaveFilePath()
        {
            return Path.Combine(ModFolder, $"{_currentSaveSlot}.json");
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(ModFolder))
            {
                Directory.CreateDirectory(ModFolder);
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
