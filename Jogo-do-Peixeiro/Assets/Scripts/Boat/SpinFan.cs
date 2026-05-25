using UnityEngine;

public class SpinFan : MonoBehaviour
{
   
    enum Angle { X, Y, Z }
    [SerializeField] private Angle rotationAxis = Angle.Z;
    public float rotationSpeed = 10f;


    void Update()
    {
        transform.RotateAround(transform.position, GetRotationAxis (rotationAxis), rotationSpeed * Time.deltaTime);
    }

    Vector3 GetRotationAxis(Angle angle)
    {
        switch (rotationAxis)
        {
            case Angle.X:
                return Vector3.right;
            case Angle.Y: 
                return Vector3.up; 
            case Angle.Z: 
                return Vector3.forward;
        }
        return Vector3.zero;
    }
}
