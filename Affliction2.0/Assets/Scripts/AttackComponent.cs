using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ComboContext
{
    public bool GroundRequired;
    public bool WallRequired;
}

[System.Serializable]
public class AttackData
{
    public string attackName;
    public GameObject hitbox;
    public float damage;
    public float duration;
    public float comboWindow = 0.5f;
    public ComboContext context;

    [Header("Input direct (optionnel)")]
    public bool hasDirectInput = false;
    public List<int> directInputSequence; 
    public float inputWindow = 0.2f;
}

public class AttackComponent : MonoBehaviour
{
    public List<AttackData> attacks = new List<AttackData>();

    private Dictionary<string, AttackData> attackDict;

    public bool isAttacking = false;
    public bool inComboWindow = false;

    private List<int> inputBuffer = new List<int>();
    private float lastInputTime;

    private Player player;

    void Start()
    {
        player = GetComponent<Player>();

        attackDict = new Dictionary<string, AttackData>();
        foreach (var atk in attacks)
            attackDict[atk.attackName] = atk;
    }

    void Update()
    {
        if (inputBuffer.Count > 0 && !inComboWindow && Time.time - lastInputTime > GetMaxWindow())
            inputBuffer.Clear();
    }

    public void RegisterDirectInput(int inputType)
    {
        if (isAttacking && !inComboWindow) return;

        inputBuffer.Add(inputType);
        lastInputTime = Time.time;

        var combo = GetComponent<ComboComponent>();
        if (combo != null && combo.HasTransitionFor(inputType))
        {
            combo.RegisterInput(inputType);
            return;
        }

        TryDirectInput();
    }

    private void TryDirectInput()
    {
        AttackData bestMatch = null;

        foreach (var atk in attacks)
        {
            if (!atk.hasDirectInput) continue;
            if (!MatchesBuffer(atk.directInputSequence, atk.inputWindow)) continue;
            if (!MatchesContext(atk.context)) continue;

            if (bestMatch == null || atk.directInputSequence.Count > bestMatch.directInputSequence.Count)
                bestMatch = atk;
        }

        if (bestMatch != null)
        {
            inputBuffer.Clear();
            StartCoroutine(ExecuteAttack(bestMatch, () => { }));
        }
    }

    private bool MatchesBuffer(List<int> sequence, float window)
    {
        if (sequence == null || sequence.Count == 0) return false;
        if (sequence.Count > inputBuffer.Count) return false;

        int offset = inputBuffer.Count - sequence.Count;
        for (int i = 0; i < sequence.Count; i++)
            if (inputBuffer[offset + i] != sequence[i]) return false;

        return true;
    }

    public AttackData GetAttack(string name)
    {
        attackDict.TryGetValue(name, out var atk);
        return atk;
    }

    public bool MatchesContext(ComboContext ctx)
    {
        if (ctx.GroundRequired && !player.IsGrounded()) return false;
        if (ctx.WallRequired && !player.IsOnWall()) return false;
        return true;
    }

    private float GetMaxWindow()
    {
        float max = 0.2f;
        foreach (var atk in attacks)
            if (atk.hasDirectInput && atk.inputWindow > max) max = atk.inputWindow;
        return max;
    }

    public IEnumerator ExecuteAttack(AttackData atk, System.Action onComboWindowEnd)
    {
        isAttacking = true;
        inComboWindow = false;
        GetComponent<ComboComponent>()?.NotifyAttackStarted(atk.attackName);
        Debug.Log($"Executing: {atk.attackName}");

        if (atk.hitbox != null)
        {
            Vector3 pos = atk.hitbox.transform.localPosition;
            pos.x = Mathf.Abs(pos.x) * (player.right ? 1f : -1f);
            atk.hitbox.transform.localPosition = pos;

            atk.hitbox.SetActive(true);
            yield return new WaitForSeconds(atk.duration);
            atk.hitbox.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(atk.duration);
        }

        isAttacking = false;
        inComboWindow = true;
        yield return new WaitForSeconds(atk.comboWindow);
        inComboWindow = false;
        onComboWindowEnd?.Invoke();
    }
}