using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoteiroPrototypeSceneSetup : MonoBehaviour
{
    private const string LibraryResourcePath = "RoteiroDialogLibrary";

    [Header("Library")]
    [SerializeField] private RoteiroDialogLibrary library;

    [Header("Auto Setup")]
    [SerializeField] private bool createDialogPlayer = true;
    [SerializeField] private bool configureMoneyLenderExtras = true;
    [SerializeField] private bool configureDockOwnerDialog = true;

    [Header("Prototype Playback")]
    [SerializeField] private bool playIntroOnStart;
    [SerializeField] private bool playIntroPartTwoAfterPartOne = true;
    [SerializeField] private float introStartDelay = 0.25f;

    private DialogSequencePlayer dialogPlayer;

    private IEnumerator Start()
    {
        ResolveLibrary();
        EnsureDialogPlayer();
        ConfigureSceneDialogs();

        if (!playIntroOnStart || library == null || dialogPlayer == null)
            yield break;

        if (introStartDelay > 0f)
            yield return new WaitForSecondsRealtime(introStartDelay);

        PlayIntroPrototype();
    }

    [ContextMenu("Configure Scene Dialogs")]
    public void ConfigureSceneDialogs()
    {
        ResolveLibrary();
        EnsureDialogPlayer();

        if (library == null || dialogPlayer == null)
            return;

        if (configureMoneyLenderExtras)
            ConfigureMoneyLender();

        if (configureDockOwnerDialog)
            ConfigureDockOwner();
    }

    [ContextMenu("Play Intro Prototype")]
    public void PlayIntroPrototype()
    {
        if (library == null || dialogPlayer == null || library.IntroMarinaLoja == null)
            return;

        dialogPlayer.Play(library.IntroMarinaLoja, null, () =>
        {
            if (playIntroPartTwoAfterPartOne && library.IntroCobradorCabana != null)
                dialogPlayer.Play(library.IntroCobradorCabana);
        });
    }

    [ContextMenu("Play Campaign Ending Prototype")]
    public void PlayCampaignEndingPrototype()
    {
        if (library == null || dialogPlayer == null || library.FimCampanhaLoja == null)
            return;

        dialogPlayer.Play(library.FimCampanhaLoja, null, () =>
        {
            if (library.FimCampanhaAirFishers != null)
                dialogPlayer.Play(library.FimCampanhaAirFishers);
        });
    }

    private void ConfigureMoneyLender()
    {
        MoneyLenderController[] moneyLenders = FindObjectsByType<MoneyLenderController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < moneyLenders.Length; i++)
        {
            DialogCameraFocusTarget focusTarget = moneyLenders[i].GetComponentInChildren<DialogCameraFocusTarget>(true);
            moneyLenders[i].ConfigureOptionalDialogs(dialogPlayer, library.CobradorExtras, focusTarget);
        }
    }

    private void ConfigureDockOwner()
    {
        FishMarketController[] markets = FindObjectsByType<FishMarketController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        DialogSequenceAsset firstDialog = SceneManager.GetActiveScene().name.Contains("Lava")
            ? library.DonoDocaVulcao
            : library.DonoDocaPrimeiroEncontro;

        for (int i = 0; i < markets.Length; i++)
        {
            DialogCameraFocusTarget focusTarget = markets[i].GetComponentInChildren<DialogCameraFocusTarget>(true);
            markets[i].ConfigureOptionalDialogs(dialogPlayer, firstDialog, null, focusTarget);
        }
    }

    private void ResolveLibrary()
    {
        if (library == null)
            library = Resources.Load<RoteiroDialogLibrary>(LibraryResourcePath);
    }

    private void EnsureDialogPlayer()
    {
        if (dialogPlayer != null)
            return;

        dialogPlayer = FindFirstObjectByType<DialogSequencePlayer>(FindObjectsInactive.Include);

        if (dialogPlayer == null && createDialogPlayer)
            dialogPlayer = gameObject.AddComponent<DialogSequencePlayer>();
    }
}
