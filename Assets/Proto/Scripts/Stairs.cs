using System.Collections.Generic;
using UnityEngine;

// Everything the game needs to know about one step.
public class StepData
{
    public int owner;      // which player the step belongs to: 0 = player 1, 1 = player 2, -1 = nobody (grey from the start)
    public bool isGrey;    // safe for both players: a group's first step, or a step greyed by its owner
    public Color[] colors; // the two colors of the step's group: colors[0] = player 1, colors[1] = player 2
}

// Endless stairs: new steps are created above the players as they climb, in groups.
// Each group = one grey (safe) step + random steps in two new random colors.
public class Stairs : MonoBehaviour
{
    [SerializeField] private StepView stepPrefab;
    [SerializeField] private int stepsPerColorGroup = 6; // colored steps in a group, after its grey step
    [SerializeField] private int stepsBelow = 4;         // steps drawn below the center row
    [SerializeField] private int stepsAbove = 16;        // steps drawn above the center row (the far ones are tiny)
    // Colors that can be picked: two different ones per group, one per player.
    [SerializeField] private Color[] palette = { Color.red, Color.blue, Color.green, Color.yellow, new Color(1f, 0.5f, 0f), new Color(0.6f, 0.2f, 0.8f) };
    [SerializeField] private Color safeColor = Color.gray; // color of a grey step: safe for both players

    [Header("Pattern rules")]
    [SerializeField] private int maxStepsInARow = 3;     // no more than this many steps of the same player in a row
    [SerializeField] private int minStepsPerPlayer = 2;  // each player gets at least this many steps in a group

    private List<StepData> steps = new List<StepData>(); // every step created so far: steps[0] is the bottom one
    private StepView[] stepViews;                        // only the steps on screen are drawn, by these views

    private void Awake()
    {
        // The views are reused: each frame they show the steps around the players.
        stepViews = new StepView[stepsBelow + stepsAbove];
        for (int i = 0; i < stepViews.Length; i++)
        {
            stepViews[i] = Instantiate(stepPrefab, transform);
            stepViews[i].Setup(i);
        }

        CreateStepsUpTo(stepsAbove);
    }

    // LateUpdate runs after every Update: the players have already moved this frame, so the steps follow them exactly.
    private void LateUpdate()
    {
        float anchorStep = GetAnchorStep();
        CreateStepsUpTo(Mathf.FloorToInt(anchorStep) + stepsAbove);

        UpdatePlayerColor(GameLoop.Instance.GetPlayer(0), 0);
        UpdatePlayerColor(GameLoop.Instance.GetPlayer(1), 1);

        RefreshStepViews(anchorStep);
    }

    // ---------- Creating steps ----------

    private void CreateStepsUpTo(int stepIndex)
    {
        while (steps.Count <= stepIndex)
        {
            CreateColorGroup();
        }
    }

    // Adds one group on top of the stairs: a grey step, then random steps in two new colors.
    // Old steps keep their colors.
    private void CreateColorGroup()
    {
        Color[] colors = PickTwoColors();
        int[] pattern = PickRandomPattern();

        AddStep(-1, true, colors); // the grey step: safe for both, and where players switch to the new colors

        for (int i = 0; i < pattern.Length; i++)
        {
            AddStep(pattern[i], false, colors);
        }
    }

    private void AddStep(int owner, bool isGrey, Color[] colors)
    {
        StepData step = new StepData();
        step.owner = owner;
        step.isGrey = isGrey;
        step.colors = colors;
        steps.Add(step);
    }

    // Two different random colors from the palette: [0] for player 1, [1] for player 2.
    private Color[] PickTwoColors()
    {
        int first = Random.Range(0, palette.Length);
        int second;
        do
        {
            second = Random.Range(0, palette.Length);
        }
        while (second == first); // pick again until it's a different color

        return new Color[] { palette[first], palette[second] };
    }

    // A random owner (0 or 1) for every step of a group. Redrawn until the pattern rules accept it.
    private int[] PickRandomPattern()
    {
        int[] pattern = RandomPattern();
        int tries = 1;

        while (!IsPatternValid(pattern) && tries < 100) // 100 tries max, in case the rules can never be met
        {
            pattern = RandomPattern();
            tries++;
        }

        return pattern;
    }

    private int[] RandomPattern()
    {
        int[] pattern = new int[stepsPerColorGroup];
        for (int i = 0; i < pattern.Length; i++)
        {
            pattern[i] = Random.Range(0, 2); // 0 or 1
        }
        return pattern;
    }

    // ---------- Pattern rules: add a new rule here to forbid more patterns ----------

    private bool IsPatternValid(int[] pattern)
    {
        return NoLongRunOfOnePlayer(pattern) && EnoughStepsForEachPlayer(pattern);
    }

    // Too many steps of one player in a row and the other player waits too long.
    private bool NoLongRunOfOnePlayer(int[] pattern)
    {
        int run = 1;
        for (int i = 1; i < pattern.Length; i++)
        {
            if (pattern[i] == pattern[i - 1])
            {
                run++;
            }
            else
            {
                run = 1;
            }

            if (run > maxStepsInARow)
            {
                return false;
            }
        }
        return true;
    }

    // Both players must have steps to grey in every group.
    private bool EnoughStepsForEachPlayer(int[] pattern)
    {
        int player1Steps = 0;
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == 0)
            {
                player1Steps++;
            }
        }
        int player2Steps = pattern.Length - player1Steps;

        return player1Steps >= minStepsPerPlayer && player2Steps >= minStepsPerPlayer;
    }

    // ---------- Reading and changing steps ----------

    public Color GetStepColor(int stepIndex)
    {
        StepData step = steps[stepIndex];
        if (step.isGrey)
        {
            return safeColor;
        }

        return step.colors[step.owner];
    }

    public int GetStepOwner(int stepIndex)
    {
        return steps[stepIndex].owner;
    }

    public bool IsGrey(int stepIndex)
    {
        return steps[stepIndex].isGrey;
    }

    // The step's own player landed on it: it becomes grey, safe for the other player too.
    public void GreyStep(int stepIndex)
    {
        steps[stepIndex].isGrey = true;
    }

    // A player shows the color they need for the next steps: their color in the group of the step just above them.
    // Thanks to the grey step at the start of each group, the steps they can reach (+1 or +2) are never from two groups.
    private void UpdatePlayerColor(Player player, int playerIndex)
    {
        if (!player.IsGrounded())
        {
            return; // change color only while standing still, not in the middle of a hop
        }

        Color newColor = steps[player.GetStepIndex() + 1].colors[playerIndex];
        if (player.GetColor() != newColor)
        {
            player.SetColor(newColor);
        }
    }

    // ---------- Drawing ----------

    // The step drawn at row 0 (the center row): the step of the player in front.
    // It uses the drawn step, which moves during the step-up hop, so the stairs scroll
    // exactly at the speed of the hop and the front player never leaves the center row.
    public float GetAnchorStep()
    {
        Player player1 = GameLoop.Instance.GetPlayer(0);
        Player player2 = GameLoop.Instance.GetPlayer(1);

        return Mathf.Max(player1.GetDrawnStep(), player2.GetDrawnStep());
    }

    private void RefreshStepViews(float anchorStep)
    {
        int firstStep = Mathf.FloorToInt(anchorStep) - stepsBelow;

        for (int i = 0; i < stepViews.Length; i++)
        {
            int stepIndex = firstStep + i;

            if (stepIndex < 0)
            {
                stepViews[i].gameObject.SetActive(false); // below the first step: nothing to draw
            }
            else
            {
                stepViews[i].gameObject.SetActive(true);
                stepViews[i].SetColor(GetStepColor(stepIndex));
                stepViews[i].Place(stepIndex - anchorStep);
            }
        }
    }
}
