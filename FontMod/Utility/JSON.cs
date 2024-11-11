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
            TypeNameHandling = TypeNameHandling.None
        });
    }

    public static T LoadJSONFromFile<T>(string filePath)
    {
        if (File.Exists(filePath))
            return JSON.FromJSON<T>(File.ReadAllText(filePath));

        return default;
    }

    public static void SaveJSONToFile<T>(string path, T obj)
    {
        File.WriteAllText(path, JSON.ToJSON(obj));
    }
}
