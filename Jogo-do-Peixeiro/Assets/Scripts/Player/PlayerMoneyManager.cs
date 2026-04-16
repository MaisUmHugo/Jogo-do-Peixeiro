using UnityEngine;

public class PlayerMoneyManager : MonoBehaviour
{
    private float playerMoney;
    public float PlayerMoney => playerMoney;

    public delegate void OnMoneyChangeDelegate(float playerMoney);
    public static event OnMoneyChangeDelegate OnMoneyChangeEvent;

    public bool TrySpendMoney(float _amount)
    {
        if (_amount <= 0 || playerMoney < _amount) return false;        

        playerMoney -= _amount;

        OnMoneyChangeEvent(playerMoney);

        Debug.Log($"gastou {_amount} dinheiros e agora é: {playerMoney}");

        return true;
    }

    public void ReciveMoney(float _amount)
    {
        if (_amount <= 0) return;

        playerMoney += _amount;
        OnMoneyChangeEvent(playerMoney);

        Debug.Log($"recebeu {_amount} dinheiros e agora é: {playerMoney}");


    }
}
