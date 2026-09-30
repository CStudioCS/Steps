using UnityEngine;
using UnityEngine.InputSystem;

public class Enemy : MonoBehaviour
{
    private PlayerInput enemyInput;
    private InputAction throwAction;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnTransform;
    [SerializeField] private float speed = 5f;

    private void Start()
    {
        enemyInput = GetComponent<PlayerInput>();
        throwAction = enemyInput.actions["Throw"];
    }

    private void Update()
    {
        if (throwAction.WasPressedThisFrame())
        {
            Player target = FindAnyObjectByType<Player>();
            if (target != null)
            {
                Throw(target);
            }
        }
    }

    public void OnBeat(int beat)
    {
    }

    private Player ChooseTarget()
    {
        return default;
    }

    private void Throw(Player target)
    {
        Vector2 targetPosition = target.transform.position;
        GameObject projectile = GameObject.Instantiate(projectilePrefab.gameObject, projectileSpawnTransform.position, Quaternion.identity);
        projectile.GetComponent<Projectile>().Launch(targetPosition, speed); //mettre la vitesse de défilement du décor/rythme * 2);
    }

    private void UpdateVisuals()
    {
    }
}
