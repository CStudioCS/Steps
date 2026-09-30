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
    private static readonly string[] KeyboardSchemes = { "KeyboardWASD", "KeyboardArrows" };

    [SerializeField] private Player playerPrefab; 
    [SerializeField] private Color[] playerColors = { Color.cyan, Color.magenta };
    [SerializeField] private int[] startLanes = { -1, 1 };

    private readonly Player[] players = new Player[2];

    private void Awake()
    {
        SpawnPlayers();
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
