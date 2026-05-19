using UnityEngine;

public class DebtSystem : MonoBehaviour
{
    public static DebtSystem Instance { get; private set; }

    [SerializeField, Min(0)] private int initialDebt = 1100;

    private int currentDebt;
    private bool hasInitialized;

    public int InitialDebt => initialDebt;
    public int CurrentDebt => currentDebt;
    public bool HasDebt => currentDebt > 0;

    public delegate void OnDebtChangedDelegate(int currentDebt, int changeAmount);
    public static event OnDebtChangedDelegate OnDebtChangedEvent;

    public delegate void OnDebtPaidOffDelegate();
    public static event OnDebtPaidOffDelegate OnDebtPaidOffEvent;

    public static DebtSystem GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<DebtSystem>();

        if (Instance != null)
            return Instance;

        GameObject debtSystemObject = new GameObject("DebtSystem");
        Instance = debtSystemObject.AddComponent<DebtSystem>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeDebt();
    }

    private void OnEnable()
    {
        InitializeDebt();
        NotifyDebtChanged(0);
    }

    public int ReduceDebt(int amount)
    {
        InitializeDebt();

        amount = Mathf.Max(0, amount);

        if (amount == 0 || currentDebt <= 0)
            return 0;

        int reducedAmount = Mathf.Min(currentDebt, amount);
        currentDebt -= reducedAmount;

        NotifyDebtChanged(-reducedAmount);

        if (currentDebt <= 0)
            OnDebtPaidOffEvent?.Invoke();

        return reducedAmount;
    }

    public int IncreaseDebt(int amount)
    {
        InitializeDebt();

        amount = Mathf.Max(0, amount);

        if (amount == 0)
            return 0;

        currentDebt += amount;
        NotifyDebtChanged(amount);
        return amount;
    }

    public void SetDebt(int amount)
    {
        InitializeDebt();

        int previousDebt = currentDebt;
        currentDebt = Mathf.Max(0, amount);
        NotifyDebtChanged(currentDebt - previousDebt);

        if (currentDebt <= 0 && previousDebt > 0)
            OnDebtPaidOffEvent?.Invoke();
    }

    public void ResetDebt()
    {
        SetDebt(initialDebt);
    }

    private void InitializeDebt()
    {
        if (hasInitialized)
            return;

        currentDebt = Mathf.Max(0, initialDebt);
        hasInitialized = true;
    }

    private void NotifyDebtChanged(int changeAmount)
    {
        OnDebtChangedEvent?.Invoke(currentDebt, changeAmount);
    }
}
