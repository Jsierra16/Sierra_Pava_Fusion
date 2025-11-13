using UnityEngine;
using TMPro;

public class HitManager : MonoBehaviour
{
    public static HitManager Instance;

    [Header("UI")]
    public TMP_Text hitsText;

    private int hits = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterHit()
    {
        hits++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (hitsText != null)
            hitsText.text = "Hits: " + hits;
    }
}
