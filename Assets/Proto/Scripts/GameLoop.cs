using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Countdown,
    Playing,
    Won,
    Lost
}

public class GameLoop : MonoBehaviour
{
    // Singleton: there is only one GameLoop, and any script can reach it with GameLoop.Instance
    // (use it from Start or later, not from Awake, so the GameLoop is sure to exist).
    public static GameLoop Instance { get; private set; }

    private static readonly string[] KeyboardSchemes = { "KeyboardWASD", "KeyboardArrows" };

    [SerializeField] private Player playerPrefab;
    [SerializeField] private Color[] playerColors = { Color.cyan, Color.magenta };
    [SerializeField] private int[] startLanes = { -1, 1 };
    [SerializeField] private Stairs stairs;
    [SerializeField] private StairPerspective perspective;

    private readonly Player[] players = new Player[2];

    private void Awake()
    {
        // A second GameLoop (e.g. one dropped in the scene by mistake) removes itself.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SpawnPlayers();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public Stairs GetStairs()
    {
        return stairs;
    }

    public StairPerspective GetPerspective()
    {
        return perspective;
    }

    public Player GetPlayer(int index)
    {
        return players[index];
    }

    // The other player of the pair: GetOtherPlayer(player 1) gives player 2, and the reverse.
    public Player GetOtherPlayer(Player player)
    {
        if (players[0] == player)
        {
            return players[1];
        }

        return players[0];
    }

    private void SpawnPlayers()
    {
        for (int i = 0; i < players.Length; i++)
        {
            PlayerInput input = i < Gamepad.all.Count
                ? PlayerInput.Instantiate(playerPrefab.gameObject, i, "Gamepad", -1, Gamepad.all[i])
                : PlayerInput.Instantiate(playerPrefab.gameObject, i, KeyboardSchemes[i], -1, Keyboard.current);

            input.neverAutoSwitchControlSchemes = true;
            players[i] = input.GetComponent<Player>();
            players[i].Setup(playerColors[i], startLanes[i]);
        } 
    }

    private void Update()
    {
    }

    public void StartCountdown()
    {
    }

    public void StartPlaying()
    {
    }

    public bool CheckWin()
    {
        return default;
    }

    public bool CheckLose()
    {
        return default;
    }

    public void Win()
    {
    }

    public void Lose()
    {
    }
}
