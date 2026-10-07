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
    [SerializeField] private int maxStepsAhead = 2;       // how far the front player can get ahead: more and the player behind falls off the screen

    // Asked before every step-up and hop. "reason" explains a refusal (for the Console).
    public bool CanMove(Player player, Player other, MoveIntent intent, out string reason)
    {
        // The front player can't climb so far that the player behind disappears off the screen.
        if (intent == MoveIntent.StepUp1 || intent == MoveIntent.StepUp2)
        {
            int landingStep = player.GetStepIndex() + 1;
            if (intent == MoveIntent.StepUp2)
            {
                landingStep = player.GetStepIndex() + 2;
            }

            if (landingStep - other.GetStepIndex() > maxStepsAhead)
            {
                reason = "the other player would be left off screen";
                return false;
            }
        }

        reason = "";
        return true;
    }

    // Called when a player lands on a step:
    // - grey step: safe, nothing happens
    // - our color: we grey it (now safe for the other player too), +20
    // - the other player's color: frozen for a while, -20
    public void CheckLanding(Player player)
    {
        Stairs stairs = GameLoop.Instance.GetStairs();
        int step = player.GetStepIndex();

        if (stairs.IsGrey(step))
        {
            return;
        }

        if (MustMatchPlayerColor(player, step))
        {
            stairs.GreyStep(step);
            GameLoop.Instance.AddScore(20);
        }
        else
        {
            Debug.Log("Wrong color: frozen");
            player.Freeze(wrongColorFreeze);
            GameLoop.Instance.AddScore(-20);
        }
    }

    // True when the step has the player's color, i.e. the step belongs to the player.
    // (We compare owners rather than colors: colors change from one group of steps to the next.)
    public bool MustMatchPlayerColor(Player player, int targetStep)
    {
        return GameLoop.Instance.GetStairs().GetStepOwner(targetStep) == player.GetPlayerIndex();
    }
}
