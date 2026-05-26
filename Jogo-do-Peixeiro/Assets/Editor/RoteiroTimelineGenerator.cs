using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public static class RoteiroTimelineGenerator
{
    private const string TimelineFolder = "Assets/Data/Timelines/Roteiro";
    private const string SignalFolder = "Assets/Data/Timelines/Roteiro/Signals";

    private const string OldIntroTimelinePath = TimelineFolder + "/Cutscene_Inicial_Roteiro.playable";
    private const string OpeningTimelinePath = TimelineFolder + "/Cutscene_Inicial_Loja_Roteiro.playable";
    private const string MoneyLenderIntroTimelinePath = TimelineFolder + "/Cutscene_Cobrador_Roteiro.playable";
    private const string FinalTimelinePath = TimelineFolder + "/Cutscene_Final_Roteiro.playable";
    private const string CutsceneControllerObjectName = "Campaign Cutscene Controller";

    [MenuItem("Jogo do Peixeiro/Roteiro/Gerar Timelines do roteiro")]
    public static void GenerateRoteiroTimelines()
    {
        EnsureFolders();
        AssetDatabase.DeleteAsset(OldIntroTimelinePath);
        DestroySceneObject("Timeline - Cutscene Inicial Roteiro");

        CutsceneDefinition[] definitions = CreateDefinitions();

        foreach (CutsceneDefinition definition in definitions)
            GenerateTimeline(definition);

        foreach (CutsceneDefinition definition in definitions)
            SetupSceneObject(definition);

        SetupCampaignCutsceneController(definitions[0], definitions[1], definitions[2]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Timelines do roteiro geradas. Abra Window > Sequencing > Timeline e selecione Timeline - Cutscene Inicial Loja Roteiro, Timeline - Cutscene Cobrador Roteiro ou Timeline - Cutscene Final Roteiro.");
    }

    [MenuItem("Jogo do Peixeiro/Roteiro/Atualizar somente sinais de fade e fluxo")]
    public static void UpdateRoteiroTimelineFlowSignals()
    {
        EnsureFolders();

        CutsceneDefinition[] definitions = CreateDefinitions();

        foreach (CutsceneDefinition definition in definitions)
        {
            UpdateFlowSignalTrack(definition);
            RefreshSceneObjectBindings(definition);
        }

        SetupCampaignCutsceneController(definitions[0], definitions[1], definitions[2]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Sinais de fade e fluxo atualizados sem recriar as Timelines. As tracks de camera e clips editados foram preservados.");
    }

    private static CutsceneDefinition[] CreateDefinitions()
    {
        CutsceneDefinition opening = new CutsceneDefinition(
            "Cutscene Inicial Loja Roteiro",
            "Timeline - Cutscene Inicial Loja Roteiro",
            OpeningTimelinePath,
            8,
            true,
            new[]
            {
                new DialogBeat("intro_loja", "01 - Marina na loja", "Assets/Data/Dialogues/Roteiro/Cutscenes/01_Intro_Marina_Loja.asset", 1.0),
            },
            new[]
            {
                new NoteBeat(0.0, "FADE", "Tela escura clareia.", "Use SetFadeBlackImmediate no inicio e FadeIn logo depois."),
                new NoteBeat(0.5, "CAMERA", "Marina em frente a loja.", "Use uma Virtual Camera apontando para a loja/Marina."),
            });

        CutsceneDefinition moneyLenderIntro = new CutsceneDefinition(
            "Cutscene Cobrador Roteiro",
            "Timeline - Cutscene Cobrador Roteiro",
            MoneyLenderIntroTimelinePath,
            16,
            true,
            new[]
            {
                new DialogBeat("intro_cobrador", "02 - Cobrador na cabana", "Assets/Data/Dialogues/Roteiro/Cutscenes/02_Intro_Cobrador_Cabana.asset", 0.5),
            },
            new[]
            {
                new NoteBeat(0.0, "CAMERA", "Marina em frente a cabana do cobrador.", "Use uma Virtual Camera da cabana."),
                new NoteBeat(12.0, "CAMERA", "Camera vira para a doca.", "Adicione um Cinemachine Shot olhando para a doca antes do objetivo tutorial."),
            });

        CutsceneDefinition finale = new CutsceneDefinition(
            "Cutscene Final Roteiro",
            "Timeline - Cutscene Final Roteiro",
            FinalTimelinePath,
            18,
            true,
            new[]
            {
                new DialogBeat("fim_loja", "03 - Fim campanha loja", "Assets/Data/Dialogues/Roteiro/Cutscenes/03_Fim_Campanha_Loja.asset", 1.0),
                new DialogBeat("fim_airfishers", "04 - Fim campanha Air Fishers", "Assets/Data/Dialogues/Roteiro/Cutscenes/04_Fim_Campanha_AirFishers.asset", 7.0),
            },
            new[]
            {
                new NoteBeat(0.0, "CAMERA", "Marina em frente a loja depois da ultima entrega.", "Comece com uma Virtual Camera calma na frente da loja."),
                new NoteBeat(4.8, "FADE", "Camera sobe para o por do sol. Fade escuro. Palavra Fim.", "Esse beat fica entre as duas partes do dialogo final."),
                new NoteBeat(6.5, "CAMERA", "Cena volta em frente a cabana do cobrador.", "Troque para uma Virtual Camera da cabana."),
                new NoteBeat(14.5, "FIM", "Camera sobe e fade escuro. Entra modo sem fim.", "Os sinais finais liberam o modo sem fim e salvam o jogo."),
            });

        return new[] { opening, moneyLenderIntro, finale };
    }

    private static void GenerateTimeline(CutsceneDefinition definition)
    {
        AssetDatabase.DeleteAsset(definition.TimelinePath);

        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.name = definition.TimelineName;
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = definition.Duration;
        AssetDatabase.CreateAsset(timeline, definition.TimelinePath);

        timeline.CreateMarkerTrack();
        timeline.markerTrack.name = "Roteiro - notas editaveis";
        CreateNoteMarkers(timeline.markerTrack, definition);

        SignalTrack dialogSignalTrack = timeline.CreateTrack<SignalTrack>(null, "Sinais - dialogos");
        SignalTrack flowSignalTrack = timeline.CreateTrack<SignalTrack>(null, "Sinais - fade e fluxo");

        foreach (DialogBeat beat in definition.DialogBeats)
        {
            SignalAsset signal = CreateSignal(definition, beat.CueId);
            SignalEmitter emitter = dialogSignalTrack.CreateMarker<SignalEmitter>(beat.Time);
            emitter.name = beat.Title;
            emitter.asset = signal;
            emitter.emitOnce = true;
        }

        CreateFlowSignals(definition, flowSignalTrack);
        EditorUtility.SetDirty(timeline);
    }

    private static void UpdateFlowSignalTrack(CutsceneDefinition definition)
    {
        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(definition.TimelinePath);

        if (timeline == null)
        {
            Debug.LogWarning($"Nao foi possivel atualizar sinais: Timeline nao encontrada em {definition.TimelinePath}.");
            return;
        }

        SignalTrack existingFlowTrack = timeline.GetOutputTracks()
            .OfType<SignalTrack>()
            .FirstOrDefault(track => track.name == "Sinais - fade e fluxo");

        if (existingFlowTrack != null)
            timeline.DeleteTrack(existingFlowTrack);

        SignalTrack flowSignalTrack = timeline.CreateTrack<SignalTrack>(null, "Sinais - fade e fluxo");
        CreateFlowSignals(definition, flowSignalTrack);
        EditorUtility.SetDirty(timeline);
    }

    private static void CreateNoteMarkers(MarkerTrack markerTrack, CutsceneDefinition definition)
    {
        foreach (NoteBeat note in definition.NoteBeats)
        {
            RoteiroTimelineNoteMarker marker = markerTrack.CreateMarker<RoteiroTimelineNoteMarker>(note.Time);
            marker.title = note.Title;
            marker.speaker = note.Speaker;
            marker.text = note.Text;
        }

        foreach (DialogBeat beat in definition.DialogBeats)
        {
            DialogSequenceAsset dialog = LoadDialog(beat.DialogPath);
            if (dialog == null)
                continue;

            DialogSequenceLineData[] lines = dialog.ToDialogSequenceData().GetLines();
            for (int i = 0; i < lines.Length; i++)
            {
                DialogSequenceLineData line = lines[i];
                if (line == null)
                    continue;

                RoteiroTimelineNoteMarker marker = markerTrack.CreateMarker<RoteiroTimelineNoteMarker>(beat.Time + 0.25 + i * 0.35);
                marker.cueId = beat.CueId;
                marker.title = $"{beat.Title} / fala {i + 1:00}";
                marker.speaker = line.SpeakerName;
                marker.text = line.Sentence;
                marker.dialogAsset = dialog;
            }
        }
    }

    private static void CreateFlowSignals(CutsceneDefinition definition, SignalTrack flowSignalTrack)
    {
        AddFlowSignal(definition, flowSignalTrack, "lock_gameplay", 0.0);
        AddFlowSignal(definition, flowSignalTrack, "begin_cutscene_camera", 0.0);

        if (definition.UseOpeningFade)
        {
            AddFlowSignal(definition, flowSignalTrack, "set_black", 0.0);
            AddFlowSignal(definition, flowSignalTrack, "fade_in", 0.05);
        }

        AddFlowSignal(definition, flowSignalTrack, "fade_out", Math.Max(0.0, definition.Duration - 1.4));

        if (definition.TimelinePath == FinalTimelinePath)
        {
            AddFlowSignal(definition, flowSignalTrack, "unlock_endless", definition.Duration - 1.0);
            AddFlowSignal(definition, flowSignalTrack, "save_game", definition.Duration - 0.8);
            AddFlowSignal(definition, flowSignalTrack, "end_cutscene_camera", definition.Duration - 0.35);
            AddFlowSignal(definition, flowSignalTrack, "unlock_gameplay", definition.Duration - 0.3);
            AddFlowSignal(definition, flowSignalTrack, "queue_endless_notice", definition.Duration - 0.2);
            AddFlowSignal(definition, flowSignalTrack, "load_main_menu", definition.Duration - 0.05);
            return;
        }

        AddFlowSignal(definition, flowSignalTrack, "end_cutscene_camera", definition.Duration - 0.35);
        AddFlowSignal(definition, flowSignalTrack, "unlock_gameplay", definition.Duration - 0.3);
        AddFlowSignal(definition, flowSignalTrack, "fade_in_return", definition.Duration - 0.25);
    }

    private static void AddFlowSignal(CutsceneDefinition definition, SignalTrack track, string id, double time)
    {
        SignalAsset signal = CreateSignal(definition, id);
        SignalEmitter emitter = track.CreateMarker<SignalEmitter>(time);
        emitter.name = id;
        emitter.asset = signal;
        emitter.emitOnce = true;
    }

    private static SignalAsset CreateSignal(CutsceneDefinition definition, string id)
    {
        string path = $"{SignalFolder}/{SanitizeFileName(definition.TimelineName)}_{SanitizeFileName(id)}.signal";
        SignalAsset existingSignal = AssetDatabase.LoadAssetAtPath<SignalAsset>(path);

        if (existingSignal != null)
            return existingSignal;

        SignalAsset signal = ScriptableObject.CreateInstance<SignalAsset>();
        signal.name = $"{definition.TimelineName}_{id}";
        AssetDatabase.CreateAsset(signal, path);
        return signal;
    }

    private static void SetupSceneObject(CutsceneDefinition definition)
    {
        GameObject existing = GameObject.Find(definition.SceneObjectName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);

        GameObject root = new GameObject(definition.SceneObjectName);
        PlayableDirector director = root.AddComponent<PlayableDirector>();
        SignalReceiver signalReceiver = root.AddComponent<SignalReceiver>();
        DialogSequencePlayer dialogPlayer = root.AddComponent<DialogSequencePlayer>();
        TimelineDialogEventReceiver eventReceiver = root.AddComponent<TimelineDialogEventReceiver>();

        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(definition.TimelinePath);
        director.playableAsset = timeline;
        director.playOnAwake = false;
        director.timeUpdateMode = DirectorUpdateMode.GameTime;

        ConfigureEventReceiver(eventReceiver, director, dialogPlayer, definition);
        ConfigureSignalReceiver(signalReceiver, eventReceiver, definition);

        foreach (SignalTrack signalTrack in timeline.GetOutputTracks().OfType<SignalTrack>())
            director.SetGenericBinding(signalTrack, signalReceiver);

        ConfigureTutorialCutsceneDirector(definition, director);
    }

    private static void RefreshSceneObjectBindings(CutsceneDefinition definition)
    {
        GameObject root = GameObject.Find(definition.SceneObjectName);

        if (root == null)
        {
            Debug.LogWarning($"Nao foi possivel atualizar bindings: objeto de cena '{definition.SceneObjectName}' nao encontrado.");
            return;
        }

        PlayableDirector director = root.GetComponent<PlayableDirector>();
        if (director == null)
            director = root.AddComponent<PlayableDirector>();

        SignalReceiver oldSignalReceiver = root.GetComponent<SignalReceiver>();
        if (oldSignalReceiver != null)
            UnityEngine.Object.DestroyImmediate(oldSignalReceiver);

        SignalReceiver signalReceiver = root.AddComponent<SignalReceiver>();

        DialogSequencePlayer dialogPlayer = root.GetComponent<DialogSequencePlayer>();
        if (dialogPlayer == null)
            dialogPlayer = root.AddComponent<DialogSequencePlayer>();

        TimelineDialogEventReceiver eventReceiver = root.GetComponent<TimelineDialogEventReceiver>();
        if (eventReceiver == null)
            eventReceiver = root.AddComponent<TimelineDialogEventReceiver>();

        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(definition.TimelinePath);
        if (timeline == null)
            return;

        director.playableAsset = timeline;
        director.playOnAwake = false;
        director.timeUpdateMode = DirectorUpdateMode.GameTime;

        ConfigureEventReceiver(eventReceiver, director, dialogPlayer, definition);
        ConfigureSignalReceiver(signalReceiver, eventReceiver, definition);

        foreach (SignalTrack signalTrack in timeline.GetOutputTracks().OfType<SignalTrack>())
            director.SetGenericBinding(signalTrack, signalReceiver);

        ConfigureTutorialCutsceneDirector(definition, director);
        EditorUtility.SetDirty(root);
    }

    private static void ConfigureTutorialCutsceneDirector(CutsceneDefinition definition, PlayableDirector director)
    {
        CampaignQuestGuidanceController tutorial = UnityEngine.Object.FindFirstObjectByType<CampaignQuestGuidanceController>(FindObjectsInactive.Include);

        if (tutorial == null)
            return;

        SerializedObject serialized = new SerializedObject(tutorial);
        serialized.FindProperty("cutsceneController").objectReferenceValue =
            UnityEngine.Object.FindFirstObjectByType<CampaignCutsceneController>(FindObjectsInactive.Include);

        if (definition.TimelinePath == OpeningTimelinePath)
            serialized.FindProperty("openingCutsceneDirector").objectReferenceValue = director;

        if (definition.TimelinePath == MoneyLenderIntroTimelinePath)
            serialized.FindProperty("moneyLenderIntroCutsceneDirector").objectReferenceValue = director;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tutorial);
    }

    private static void SetupCampaignCutsceneController(
        CutsceneDefinition opening,
        CutsceneDefinition moneyLenderIntro,
        CutsceneDefinition finale)
    {
        CampaignCutsceneController existingController =
            UnityEngine.Object.FindFirstObjectByType<CampaignCutsceneController>(FindObjectsInactive.Include);

        GameObject root = existingController != null
            ? existingController.gameObject
            : GameObject.Find(CutsceneControllerObjectName);

        if (root == null)
            root = new GameObject(CutsceneControllerObjectName);

        CampaignCutsceneController controller = existingController != null
            ? existingController
            : root.GetComponent<CampaignCutsceneController>();

        if (controller == null)
            controller = root.AddComponent<CampaignCutsceneController>();

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("openingCutsceneDirector").objectReferenceValue = FindSceneDirector(opening.SceneObjectName);
        serialized.FindProperty("moneyLenderIntroCutsceneDirector").objectReferenceValue = FindSceneDirector(moneyLenderIntro.SceneObjectName);
        serialized.FindProperty("finalCutsceneDirector").objectReferenceValue = FindSceneDirector(finale.SceneObjectName);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        CampaignQuestGuidanceController tutorial = UnityEngine.Object.FindFirstObjectByType<CampaignQuestGuidanceController>(FindObjectsInactive.Include);

        if (tutorial != null)
        {
            SerializedObject tutorialSerialized = new SerializedObject(tutorial);
            tutorialSerialized.FindProperty("cutsceneController").objectReferenceValue = controller;
            tutorialSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorial);
        }

        EditorUtility.SetDirty(root);
    }

    private static PlayableDirector FindSceneDirector(string sceneObjectName)
    {
        GameObject root = GameObject.Find(sceneObjectName);
        return root != null ? root.GetComponent<PlayableDirector>() : null;
    }

    private static void ConfigureEventReceiver(
        TimelineDialogEventReceiver eventReceiver,
        PlayableDirector director,
        DialogSequencePlayer dialogPlayer,
        CutsceneDefinition definition)
    {
        SerializedObject serialized = new SerializedObject(eventReceiver);
        serialized.FindProperty("director").objectReferenceValue = director;
        serialized.FindProperty("dialogPlayer").objectReferenceValue = dialogPlayer;

        SerializedProperty cues = serialized.FindProperty("dialogCues");
        cues.arraySize = definition.DialogBeats.Length;

        for (int i = 0; i < definition.DialogBeats.Length; i++)
        {
            DialogBeat beat = definition.DialogBeats[i];
            SerializedProperty cue = cues.GetArrayElementAtIndex(i);
            cue.FindPropertyRelative("id").stringValue = beat.CueId;
            cue.FindPropertyRelative("dialog").objectReferenceValue = LoadDialog(beat.DialogPath);
            cue.FindPropertyRelative("cameraFocusTarget").objectReferenceValue = null;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureSignalReceiver(
        SignalReceiver signalReceiver,
        TimelineDialogEventReceiver eventReceiver,
        CutsceneDefinition definition)
    {
        foreach (DialogBeat beat in definition.DialogBeats)
        {
            SignalAsset signal = LoadSignal(definition, beat.CueId);
            UnityEvent reaction = new UnityEvent();
            UnityEventTools.AddStringPersistentListener(reaction, eventReceiver.PlayDialogCueByIdAndPauseTimeline, beat.CueId);
            signalReceiver.AddReaction(signal, reaction);
        }

        AddNoArgReaction(signalReceiver, LoadSignal(definition, "set_black"), eventReceiver.SetFadeBlackImmediate);
        AddFloatReaction(signalReceiver, LoadSignal(definition, "fade_in"), eventReceiver.FadeIn, 1f);
        AddNoArgReaction(signalReceiver, LoadSignal(definition, "fade_out"), eventReceiver.FadeOutDefault);
        AddNoArgReaction(signalReceiver, LoadSignal(definition, "fade_in_return"), eventReceiver.FadeInDefault);
        AddNoArgReaction(signalReceiver, LoadSignal(definition, "lock_gameplay"), eventReceiver.LockGameplay);
        AddNoArgReaction(signalReceiver, LoadSignal(definition, "unlock_gameplay"), eventReceiver.UnlockGameplay);
        AddNoArgReaction(signalReceiver, LoadSignal(definition, "begin_cutscene_camera"), eventReceiver.BeginCutsceneCameraMode);
        AddNoArgReaction(signalReceiver, LoadSignal(definition, "end_cutscene_camera"), eventReceiver.EndCutsceneCameraMode);

        if (definition.TimelinePath == FinalTimelinePath)
        {
            AddNoArgReaction(signalReceiver, LoadSignal(definition, "unlock_endless"), eventReceiver.StartUnlockedEndlessMode);
            AddNoArgReaction(signalReceiver, LoadSignal(definition, "save_game"), eventReceiver.SaveGame);
            AddNoArgReaction(signalReceiver, LoadSignal(definition, "queue_endless_notice"), eventReceiver.QueueEndlessUnlockedNotice);
            AddNoArgReaction(signalReceiver, LoadSignal(definition, "load_main_menu"), eventReceiver.LoadMainMenu);
        }
    }

    private static void AddNoArgReaction(SignalReceiver receiver, SignalAsset signal, UnityAction action)
    {
        if (signal == null || action == null)
            return;

        UnityEvent reaction = new UnityEvent();
        UnityEventTools.AddPersistentListener(reaction, action);
        receiver.AddReaction(signal, reaction);
    }

    private static void AddFloatReaction(SignalReceiver receiver, SignalAsset signal, UnityAction<float> action, float value)
    {
        if (signal == null || action == null)
            return;

        UnityEvent reaction = new UnityEvent();
        UnityEventTools.AddFloatPersistentListener(reaction, action, value);
        receiver.AddReaction(signal, reaction);
    }

    private static DialogSequenceAsset LoadDialog(string path)
    {
        return AssetDatabase.LoadAssetAtPath<DialogSequenceAsset>(path);
    }

    private static SignalAsset LoadSignal(CutsceneDefinition definition, string id)
    {
        string path = $"{SignalFolder}/{SanitizeFileName(definition.TimelineName)}_{SanitizeFileName(id)}.signal";
        return AssetDatabase.LoadAssetAtPath<SignalAsset>(path);
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(TimelineFolder);
        Directory.CreateDirectory(SignalFolder);
        AssetDatabase.Refresh();
    }

    private static void DestroySceneObject(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);

        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar, '_');

        return value.Replace(' ', '_');
    }

    private readonly struct CutsceneDefinition
    {
        public readonly string TimelineName;
        public readonly string SceneObjectName;
        public readonly string TimelinePath;
        public readonly double Duration;
        public readonly bool UseOpeningFade;
        public readonly DialogBeat[] DialogBeats;
        public readonly NoteBeat[] NoteBeats;

        public CutsceneDefinition(
            string timelineName,
            string sceneObjectName,
            string timelinePath,
            double duration,
            bool useOpeningFade,
            DialogBeat[] dialogBeats,
            NoteBeat[] noteBeats)
        {
            TimelineName = timelineName;
            SceneObjectName = sceneObjectName;
            TimelinePath = timelinePath;
            Duration = duration;
            UseOpeningFade = useOpeningFade;
            DialogBeats = dialogBeats;
            NoteBeats = noteBeats;
        }
    }

    private readonly struct DialogBeat
    {
        public readonly string CueId;
        public readonly string Title;
        public readonly string DialogPath;
        public readonly double Time;

        public DialogBeat(string cueId, string title, string dialogPath, double time)
        {
            CueId = cueId;
            Title = title;
            DialogPath = dialogPath;
            Time = time;
        }
    }

    private readonly struct NoteBeat
    {
        public readonly double Time;
        public readonly string Speaker;
        public readonly string Title;
        public readonly string Text;

        public NoteBeat(double time, string speaker, string title, string text)
        {
            Time = time;
            Speaker = speaker;
            Title = title;
            Text = text;
        }
    }
}
