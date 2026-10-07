using UnityEngine;

// One column of one step.
public class StepView : MonoBehaviour
{
    private int lane; // -3 (left) to 3 (right)

    public void Setup(int stepIndex, int laneIndex, Color color)
    {
        lane = laneIndex;
        name = "Step " + stepIndex + " lane " + lane;
        GetComponent<SpriteRenderer>().color = Color.Lerp(color, Color.black, 0.3f); // a bit darker, so players stand out on a step of their color
    }

    // rowOffset = how many steps above the anchor step this step is (negative = below).
    public void Place(float rowOffset)
    {
        StairPerspective perspective = GameLoop.Instance.GetPerspective();

        // Same column positions as the Player: lane * columnWidth.
        float columnWidth = perspective.GetHalfWidth(0f) * 2f / Stairs.LaneCount;
        transform.position = perspective.ToScreenPosition(rowOffset, lane * columnWidth);

        float width = columnWidth * perspective.GetScale(rowOffset) * 0.9f; // a bit less than a full column, so a gap shows between columns
        float height = perspective.GetStepHeight(rowOffset) * 0.85f;        // a bit less than a full row, so a gap shows between steps
        transform.localScale = new Vector3(width, height, 1f);
    }
}
