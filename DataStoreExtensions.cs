using Google.Apis.Util.Store;

namespace CalendarReminder
{
    static class DataStoreExtensions
    {
        public static void Delete<T>(this IDataStore store, string key) => store.DeleteAsync<T>(key).GetAwaiter().GetResult();
        public static T Get<T>(this IDataStore store, string key) => store.GetAsync<T>(key).GetAwaiter().GetResult();
        public static void Set<T>(this IDataStore store, string key, T value) => store.StoreAsync(key, value).GetAwaiter().GetResult();
    }
}
