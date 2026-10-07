using UnityEngine;

public class StepView : MonoBehaviour
{
    public void Setup(int stepIndex, Color color)
    {
        name = "Step " + stepIndex;
        GetComponent<SpriteRenderer>().color = color;
    }

    // rowOffset = how many steps above the anchor step this step is (negative = below).
    public void Place(float rowOffset)
    {
        StairPerspective perspective = GameLoop.Instance.GetPerspective();

        transform.position = perspective.ToScreenPosition(rowOffset, 0f);

        float width = perspective.GetHalfWidth(rowOffset) * 2f;
        float height = perspective.GetStepHeight(rowOffset) * 0.85f; // a bit less than a full row, so a gap shows between steps
        transform.localScale = new Vector3(width, height, 1f);
    }
}
