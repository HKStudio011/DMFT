using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace DMFT.Model
{
    public class HistoryContainer : BaseContainer
    {
        public int MaxRecords { get; set; } = 1000;

        public HistoryContainer() : base("history_data.json")
        {
            IsLoading = true; // Start with loading state
            ToastScope = "History";
        }

        public async Task EnforceCapacityAsync()
        {
            if (Links.Count <= MaxRecords) return;
            int excess = Links.Count - MaxRecords;
            if (excess > 0)
            {
                Links.RemoveRange(0, excess);
                await SaveContainerAsync();
            }
        }

        public async Task ClearAllAsync()
        {
            Links.Clear();
            await SaveContainerAsync();
        }

        // Legacy sync methods removed: use async APIs directly


        public async Task ReInstallAsync(LinkInfo item, MainContainer mainContainer)
        {
            if (item == null || mainContainer == null) return;

            // Attempt to remove by reference first
            bool removed = Links.Remove(item);
            if (!removed)
            {
                // Fallback: try to find a matching item by key fields
                var match = Links.FirstOrDefault(l => l.Url == item.Url && l.Time == item.Time);
                if (match != null)
                {
                    Links.Remove(match);
                    item = match;
                    removed = true;
                }
            }

            if (!removed) return;

            // Avoid duplicates in main container
            if (!mainContainer.Links.Contains(item))
            {
                item.Status = StatusMessage.Waiting;
                mainContainer.Links.Add(item);
            }

            await SaveContainerAsync();
            await mainContainer.SaveContainerAsync();
        }

        public async Task ReInstall(LinkInfo item, MainContainer mainContainer) => await ReInstallAsync(item, mainContainer);
    }
}
