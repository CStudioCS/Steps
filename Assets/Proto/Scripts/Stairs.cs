using UnityEngine;

public class Stairs : MonoBehaviour
{
    public const int LaneCount = 7; // columns on each step: lanes -3 to 3, like the Player

    [SerializeField] private StepView stepPrefab;
    [SerializeField] private int stepCount = 30;
    [SerializeField] private float scrollSpeed = 5f; // how fast the stairs catch up with the players

    // Hand-authored colors, read 7 at a time: the first 7 are step 0 from left to right, the next 7 are step 1...
    // If the list is too short, it starts again from the beginning.
    [SerializeField] private Color[] stepColors = new Color[0];
    // Used only when stepColors is empty: every column of every step gets a random color from this list.
    // Keep it in sync with GameLoop's playerColors.
    [SerializeField] private Color[] randomPalette = { Color.cyan, Color.magenta };

    private Color[,] colors;       // the final color of every column: colors[step, column]
    private StepView[,] stepViews; // one StepView per column: stepViews[step, column]
    private float anchorStep; // the step drawn at row 0. It follows the players smoothly, which scrolls the stairs.

    private void Awake()
    {
        PickStepColors();

        // One StepView per column of every step, all created once at the start.
        stepViews = new StepView[stepCount, LaneCount];

        for (int step = 0; step < stepCount; step++)
        {
            for (int column = 0; column < LaneCount; column++)
            {
                int lane = column - 3; // columns go from 0 to 6, lanes from -3 to 3
                stepViews[step, column] = Instantiate(stepPrefab, transform);
                stepViews[step, column].Setup(step, lane, GetStepColor(step, lane));
            }
        }
    }

    private void Update()
    {
        UpdateAnchor();
        RefreshStepViews();
    }

    private void PickStepColors()
    {
        colors = new Color[stepCount, LaneCount];

        for (int step = 0; step < stepCount; step++)
        {
            for (int column = 0; column < LaneCount; column++)
            {
                if (stepColors.Length > 0)
                {
                    int index = step * LaneCount + column;
                    colors[step, column] = stepColors[index % stepColors.Length]; // % wraps around to the start of the list
                }
                else
                {
                    colors[step, column] = randomPalette[Random.Range(0, randomPalette.Length)];
                }
            }
        }
    }

    // lane goes from -3 to 3, the array columns from 0 to 6: hence the + 3.
    public Color GetStepColor(int stepIndex, int lane)
    {
        return colors[stepIndex, lane + 3];
    }

    public int GetStepCount()
    {
        return stepCount;
    }

    public int GetTargetStep()
    {
        return stepCount - 1;
    }

    public float GetAnchorStep()
    {
        return anchorStep;
    }

    // The stairs follow the middle point between the two players. Lerp makes it smooth:
    // it moves fast when far from the target and slows down when close.
    private void UpdateAnchor()
    {
        Player player1 = GameLoop.Instance.GetPlayer(0);
        Player player2 = GameLoop.Instance.GetPlayer(1);
        float middleStep = (player1.GetStepIndex() + player2.GetStepIndex()) / 2f;

        anchorStep = Mathf.Lerp(anchorStep, middleStep, scrollSpeed * Time.deltaTime);
    }

    private void RefreshStepViews()
    {
        for (int step = 0; step < stepCount; step++)
        {
            for (int column = 0; column < LaneCount; column++)
            {
                stepViews[step, column].Place(step - anchorStep);
            }
        }
    }
}
