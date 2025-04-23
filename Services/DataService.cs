using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SoccerScheduler.Services
{
    public class DataService
    {
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task<T> LoadDataAsync<T>(string title, string filter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var stream = File.OpenRead(openFileDialog.FileName))
                    {
                        return await JsonSerializer.DeserializeAsync<T>(stream, _options);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка загрузки данных: {ex.Message}");
                }
            }

            return default;
        }

        public async Task SaveDataAsync<T>(T data, string title, string filter)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var stream = File.Create(saveFileDialog.FileName))
                    {
                        await JsonSerializer.SerializeAsync(stream, data, _options);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка сохранения данных: {ex.Message}");
                }
            }
        }
    }
}