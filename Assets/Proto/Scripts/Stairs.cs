using UnityEngine;

public class Stairs : MonoBehaviour
{
    [SerializeField] private StepView stepPrefab;
    [SerializeField] private int stepCount = 30;
    [SerializeField] private float scrollSpeed = 5f; // how fast the stairs catch up with the players

    private StepView[] stepViews;
    private float anchorStep; // the step drawn at row 0. It follows the players smoothly, which scrolls the stairs.

    private void Awake()
    {
        // One StepView per step, all created once at the start.
        stepViews = new StepView[stepCount];

        for (int i = 0; i < stepCount; i++)
        {
            stepViews[i] = Instantiate(stepPrefab, transform);
            stepViews[i].Setup(i, GetStepColor(i));
        }
    }

    private void Update()
    {
        UpdateAnchor();
        RefreshStepViews();
    }

    public Color GetStepColor(int stepIndex)
    {
        return Color.white; // every step is white until the step colors are added
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
        for (int i = 0; i < stepCount; i++)
        {
            stepViews[i].Place(i - anchorStep);
        }
    }
}
