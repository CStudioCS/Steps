using UnityEngine;
using UnityEngine.UIElements;

public class Projectile : MonoBehaviour
{
    public enum ProjectileGoal
    {
        TargetLane,
        TargetStep,
        Speed
    }

    private void Update()
    {
    }

    public void Launch(Vector2 targetPosition, float speed) //à rajouter : effet scale
    {
        Vector2 position = transform.position;
        gameObject.GetComponent<Rigidbody2D>().linearVelocity = (targetPosition - position).normalized * speed;

    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Player>(out var player))
        {
            player.TakeDamage();
            GameObject.Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            GameObject.Destroy(gameObject);
        }
    }
    private bool CheckHit(Player player)
    {
        return player.IsGrounded();
    }
}
