using System;

namespace UDocStoreApp.Infrastructure
{
    public static class DataBus
    {
        public static event Action RefreshStructureRequested;

        public static void SendRefreshRequest()
        {
            RefreshStructureRequested?.Invoke();
        }
    }
}
