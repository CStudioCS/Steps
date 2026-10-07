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
    [SerializeField] private float laneHalfWidth = 3f; // temporary until StairPerspective places the player
    [SerializeField] private float slideSpeed = 10f; // speed to move from one lane to the next
    [SerializeField] private float hopSpeed = 4f;    // speed of the hop over the other player
    [SerializeField] private float hopHeight = 0.6f; // how high the hop arc goes

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

    private PlayerState state = PlayerState.Grounded;
    private int stepIndex;
    private int lane;

    private void Awake()
    {
    }

    // Actions are cached in Start: PlayerInput gives each player its own copy of the actions during its own setup.
    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();
        slideAction = playerInput.actions["Slide"];
        stepUp1Action = playerInput.actions["StepUp1"];
        stepUp2Action = playerInput.actions["StepUp2"];
        hopAction = playerInput.actions["Hop"];
    }

    public void Setup(Color playerColor, int startLane)
    {
        color = playerColor;
        lane = startLane;
        GetComponent<SpriteRenderer>().color = playerColor;
    }

    private void Update()
    {
        ReadInput();
        UpdateVisuals();
    }

    private void ReadInput()
    {
        if (playerInput == null)
        {
            return;
        }

        if (slideAction.WasPressedThisFrame() && state != PlayerState.Sliding)
        {
            Slide(slideAction.ReadValue<float>());
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
        stepIndex += count;
        Debug.Log($"Player {playerInput.playerIndex + 1} stepped up {count} -> step {stepIndex}");
    }

    private void HopInPlace()
    {
        origPos = transform.localPosition;
    }

    private void Land()
    {
    }

    public void TakeDamage()
    {
        switch (state)
        {
            case PlayerState.Grounded:
                state = PlayerState.Stunned;
                break;
            case PlayerState.Hopping:
                state = PlayerState.Grounded;
                break;
            case PlayerState.Airborne:
                state = PlayerState.Grounded;
                //garder la lane en mémoire pour renvoyer le joueur à sa colonne initiale
                break;
            case PlayerState.Stunned:
                // Already stunned, maybe do nothing or reset stun timer
                break;
        }
    }

    public void DuringStun()
    {
        // Handle stun duration and recovery logic here
    }

    private void Slide(float input)
    {
        origPos = transform.localPosition;

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

        origPos = transform.localPosition;
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

    public float GetLane()
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
        Vector3 position = transform.localPosition;
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

        transform.localPosition = position;
    }
}
