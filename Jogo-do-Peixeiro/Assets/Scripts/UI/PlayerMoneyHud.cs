using TMPro;
using UnityEngine;

public class PlayerMoneyHud : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;

    private void OnEnable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent += UpdateMoneyText;

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (playerMoneyManager != null)
            UpdateMoneyText(playerMoneyManager.PlayerMoney);
    }

    private void OnDisable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent -= UpdateMoneyText;        
    }

    private void UpdateMoneyText(float _money) 
    {
        if (moneyText != null)
            moneyText.text = $"R$: {_money:0}";        
    }
}
