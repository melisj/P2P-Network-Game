using P2P;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entity interface
/// </summary>
public interface IEntity
{
    [SyncData] byte id { get; set; }
    [SyncData] byte health { get; set; }
    [SyncData] Vector2 position { get; set; }
}

/// <summary>
/// Manager keeping track of entities within the game and the state of game
/// </summary>
public class GameManager : MonoBehaviour
{
    public static ProjectileManager ProjectileM;
    public static EnemyManager EnemyM;
    public static ObjectManager ObjectM;
    public static PlayerManager PlayerM;
    public static InteractableManager InteractableM;
    public static GameMenu IngameMenu;

    public static List<IEntity> entityList = new List<IEntity>();

    private void Awake() {
        ProjectileM = FindObjectOfType<ProjectileManager>();
        EnemyM = FindObjectOfType<EnemyManager>();
        ObjectM = FindObjectOfType<ObjectManager>();
        PlayerM = FindObjectOfType<PlayerManager>();
        InteractableM = FindObjectOfType<InteractableManager>();
        IngameMenu = FindObjectOfType<GameMenu>();
    }

    #region Entity Handler 
    /// <summary>
    /// Register an entity
    /// </summary>
    /// <param name="entity"></param>
    public static void AddEntity(IEntity entity) { 
        if(!entityList.Contains(entity))
            entityList.Add(entity);
    }

    /// <summary>
    /// Remove an entity
    /// </summary>
    /// <param name="entity"></param>
    public static void RemoveEntity(IEntity entity) {
        entityList.Remove(entity);
    }

    /// <summary>
    /// Get an entity with a specific ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static IEntity GetEntityFromID(byte id) {
        foreach (IEntity entity in entityList) {
            if (entity.id == id) {
                return entity;
            }
        }
        return null;
    }
    #endregion
}
