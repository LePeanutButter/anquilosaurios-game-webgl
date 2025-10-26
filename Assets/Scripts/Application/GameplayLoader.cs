using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the asynchronous loading and initialization of the gameplay scene.
/// 
/// This component ensures that required gameplay systems (such as <see cref="PlayerSpawner"/>
/// and <see cref="MapGenerator"/>) are present after loading the scene. If not found,
/// it instantiates the provided prefabs automatically.
/// 
/// The loader persists across scenes to provide a global entry point for gameplay setup.
/// </summary>
public class GameplayLoader : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Singleton instance of the <see cref="GameplayLoader"/>.
    /// Ensures only one loader exists throughout the game lifecycle.
    /// </summary>
    public static GameplayLoader Instance { get; private set; }

    #endregion

    #region Inspector Fields

    [Header("Scene names")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    [Header("Prefabs to instantiate if not present in scene")]
    [Tooltip("Prefabs that contain PlayerSpawner and MapGenerator scripts")]
    [SerializeField] private GameObject playerSpawnerPrefab;
    [SerializeField] private GameObject mapGeneratorPrefab;

    [Header("Optional runtime config")]
    [SerializeField] private Vector2 initialSpawnPosition = new Vector2(0f, 0f);

    [Header("Lethal objects spawning")]
    [Tooltip("Prefab of the lethal object (must have Rigidbody2D)")]
    [SerializeField] private GameObject lethalPrefab;
    [Tooltip("Allowed horizontal width in pixels to spawn inside (centered). Objects won't spawn on edges")]
    [SerializeField] private int allowedSpawnWidthPixels = 1850;
    [Tooltip("Extra vertical margin in pixels above the screen to spawn from")]
    [SerializeField] private int spawnAboveScreenPixels = 50;
    [Tooltip("Seconds that the match lasts (during which spawns may occur)")]
    [SerializeField] private float matchDurationSeconds = 30f;
    [Tooltip("Delay in seconds before lethal objects start spawning")]
    [SerializeField] private float spawnStartDelaySeconds = 5f;
    [Tooltip("Initial spawn interval in seconds")]
    [SerializeField] private float initialSpawnInterval = 1.0f;
    [Tooltip("Minimum fall speed (units/sec) at start")]
    [SerializeField] private float minFallSpeed = 2f;
    [Tooltip("Maximum fall speed (units/sec) at end of match")]
    [SerializeField] private float maxFallSpeed = 12f;
    [Tooltip("Optional: gradually reduce spawn interval to this minimum")]
    [SerializeField] private float minSpawnInterval = 0.2f;

    #endregion

    #region Private Fields

    private Coroutine _spawnCoroutine;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Ensures only one instance of <see cref="GameplayLoader"/> exists.
    /// If another instance is found, it is destroyed automatically.
    /// The surviving instance is marked as <see cref="Object.DontDestroyOnLoad"/>.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);

            if (lethalPrefab == null)
            {
                lethalPrefab = Resources.Load<GameObject>("Prefab/Obstacles/Herbicide");
                if (lethalPrefab == null)
                    Debug.LogError("GameplayLoader: Failed to load prefab 'Prefab/Obstacles/Herbicide' from Resources.");
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Always remove scene event subscriptions when this object is destroyed
    /// to avoid ghost callbacks to destroyed instances.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Loads the gameplay scene and automatically initializes
    /// the map and player spawning systems once the scene is loaded.
    /// </summary>
    public void LoadGameplay()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(gameplaySceneName);
    }

    #endregion

    #region Scene Initialization

    /// <summary>
    /// Callback invoked when a new scene has finished loading.
    /// Ensures that required gameplay systems (<see cref="MapGenerator"/> and <see cref="PlayerSpawner"/>)
    /// exist and are properly initialized.
    /// </summary>
    /// <param name="scene">The scene that was loaded.</param>
    /// <param name="mode">The scene loading mode.</param>
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
                    Debug.LogError("GameplayLoader: The assigned mapGeneratorPrefab does not contain a MapGenerator component.");
            }
            else
            {
                Debug.LogWarning("GameplayLoader: No MapGenerator found and no prefab assigned.");
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
                Debug.LogWarning("GameplayLoader: No PlayerSpawner found and no prefab assigned.");
            }
        }

        if (mapGenerator == null || playerSpawner == null)
        {
            Debug.LogError("GameplayLoader: Failed to initialize required systems (MapGenerator or PlayerSpawner). Aborting setup.");
            SceneManager.sceneLoaded -= OnSceneLoaded;
            return;
        }

        mapGenerator.GenerateMap();
        playerSpawner.SpawnPlayer(initialSpawnPosition);

        if (lethalPrefab != null)
        {
            if (Camera.main != null)
            {
                if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = StartCoroutine(SpawnLethalWithDelayRoutine());
            }
            else
            {
                Debug.LogWarning("GameplayLoader: Camera.main not found; skipping lethal spawn routine.");
            }
        }


        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region Lethal Spawning Routine

    private IEnumerator SpawnLethalWithDelayRoutine()
    {
        if (spawnStartDelaySeconds <= 0f)
        {
            yield return SpawnLethalRoutineInternal(matchDurationSeconds);
            _spawnCoroutine = null;
            yield break;
        }

        float waited = 0f;
        while (waited < spawnStartDelaySeconds)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        float remainingMatchTime = Mathf.Max(0f, matchDurationSeconds - spawnStartDelaySeconds);
        if (remainingMatchTime > 0f)
        {
            yield return SpawnLethalRoutineInternal(remainingMatchTime);
        }

        _spawnCoroutine = null;
    }

    /// <summary>
    /// Continuously spawns lethal objects for a specified duration.
    /// 
    /// The spawn interval and fall speed both evolve dynamically during the match:
    /// - The spawn rate increases as time progresses.
    /// - The falling speed of spawned objects interpolates between <see cref="minFallSpeed"/>
    ///   and <see cref="maxFallSpeed"/> across the total match duration.
    /// </summary>
    /// <param name="durationSeconds">Total duration (in seconds) for which spawning will occur.</param>
    private IEnumerator SpawnLethalRoutineInternal(float durationSeconds)
    {
        float elapsed = 0f;
        float spawnTimer = 0f;
        float spawnInterval = initialSpawnInterval;

        while (elapsed < durationSeconds)
        {
            elapsed += Time.deltaTime;
            spawnTimer += Time.deltaTime;

            float globalT = Mathf.Clamp01((spawnStartDelaySeconds + elapsed) / matchDurationSeconds);
            float currentFallSpeed = Mathf.Lerp(minFallSpeed, maxFallSpeed, globalT);
            float currentSpawnInterval = Mathf.Lerp(initialSpawnInterval, minSpawnInterval, globalT);

            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                spawnInterval = currentSpawnInterval;
                SpawnSingleLethal(currentFallSpeed);
            }

            yield return null;
        }
    }

    /// <summary>
    /// Instantiates a single lethal object at a random position above the screen.
    /// 
    /// The spawn position is randomly selected within a restricted horizontal range defined by
    /// <see cref="allowedSpawnWidthPixels"/>, ensuring that objects do not spawn near screen edges.
    /// 
    /// The object is given a downward velocity using its <see cref="Rigidbody2D"/> component,
    /// and is automatically assigned to the "Lethal" layer if it exists.
    /// </summary>
    /// <param name="fallSpeed">Vertical velocity (units/second) applied to the spawned object.</param>
    private void SpawnSingleLethal(float fallSpeed)
    {
        if (lethalPrefab == null || Camera.main == null) return;

        int screenW = Screen.width;
        int screenH = Screen.height;
        int allowedW = Mathf.Clamp(allowedSpawnWidthPixels, 1, screenW);
        int leftPx = (screenW - allowedW) / 2;
        int rightPx = leftPx + allowedW;

        float xPixel = Random.Range(leftPx, rightPx);
        float yPixel = screenH + spawnAboveScreenPixels;

        Vector3 spawnScreenPos = new Vector3(xPixel, yPixel, Mathf.Abs(Camera.main.transform.position.z));
        Vector3 spawnWorldPos = Camera.main.ScreenToWorldPoint(spawnScreenPos);
        spawnWorldPos.z = 0f;

        GameObject go = Instantiate(lethalPrefab, spawnWorldPos, Quaternion.identity);
        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, -fallSpeed);
        }
        else
        {
            rb = go.AddComponent<Rigidbody2D>();
            rb.linearVelocity = new Vector2(0f, -fallSpeed);
        }

        int lethalLayer = LayerMask.NameToLayer("Lethal");
        if (lethalLayer >= 0)
        {
            go.layer = lethalLayer;
        }

    }

    #endregion
}
