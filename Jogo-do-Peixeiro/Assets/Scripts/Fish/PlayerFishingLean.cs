using UnityEngine;

/// <summary>
/// Inclina o corpo do player na direção oposta à do peixe durante a pesca.
/// Coloque esse script no mesmo GameObject do PlayerMove (ou no root do player).
/// Arraste o campo "leanPivot" para um filho vazio entre o root e o modelo 3D:
///   Player (root)
///     └─ LeanPivot  ← arraste aqui
///          └─ Model (malha do personagem)
/// Assim o lean não interfere na rotação de movimento do PlayerMove.
/// </summary>
public class PlayerFishingLean : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishDirectionPull _directionPull;

    [Tooltip("Filho vazio que contém o modelo 3D do player. O lean é aplicado nele.")]
    [SerializeField] private Transform _leanPivot;

    [Header("Lean Settings")]
    [SerializeField] private float _maxLeanAngle = 18f;
    [SerializeField] private float _leanSpeed = 6f;
    [SerializeField] private float _returnSpeed = 4f;

    [Header("Axis Weights")]
    [Tooltip("Quanto o lean lateral (esquerda/direita) rotaciona no eixo Z")]
    [SerializeField] private float _lateralZWeight = 1f;
    [Tooltip("Quanto o lean para trás (puxando o peixe) rotaciona no eixo X")]
    [SerializeField] private float _backwardXWeight = 0.6f;

    private Quaternion _neutralRotation;
    private Quaternion _targetRotation;

    private void Awake()
    {
        if (_directionPull == null)
            _directionPull = FindFirstObjectByType<FishDirectionPull>(FindObjectsInactive.Include);

        if (_leanPivot == null)
            _leanPivot = transform;

        _neutralRotation = _leanPivot.localRotation;
        _targetRotation = _neutralRotation;
    }

    private void LateUpdate()
    {
        // LateUpdate garante que o lean é aplicado depois da rotação do PlayerMove
        UpdateTargetLean();

        float speed = _directionPull != null && _directionPull.IsPullActive ? _leanSpeed : _returnSpeed;

        _leanPivot.localRotation = Quaternion.Slerp(
            _leanPivot.localRotation,
            _targetRotation,
            speed * Time.deltaTime
        );
    }

    private void UpdateTargetLean()
    {
        // Sem pull ativo → volta ao neutro
        if (_directionPull == null || !_directionPull.IsPullActive)
        {
            _targetRotation = _neutralRotation;
            return;
        }

        // RequiredPullDirection = direção que o jogador DEVE puxar (oposta ao peixe)
        // Ex: peixe vai pra esquerda → jogador puxa pra direita → lean pra direita
        FishDirectionPull.FishForceDirection pullDir = _directionPull.RequiredPullDirection;

        float angleZ = 0f;
        float angleX = 0f;

        switch (pullDir)
        {
            case FishDirectionPull.FishForceDirection.Left:
                angleZ = _maxLeanAngle * _lateralZWeight;
                break;

            case FishDirectionPull.FishForceDirection.Right:
                angleZ = -_maxLeanAngle * _lateralZWeight;
                break;

            case FishDirectionPull.FishForceDirection.Up:
                // Puxando pra cima = recua o tronco
                angleX = -_maxLeanAngle * _backwardXWeight;
                break;

            case FishDirectionPull.FishForceDirection.Down:
                // Puxando pra baixo = inclina pra frente
                angleX = _maxLeanAngle * _backwardXWeight;
                break;
        }

        // Intensidade do lean escala com o quanto o jogador está realmente puxando
        float inputStrength = _directionPull.PullInputNormalized;
        float leanMultiplier = Mathf.Lerp(0.3f, 1f, inputStrength);

        _targetRotation = _neutralRotation * Quaternion.Euler(
            angleX * leanMultiplier,
            0f,
            angleZ * leanMultiplier
        );
    }

    // Chamado externamente se precisar resetar o lean imediatamente (ex: saiu do barco)
    public void ResetLeanImmediate()
    {
        _targetRotation = _neutralRotation;
        _leanPivot.localRotation = _neutralRotation;
    }
}
