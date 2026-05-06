using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomAttackFile : MonoBehaviour
{
    private AttackComponent attackComponent;
    private Player player;

    void Start()
    {
        attackComponent = GetComponent<AttackComponent>();
        player = GetComponent<Player>();

        Register("Neutral_3", OnNeutral_3);
        Register("Neutral_4", OnNeutral_4);
        Register("Neutral_5", OnNeutral_5);

    }


    private Dictionary<string, System.Action<AttackData>> behaviours = new Dictionary<string, System.Action<AttackData>>();

    public void Register(string attackName, System.Action<AttackData> behaviour)
    {
        behaviours[attackName] = behaviour;
    }

    public void Execute(string attackName, AttackData atk)
    {
        if (behaviours.TryGetValue(attackName, out var b))
            b?.Invoke(atk);
    }

    //custom

    private void OnNeutral_3(AttackData atk)
    {
        StartCoroutine(TripleHit(atk));
    }

    private IEnumerator TripleHit(AttackData atk)
    {
        for (int i = 0; i < 3; i++)
        {
      
            GameObject hit = Instantiate(
                atk.hitbox,
                atk.hitbox.transform.position,
                atk.hitbox.transform.rotation,
                atk.hitbox.transform.parent
            );

        
            Vector3 pos = hit.transform.localPosition;
            pos.x = Mathf.Abs(pos.x) * (player.right ? 1f : -1f);
            hit.transform.localPosition = pos;

            hit.SetActive(true);
            yield return new WaitForSeconds(0.2f); 
            hit.SetActive(false);
            Destroy(hit);

            yield return new WaitForSeconds(0.05f); 
        }
    }

    private void OnNeutral_4(AttackData atk)
    {
        StartCoroutine(Dash(2f,0.15f, opposite: true));

    }
    private IEnumerator Dash(float length,float duration, bool opposite = false)
    {
        var rb = GetComponent<Rigidbody2D>();
        player.isAttackDashing = true;

        float direction = player.right ? 1f : -1f;
        if (opposite) direction *= -1f;

        float speed = length / duration;
        float timer = 0f;

        while (timer < duration)
        {
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        player.isAttackDashing = false; 
    }

    private void OnNeutral_5(AttackData atk)
    {
        StartCoroutine(Dash(3f,0.1f, opposite: false));
    }
}