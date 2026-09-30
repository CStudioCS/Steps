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
            RequestMove(MoveIntent.HopInPlace);
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

        if (input > 0 && lane < 3)
        {
            state = PlayerState.Sliding;
            lane += 1;
        }

        if (input < 0 && lane > -3) 
        {
            state = PlayerState.Sliding;
            lane += -1;
        }

        targetPos = origPos;
        targetPos.x = lane * laneHalfWidth * 2 / nbOfLanes;
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
        transform.localPosition = position;
    }
}
