using TMPro;
using UnityEngine;

public class PlayerMoneyHud : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;

    private void OnEnable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent += UpdateMoneyText;
    }

    private void OnDisable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent -= UpdateMoneyText;        
    }

    private void UpdateMoneyText(float _money) 
    {
        moneyText.text = $"R$:{_money}";        
    }
}
