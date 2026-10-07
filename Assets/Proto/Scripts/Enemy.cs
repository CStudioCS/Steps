using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnTransform;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float minThrowDelay = 1f; // shortest wait between two throws (seconds)
    [SerializeField] private float maxThrowDelay = 3f; // longest wait between two throws (seconds)

    private float throwTimer; // seconds left before the next throw

    private void Start()
    {
        throwTimer = Random.Range(minThrowDelay, maxThrowDelay);
    }

    private void Update()
    {
        throwTimer -= Time.deltaTime;

        if (throwTimer <= 0f)
        {
            Player target = ChooseTarget();
            if (target != null)
            {
                Throw(target);
            }

            throwTimer = Random.Range(minThrowDelay, maxThrowDelay); // the next throw comes after a new random delay
        }
    }

    public void OnBeat(int beat)
    {
    }

    // One of the two players, at random.
    private Player ChooseTarget()
    {
        return GameLoop.Instance.GetPlayer(Random.Range(0, 2)); // Random.Range with ints never returns the max: 0 or 1
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
