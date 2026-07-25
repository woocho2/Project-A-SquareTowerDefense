using UnityEngine;

public static class CommonUtil
{
    public static bool ContainsLayer(LayerMask layerMask, int layer)
    {     
        return (layerMask.value & (1 << layer)) != 0;
    }
}
