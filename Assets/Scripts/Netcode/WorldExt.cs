using Unity.Entities;
using Unity.NetCode;

public static class WorldExt
{
    public static World GetClientWorld()
    {
        foreach (var item in World.All)
        {
            if (item.IsClient())
                return item;
        }

        return null;
    }

    public static World GetServerWorld()
    {
        foreach (var item in World.All)
        {
            if (item.IsServer())
                return item;
        }

        return null;
    }
}