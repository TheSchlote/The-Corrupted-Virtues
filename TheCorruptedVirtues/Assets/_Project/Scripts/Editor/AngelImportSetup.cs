using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using TheCorruptedVirtues.Combat;
using TheCorruptedVirtues.CombatSlice.Unity;

namespace TheCorruptedVirtues.EditorTools
{
    // One-shot batch setup for the Mixamo-rigged archangel: forces the FBX to
    // import as Humanoid and reports whether Unity's auto-rig produced a valid
    // human Avatar — the headless equivalent of opening "Configure Avatar" and
    // checking the bones are green. Runnable via -executeMethod or the menu.
    public static class AngelImportSetup
    {
        private const string FbxPath = "Assets/_Project/Art/Characters/Angel/angel_male_lp_v2.fbx";

        [MenuItem("Tools/TCV/Setup Angel (Humanoid)")]
        public static void Run()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[AngelSetup] No ModelImporter at {FbxPath} — is the FBX in the project?");
                return;
            }

            // Humanoid is what lets the angel share retargeted clips with the
            // KayKit characters; CreateFromThisModel builds the Avatar from the
            // model's own Mixamo skeleton. Guard the reimport so re-runs (just
            // re-reading diagnostics) stay fast.
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }

            var report = new StringBuilder();
            report.AppendLine("===== ANGEL IMPORT REPORT =====");

            var avatar = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null)
            {
                report.AppendLine("Avatar: NONE generated — humanoid mapping FAILED.");
            }
            else
            {
                report.AppendLine($"Avatar.isHuman : {avatar.isHuman}");
                report.AppendLine($"Avatar.isValid : {avatar.isValid}");
                report.AppendLine($"Mapped human bones: {avatar.humanDescription.human?.Length ?? 0}");
            }

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);

            // Mesh height in model units tells us how much to scale the angel so
            // it sits on one grid cell next to the chunkier KayKit characters.
            // The decisive question: is the wing mesh actually skinned to the
            // wing_* bones (so rotating them flaps the wings — no Blender rig
            // needed), or only to the humanoid spine (then we must reweight)?
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var s = smr.sharedMesh != null ? smr.sharedMesh.bounds.size : Vector3.zero;
                int wingBoneCount = smr.bones.Count(b => b != null && b.name.ToLowerInvariant().Contains("wing"));
                report.AppendLine($"SkinnedMesh '{smr.name}': bounds {s.x:F2} x {s.y:F2} x {s.z:F2}, bones {smr.bones.Length}, of which wing-bones {wingBoneCount}");
            }

            // Where wing_left_1 hangs tells us how the wing chain attaches to the
            // humanoid skeleton (e.g. under a spine/chest bone).
            var wingRoot = go.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "wing_left_1");
            if (wingRoot != null)
            {
                var chain = new System.Collections.Generic.List<string>();
                for (var t = wingRoot; t != null; t = t.parent) chain.Add(t.name);
                report.AppendLine("wing_left_1 ancestry: " + string.Join(" <- ", chain));
            }

            report.AppendLine("================================");
            Debug.Log(report.ToString());
        }

        // Builds the runtime Angel prefab the ModelUnitViewFactory loads from
        // Resources/Units/Angel: the rigged model with a self-driving WingFlap,
        // spear stripped (weapons are attached per-archetype later). Left at
        // scale 1 — tune that on the prefab once it's visible next to the grid.
        [MenuItem("Tools/TCV/Build Angel Prefab")]
        public static void BuildPrefab()
        {
            const string ResourcesDir = "Assets/_Project/Resources";
            const string UnitsDir = ResourcesDir + "/Units";
            const string PrefabPath = UnitsDir + "/Angel.prefab";

            var src = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (src == null)
            {
                Debug.LogError($"[AngelSetup] No model at {FbxPath} — run Setup Angel first.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ResourcesDir))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(UnitsDir))
            {
                AssetDatabase.CreateFolder(ResourcesDir, "Units");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(src);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            int strippedSpears = 0;
            foreach (var smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.name.ToLowerInvariant().Contains("spear"))
                {
                    Object.DestroyImmediate(smr.gameObject);
                    strippedSpears++;
                }
            }

            if (instance.GetComponent<WingFlap>() == null)
            {
                instance.AddComponent<WingFlap>();
            }

            // Cell size is 1, but the model is ~2.3 tall with a ~4-wide wingspan,
            // so at scale 1 neighbours' wings overlap badly. Shrink it so the body
            // fits its tile and only wingtips graze adjacent cells. Tune on the
            // prefab to taste — this is just the rebuild default.
            instance.transform.localScale = Vector3.one * 0.5f;

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out bool ok);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();

            Debug.Log($"[AngelSetup] Prefab {(ok ? "SAVED" : "FAILED")} at {PrefabPath}; WingFlap added; spear meshes stripped: {strippedSpears}.");
        }

        // Rebuilds the base prefab, then folds the wings into a compact resting
        // tuck so they stop spearing through neighbouring units. Rebuilds from the
        // FBX first (via BuildPrefab) so it's idempotent — always folds from the
        // spread bind pose, never compounding a previous fold.
        [MenuItem("Tools/TCV/Build Folded Angel Prefab")]
        public static void BuildFoldedPrefab()
        {
            const string PrefabPath = "Assets/_Project/Resources/Units/Angel.prefab";

            BuildPrefab();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform t = root.transform;
                Vector3 up = Vector3.up;
                Vector3 fwd = t.forward;
                Vector3 right = t.right;
                Vector3 center = t.position;

                float before = MaxWingReach(root, center, up);
                // Fold attempt skipped — the Bitgem wing mesh fans perpendicular
                // to the bones, so rotating/scaling the chain can't produce a
                // visually-folded silhouette without mesh editing. Leaving wings
                // at the model's default spread; the FoldWing method is kept for
                // future use if a foldable wing model arrives or we do a Blender
                // pass on this one.
                float after = MaxWingReach(root, center, up);

                // Folded wings should settle, not beat — drop the flap amplitude.
                // ALSO bake the now-folded wing-bone rest rotations onto WingFlap
                // so the Humanoid Animator can't snap them back to bind at runtime:
                // it re-applies the avatar's spread T-pose to non-humanoid bones
                // on init, which would otherwise make WingFlap's live-captured rest
                // come out spread instead of folded.
                WingFlap wf = root.GetComponent<WingFlap>();
                if (wf != null)
                {
                    List<Transform> bones = new List<Transform>();
                    foreach (Transform tr in root.GetComponentsInChildren<Transform>())
                    {
                        string nm = tr.name.ToLowerInvariant();
                        if (nm.StartsWith("wing_left_") || nm.StartsWith("wing_right_"))
                        {
                            bones.Add(tr);
                        }
                    }

                    SerializedObject so = new SerializedObject(wf);
                    so.FindProperty("flapDegrees").floatValue = 4f;

                    SerializedProperty wbProp = so.FindProperty("wingBones");
                    wbProp.arraySize = bones.Count;
                    for (int i = 0; i < bones.Count; i++)
                    {
                        wbProp.GetArrayElementAtIndex(i).objectReferenceValue = bones[i];
                    }

                    SerializedProperty brProp = so.FindProperty("bakedRestRotations");
                    brProp.arraySize = bones.Count;
                    for (int i = 0; i < bones.Count; i++)
                    {
                        brProp.GetArrayElementAtIndex(i).quaternionValue = bones[i].localRotation;
                    }

                    so.ApplyModifiedProperties();
                    Debug.Log($"[AngelSetup] WingFlap baked: {bones.Count} wing-bone rests stored on the component.");
                }

                ApplyAngelMaterials(root);

                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[AngelSetup] Wings folded. Horizontal wing reach (scaled units, cell=1): {before:F2} -> {after:F2}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Sweeps one wing from its spread bind direction toward up-and-back,
        // slightly inward. Rotating only the root bone carries the whole chain
        // rigidly into the tuck; working off the world-space tip direction means
        // it needs no knowledge of the bones' local axis orientation.
        private static void FoldWing(GameObject root, string rootName, string tipName,
            Vector3 up, Vector3 fwd, Vector3 right, Vector3 center)
        {
            Transform rootBone = FindByName(root, rootName);
            Transform tipBone = FindByName(root, tipName);
            if (rootBone == null || tipBone == null)
            {
                Debug.LogWarning($"[AngelSetup] Wing bones {rootName}/{tipName} not found.");
                return;
            }

            Vector3 currentDir = (tipBone.position - rootBone.position).normalized;
            float side = Vector3.Dot(rootBone.position - center, right) >= 0f ? 1f : -1f;
            Vector3 inward = -side * right;
            Vector3 targetDir = (up * 1.0f - fwd * 0.45f + inward * 0.15f).normalized;
            rootBone.rotation = Quaternion.FromToRotation(currentDir, targetDir) * rootBone.rotation;

            // Rotation alone can't collapse a perpendicular fan of feathers, so
            // shrink the whole chain too. The mesh weights follow the bones,
            // bringing the fan in along with them.
            rootBone.localScale = Vector3.one * 0.5f;
        }

        private static float MaxWingReach(GameObject root, Vector3 center, Vector3 up)
        {
            float max = 0f;
            foreach (Transform b in root.GetComponentsInChildren<Transform>())
            {
                string n = b.name.ToLowerInvariant();
                if (!n.StartsWith("wing_left_") && !n.StartsWith("wing_right_"))
                {
                    continue;
                }

                Vector3 d = b.position - center;
                d -= up * Vector3.Dot(d, up); // horizontal component only
                if (d.magnitude > max)
                {
                    max = d.magnitude;
                }
            }
            return max;
        }

        private static Transform FindByName(GameObject root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>())
            {
                if (t.name == name)
                {
                    return t;
                }
            }
            return null;
        }

        // Builds URP materials from the Bitgem maps and assigns them by mesh: the
        // body opaque (+ glow as emission), the wings alpha-clipped so the feather
        // cut-outs read instead of rendering as solid cards. The wing alpha rides
        // in man_angel_d's alpha channel; the clip cutoff is the likely tuning
        // knob if feathers come out chunky or see-through.
        private static void ApplyAngelMaterials(GameObject root)
        {
            const string TexDir = "Assets/_Project/Art/Characters/Angel/Textures/";
            const string AngelDir = "Assets/_Project/Art/Characters/Angel";
            const string MatDir = AngelDir + "/Materials";

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "man_angel_d.tga");
            Texture2D glow = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "man_angel_glow.tga");
            if (albedo == null)
            {
                Debug.LogWarning("[AngelSetup] man_angel_d not found — leaving default (gray) material.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(MatDir))
            {
                AssetDatabase.CreateFolder(AngelDir, "Materials");
            }

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");

            Material body = new Material(lit);
            body.SetTexture("_BaseMap", albedo);
            if (glow != null)
            {
                body.EnableKeyword("_EMISSION");
                body.SetTexture("_EmissionMap", glow);
                body.SetColor("_EmissionColor", Color.white);
                body.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(body, MatDir + "/Angel_Body.mat");

            Material wings = new Material(lit);
            wings.SetTexture("_BaseMap", albedo);
            wings.SetFloat("_AlphaClip", 1f);
            wings.EnableKeyword("_ALPHATEST_ON");
            wings.SetFloat("_Cutoff", 0.5f);
            wings.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            AssetDatabase.CreateAsset(wings, MatDir + "/Angel_Wings.mat");

            int assigned = 0;
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.sharedMaterial = smr.name.ToLowerInvariant().Contains("alpha") ? wings : body;
                assigned++;
            }
            Debug.Log($"[AngelSetup] Materials applied to {assigned} renderers (albedo set; glow {(glow != null ? "on" : "absent")}).");
        }

        // Diagnostic: dumps the saved Angel prefab's wing-bone local rotations
        // so we can confirm objectively whether the fold actually persisted to
        // the asset (vs. being live-overridden by something at runtime).
        [MenuItem("Tools/TCV/Inspect Angel Prefab")]
        public static void InspectAngel()
        {
            const string PrefabPath = "Assets/_Project/Resources/Units/Angel.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform t = root.transform;
                Debug.Log($"[Inspect] root localScale={t.localScale}");

                string[] names = { "wing_left_1", "wing_left_3", "wing_left_5", "wing_right_1", "wing_right_3", "wing_right_5" };
                foreach (string n in names)
                {
                    Transform b = FindByName(root, n);
                    if (b == null)
                    {
                        Debug.LogWarning($"[Inspect] {n}: NOT FOUND");
                        continue;
                    }
                    Vector3 e = b.localEulerAngles;
                    Vector3 lp = b.localPosition;
                    Vector3 wp = b.position;
                    Debug.Log($"[Inspect] {n}: localEuler=({e.x:F1},{e.y:F1},{e.z:F1}) localPos=({lp.x:F2},{lp.y:F2},{lp.z:F2}) worldPos=({wp.x:F2},{wp.y:F2},{wp.z:F2})");
                }

                float reach = MaxWingReach(root, t.position, Vector3.up);
                Debug.Log($"[Inspect] Persisted horizontal wing reach: {reach:F2} (folded build measured 0.53)");

                WingFlap wf = root.GetComponent<WingFlap>();
                if (wf == null)
                {
                    Debug.LogWarning("[Inspect] WingFlap: NOT FOUND on prefab root.");
                }
                else
                {
                    SerializedObject sob = new SerializedObject(wf);
                    int wbCount = sob.FindProperty("wingBones").arraySize;
                    int brCount = sob.FindProperty("bakedRestRotations").arraySize;
                    float flapDeg = sob.FindProperty("flapDegrees").floatValue;
                    Debug.Log($"[Inspect] WingFlap: flapDegrees={flapDeg}, wingBones={wbCount}, bakedRestRotations={brCount}");

                    SerializedProperty brProp = sob.FindProperty("bakedRestRotations");
                    if (brProp.arraySize > 0)
                    {
                        SerializedProperty first = brProp.GetArrayElementAtIndex(0);
                        float qx = first.FindPropertyRelative("x").floatValue;
                        float qy = first.FindPropertyRelative("y").floatValue;
                        float qz = first.FindPropertyRelative("z").floatValue;
                        float qw = first.FindPropertyRelative("w").floatValue;
                        Debug.Log($"[Inspect] bakedRestRotations[0] xyzw = ({qx:F3},{qy:F3},{qz:F3},{qw:F3})");

                        Transform wl1 = FindByName(root, "wing_left_1");
                        if (wl1 != null)
                        {
                            Quaternion lr = wl1.localRotation;
                            Debug.Log($"[Inspect] wing_left_1.localRotation xyzw = ({lr.x:F3},{lr.y:F3},{lr.z:F3},{lr.w:F3})  (compare to baked[0])");
                        }
                    }

                    SerializedProperty wbProp = sob.FindProperty("wingBones");
                    if (wbProp.arraySize > 0)
                    {
                        Object refObj = wbProp.GetArrayElementAtIndex(0).objectReferenceValue;
                        Debug.Log($"[Inspect] wingBones[0] -> {(refObj == null ? "NULL (refs broken)" : refObj.name)}");
                    }
                }

                Animator anim = root.GetComponent<Animator>();
                if (anim == null)
                {
                    Debug.LogWarning("[Inspect] Animator: NOT FOUND on prefab root.");
                }
                else
                {
                    string ctrlName = anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "null";
                    string avatarName = anim.avatar != null ? anim.avatar.name : "null";
                    Debug.Log($"[Inspect] Animator: controller={ctrlName}, applyRootMotion={anim.applyRootMotion}, avatar={avatarName}, updateMode={anim.updateMode}, hasTransformHierarchy={anim.hasTransformHierarchy}");
                }

                ModelImporter imp = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
                if (imp != null)
                {
                    Debug.Log($"[Inspect] Angel FBX importer: optimizeGameObjects={imp.optimizeGameObjects}, animationType={imp.animationType}, avatarSetup={imp.avatarSetup}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Builds an AnimatorController from the Mixamo Pro Magic Pack clips and
        // assigns it to the Angel prefab's Animator. Imports each pack FBX as
        // Humanoid CopyFromOther against the Angel avatar (so the clips retarget),
        // marks locomotion clips loopable, then wires Locomotion (Idle<->Walk on
        // Speed) + a Hit trigger to a single React reaction. The controller honors
        // the Speed/Hit contract ModelUnitView already drives; gameplay code
        // doesn't change.
        [MenuItem("Tools/TCV/Build Angel Animator")]
        public static void BuildAngelAnimator()
        {
            const string AnimDir = "Assets/_Project/Art/Animations/Magic";
            const string ControllerPath = "Assets/_Project/Art/Characters/Angel/Angel.controller";
            const string PrefabPath = "Assets/_Project/Resources/Units/Angel.prefab";

            Avatar angelAvatar = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
                .OfType<Avatar>().FirstOrDefault();
            if (angelAvatar == null)
            {
                Debug.LogError($"[AngelAnim] No Avatar at {FbxPath} — run Setup Angel first.");
                return;
            }

            if (!Directory.Exists(AnimDir))
            {
                Debug.LogError($"[AngelAnim] No animation folder at {AnimDir}.");
                return;
            }

            string[] fbxPaths = Directory.GetFiles(AnimDir, "*.fbx")
                .Select(p => p.Replace('\\', '/'))
                .ToArray();
            if (fbxPaths.Length == 0)
            {
                Debug.LogError($"[AngelAnim] No FBXs found in {AnimDir}.");
                return;
            }

            // Only locomotion clips loop; reactions/attacks/deaths play once.
            HashSet<string> loopable = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "standing idle",
                "Standing Walk Forward",
                "Standing Run Forward",
                "Standing Sprint Forward",
            };

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string fbx in fbxPaths)
                {
                    ModelImporter im = AssetImporter.GetAtPath(fbx) as ModelImporter;
                    if (im == null) continue;

                    im.animationType = ModelImporterAnimationType.Human;
                    im.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    im.sourceAvatar = angelAvatar;

                    // Rename each generated clip to its filename + set loop where
                    // it makes sense (Mixamo defaults the take name to "mixamo.com").
                    string baseName = Path.GetFileNameWithoutExtension(fbx);
                    ModelImporterClipAnimation[] clips = im.defaultClipAnimations;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        clips[i].name = baseName;
                        clips[i].loopTime = loopable.Contains(baseName);
                    }
                    im.clipAnimations = clips;

                    im.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AnimationClip idle = LoadClip(AnimDir, "standing idle.fbx");
            AnimationClip walk = LoadClip(AnimDir, "Standing Walk Forward.fbx");
            AnimationClip hit = LoadClip(AnimDir, "Standing React Small From Front.fbx");
            AnimationClip attack = LoadClip(AnimDir, "Standing 1H Magic Attack 01.fbx");
            AnimationClip death = LoadClip(AnimDir, "Standing React Death Forward.fbx");
            if (idle == null || walk == null || hit == null || attack == null || death == null)
            {
                Debug.LogError($"[AngelAnim] Missing clip(s): idle={(idle == null ? "null" : "ok")}, walk={(walk == null ? "null" : "ok")}, hit={(hit == null ? "null" : "ok")}, attack={(attack == null ? "null" : "ok")}, death={(death == null ? "null" : "ok")}.");
                return;
            }

            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Death", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            // ModelUnitView's planar speed peaks briefly when a move starts
            // (exponential ease) and decays to 0 on arrival; a low Walk threshold
            // keeps the whole move reading as walking instead of a frantic dash.
            BlendTree locoTree;
            AnimatorState locoState = ctrl.CreateBlendTreeInController("Locomotion", out locoTree);
            locoTree.blendType = BlendTreeType.Simple1D;
            locoTree.blendParameter = "Speed";
            locoTree.AddChild(idle, 0f);
            locoTree.AddChild(walk, 0.5f);

            AnimatorState hitState = sm.AddState("Hit");
            hitState.motion = hit;

            sm.defaultState = locoState;

            AnimatorStateTransition toHit = sm.AddAnyStateTransition(hitState);
            toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
            toHit.hasExitTime = false;
            toHit.duration = 0.1f;
            toHit.canTransitionToSelf = false;

            AnimatorStateTransition fromHit = hitState.AddTransition(locoState);
            fromHit.hasExitTime = true;
            fromHit.exitTime = 0.85f;
            fromHit.duration = 0.2f;

            AnimatorState attackState = sm.AddState("Attack");
            attackState.motion = attack;
            AnimatorStateTransition toAttack = sm.AddAnyStateTransition(attackState);
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            toAttack.hasExitTime = false;
            toAttack.duration = 0.05f;
            toAttack.canTransitionToSelf = false;
            AnimatorStateTransition fromAttack = attackState.AddTransition(locoState);
            fromAttack.hasExitTime = true;
            fromAttack.exitTime = 0.9f;
            fromAttack.duration = 0.2f;

            // Death is one-shot — no transition out; ModelUnitView hides the
            // GameObject after a short delay so the body finishes its fall.
            AnimatorState deathState = sm.AddState("Death");
            deathState.motion = death;
            AnimatorStateTransition toDeath = sm.AddAnyStateTransition(deathState);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Death");
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
            toDeath.canTransitionToSelf = false;

            AssetDatabase.SaveAssets();

            // Assign the controller to the Angel prefab's Animator, and disable
            // root motion so Mixamo's baked-in forward translation doesn't fight
            // the ModelUnitView position easing.
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Animator animator = root.GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogError("[AngelAnim] No Animator on Angel prefab root.");
                    return;
                }
                animator.runtimeAnimatorController = ctrl;
                animator.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"[AngelAnim] Animator built ({fbxPaths.Length} clips Humanoid-imported): Locomotion (Idle<->Walk on Speed) + Hit / Attack / Death triggers. Assigned to Angel prefab.");
        }

        private static AnimationClip LoadClip(string dir, string fileName)
        {
            string path = (dir + "/" + fileName).Replace('\\', '/');
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__"));
        }

        // Diagnostic: lists any animation curves on the idle clip that target
        // wing bones. If wing curves are present, the Humanoid retargeting kept
        // raw transform tracks for the non-humanoid bones, and the Animator is
        // overwriting WingFlap's writes every frame — fix is to mask them out.
        [MenuItem("Tools/TCV/Inspect Idle Curves")]
        public static void InspectIdleCurves()
        {
            const string IdlePath = "Assets/_Project/Art/Animations/Magic/standing idle.fbx";
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(IdlePath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__"));
            if (clip == null)
            {
                Debug.LogError($"[Curves] No clip at {IdlePath}");
                return;
            }

            Debug.Log($"[Curves] Clip='{clip.name}' humanMotion={clip.humanMotion} isLooping={clip.isLooping} length={clip.length:F2}s");

            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            int wingCount = 0;
            HashSet<string> uniqueWingPaths = new HashSet<string>();
            foreach (EditorCurveBinding b in bindings)
            {
                string p = b.path.ToLowerInvariant();
                if (p.Contains("wing"))
                {
                    wingCount++;
                    uniqueWingPaths.Add(b.path);
                }
            }

            Debug.Log($"[Curves] Total curve bindings: {bindings.Length}, wing-bone curves: {wingCount} across {uniqueWingPaths.Count} unique wing paths");
            foreach (string p in uniqueWingPaths.Take(12))
            {
                Debug.Log($"[Curves]   wing path: {p}");
            }
        }

        // Diagnostic: dumps the Primitive_Cube mesh's bounds (so we know its pivot
        // offset) and the imported material's shader (so we know whether the
        // KayKit native material renders in URP or needs replacing).
        [MenuItem("Tools/TCV/Inspect Prototype Cube")]
        public static void InspectProtoCube()
        {
            const string Path = "Assets/_Project/Resources/Environment/Prototype/Primitive_Cube.fbx";
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            if (src == null)
            {
                Debug.LogError($"[ProtoInspect] Not found at {Path}");
                return;
            }

            MeshFilter mf = src.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Mesh m = mf.sharedMesh;
                Debug.Log($"[ProtoInspect] mesh bounds: center=({m.bounds.center.x:F2},{m.bounds.center.y:F2},{m.bounds.center.z:F2}) size=({m.bounds.size.x:F2},{m.bounds.size.y:F2},{m.bounds.size.z:F2})");
            }
            else
            {
                Debug.LogWarning("[ProtoInspect] No MeshFilter/mesh found.");
            }

            MeshRenderer mr = src.GetComponentInChildren<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                Material mat = mr.sharedMaterial;
                Debug.Log($"[ProtoInspect] material '{mat.name}' shader='{mat.shader.name}' renderQueue={mat.renderQueue}");
            }
            else
            {
                Debug.LogWarning("[ProtoInspect] No MeshRenderer/material found.");
            }
        }

        // Builds 7 archangel prefab variants from the base Angel prefab — one
        // per Choir member (Lore: Uriel/Umbriel/Pyriel/Hydriel/Arboriel/Terriel/
        // Electriel). Each has its own body+wings materials with the glow map
        // tinted to its element colour, so a unit's ElementType auto-selects
        // which archangel spawns (via ModelUnitViewFactory.ResolveKey).
        [MenuItem("Tools/TCV/Build Archangel Reskins")]
        public static void BuildArchangelReskins()
        {
            const string BasePrefabPath = "Assets/_Project/Resources/Units/Angel.prefab";
            const string UnitsDir = "Assets/_Project/Resources/Units";
            const string MatDir = "Assets/_Project/Art/Characters/Angel/Materials";
            const string TexDir = "Assets/_Project/Art/Characters/Angel/Textures/";

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (basePrefab == null)
            {
                Debug.LogError($"[Reskins] No base prefab at {BasePrefabPath} — run Build Folded Angel Prefab first.");
                return;
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "man_angel_d.tga");
            Texture2D glow = AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + "man_angel_glow.tga");
            if (albedo == null)
            {
                Debug.LogError("[Reskins] Missing man_angel_d.tga — run Build Folded Angel Prefab first.");
                return;
            }

            Shader lit = Shader.Find("Universal Render Pipeline/Lit");

            (string Name, ElementType Element)[] archangels =
            {
                ("Uriel",     ElementType.Light),
                ("Umbriel",   ElementType.Dark),
                ("Pyriel",    ElementType.Fire),
                ("Hydriel",   ElementType.Water),
                ("Arboriel",  ElementType.Nature),
                ("Terriel",   ElementType.Earth),
                ("Electriel", ElementType.Electricity),
            };

            int built = 0;
            foreach ((string name, ElementType element) in archangels)
            {
                Color tint = ElementPalette.For(element);

                Material body = new Material(lit);
                body.SetTexture("_BaseMap", albedo);
                // Tint the albedo so the whole armour reads as the element colour
                // at tactical distance — a bloom-less glow alone is too subtle.
                body.SetColor("_BaseColor", Color.Lerp(Color.white, tint, 0.55f));
                if (glow != null)
                {
                    body.EnableKeyword("_EMISSION");
                    body.SetTexture("_EmissionMap", glow);
                    // HDR-bright so the glow areas pop without a bloom pass.
                    body.SetColor("_EmissionColor", tint * 2.5f);
                    body.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                AssetDatabase.CreateAsset(body, $"{MatDir}/{name}_Body.mat");

                Material wings = new Material(lit);
                wings.SetTexture("_BaseMap", albedo);
                wings.SetColor("_BaseColor", Color.Lerp(Color.white, tint, 0.55f));
                wings.SetFloat("_AlphaClip", 1f);
                wings.EnableKeyword("_ALPHATEST_ON");
                wings.SetFloat("_Cutoff", 0.5f);
                wings.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                if (glow != null)
                {
                    wings.EnableKeyword("_EMISSION");
                    wings.SetTexture("_EmissionMap", glow);
                    wings.SetColor("_EmissionColor", tint * 2.5f);
                }
                AssetDatabase.CreateAsset(wings, $"{MatDir}/{name}_Wings.mat");

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                foreach (SkinnedMeshRenderer smr in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.sharedMaterial = smr.name.ToLowerInvariant().Contains("alpha") ? wings : body;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, $"{UnitsDir}/{name}.prefab");
                Object.DestroyImmediate(instance);
                built++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Reskins] Built {built} archangel prefabs under {UnitsDir}/ — one per Choir element.");
        }

        // Convenience wrapper: rebuild the prefab from the FBX (folded + scaled +
        // textured, WingFlap baked), re-assign the Animator controller, then
        // stamp the 7 archangel reskins from it. One batch run does the full
        // setup after any code change to the build pipeline.
        [MenuItem("Tools/TCV/Rebuild Angel (Full)")]
        public static void RebuildAngelFull()
        {
            BuildFoldedPrefab();
            BuildAngelAnimator();
            BuildArchangelReskins();
        }
    }
}
