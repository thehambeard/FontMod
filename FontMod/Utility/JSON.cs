using Newtonsoft.Json;
using System.IO;

namespace FontMod.Utility;

internal class JSON
{
    public static string ToJSON<T>(T input)
    {
        return JsonConvert.SerializeObject(input, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None
        });
    }

    public static T FromJSON<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });
    }

    public static T LoadJSONFromFile<T>(string filePath) where T : class, new()
    {
        T obj = default;

        if (File.Exists(filePath))
            obj = JSON.FromJSON<T>(File.ReadAllText(filePath));

        return obj;
    }

    public static void SaveJSONToFile<T>(string path, T obj)
    {
        File.WriteAllText(path, JSON.ToJSON(obj));
    }
}
