using UnityEngine;

// Turns a position on the stairs into a position on the screen.
// "rowOffset" = how many steps above the anchor step (the one the stairs are following). Negative = below.
// Each step further up is drawn smaller (shrink) and the steps pile up toward one point at the top
// of the screen: the vanishing point, where the enemy sits.
public class StairPerspective : MonoBehaviour
{
    [SerializeField] private float bottomY = -1f;     // screen height of the anchor step (row 0)
    [SerializeField] private float stepHeight = 1f;   // screen height of the anchor step
    [SerializeField] private float halfWidth = 3f;    // half width of the anchor step
    [SerializeField] private float shrink = 0.8f;     // each step up is 80% the size of the one below

    // With the default values the vanishing point is at y = bottomY + stepHeight / (1 - shrink) = 4.

    // flatX = left/right position on a flat, full size step (what Player computes from its lane).
    public Vector3 ToScreenPosition(float rowOffset, float flatX)
    {
        return new Vector3(flatX * GetScale(rowOffset), GetY(rowOffset), 0f);
    }

    // 1 = anchor step size, 0.8 = one step up, 0.64 = two steps up, 1.25 = one step down...
    public float GetScale(float rowOffset)
    {
        return Mathf.Pow(shrink, rowOffset);
    }

    public float GetDepth(float rowOffset)
    {
        return default;
    }

    public float GetHalfWidth(float rowOffset)
    {
        return halfWidth * GetScale(rowOffset);
    }

    public float GetStepHeight(float rowOffset)
    {
        return stepHeight * GetScale(rowOffset);
    }

    // Screen height of a row = bottomY + the heights of all the (shrinking) steps below it.
    public float GetY(float rowOffset)
    {
        return bottomY + stepHeight * (1f - GetScale(rowOffset)) / (1f - shrink);
    }
}
