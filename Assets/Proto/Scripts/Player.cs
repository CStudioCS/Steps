using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    Grounded,
    Hopping,
    Airborne,
    Stunned,
    Sliding,
}

public class Player : MonoBehaviour
{
    [SerializeField] private Color color = Color.white;
    [SerializeField] private float laneHalfWidth = 3f; // half width of the stairs, copied from StairPerspective in Start
    [SerializeField] private float slideSpeed = 10f; // speed to move from one lane to the next
    [SerializeField] private float hopSpeed = 4f;    // speed of the hop over the other player
    [SerializeField] private float hopHeight = 0.6f; // how high the hop arc goes
    [SerializeField] private float stunDuration = 1.5f; // seconds frozen after being hit
    [SerializeField] private float stepUpSpeed = 5f;    // speed of the hop up the stairs

    private PlayerInput playerInput;
    private InputAction slideAction;
    private InputAction stepUp1Action;
    private InputAction stepUp2Action;
    private InputAction hopAction;
    private Vector3 origPos, targetPos;
    private int nbOfLanes = 7;
    private bool isSliding;
    private float target;
    private float t = 0f;
    private int moveDirection; // -1 = moving left, 1 = moving right (only meaningful while Sliding or Airborne)
    private int laneBeforeHop; // where to send the player back if hit during a hop over
    private float stunTimer;   // seconds left before the player can move again
    private Vector3 flatPosition; // position on a flat, full size step. The perspective turns it into the screen position.
    private Vector3 baseScale;    // prefab scale, before the perspective makes the player smaller or bigger
    private int stepBeforeHop;    // the step a step-up hop started from
    private float drawnStep;      // the step the player is drawn on: goes smoothly from one step to the next during a step-up
    private int queuedSlides;     // slide taps waiting to be played: 2 = two columns to the right, -1 = one to the left

    private PlayerState state = PlayerState.Grounded;
    private int stepIndex;
    private int lane;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    // Actions are cached in Start: PlayerInput gives each player its own copy of the actions during its own setup.
    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        slideAction = playerInput.actions["Slide"];
        stepUp1Action = playerInput.actions["StepUp1"];
        stepUp2Action = playerInput.actions["StepUp2"];
        hopAction = playerInput.actions["Hop"];

        laneHalfWidth = GameLoop.Instance.GetPerspective().GetHalfWidth(0f);
    }

    public void Setup(Color playerColor, int startLane)
    {
        color = playerColor;
        lane = startLane;
        GetComponent<SpriteRenderer>().color = playerColor;
    }

    private void Update()
    {
        if (state == PlayerState.Stunned)
        {
            DuringStun();
        }
        else
        {
            ReadInput();
        }

        UpdateVisuals();
    }

    private void ReadInput()
    {
        if (playerInput == null)
        {
            return;
        }

        // Every tap is remembered, even while still busy (sliding, hopping...)
        if (slideAction.WasPressedThisFrame())
        {
            float tap = slideAction.ReadValue<float>();
            if (tap > 0)
            {
                queuedSlides += 1;
            }
            else if (tap < 0)
            {
                queuedSlides -= 1;
            }
        }

        // ...and played one by one, as soon as the player is standing still again.
        if (queuedSlides != 0 && state == PlayerState.Grounded)
        {
            Slide(queuedSlides); // Slide only looks at the sign: > 0 = right, < 0 = left

            if (queuedSlides > 0)
            {
                queuedSlides -= 1;
            }
            else
            {
                queuedSlides += 1;
            }
        }

        if (stepUp1Action.WasPressedThisFrame())
        {
            RequestMove(MoveIntent.StepUp1);
        }

        if (stepUp2Action.WasPressedThisFrame())
        {
            RequestMove(MoveIntent.StepUp2);
        }

        if (hopAction.WasPressedThisFrame())
        {
            // Hop + a direction held = hop over the other player. Hop alone = hop in place.
            float direction = slideAction.ReadValue<float>();
            if (direction > 0)
            {
                HopOver(1);
            }
            else if (direction < 0)
            {
                HopOver(-1);
            }
            else
            {
                RequestMove(MoveIntent.HopInPlace);
            }
        }
    }

    public void OnBeat(int beat)
    {
    }

    // TODO: wait for the beat (BeatClock) and ask MoveRules before moving.
    public void RequestMove(MoveIntent intent)
    {
        switch (intent)
        {
            case MoveIntent.StepUp1:
                StepUp(1);
                break;
            case MoveIntent.StepUp2:
                StepUp(2);
                break;
            case MoveIntent.HopInPlace:
                HopInPlace();
                break;
        }
    }

    private void StepUp(int count)
    {
        if (state != PlayerState.Grounded)
        {
            return;
        }

        int landingStep = stepIndex + count;

        // No step to land on above the top of the stairs.
        if (landingStep > GameLoop.Instance.GetStairs().GetTargetStep())
        {
            Debug.Log("Step up refused: top of the stairs");
            return;
        }

        // The other player is standing in our column on the landing step.
        Player other = GameLoop.Instance.GetOtherPlayer(this);
        if (other.GetStepIndex() == landingStep && other.GetLane() == lane)
        {
            Debug.Log("Step up refused: the other player is in the way");
            return;
        }

        stepBeforeHop = stepIndex;
        stepIndex += count;
        t = 0f;
        state = PlayerState.Hopping; // UpdateVisuals animates the hop, then calls Land()
        Debug.Log($"Player {playerInput.playerIndex + 1} stepped up {count} -> step {stepIndex}");
    }

    private void HopInPlace()
    {
        origPos = flatPosition;
    }

    // End of a step-up hop: back on the ground, on the new step.
    private void Land()
    {
        state = PlayerState.Grounded;
        t = 0f;
        flatPosition.y = 0f;

        // The color rule lives in MoveRules: it freezes us if this step is not our color.
        GameLoop.Instance.GetMoveRules().CheckLanding(this);
    }

    public void TakeDamage()
    {
        switch (state)
        {
            case PlayerState.Grounded:
                state = PlayerState.Stunned;
                Freeze(stunDuration);
                break;
            case PlayerState.Sliding:
                // the slide finishes instantly (UpdateVisuals snaps to the new lane), then freeze
                Freeze(stunDuration);
                break;
            case PlayerState.Hopping:
                state = PlayerState.Grounded;
                Land(); // lands right away on the new step
                break;
            case PlayerState.Airborne:
                state = PlayerState.Grounded;
                //garder la lane en mémoire pour renvoyer le joueur à sa colonne initiale
                lane = laneBeforeHop;
                t = 0f;
                flatPosition = origPos; // back on the ground where the hop started
                break;
            case PlayerState.Stunned:
                // Already stunned, maybe do nothing or reset stun timer
                break;
        }
    }

    public void DuringStun()
    {
        // Handle stun duration and recovery logic here
        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0f)
        {
            state = PlayerState.Grounded;
            GetComponent<SpriteRenderer>().color = color; // back to normal color
        }
    }

    // Freeze the player: no input until the duration is over. Also used for penalties.
    public void Freeze(float duration)
    {
        state = PlayerState.Stunned;
        stunTimer = duration;
        t = 0f;
        queuedSlides = 0; // taps made before the freeze are forgotten
        GetComponent<SpriteRenderer>().color = Color.Lerp(color, Color.gray, 0.7f); // greyed out while frozen
    }

    private void Slide(float input)
    {
        origPos = flatPosition;

        if (input > 0 && lane < 3 && !IsLaneTaken(lane + 1))
        {
            state = PlayerState.Sliding;
            lane += 1;
            moveDirection = 1;
        }

        if (input < 0 && lane > -3 && !IsLaneTaken(lane - 1)) 
        {
            state = PlayerState.Sliding;
            lane += -1;
            moveDirection = -1;
        }

        targetPos = origPos;
        targetPos.x = lane * laneHalfWidth * 2 / nbOfLanes;
    } 

    // Jump 2 lanes in one go, over the other player standing right next to us.
    private void HopOver(int direction)
    {
        if (state != PlayerState.Grounded)
        {
            return;
        }

        Player other = GameLoop.Instance.GetOtherPlayer(this);
        bool otherOnMyStep = other.GetStepIndex() == stepIndex;
        int landingLane = lane + 2 * direction;

        // The other player is already moving that way: wait for them to finish.
        if (otherOnMyStep && other.IsMovingInDirection(direction))
        {
            Debug.Log("Hop refused: the other player is moving that way");
            return;
        }

        // Nobody next to us on that side: nothing to hop over, so just hop in place.
        if (!otherOnMyStep || other.GetLane() != lane + direction)
        {
            RequestMove(MoveIntent.HopInPlace);
            return;
        }

        // We would land outside the stairs.
        if (landingLane < -3 || landingLane > 3)
        {
            Debug.Log("Hop refused: no room on the other side");
            return;
        }

        origPos = flatPosition;
        laneBeforeHop = lane;
        lane = landingLane;
        targetPos = origPos;
        targetPos.x = lane * laneHalfWidth * 2 / nbOfLanes;
        moveDirection = direction;
        state = PlayerState.Airborne;
    }

    // A lane is taken when the other player is in it (or already sliding into it) on the same step.
    private bool IsLaneTaken(int targetLane)
    {
        Player other = GameLoop.Instance.GetOtherPlayer(this);
        return other.GetStepIndex() == stepIndex && other.GetLane() == targetLane;
    }

    public bool IsMovingInDirection(int direction)
    {
        bool isMoving = state == PlayerState.Sliding || state == PlayerState.Airborne;
        return isMoving && moveDirection == direction;
    }

    public bool IsBlockedBy(Player other)
    {
        return default;
    }

    public int GetStepIndex()
    {
        return stepIndex;
    }

    // Where the player is drawn: between two steps during a step-up hop, otherwise equal to GetStepIndex().
    public float GetDrawnStep()
    {
        return drawnStep;
    }

    public int GetLane()
    {
        return lane;
    }

    public Color GetColor()
    {
        return color;
    }

    public bool IsGrounded()
    {
        return state == PlayerState.Grounded;
    }

    public void Hit()
    {
    }

    private void UpdateVisuals()
    {
        Vector3 position = flatPosition;
        position.x = lane * laneHalfWidth * 2 / nbOfLanes ;
        if (state == PlayerState.Sliding)
        {
            position = Vector3.Lerp(origPos, targetPos, t) ;
            t += slideSpeed * Time.deltaTime;

            if(t > 1.0f)
            {

                t = 0f;
                state = PlayerState.Grounded ;
            }
        }

        if (state == PlayerState.Airborne)
        {
            position = Vector3.Lerp(origPos, targetPos, t);
            position.y += Mathf.Sin(t * Mathf.PI) * hopHeight; // goes up then down
            t += hopSpeed * Time.deltaTime;

            if (t > 1.0f)
            {
                t = 0f;
                position = targetPos;
                state = PlayerState.Grounded;
            }
        }

        if (state == PlayerState.Hopping)
        {
            drawnStep = Mathf.Lerp(stepBeforeHop, stepIndex, t);
            position.y = Mathf.Sin(t * Mathf.PI) * hopHeight; // goes up then down
            t += stepUpSpeed * Time.deltaTime;

            if (t > 1.0f)
            {
                position.y = 0f;
                Land();
            }
        }
        else
        {
            drawnStep = stepIndex;
        }

        flatPosition = position;

        // Perspective: draw the flat position on our step. The higher the step, the smaller and higher on screen.
        StairPerspective perspective = GameLoop.Instance.GetPerspective();
        float rowOffset = drawnStep - GameLoop.Instance.GetStairs().GetAnchorStep();
        float scale = perspective.GetScale(rowOffset);

        transform.localPosition = perspective.ToScreenPosition(rowOffset, flatPosition.x) + Vector3.up * flatPosition.y * scale;
        transform.localScale = baseScale * scale;
    }
}
