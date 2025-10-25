using UnityEngine;

/// <summary>
/// Responsible for generating a random map from a predefined set of map prefabs.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    /// <summary>
    /// Array of available map prefabs to choose from during generation.
    /// </summary>
    [Header("Available Map Prefabs")]
    [SerializeField] private GameObject[] mapPrefabs;

    /// <summary>
    /// Instantiates a randomly selected map prefab at the origin of the scene.
    /// Logs an error if no map prefabs are assigned.
    /// </summary>
    public void GenerateMap()
    {
        if (mapPrefabs == null || mapPrefabs.Length == 0)
        {
            Debug.LogError("MapGenerator: No map prefabs have been assigned.");
            return;
        }

        int index = UnityEngine.Random.Range(0, mapPrefabs.Length);
        GameObject prefab = mapPrefabs[index];

        GameObject instance = Instantiate(prefab);
        instance.transform.position = prefab.transform.position;
        instance.transform.rotation = prefab.transform.rotation;
    }

}