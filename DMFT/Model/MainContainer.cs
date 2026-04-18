using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DMFT.Model
{
    public class MainContainer : BaseContainer
    {
        public event System.Action? OnRefreshRequested;

        public MainContainer() : base("main_data.json")
        {
            IsLoading = true;
            ToastScope = "Main";
        }

        public void RequestRefresh()
        {
            OnRefreshRequested?.Invoke();
        }

        // Legacy sync methods removed: use async APIs directly

        // Backward-compat wrapper for existing UI calls
        public async Task ClearAllAsync() => await ClearAllFromMainAsync();


        // New helpers to support UI actions routed from History page
        public async Task CancelItemAsync(LinkInfo item)
        {
            if (item == null) return;
            item.Status = StatusMessage.Canceled;
            await SaveContainerAsync();
        }

        public async Task ClearAllFromMainAsync()
        {
            Links.Clear();
            await SaveContainerAsync();
        }
    }
}
