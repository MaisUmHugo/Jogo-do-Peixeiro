using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoteiroDialogLibrary", menuName = "Jogo do Peixeiro/Roteiro Dialog Library")]
public class RoteiroDialogLibrary : ScriptableObject
{
    [Header("Cutscenes")]
    [SerializeField] private DialogSequenceAsset introMarinaLoja;
    [SerializeField] private DialogSequenceAsset introCobradorCabana;
    [SerializeField] private DialogSequenceAsset fimCampanhaLoja;
    [SerializeField] private DialogSequenceAsset fimCampanhaAirFishers;

    [Header("Regular")]
    [SerializeField] private DialogSequenceAsset donoDocaPrimeiroEncontro;
    [SerializeField] private DialogSequenceAsset donoDocaVulcao;
    [SerializeField] private DialogSequenceAsset[] cobradorExtras;

    [Header("Edge Cases")]
    [SerializeField] private DialogSequenceAsset edgeDonoDocaAntesIntro;
    [SerializeField] private DialogSequenceAsset edgeBarcoAntesIntro;

    public DialogSequenceAsset IntroMarinaLoja => introMarinaLoja;
    public DialogSequenceAsset IntroCobradorCabana => introCobradorCabana;
    public DialogSequenceAsset FimCampanhaLoja => fimCampanhaLoja;
    public DialogSequenceAsset FimCampanhaAirFishers => fimCampanhaAirFishers;
    public DialogSequenceAsset DonoDocaPrimeiroEncontro => donoDocaPrimeiroEncontro;
    public DialogSequenceAsset DonoDocaVulcao => donoDocaVulcao;
    public DialogSequenceAsset[] CobradorExtras => cobradorExtras;
    public DialogSequenceAsset EdgeDonoDocaAntesIntro => edgeDonoDocaAntesIntro;
    public DialogSequenceAsset EdgeBarcoAntesIntro => edgeBarcoAntesIntro;
}

public static class RoteiroDialogPlayback
{
    private const string LibraryResourcePath = "RoteiroDialogLibrary";

    public static RoteiroDialogLibrary LoadLibrary()
    {
        return Resources.Load<RoteiroDialogLibrary>(LibraryResourcePath);
    }

    public static bool TryPlayFromLibrary(
        RoteiroDialogLibrary _library,
        Func<RoteiroDialogLibrary, DialogSequenceAsset[]> _selectDialogs,
        Action _onFinished)
    {
        if (_selectDialogs == null)
            return false;

        if (_library == null)
            _library = LoadLibrary();

        if (_library == null)
            return false;

        return TryPlaySequence(_selectDialogs.Invoke(_library), _onFinished);
    }

    public static bool TryPlaySequence(DialogSequenceAsset[] _dialogs, Action _onFinished)
    {
        if (_dialogs == null || _dialogs.Length == 0)
            return false;

        List<DialogSequenceAsset> validDialogs = new List<DialogSequenceAsset>();

        for (int i = 0; i < _dialogs.Length; i++)
        {
            if (_dialogs[i] != null && _dialogs[i].HasLines)
                validDialogs.Add(_dialogs[i]);
        }

        if (validDialogs.Count == 0)
            return false;

        DialogSequencePlayer player = DialogSequencePlayer.GetOrCreate();

        if (player == null)
            return false;

        PlaySequenceAt(player, validDialogs, 0, _onFinished);
        return true;
    }

    private static void PlaySequenceAt(
        DialogSequencePlayer _player,
        IReadOnlyList<DialogSequenceAsset> _dialogs,
        int _index,
        Action _onFinished)
    {
        if (_player == null || _dialogs == null || _index >= _dialogs.Count)
        {
            _onFinished?.Invoke();
            return;
        }

        _player.Play(_dialogs[_index], null, () => PlaySequenceAt(_player, _dialogs, _index + 1, _onFinished));
    }
}
