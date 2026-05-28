using UnityEngine;

public class PlayerMoneyManager : MonoBehaviour
{
    [SerializeField] private float initialMoney;

    private float playerMoney;
    public float PlayerMoney => playerMoney;

    public delegate void OnMoneyChangeDelegate(float playerMoney);
    public static event OnMoneyChangeDelegate OnMoneyChangeEvent;

    private void Awake()
    {
        playerMoney = Mathf.Max(0f, initialMoney);
    }

    private void OnEnable()
    {
        NotifyMoneyChanged();
    }

    public bool TrySpendMoney(float _amount)
    {
        if (_amount <= 0 || playerMoney < _amount) return false;        

        playerMoney -= _amount;

        NotifyMoneyChanged();

        Debug.Log($"gastou {_amount} moedas e agora é: {playerMoney}");

        return true;
    }

    public void ReceiveMoney(float _amount)
    {
        if (_amount <= 0) return;

        playerMoney += _amount;
        NotifyMoneyChanged();

        Debug.Log($"recebeu {_amount} moedas e agora é: {playerMoney}");
    }

    public void ReciveMoney(float _amount)
    {
        ReceiveMoney(_amount);
    }

    public void SetMoney(float _amount)
    {
        playerMoney = Mathf.Max(0f, _amount);
        NotifyMoneyChanged();
    }

    public void NotifyMoneyChanged()
    {
        OnMoneyChangeEvent?.Invoke(playerMoney);
    }
}

