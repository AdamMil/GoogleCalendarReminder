using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// TODO: it seems like this sometimes loses data (especially the "play sound" setting). investigate

namespace CalendarReminder
{
    sealed class JsonDataStore : IDataStore
    {
        public JsonDataStore(string folderName)
        {
            if(folderName != null)
            {
                dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), folderName);
                Directory.CreateDirectory(dir);

                path = Path.Combine(dir, "settings.json");
                if(File.Exists(path))
                {
                    try
                    {
                        using(var sr = new StreamReader(path)) obj = JObject.Load(new JsonTextReader(sr));
                    }
                    catch(JsonException) { } // ignore invalid JSON
                }
            }
        }

        public Task ClearAsync()
        {
            obj = null;
            if(dicts.Count == 0) return Task.CompletedTask;
            dicts.Clear();
            return Save();
        }

        public void CopyTo(IDataStore dest)
        {
            if(dest == null) throw new ArgumentNullException(nameof(dest));
            var param = new object[3];
            foreach(KeyValuePair<Type, object> tpair in dicts)
            {
                Type pairType = typeof(KeyValuePair<,>).MakeGenericType(typeof(string), tpair.Key);
                MethodInfo getName = pairType.GetProperty("Key").GetGetMethod(), getValue = pairType.GetProperty("Value").GetGetMethod();
                MethodInfo set =
                    typeof(DataStoreExtensions).GetMethod("Set", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(tpair.Key);
                param[0] = dest;
                foreach(object opair in (System.Collections.IEnumerable)tpair.Value)
                {
                    param[1] = getName.Invoke(opair, null);
                    param[2] = getValue.Invoke(opair, null);
                    set.Invoke(null, param);
                }
            }
        }

        public Task DeleteAsync<T>(string key) => GetDict<T>(false)?.Remove(key) == true ? Save() : Task.CompletedTask;

        public Task<T> GetAsync<T>(string key) =>
            Task.FromResult(GetDict<T>(false)?.TryGetValue(key, out T value) == true ? value : default);

        public Task StoreAsync<T>(string key, T value)
        {
            GetDict<T>(true)[key] = value;
            return Save();
        }

        Dictionary<string, T> GetDict<T>(bool create)
        {
            if(dicts.TryGetValue(typeof(T), out object o)) return (Dictionary<string, T>)o;

            Dictionary<string, T> dict = null;
            if(obj != null && obj.TryGetValue(typeof(T).FullName, out JToken t) && t is JObject to)
            {
                dicts[typeof(T)] = dict = new Dictionary<string, T>();
                foreach(JProperty prop in to.Properties())
                {
                    try { dict[prop.Name] = prop.Value.ToObject<T>(serializer); }
                    catch(JsonException) { }
                }
            }
            else if(create)
            {
                dicts[typeof(T)] = dict = new Dictionary<string, T>();
            }
            return dict;
        }

        async Task Save()
        {
            if(path != null)
            {
                var o = new JObject();
                foreach(KeyValuePair<Type, object> pair in dicts)
                {
                    o[pair.Key.FullName] = JObject.FromObject(pair.Value, serializer);
                }

                Directory.CreateDirectory(dir);
                using(var sw = new StreamWriter(path))
                using(var jw = new JsonTextWriter(sw))
                {
                    await o.WriteToAsync(jw).ConfigureAwait(false);
                }
            }
        }

        readonly string dir, path;
        readonly Dictionary<Type, object> dicts = new Dictionary<Type, object>();
        readonly JsonSerializer serializer = new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore };
        JObject obj;
    }
}
