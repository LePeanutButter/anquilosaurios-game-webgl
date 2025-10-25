using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayLoader : MonoBehaviour
{
    public static GameplayLoader Instance { get; private set; }

    [Header("Scene names")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    [Header("Prefabs to instantiate if not present in scene")]
    [Tooltip("Prefabs that contain PlayerSpawner and MapGenerator scripts")]
    [SerializeField] private GameObject playerSpawnerPrefab;
    [SerializeField] private GameObject mapGeneratorPrefab;

    [Header("Optional runtime config")]
    [SerializeField] private Vector2 initialSpawnPosition = new Vector2(0f, 0f);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void LoadGameplay()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameplaySceneName) return;

        PlayerSpawner playerSpawner = Object.FindFirstObjectByType<PlayerSpawner>();
        MapGenerator mapGenerator = Object.FindFirstObjectByType<MapGenerator>();

        if (mapGenerator == null)
        {
            if (mapGeneratorPrefab != null)
            {
                GameObject mapGO = Instantiate(mapGeneratorPrefab);
                mapGenerator = mapGO.GetComponent<MapGenerator>();
                if (mapGenerator == null)
                    Debug.LogError("GameplayLoader: mapGeneratorPrefab no contiene MapGenerator.");
            }
            else
            {
                Debug.LogWarning("GameplayLoader: mapGenerator no encontrado y mapGeneratorPrefab no asignado.");
            }
        }

        if (playerSpawner == null)
        {
            if (playerSpawnerPrefab != null)
            {
                GameObject spawnerGO = Instantiate(playerSpawnerPrefab);
                playerSpawner = spawnerGO.GetComponent<PlayerSpawner>();
                if (playerSpawner == null)
                    Debug.LogError("GameplayLoader: playerSpawnerPrefab no contiene PlayerSpawner.");
            }
            else
            {
                Debug.LogWarning("GameplayLoader: playerSpawner no encontrado y playerSpawnerPrefab no asignado.");
            }
        }

        if (mapGenerator == null || playerSpawner == null)
        {
            Debug.LogError("GameplayLoader: No se pudieron obtener MapGenerator o PlayerSpawner. Abortando generación.");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            return;
        }

        mapGenerator.GenerateMap();
        playerSpawner.SpawnPlayer(initialSpawnPosition);

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
