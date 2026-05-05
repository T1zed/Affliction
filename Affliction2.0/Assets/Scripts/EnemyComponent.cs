using UnityEngine;

public enum Estate
{
    IDLE,
    PATROL,
    SPOTTING,
    ALERTED,
    INFIGHT,
    BUMPED,
    STUN,
    FORM,
}

public class EnemyComponent : MonoBehaviour
{
    [Header("State")]
    public Estate currentState = Estate.PATROL;

    [Header("Spotting")]
    public GameObject spotradius;
    public float spottingDuration;
    [HideInInspector] public bool isSpotting;
    [HideInInspector] public bool spotted;
    private float spottingTimer;

    [Header("Life")]
    public int hp;
    public int armor;
    public int stuntrigger;
    public int stunduration;

    [Header("Form")]
    public bool hasform;
    public float bumpedTime;

    void Start()
    {

        if (spotradius != null)
        {
            SpotRadiusTrigger trigger = spotradius.GetComponent<SpotRadiusTrigger>();
            if (trigger != null)
                trigger.owner = this;
        }
    }

    void Update()
    {
        HandleSpotting();
    }

    void HandleSpotting()
    {
        if (spotted) return;

        if (isSpotting)
        {
            spottingTimer += Time.deltaTime;

            if (spottingTimer >= spottingDuration)
            {
                spotted = true;
                currentState = Estate.ALERTED;
                spottingTimer = 0f;
                Debug.Log("alerted");
            }
        }
        else
        {
         
            spottingTimer = 0f;

            if (currentState == Estate.SPOTTING)
                currentState = Estate.PATROL;
        }
    }

   
    public void OnPlayerEnterSpotRadius()
    {
        if (spotted) return;
        isSpotting = true;
        currentState = Estate.SPOTTING;
        Debug.Log("insight");
    }

    public void OnPlayerExitSpotRadius()
    {
        if (spotted) return;
        isSpotting = false;
        Debug.Log("outside");
    }
}