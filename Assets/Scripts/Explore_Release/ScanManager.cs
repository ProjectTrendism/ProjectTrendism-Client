using UnityEngine;
using TMPro;

public class ScanManager : MonoBehaviour
{
    public Transform player;
    public float scanRadius = 3f;
    public float scanCooldown = 3f;

    public TextMeshProUGUI scanCooldownText;

    private float currentCooldown = 0f;

    void Update()
    {
        if (currentCooldown > 0f)
        {
            currentCooldown -= Time.deltaTime;
        }

        UpdateCooldownUI();

        if (Input.GetKeyDown(KeyCode.Q) && currentCooldown <= 0f)
        {
            PerformScan();
            currentCooldown = scanCooldown;
        }
    }

    void PerformScan()
    {
        HiddenInteractable[] hiddenObjects = FindObjectsOfType<HiddenInteractable>();

        for (int i = 0; i < hiddenObjects.Length; i++)
        {
            float distance = Vector2.Distance(player.position, hiddenObjects[i].transform.position);

            if (distance <= scanRadius)
            {
                hiddenObjects[i].Reveal();
            }
        }

        Debug.Log("스캔 사용");
    }

    void UpdateCooldownUI()
    {
        if (scanCooldownText == null) return;

        if (currentCooldown <= 0f)
        {
            scanCooldownText.text = "스캔 준비 완료";
        }
        else
        {
            scanCooldownText.text = "스캔 쿨타임: " + currentCooldown.ToString("F1") + "초";
        }
    }
    void OnDrawGizmosSelected()
    {
    if (player == null) return;

    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(player.position, scanRadius);
    }   


    
}