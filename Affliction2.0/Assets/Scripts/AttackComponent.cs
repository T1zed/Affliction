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
public class ComboAttack
{
    public string name;
    public GameObject hitbox;
    public float damage;
    public float duration;
    public float comboWindow = 0.5f; 

    public List<int> inputSequence;
    public float inputWindow = 0.2f;
    public ComboContext context;
}

public class AttackComponent : MonoBehaviour
{
    public List<ComboAttack> combos = new List<ComboAttack>();

    private List<int> inputBuffer = new List<int>();
    private float lastInputTime;

    private bool isAttacking = false;
    private bool inComboWindow = false;
    private Coroutine comboWindowCoroutine;

    private Player player;

    void Start()
    {
        player = GetComponent<Player>();
    }

    void Update()
    {

        if (inputBuffer.Count > 0 && !inComboWindow && Time.time - lastInputTime > GetMaxWindow())
        {
            inputBuffer.Clear();
        }
    }

    public void RegisterInput(int inputType)
    {

        if (isAttacking && !inComboWindow) return;

        inputBuffer.Add(inputType);
        lastInputTime = Time.time;

        TryExecuteCombo();
    }

    private void TryExecuteCombo()
    {
        ComboAttack bestMatch = null;

        foreach (var combo in combos)
        {
            if (!MatchesBuffer(combo.inputSequence)) continue;
            if (!MatchesContext(combo.context)) continue;

            if (bestMatch == null || combo.inputSequence.Count > bestMatch.inputSequence.Count)
                bestMatch = combo;
        }

        if (bestMatch != null)
        {
            inputBuffer.Clear();

  
            if (comboWindowCoroutine != null)
                StopCoroutine(comboWindowCoroutine);

            StartCoroutine(ExecuteAttack(bestMatch));
        }
    }

    private bool MatchesBuffer(List<int> sequence)
    {
        if (sequence.Count > inputBuffer.Count) return false;

        int offset = inputBuffer.Count - sequence.Count;
        for (int i = 0; i < sequence.Count; i++)
        {
            if (inputBuffer[offset + i] != sequence[i]) return false;
        }
        return true;
    }

    private bool MatchesContext(ComboContext ctx)
    {
        if (ctx.GroundRequired && !player.IsGrounded()) return false;
        if (ctx.WallRequired && !player.IsOnWall()) return false;
        return true;
    }

    private float GetMaxWindow()
    {
        float max = 0.2f;
        foreach (var combo in combos)
            if (combo.inputWindow > max) max = combo.inputWindow;
        return max;
    }

    private IEnumerator ExecuteAttack(ComboAttack combo)
    {
        isAttacking = true;
        inComboWindow = false;
        Debug.Log($"Executing: {combo.name}");

        if (combo.hitbox != null)
        {
            Vector3 pos = combo.hitbox.transform.localPosition;
            pos.x = Mathf.Abs(pos.x) * (player.right ? 1f : -1f);
            combo.hitbox.transform.localPosition = pos;

            combo.hitbox.SetActive(true);
            yield return new WaitForSeconds(combo.duration);
            combo.hitbox.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(combo.duration);
        }

        isAttacking = false;

      
        comboWindowCoroutine = StartCoroutine(ComboWindowCoroutine(combo.comboWindow));
    }

    private IEnumerator ComboWindowCoroutine(float window)
    {
        inComboWindow = true;
        Debug.Log($"Combo window ouverte ({window}s)");

        yield return new WaitForSeconds(window);

    
        inComboWindow = false;
        inputBuffer.Clear();
        Debug.Log("Retour idle");
    }
}