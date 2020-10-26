using UnityEngine;

/// <summary>
/// Class for storing permanent game data
/// </summary>
public class GameData : MonoBehaviour
{
    public static GameData instance;

    public void OnEnable() {
        instance = this;
    }

    // Color data for each player based on index
    public static Color[] PlayerColors = {
        new Color(0.98f, 0.49f, 0.33f),
        new Color(0.34f, 0.44f, 0.98f),
        new Color(0.61f, 0.98f, 0.31f)
    };

    public GameObject[] projectilePrefabs;
}
