using System.Collections.Generic;
using UnityEngine;

public class FishResultPanelUI : MonoBehaviour
{
    private List<FishScriptableObject> fishList = new List<FishScriptableObject>();
    [SerializeField] private GameObject newFishText;

    [Header("Star Image")]
    [SerializeField] private Transform starImage;
    [SerializeField] private float rotationSpeed;
    private float starAngle;

    [Header("Fish Variables")]
    [SerializeField] private MeshFilter mesh;
    [SerializeField] private Renderer objectRenderer;
    [SerializeField] private GameObject[] fishStarsRateObjects;

    private int fishRarity;
    private Mesh fishMesh;
    private Material fishMaterial;

    public void SetNewFish(FishData _fish)
    {

        fishMesh = _fish.typeOfFish.mesh;
        fishMaterial = _fish.typeOfFish.material;
        fishRarity = _fish.typeOfFish.rarity;

        mesh.mesh = fishMesh;
        objectRenderer.material = fishMaterial;

        ShowRarityStars(fishRarity);

        if (!fishList.Contains(_fish.typeOfFish))
        {

            fishList.Add(_fish.typeOfFish);
            newFishText.SetActive(true);

        }
        else
        {
            newFishText.SetActive(false);
        }
    }

    private void RotateStarImage()
    {
        if (starImage != null)
        {

            starAngle += rotationSpeed * Time.fixedDeltaTime;
            starAngle %= 360f;
            starImage.localRotation = Quaternion.Euler(0, 0, starAngle);

        }
    }

    private void ShowRarityStars(int _fishRarity)
    {

        for (int i = 0; i < fishRarity; i++)
        {

            fishStarsRateObjects[i].SetActive(true);

        }
    }

    private void FixedUpdate()
    {

        RotateStarImage();

    }
}
