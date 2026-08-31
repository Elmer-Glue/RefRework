using SPTarkov.Server.Core.Models.Common;

namespace RefReworkServer;

public static class Utils
{
    public static HashSet<MongoId> GetDogtagsList(Context context)
    {
        HashSet<MongoId> list = new();

        foreach (var kvp in context.templates.Items)
        {
            if (kvp.Value.Properties?.DogTagQualities != null && kvp.Value.Properties.DogTagQualities == true)
            {
                list.Add(kvp.Key);
            }
        }

        return list;
    }
}
