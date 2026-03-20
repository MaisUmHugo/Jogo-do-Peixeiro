using UnityEngine;
using UnityEngine.InputSystem;

public class Camera_Livre : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidadeMovimento = 10f;
    public float velocidadeSubida = 8f;

    [Header("Mouse")]
    public float sensibilidadeMouse = 100f;
    private float rotacaoX = 0f;

    void Start()
    {
        // Trava e esconde o cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Movimento();
        RotacaoMouse();
    }

    void Movimento()
    {
        Vector2 movimento = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            movimento.y += 1;

        if (Keyboard.current.sKey.isPressed)
            movimento.y -= 1;

        if (Keyboard.current.aKey.isPressed)
            movimento.x -= 1;

        if (Keyboard.current.dKey.isPressed)
            movimento.x += 1;

        Vector3 direcao = new Vector3(movimento.x, 0, movimento.y);
        transform.Translate(direcao * velocidadeMovimento * Time.deltaTime, Space.Self);

        if (Keyboard.current.eKey.isPressed)
            transform.Translate(Vector3.up * velocidadeSubida * Time.deltaTime, Space.World);

        if (Keyboard.current.qKey.isPressed)
            transform.Translate(Vector3.down * velocidadeSubida * Time.deltaTime, Space.World);
    }

    void RotacaoMouse()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * sensibilidadeMouse * Time.deltaTime;

        // Rotação vertical (cima/baixo)
        rotacaoX -= mouseDelta.y;
        rotacaoX = Mathf.Clamp(rotacaoX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotacaoX, 0f, 0f);

        // Rotação horizontal (esquerda/direita)
        transform.parent.Rotate(Vector3.up * mouseDelta.x);
    }
}