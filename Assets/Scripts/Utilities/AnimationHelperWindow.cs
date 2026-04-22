#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// PSEUDOCODE / PLAN (detailed):
// 1. Validate input clip is not null.
// 2. Begin an undo record for the clip so changes can be reverted via Unity editor.
// 3. Define epsilons for comparing times and float values.
// 4. Iterate float animation curves obtained via AnimationUtility.GetCurveBindings.
//    a. Skip null curves.
//    b. If curve has zero keys -> remove curve.
//    c. If all keyframe values are equal (within epsilon) -> remove curve.
//    d. Else if there are only up to two keyframes and those keyframe times are at the clip start and clip end (within epsilon) -> remove curve.
// 5. Iterate object reference curves obtained via AnimationUtility.GetObjectReferenceCurveBindings.
//    a. If no keys -> remove.
//    b. If all keyframe object references are equal -> remove.
//    c. Else if there are only up to two keyframes at start/end -> remove.
// 6. Mark clip dirty and save assets, log removal summary.
// 7. Keep changes minimal and use AnimationUtility.SetEditorCurve / SetObjectReferenceCurve to remove channels.
//
// NOTES:
// - This uses conservative heuristics because "default value" is not reliably queryable from the clip alone.
// - Epsilon values tuned to typical clip lengths; adjust if necessary.

public class AnimationHelperWindow : EditorWindow
{
    [MenuItem("Rage Rooster Tooling/Animation Helper Window")]
    public static new void Show()
    {
        AnimationHelperWindow w = ScriptableObject.CreateInstance<AnimationHelperWindow>();
        w.titleContent = new("Debug Tools Window");
        w.ShowUtility();
    }

    Button ClearUnecessaryChannelsButton;
    Button TransferAllAttackTagsButton;

    private void OnEnable()
    {
        ClearUnecessaryChannelsButton = new Button(ClearUnecessaryChannelsButtonPress)
        {
            text = "Clear Unecessary Channels"
        };
        rootVisualElement.Add(ClearUnecessaryChannelsButton);
        TransferAllAttackTagsButton = new Button(TransferAllAttackTags)
        {
            text = "Transfer All Attack Tags"
        };
        rootVisualElement.Add(TransferAllAttackTagsButton);

    }

    public void ClearUnecessaryChannelsButtonPress()
    {
        for (int i = 0; i < Selection.objects.Length; i++)
        {
            if (Selection.objects[i] is AnimationClip c) ClearUnnecessaryChannels(c);
        }


    }

    public void ClearUnnecessaryChannels(AnimationClip source)
    {
        if (source == null)
        {
            Debug.LogWarning("No AnimationClip provided to ClearUnnecessaryChannels.");
            return;
        }

        // Record undo so user can revert.
        Undo.RecordObject(source, "Clear Unnecessary Animation Channels");

        const float timeEpsilon = 0.001f;
        const float valueEpsilon = 0.0001f;

        int removedCount = 0;
        float clipLength = Mathf.Max(0f, source.length);

        // Float/curve bindings
        var floatBindings = AnimationUtility.GetCurveBindings(source);
        foreach (var binding in floatBindings)
        {
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            if (curve == null)
                continue;

            var keys = curve.keys;
            if (keys == null || keys.Length == 0)
            {
                AnimationUtility.SetEditorCurve(source, binding, null);
                removedCount++;
                continue;
            }

            // If all key values are effectively identical -> remove channel
            bool allSameValue = true;
            float firstVal = keys[0].value;
            for (int i = 1; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].value - firstVal) > valueEpsilon)
                {
                    allSameValue = false;
                    break;
                }
            }

            if (allSameValue)
            {
                AnimationUtility.SetEditorCurve(source, binding, null);
                removedCount++;
                continue;
            }

            // If only keys are at the very beginning and very end (<=2 keys) -> remove channel
            if (keys.Length <= 2)
            {
                float firstTime = keys[0].time;
                float lastTime = keys[keys.Length - 1].time;
                bool firstAtStart = Mathf.Abs(firstTime - 0f) <= timeEpsilon;
                bool lastAtEnd = Mathf.Abs(lastTime - clipLength) <= timeEpsilon;
                if (firstAtStart && lastAtEnd)
                {
                    AnimationUtility.SetEditorCurve(source, binding, null);
                    removedCount++;
                    continue;
                }
            }
        }

        // Object reference bindings (e.g., sprite swaps)
        var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
        foreach (var binding in objBindings)
        {
            var keyframes = AnimationUtility.GetObjectReferenceCurve(source, binding);
            if (keyframes == null || keyframes.Length == 0)
            {
                AnimationUtility.SetObjectReferenceCurve(source, binding, null);
                removedCount++;
                continue;
            }

            bool allSameRef = true;
            var firstRef = keyframes[0].value;
            for (int i = 1; i < keyframes.Length; i++)
            {
                if (keyframes[i].value != firstRef)
                {
                    allSameRef = false;
                    break;
                }
            }

            if (allSameRef)
            {
                AnimationUtility.SetObjectReferenceCurve(source, binding, null);
                removedCount++;
                continue;
            }

            if (keyframes.Length <= 2)
            {
                float firstTime = keyframes[0].time;
                float lastTime = keyframes[keyframes.Length - 1].time;
                bool firstAtStart = Mathf.Abs(firstTime - 0f) <= timeEpsilon;
                bool lastAtEnd = Mathf.Abs(lastTime - clipLength) <= timeEpsilon;
                if (firstAtStart && lastAtEnd)
                {
                    AnimationUtility.SetObjectReferenceCurve(source, binding, null);
                    removedCount++;
                    continue;
                }
            }
        }

        EditorUtility.SetDirty(source);
        try
        {
            AssetDatabase.SaveAssets();
        }
        catch
        {
            // Ignored - saving assets may fail in some contexts; clip changes still applied in memory.
        }

        Debug.Log($"ClearUnnecessaryChannels: Removed {removedCount} channel(s) from clip '{source.name}'.");
    }

    public void TransferAllAttackTags()
    {
        // PSEUDOCODE / PLAN:
        // 1. Query all prefab asset GUIDs.
        // 2. For each prefab:
        //    a. Load the prefab asset root with AssetDatabase.LoadAssetAtPath<GameObject>(path).
        //    b. Inspect the prefab asset (non-editing) for any components of interest (IAttackSource or Health)
        //       by enumerating root.GetComponentsInChildren<Component>(true) and checking types.
        //    c. If none found -> skip (do not open EditPrefabContentsScope).
        //    d. If found -> open EditPrefabContentsScope(path), perform TransferTags/TransferImmuneTags on matching
        //       components, call EditorUtility.SetDirty only for modified components; let the scope save changes on dispose.
        // 3. For each enabled build scene:
        //    a. Open the scene.
        //    b. Find components of type IAttackSource and Health using Object.FindObjectsByType(...).
        //    c. Filter out any components that belong to prefab instances in the scene using PrefabUtility.IsPartOfPrefabInstance.
        //    d. Transfer tags on remaining components and mark dirty; save the scene only if any changes were made.
        //
        // Notes:
        // - This avoids opening and touching prefabs that don't contain relevant components,
        //   preventing accidental serialized changes to unrelated prefabs.
        // - Uses non-destructive inspection (LoadAssetAtPath) before EditPrefabContentsScope.

        string[] prefabGUIDS = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGUIDS)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Load prefab asset for inspection without opening edit scope.
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null)
                continue;

            // Quick check: does this prefab asset contain any IAttackSource or Health components?
            Component[] allComponents = prefabAsset.GetComponentsInChildren<Component>(true);
            bool containsRelevant = allComponents.Any(c => c is IAttackSource or Health);
            if (!containsRelevant)
                continue; // Skip opening and touching this prefab.

            // Only now open the prefab for editing.
            using (PrefabUtility.EditPrefabContentsScope editingScope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                GameObject root = editingScope.prefabContentsRoot;

                IAttackSource[] attackSourceComps = root.GetComponentsInChildren<IAttackSource>(true);
                foreach (IAttackSource comp in attackSourceComps)
                {
                    if (comp == null) continue;
                    // comp should be a Component (IAttackSource implemented by MonoBehaviour). Safely cast.
                    if (comp is Component compAsComponent)
                    {
                        comp.TransferTags();
                        EditorUtility.SetDirty(compAsComponent);
                    }
                }

                Health[] healthComps = root.GetComponentsInChildren<Health>(true);
                foreach (Health comp in healthComps)
                {
                    if (comp == null) continue;
                    EditorUtility.SetDirty(comp as Component);
                    comp.TransferImmuneTags();
                }
                // EditPrefabContentsScope will save changes only if edits were actually made.
            }
        }

        // Process scenes, but only operate on components that are NOT part of prefab instances in the scene.
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            var openedScene = EditorSceneManager.OpenScene(scene.path);

            bool isDirty = false;

            // Find IAttackSource in the opened scene
            IEnumerable<IAttackSource> attackSourceObjects = Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(C => C is IAttackSource).Cast<IAttackSource>();
            foreach (var comp in attackSourceObjects)
            {
                if (comp == null) continue;
                if (!(comp is Component compAsComponent)) continue;

                // Skip components that are part of prefab instances in the scene
                if (PrefabUtility.IsPartOfPrefabInstance(compAsComponent)) continue;

                comp.TransferTags();
                EditorUtility.SetDirty(compAsComponent);
                isDirty = true;
            }

            // Find Health components in the opened scene
            Health[] healthObjects = Object.FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Health comp in healthObjects)
            {
                if (comp == null) continue;
                Component compAsComponent = comp as Component;
                if (compAsComponent == null) continue;

                // Skip components that are part of prefab instances in the scene
                if (PrefabUtility.IsPartOfPrefabInstance(compAsComponent)) continue;

                comp.TransferImmuneTags();
                EditorUtility.SetDirty(compAsComponent);
                isDirty = true;
            }

            if (isDirty) EditorSceneManager.SaveScene(openedScene);
        }
    }

}
#endif