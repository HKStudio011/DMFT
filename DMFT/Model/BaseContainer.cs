using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.Storage;

namespace DMFT.Model
{
    public class BaseContainer
    {
        public List<LinkInfo> Links { get; protected set; } = new List<LinkInfo>();
        protected string DataFileName { get; set; } = "data.json";
        public bool IsLoading { get; protected set; } = false;
        public event System.Action? OnLoadingStateChanged;

        // Optional toast service for surface-wide user feedback
        public DMFT.Model.ToastService? Toast { get; set; }
        // Scope for toast messages to categorize per container
        public string ToastScope { get; set; } = "Main";

        public BaseContainer(string? dataFileName = null)
        {
            if (!string.IsNullOrWhiteSpace(dataFileName)) DataFileName = dataFileName;
        }

        public async Task LoadContainerAsync()
        {
            IsLoading = true;
            OnLoadingStateChanged?.Invoke();
            
            try
            {
                string filePath = Path.Combine(FileSystem.AppDataDirectory, DataFileName);
                if (File.Exists(filePath))
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        Links = await JsonSerializer.DeserializeAsync<List<LinkInfo>>(fs) ?? new List<LinkInfo>();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex, "LoadContainer");
            }
            finally
            {
                IsLoading = false;
                OnLoadingStateChanged?.Invoke();
            }
        }

        public async Task SaveContainerAsync()
        {
            try
            {
                // indicate loading during save
                IsLoading = true;
                OnLoadingStateChanged?.Invoke();

                string filePath = Path.Combine(FileSystem.AppDataDirectory, DataFileName);
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await JsonSerializer.SerializeAsync(fs, Links);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex, "SaveContainer");
            }
            finally
            {
                IsLoading = false;
                OnLoadingStateChanged?.Invoke();
            }
        }

        // Centralized error reporting helper
        protected void ShowError(Exception ex, string context)
        {
            string msg = $"{context} error: {ex.GetType().Name} - {ex.Message}";
            if (ex.InnerException != null)
            {
                msg += $" (Inner: {ex.InnerException.Message})";
            }
#if DEBUG
            msg += $" | StackTrace: {ex.StackTrace}";
#endif
            Toast?.Show(msg, ToastLevel.Error, ToastScope);
            System.Diagnostics.Debug.WriteLine($"{context} error: {ex}");
        }

        // Legacy sync methods (kept for compatibility)
        public async Task LoadContainer() => await LoadContainerAsync();
        public async Task SaveContainer() => await SaveContainerAsync();
    }
}
