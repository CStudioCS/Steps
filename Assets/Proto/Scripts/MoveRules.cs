using UnityEngine;

public enum MoveIntent
{
    StepUp1,
    StepUp2,
    HopInPlace
}

public class MoveRules : MonoBehaviour
{
    [SerializeField] private float wrongColorFreeze = 2f; // seconds frozen after landing on a step of the wrong color

    public bool CanMove(Player player, Player other, MoveIntent intent, Judgment judgment, out string reason)
    {
        reason = default;
        return default;
    }

    // Called when a player lands on a step: wrong color = frozen for a while.
    public void CheckLanding(Player player)
    {
        if (!MustMatchPlayerColor(player, player.GetStepIndex()))
        {
            Debug.Log("Wrong color: frozen");
            player.Freeze(wrongColorFreeze);
        }
    }

    // True when the player's column on that step has the same color as the player.
    public bool MustMatchPlayerColor(Player player, int targetStep)
    {
        Color stepColor = GameLoop.Instance.GetStairs().GetStepColor(targetStep, player.GetLane());
        return stepColor == player.GetColor();
    }

    private bool DoubleStepRequiresPerfect(MoveIntent intent, Judgment judgment)
    {
        return default;
    }
}
