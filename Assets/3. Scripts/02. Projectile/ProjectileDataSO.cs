using UnityEngine;


[CreateAssetMenu(fileName = "NewProjectileData", menuName = "ScriptableObjects/ProjectileData")]
public class ProjectileData : ScriptableObject
{
    [Header("ÃÑ¾Ë ´É·ÂÄ¡")]
    public GameObject prefab; 
    public int projectileID;    
    public string projectileName;
    public float speed;
    public float splashradius;
    public int hiteffectID;    
}
