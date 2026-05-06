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

        //Register("DownAir", OnDownAir);

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
}