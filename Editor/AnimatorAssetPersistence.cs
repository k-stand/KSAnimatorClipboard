using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    /// <summary>
    /// クローンされたAnimatorController関連オブジェクトをアセットとして永続化するためのユーティリティです。
    /// </summary>
    public static class AnimatorAssetPersistence
    {
        /// <summary>
        /// 指定したオブジェクトとその子オブジェクトを再帰的に辿り、指定したパスのアセットへサブアセットとして追加します。
        /// AnimationClip/BlendTreeはcontextの設定内容によっては、サブアセットではなく別アセットとして保存されます。
        /// </summary>
        /// <param name="objectToAdd">追加対象のルートオブジェクト。</param>
        /// <param name="path">保存先のアセットパス。</param>
        /// <param name="context">AnimationClip/BlendTreeの保存先を制御するコンテキスト。省略した場合は新規に作成されます。</param>
        /// <returns>実際にアセットへ追加されたオブジェクトの集合。</returns>
        /// <exception cref="ArgumentNullException">objectToAddがnullの場合。</exception>
        /// <exception cref="ArgumentException">pathがnullまたは空文字列の場合。</exception>
        public static HashSet<UnityEngine.Object> AddObjectToAssetRecursively(
            UnityEngine.Object objectToAdd,
            string path,
            SaveAssetsContext context = null)
        {
            if (objectToAdd == null) throw new ArgumentNullException("指定された UnityEngine.Object は null です。");
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("無効なパスが指定されました。");

            context ??= new();
            return AddObjectToAssetRecursivelyInternal(objectToAdd, path, context);
        }

        private static HashSet<UnityEngine.Object> AddObjectToAssetRecursivelyInternal(
            UnityEngine.Object objectToAdd,
            string path,
            SaveAssetsContext context)
        {
            if (objectToAdd == null || context.searchedObjects.Contains(objectToAdd)) return new();
            context.searchedObjects.Add(objectToAdd);

            HashSet<UnityEngine.Object> addedObjects = new();

            // BlendTreeの場合は保存先を分岐
            string targetPath = path;
            if (objectToAdd is BlendTree blendTree &&
                context.blendTreeInverseCloneMap.TryGetValue(blendTree, out BlendTree origTree) &&
                context.BlendTreeSaveMap.TryGetValue(origTree, out var savePolicy) &&
                savePolicy.Policy == BlendTreeSavePolicy.SeparateAsset &&
                !string.IsNullOrEmpty(savePolicy.AssetPath))
            {
                // SeparateAssetの場合は別アセットとして保存
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(blendTree)))
                {
                    AssetDatabase.CreateAsset(blendTree, savePolicy.AssetPath);
                    addedObjects.Add(blendTree);
                }
                // 子の再帰処理はこのBlendTreeのパスを親として渡す
                targetPath = savePolicy.AssetPath;
            }
            else if (objectToAdd is AnimationClip animationClip)
            {
                if (
                    context.animetionClipInverseCloneMap.TryGetValue(animationClip, out AnimationClip origAnimationClip) &&
                    context.AnimationClipSaveMap.TryGetValue(origAnimationClip, out string savePath) &&
                    !string.IsNullOrEmpty(savePath))
                {
                    // AnimationClipの場合は別アセットとして保存
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(animationClip)))
                    {
                        AssetDatabase.CreateAsset(animationClip, savePath);
                        addedObjects.Add(animationClip);
                    }
                }
                else
                {
                    //throw new ArgumentException($"AnimationClip'{animationClip.name}'に対して対応する適切な保存パスが見つかりません");
                }
            }
            else
            {
                // 通常のサブアセット保存
                bool added = CheckAndAddObjectToAsset(objectToAdd, path);
                if (added) addedObjects.Add(objectToAdd);
            }

            // オブジェクトのパスと保存先のパスが異なる場合
            if (AssetDatabase.GetAssetPath(objectToAdd) != targetPath) return addedObjects;

            // 子を再帰的に処理
            using SerializedObject so = new(objectToAdd);
            SerializedProperty prop = so.GetIterator();
            while (prop.Next(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    addedObjects.UnionWith(AddObjectToAssetRecursivelyInternal(
                        prop.objectReferenceValue,
                        targetPath,  // SeparateAssetの場合は新しいpathが子に伝播する
                        context));
                }
            }

            return addedObjects;
        }

        internal static bool CheckAndAddObjectToAsset(UnityEngine.Object objectToAdd, AnimatorController controller) => CheckAndAddObjectToAsset(objectToAdd, AssetDatabase.GetAssetPath(controller));

        internal static bool CheckAndAddObjectToAsset(UnityEngine.Object objectToAdd, string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("無効なパスが指定されました。");

            bool doAdd = objectToAdd != null && AssetDatabase.GetAssetPath(objectToAdd) == "";
            if (doAdd)
            {
                AssetDatabase.AddObjectToAsset(objectToAdd, path);
            }
            return doAdd;
        }

        /// <summary>
        /// 指定したオブジェクトのアセットに含まれるサブアセットのうち、ルートから参照を辿って到達できないものを削除します。
        /// </summary>
        /// <param name="obj">検査対象のルートオブジェクト。</param>
        /// <param name="muteLogs">trueの場合、削除したオブジェクトのログ出力を抑制します。</param>
        /// <returns>1件以上削除した場合はtrue。</returns>
        public static bool RemoveUnusedSubAssets(UnityEngine.Object obj, bool muteLogs = false)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            HashSet<UnityEngine.Object> allAssets = AssetDatabase.LoadAllAssetsAtPath(path)
                                         .Where(a => a != null)
                                         .ToHashSet();

            // ルートから到達可能なノードをBFSでマーク
            HashSet<UnityEngine.Object> reachable = new();
            Queue<UnityEngine.Object> queue = new();
            queue.Enqueue(obj);

            while (queue.Count > 0)
            {
                UnityEngine.Object current = queue.Dequeue();
                if (!reachable.Add(current)) continue; // 訪問済みならスキップ

                // SerializedObjectで子参照を辿る
                using SerializedObject so = new(current);
                SerializedProperty prop = so.GetIterator();
                while (prop.Next(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    UnityEngine.Object referenced = prop.objectReferenceValue;
                    if (referenced != null && allAssets.Contains(referenced) && !reachable.Contains(referenced))
                    {
                        queue.Enqueue(referenced);
                    }
                }
            }

            // 到達不能なものを削除
            UnityEngine.Object[] unreachable = allAssets.Except(reachable).ToArray();
            foreach (UnityEngine.Object asset in unreachable)
            {
                if (!muteLogs) Debug.Log($"Removed Object :'{asset.name}({asset.GetType().FullName})'");
                AssetDatabase.RemoveObjectFromAsset(asset);
            }

            return unreachable.Length > 0;
        }

        /// <summary>
        /// AddObjectToAssetRecursivelyでAnimationClip/BlendTreeの保存先を制御するためのコンテキストです。
        /// </summary>
        public class SaveAssetsContext
        {
            /// <summary>
            /// クローン元のAnimationClipをキーとした保存先パスのマップを取得または設定します。
            /// </summary>
            public IReadOnlyDictionary<AnimationClip, string> AnimationClipSaveMap { get; set; }

            /// <summary>
            /// クローン元のBlendTreeをキーとした、保存ポリシーと保存先パスのマップを取得または設定します。
            /// </summary>
            public IReadOnlyDictionary<BlendTree, (BlendTreeSavePolicy Policy, string AssetPath)> BlendTreeSaveMap { get; set; }

            internal IReadOnlyDictionary<AnimationClip, AnimationClip> animetionClipInverseCloneMap;
            internal IReadOnlyDictionary<BlendTree, BlendTree> blendTreeInverseCloneMap;
            internal HashSet<object> searchedObjects = new();


            /// <summary>
            /// 保存先の指定を持たない空のSaveAssetsContextを初期化します。
            /// </summary>
            public SaveAssetsContext() : this(new()) { }

            /// <summary>
            /// AnimatorClonerのクローン結果に基づいてSaveAssetsContextを初期化します。
            /// </summary>
            /// <param name="cloner">保存対象のクローンを行ったAnimatorCloner。</param>
            /// <param name="animationClipSaveMap">クローン元のAnimationClipをキーとした保存先パスのマップ。省略した場合は空になります。</param>
            /// <param name="blendTreeSaveMap">クローン元のBlendTreeをキーとした、保存ポリシーと保存先パスのマップ。省略した場合は空になります。</param>
            public SaveAssetsContext(
                AnimatorCloner cloner,
                IReadOnlyDictionary<AnimationClip, string> animationClipSaveMap = null,
                IReadOnlyDictionary<BlendTree, (BlendTreeSavePolicy Policy, string AssetPath)> blendTreeSaveMap = null)
            {
                AnimationClipSaveMap = animationClipSaveMap ?? new Dictionary<AnimationClip, string>();
                BlendTreeSaveMap = blendTreeSaveMap ?? new Dictionary<BlendTree, (BlendTreeSavePolicy Policy, string AssetPath)>();

                // clone→origの逆引きマップを事前構築
                animetionClipInverseCloneMap = cloner.GetClonedAnimationClip().ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
                blendTreeInverseCloneMap = cloner.GetClonedBlendTrees().ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            }
        }

        /// <summary>
        /// BlendTreeをアセットへ保存する際の保存方式です。
        /// </summary>
        public enum BlendTreeSavePolicy
        {
            /// <summary>AnimatorControllerのサブアセットとして保存(デフォルト)</summary>
            SubAsset,
            /// <summary>指定されたパスに別アセットとして保存</summary>
            SeparateAsset,
        }
    }
}
