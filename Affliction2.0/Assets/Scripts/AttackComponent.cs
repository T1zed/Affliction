using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

[System.Serializable]
public class ComboAttack
{
    public string name;
    public GameObject hitbox;
    public float damage;
    public float duration;

    public List<int> inputSequence;
    public float inputWindow = 0.2f; 
}

public class AttackComponent : MonoBehaviour
{
    public List<ComboAttack> combos = new List<ComboAttack>();

    private List<int> inputBuffer = new List<int>();
    private float lastInputTime;
    private bool isAttacking = false;

    private Player player;

    void Start()
    {
        player = GetComponent<Player>();
        
    }

    void Update()
    {
        if (inputBuffer.Count > 0 && Time.time - lastInputTime > GetMaxWindow())
        {
            inputBuffer.Clear();
        }
    }

    public void RegisterInput(int inputType)
    {
        if (isAttacking) return;

        inputBuffer.Add(inputType);
        lastInputTime = Time.time;

        TryExecuteCombo();
    }

    private void TryExecuteCombo()
    {

        ComboAttack bestMatch = null;

        foreach (var combo in combos)
        {
            if (MatchesBuffer(combo.inputSequence))
            {
                if (bestMatch == null || combo.inputSequence.Count > bestMatch.inputSequence.Count)
                    bestMatch = combo;
            }
        }

        if (bestMatch != null)
        {
            inputBuffer.Clear();
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
        Debug.Log($"Executing combo: {combo.name}");

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
    }
}