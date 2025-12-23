using System;

namespace DMFT.Model
{
    public enum ToastLevel { Info, Success, Warning, Error }

    public class ToastService
    {
        public event Action<string, ToastLevel, string?>? OnToast;

        public void Show(string message, ToastLevel level = ToastLevel.Info, string scope = null)
        {
            OnToast?.Invoke(message, level, scope);
        }
    }
}
