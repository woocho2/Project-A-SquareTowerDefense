using System;
namespace UnityEngine {
    public class MonoBehaviour { public Transform transform = new Transform(); }
    public class ScriptableObject {}
    public class Sprite { public string name; public Bounds bounds = new Bounds { size = new Vector3(256f/192f,256f/192f,0) }; }
    public struct Bounds { public Vector3 size; }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
    public struct Quaternion {}
    public struct Color {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color white => new Color(1,1,1,1);
    }
    public struct Color32 {
        public byte r,g,b,a;
        public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static implicit operator Color(Color32 c)=>new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f);
    }
    public class Transform {
        public Vector3 localPosition,localScale = new Vector3(1,1,1);
        public Quaternion localRotation;
        public Transform Find(string name)=>null;
        public T GetComponent<T>()=>default(T);
    }
    public class SpriteRenderer {
        public Sprite sprite;public bool enabled,flipX,flipY;public Color color;
        public int sortingLayerID,sortingOrder;public Transform transform = new Transform();
        public void SetPropertyBlock(object value){}
    }
    public static class Resources { public static T Load<T>(string key)=>default(T); }
    public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); }
    public static class Debug { public static int Warnings;public static void LogWarning(string value,object context){Warnings++;} }
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField:Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute:Attribute { public HeaderAttribute(string value){} }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute:Attribute { public TooltipAttribute(string value){} }
    [AttributeUsage(AttributeTargets.Field)] public class MinAttribute:Attribute { public MinAttribute(float value){} }
    [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute:Attribute { public string menuName; }
}
namespace UnityEngine.Serialization {
    [AttributeUsage(AttributeTargets.Field)] public class FormerlySerializedAsAttribute:Attribute { public FormerlySerializedAsAttribute(string value){} }
}
// Only the three TowerData members read by TowerVisual are simulated.
public class TowerData { public int towerID;public int Tier;public int VisualTowerID; }
