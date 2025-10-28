//using UnityEngine;
//using UnityEngine.UI;

//public class PlayerSpawner : MonoBehaviour
//{
//    [Header("Prefabs and Canvas")]
//    [SerializeField] private GameObject playerPrefab;
//    [SerializeField] private GameObject healthBarPrefab;
//    [SerializeField] private Transform hudCanvas;

//    private int playerCount = 0;

//    private void Awake()
//    {
//        if (hudCanvas == null)
//        {
//            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
//            if (canvas != null)
//            {
//                hudCanvas = canvas.transform;
//            }
//        }
//    }

//    /// <summary>
//    /// Spawns a player and their HUD, and initializes them.
//    /// </summary>
//    /// <param name="spawnPosition">Initial position of the player.</param>
//    public void SpawnPlayer(Vector2 spawnPosition)
//    {
//        if (playerPrefab == null)
//        {
//            Debug.LogError("PlayerSpawner: playerPrefab no asignado.");
//            return;
//        }

//        GameObject playerGO = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
//        PlayerPresenter presenter = playerGO.GetComponent<PlayerPresenter>();
//        if (presenter == null) Debug.LogWarning("PlayerSpawner: playerPrefab no tiene PlayerPresenter.");

//        if (healthBarPrefab == null)
//        {
//            Debug.LogError("PlayerSpawner: healthBarPrefab no asignado.");
//            return;
//        }

//        if (hudCanvas == null)
//        {
//            Debug.LogError("PlayerSpawner: hudCanvas no asignado y no se encontró uno en la escena.");
//            return;
//        }

//        GameObject hudGO = Instantiate(healthBarPrefab);
//        HealthBarHUD hud = hudGO.GetComponent<HealthBarHUD>();
//        if (hud == null) hud = hudGO.GetComponentInChildren<HealthBarHUD>();
//        if (hud == null)
//        {
//            Debug.LogError("PlayerSpawner: healthBarPrefab no contiene HealthBarHUD.");
//            Destroy(hudGO);
//            return;
//        }

//        if (hud.healthBar == null)
//        {
//            hud.healthBar = hudGO.GetComponentInChildren<Slider>();
//            if (hud.healthBar == null)
//            {
//                Debug.LogError("PlayerSpawner: Slider no encontrado dentro de healthBarPrefab.");
//                Destroy(hudGO);
//                return;
//            }
//        }

//        hud.Initialize();

//        Player player = new Player();
//        IPlayerService service = new PlayerService();
//        PlayerController controller = new PlayerController(player, service);

//        if (presenter != null)
//            presenter.Initialize(controller, hud);

//        playerCount++;
//    }
//}
