using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class ComboTransition
{
    public string fromAttack; 
    public int input;           
    public string toAttack;
    public float inputWindow = 0.2f;
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
    public bool HasTransitionFor(int inputType)
    {
        foreach (var t in transitions)
        {
            if (t.fromAttack == currentAttack && t.input == inputType)
                return true;
        }
        return false;
    }
    public void RegisterInput(int inputType)
    {
        if (attackComponent.isAttacking && !attackComponent.inComboWindow) return;

        ComboTransition match = null;
        foreach (var t in transitions)
        {
            if (t.fromAttack == currentAttack && t.input == inputType)
            {
                match = t;
                break;
            }
        }

        if (match == null) return;

        AttackData atk = attackComponent.GetAttack(match.toAttack);
        if (atk == null)
        {
            Debug.LogWarning($"Attaque '{match.toAttack}' introuvable dans AttackComponent !");
            return;
        }

        if (!attackComponent.MatchesContext(atk.context)) return;

        if (currentExecution != null)
            StopCoroutine(currentExecution);

        string nextAttack = match.toAttack;
        currentExecution = StartCoroutine(attackComponent.ExecuteAttack(atk, () => {currentAttack = "";Debug.Log("Retour idle");}));
        currentAttack = nextAttack;
    }
}
