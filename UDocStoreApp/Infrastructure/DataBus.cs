using System;

namespace UDocStoreApp.Infrastructure
{
    public static class DataBus
    {
        // Событие, которое срабатывает при изменении структуры (Секции/Журналы)
        public static event Action RefreshStructureRequested;

        public static void SendRefreshRequest()
        {
            RefreshStructureRequested?.Invoke();
        }
    }
}
