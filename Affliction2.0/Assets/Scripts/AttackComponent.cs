using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AttackButton { Primary = 0, Secondary = 1 }
public enum AttackDirection { Neutral = 0, Side = 1, Down = 2 }

public struct AttackInput
{
    public AttackButton button;
    public AttackDirection direction;
}

[System.Serializable]
public class AttackData
{
    public string attackName;
    public GameObject hitbox;
    public float damage;
    public float duration;
    public float comboWindow = 0.5f;
    public float hitboxLatency = 0f;

    [Header("Déclenchement")]
    public AttackButton button;
    public AttackDirection direction;

    [Header("Contexte")]
    public bool groundOnly = false;
    public bool airOnly = false;
}

public class AttackComponent : MonoBehaviour
{
    public List<AttackData> attacks = new List<AttackData>();

    private Dictionary<string, AttackData> attackDict;
    private ComboComponent comboComponent;
    private Player player;

    public bool isAttacking = false;
    public bool inComboWindow = false;

    void Start()
    {
        player = GetComponent<Player>();
        comboComponent = GetComponent<ComboComponent>();

        attackDict = new Dictionary<string, AttackData>();
        foreach (var atk in attacks)
            attackDict[atk.attackName] = atk;
    }

    public void RegisterDirectInput(AttackButton button, AttackDirection direction)
    {
        if (isAttacking && !inComboWindow) return;

        var input = new AttackInput { button = button, direction = direction };

   
        if (comboComponent != null && comboComponent.HasTransitionFor(input))
        {
            comboComponent.RegisterInput(input);
            return;
        }

        AttackData best = null;
        foreach (var atk in attacks)
        {
            if (atk.button != button || atk.direction != direction) continue;
            if (!MatchesContext(atk)) continue;
            best = atk;
            break;
        }

        if (best != null)
            StartCoroutine(ExecuteAttack(best, () => { }));
    }

    public AttackData GetAttack(string name)
    {
        attackDict.TryGetValue(name, out var atk);
        return atk;
    }

    public bool MatchesContext(AttackData atk)
    {
        if (atk.groundOnly && !player.IsGrounded()) return false;
        if (atk.airOnly && player.IsGrounded()) return false;
        return true;
    }

    public IEnumerator ExecuteAttack(AttackData atk, System.Action onComboWindowEnd)
    {
        isAttacking = true;
        inComboWindow = false;
        comboComponent?.NotifyAttackStarted(atk.attackName);

        player.SetMovementLocked(true);

        Debug.Log($"Executing: {atk.attackName}");

        if (atk.hitbox != null)
        {
            Vector3 pos = atk.hitbox.transform.localPosition;
            pos.x = Mathf.Abs(pos.x) * (player.right ? 1f : -1f);
            atk.hitbox.transform.localPosition = pos;

            if (atk.hitboxLatency > 0f)
                yield return new WaitForSeconds(atk.hitboxLatency);

            atk.hitbox.SetActive(true);
            yield return new WaitForSeconds(atk.duration);
            atk.hitbox.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(atk.duration);
        }

        player.SetMovementLocked(false);

        isAttacking = false;
        inComboWindow = true;
        yield return new WaitForSeconds(atk.comboWindow);
        inComboWindow = false;
        onComboWindowEnd?.Invoke();
    }
}