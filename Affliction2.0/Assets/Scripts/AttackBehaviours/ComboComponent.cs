using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ComboTransition
{
    public string fromAttack;
    public AttackButton button;
    public AttackDirection direction;
    public string toAttack;
}

public class ComboComponent : MonoBehaviour
{
    public List<ComboTransition> transitions = new List<ComboTransition>();

    private AttackComponent attackComponent;
    private Player player;

    private string currentAttack = ""; 
    private Coroutine currentExecution;
    void Start()
    {
        attackComponent = GetComponent<AttackComponent>();
        player = GetComponent<Player>();
    }
    public void NotifyAttackStarted(string attackName)
    {
        currentAttack = attackName;
    }
    public bool HasTransitionFor(AttackInput input)
    {
        foreach (var t in transitions)
            if (t.fromAttack == currentAttack && t.button == input.button && t.direction == input.direction)
                return true;
        return false;
    }

    public void RegisterInput(AttackInput input)
    {
        if (attackComponent.isAttacking && !attackComponent.inComboWindow) return;

        ComboTransition match = null;
        foreach (var t in transitions)
        {
            if (t.fromAttack == currentAttack && t.button == input.button && t.direction == input.direction)
            { match = t; break; }
        }

        if (match == null) return;

        AttackData atk = attackComponent.GetAttack(match.toAttack);
        if (atk == null) return;
        if (!attackComponent.MatchesContext(atk)) return;

        if (currentExecution != null) StopCoroutine(currentExecution);
        currentAttack = match.toAttack;
        currentExecution = StartCoroutine(attackComponent.ExecuteAttack(atk, () => { currentAttack = ""; }));
    }
}
