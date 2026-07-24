using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorclipboard.editor.Copying;
using com.github.k_stand.ksanimatorclipboard.editor.CrossController;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    /// <summary>
    /// AnimatorController関連オブジェクトのコピー・貼り付け機能を提供する、このパッケージの主な入口となる静的クラスです。
    /// 各操作にはTry接頭辞を持つ失敗許容版と、失敗時に例外を送出する版が対になって用意されています。
    /// </summary>
    public static class AnimatorClipboard
    {
        /// <summary>
        /// 単一のAnimatorControllerLayerをコピーします。
        /// </summary>
        /// <param name="layer">コピー対象のレイヤー。</param>
        /// <param name="parentController">layerが属している親AnimatorController。</param>
        /// <param name="result">成功した場合はコピー結果のAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(AnimatorControllerLayer layer, AnimatorController parentController, out AnimatorCopyClipSet result)
            => TryCopy(new[] { layer }, parentController, out result);

        /// <summary>
        /// 単一のAnimatorControllerLayerをコピーします。
        /// </summary>
        /// <param name="layer">コピー対象のレイヤー。</param>
        /// <param name="parentController">layerが属している親AnimatorController。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static AnimatorCopyClipSet Copy(AnimatorControllerLayer layer, AnimatorController parentController)
        {
            if (!TryCopy(layer, parentController, out AnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// 複数のAnimatorControllerLayerをまとめてコピーします。
        /// </summary>
        /// <param name="layers">コピー対象のレイヤーの列挙。</param>
        /// <param name="parentController">layersが属している親AnimatorController。</param>
        /// <param name="result">成功した場合はコピー結果のAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController, out AnimatorCopyClipSet result)
        {
            AnimatorCopyClipSet clipSet = new(layers, parentController);
            if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers)
            {
                result = null;
                return false;
            }
            result = clipSet;
            return true;
        }

        /// <summary>
        /// 複数のAnimatorControllerLayerをまとめてコピーします。
        /// </summary>
        /// <param name="layers">コピー対象のレイヤーの列挙。</param>
        /// <param name="parentController">layersが属している親AnimatorController。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static AnimatorCopyClipSet Copy(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            if (!TryCopy(layers, parentController, out AnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// AnimatorStateMachine配下の単一オブジェクト(State/StateMachine/Transition等)を、その親レイヤーを祖先としてコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <param name="parentLayer">objが属している親AnimatorControllerLayer。</param>
        /// <param name="result">成功した場合はコピー結果のAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(object obj, AnimatorControllerLayer parentLayer, out AnimatorCopyClipSet result)
            => TryCopy(new[] { obj }, parentLayer.stateMachine, out result);

        /// <summary>
        /// AnimatorStateMachine配下の単一オブジェクト(State/StateMachine/Transition等)を、その親レイヤーを祖先としてコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <param name="parentLayer">objが属している親AnimatorControllerLayer。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static AnimatorCopyClipSet Copy(object obj, AnimatorControllerLayer parentLayer)
        {
            if (!TryCopy(obj, parentLayer, out AnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// AnimatorStateMachine配下の複数オブジェクトを、その親レイヤーを祖先としてまとめてコピーします。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <param name="parentLayer">objsが属している親AnimatorControllerLayer。</param>
        /// <param name="result">成功した場合はコピー結果のAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(IEnumerable<object> objs, AnimatorControllerLayer parentLayer, out AnimatorCopyClipSet result)
            => TryCopy(objs, parentLayer.stateMachine, out result);

        /// <summary>
        /// AnimatorStateMachine配下の複数オブジェクトを、その親レイヤーを祖先としてまとめてコピーします。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <param name="parentLayer">objsが属している親AnimatorControllerLayer。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static AnimatorCopyClipSet Copy(IEnumerable<object> objs, AnimatorControllerLayer parentLayer)
        {
            if (!TryCopy(objs, parentLayer, out AnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// AnimatorStateMachine配下の単一オブジェクト(State/StateMachine/Transition等)を、指定した祖先AnimatorStateMachineを基準にコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <param name="ancestorStateMachine">objの祖先となるAnimatorStateMachine。</param>
        /// <param name="result">成功した場合はコピー結果のAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(object obj, AnimatorStateMachine ancestorStateMachine, out AnimatorCopyClipSet result)
            => TryCopy(new[] { obj }, ancestorStateMachine, out result);

        /// <summary>
        /// AnimatorStateMachine配下の単一オブジェクト(State/StateMachine/Transition等)を、指定した祖先AnimatorStateMachineを基準にコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <param name="ancestorStateMachine">objの祖先となるAnimatorStateMachine。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static AnimatorCopyClipSet Copy(object obj, AnimatorStateMachine ancestorStateMachine)
        {
            if (!TryCopy(obj, ancestorStateMachine, out AnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// AnimatorStateMachine配下の複数オブジェクトを、指定した祖先AnimatorStateMachineを基準にまとめてコピーします。
        /// 対象が全てancestorStateMachineの子孫でない場合は失敗します。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <param name="ancestorStateMachine">objsの祖先となるAnimatorStateMachine。</param>
        /// <param name="result">成功した場合はコピー結果のAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine, out AnimatorCopyClipSet result)
        {
            AnimatorCopyClipSet clipSet = new(objs, ancestorStateMachine);
            if (!clipSet.Type.IsInStateMachineCategory())
            {
                result = null;
                return false;
            }
            result = clipSet;
            return true;
        }

        /// <summary>
        /// AnimatorStateMachine配下の複数オブジェクトを、指定した祖先AnimatorStateMachineを基準にまとめてコピーします。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <param name="ancestorStateMachine">objsの祖先となるAnimatorStateMachine。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static AnimatorCopyClipSet Copy(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine)
        {
            if (!TryCopy(objs, ancestorStateMachine, out AnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// 単一のStateMachineBehaviourをコピーします。
        /// </summary>
        /// <param name="behaviour">コピー対象のStateMachineBehaviour。</param>
        /// <param name="result">成功した場合はコピー結果のAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(StateMachineBehaviour behaviour, out AnimatorCopyClipSet result)
            => TryCopy(new[] { behaviour }, out result);

        /// <summary>
        /// 単一のStateMachineBehaviourをコピーします。
        /// </summary>
        /// <param name="behaviour">コピー対象のStateMachineBehaviour。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static AnimatorCopyClipSet Copy(StateMachineBehaviour behaviour)
        {
            if (!TryCopy(behaviour, out AnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// 複数のStateMachineBehaviourをまとめてコピーします。
        /// </summary>
        /// <param name="behaviours">コピー対象のStateMachineBehaviourの列挙。</param>
        /// <param name="result">成功した場合はコピー結果のAnimatorCopyClipSet、失敗した場合はnull。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryCopy(IEnumerable<StateMachineBehaviour> behaviours, out AnimatorCopyClipSet result)
        {
            AnimatorCopyClipSet clipSet = new(behaviours);
            if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours)
            {
                result = null;
                return false;
            }
            result = clipSet;
            return true;
        }

        /// <summary>
        /// 複数のStateMachineBehaviourをまとめてコピーします。
        /// </summary>
        /// <param name="behaviours">コピー対象のStateMachineBehaviourの列挙。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        /// <exception cref="ArgumentException">コピーに失敗した場合。</exception>
        public static AnimatorCopyClipSet Copy(IEnumerable<StateMachineBehaviour> behaviours)
        {
            if (!TryCopy(behaviours, out AnimatorCopyClipSet result))
            {
                throw new ArgumentException("指定されたオブジェクトが不正です");
            }
            return result;
        }

        /// <summary>
        /// 祖先や親コンテキストの妥当性検証を行わずに、単一のオブジェクトをコピーします。
        /// </summary>
        /// <param name="obj">コピー対象のオブジェクト。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        public static AnimatorCopyClipSet Copy(object obj) => new(obj);

        /// <summary>
        /// 祖先や親コンテキストの妥当性検証を行わずに、複数のオブジェクトをまとめてコピーします。
        /// </summary>
        /// <param name="objs">コピー対象のオブジェクトの列挙。</param>
        /// <returns>コピー結果のAnimatorCopyClipSet。</returns>
        public static AnimatorCopyClipSet Copy(IEnumerable<object> objs) => new(objs);

        /// <summary>
        /// clipSetの内容(Layers種別)を、destAnimatorControllerへ新しいレイヤーとして貼り付けます。
        /// クローンされたAnimationClip/BlendTreeは、animationClipSaveMap/blendTreeSaveMapで指定した保存先に応じてアセット化されます。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。Layers種別である必要があります。</param>
        /// <param name="destAnimatorController">貼り付け先のAnimatorController。</param>
        /// <param name="result">成功した場合は貼り付けられたレイヤーの配列、失敗した場合はnull。</param>
        /// <param name="animationClipSaveMap">クローンされたAnimationClipの保存先パスのマップ。省略可。</param>
        /// <param name="blendTreeSaveMap">クローンされたBlendTreeの保存ポリシーと保存先パスのマップ。省略可。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteLayers(
            AnimatorCopyClipSet clipSet,
            AnimatorController destAnimatorController,
            out AnimatorControllerLayer[] result,
            IReadOnlyDictionary<AnimationClip, string> animationClipSaveMap = null,
            IReadOnlyDictionary<BlendTree, (AnimatorAssetPersistence.BlendTreeSavePolicy Policy, string AssetPath)> blendTreeSaveMap = null)
        {
            result = null;
            if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers)
            {
                return false;
            }

            AnimatorCloner cloner = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                cloner.SetRangeClonePolicy(GetCloneScope(clip), AnimatorCloner.ClonePolicy.Clone);
            }

            foreach (AnimatorControllerLayer layer in destAnimatorController.layers)
            {
                cloner.SetRangeClonePolicyIfAbsent(AnimatorGraphTraversal.ListupObjectsInLayer(layer), AnimatorCloner.ClonePolicy.KeepReference);
            }

            AnimatorControllerLayer[] cloneLayers = cloner.CloneAnimatorControllerLayers(clipSet.Clips.Select(x => (AnimatorControllerLayer)x.Object));

            if (clipSet.ParentController != destAnimatorController)
            {
                foreach (AnimatorControllerLayer cloneLayer in cloneLayers)
                {
                    foreach (ICrossControllerPostProcessor processor in
                        CrossControllerPostProcessorRegistry.Shared.ResolveAll(typeof(AnimatorControllerLayer)))
                    {
                        processor.PostProcess(cloneLayer);
                    }
                }
            }

            string destAssetPath = AssetDatabase.GetAssetPath(destAnimatorController);

            List<AnimatorControllerLayer> layerList = new(destAnimatorController.layers);
            layerList.AddRange(cloneLayers);
            destAnimatorController.layers = layerList.ToArray();

            if (destAssetPath != "")
            {
                cloneLayers.ToList().ForEach(x => AnimatorAssetPersistence.AddObjectToAssetRecursively(x.stateMachine, destAssetPath, new AnimatorAssetPersistence.SaveAssetsContext(cloner, animationClipSaveMap, blendTreeSaveMap)));
            }

            result = cloneLayers;
            return true;
        }

        /// <summary>
        /// clipSetの内容(Layers種別)を、destAnimatorControllerへ新しいレイヤーとして貼り付けます。
        /// クローンされたAnimationClip/BlendTreeは、animationClipSaveMap/blendTreeSaveMapで指定した保存先に応じてアセット化されます。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。Layers種別である必要があります。</param>
        /// <param name="destAnimatorController">貼り付け先のAnimatorController。</param>
        /// <param name="animationClipSaveMap">クローンされたAnimationClipの保存先パスのマップ。省略可。</param>
        /// <param name="blendTreeSaveMap">クローンされたBlendTreeの保存ポリシーと保存先パスのマップ。省略可。</param>
        /// <returns>貼り付けられたレイヤーの配列。</returns>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetがLayers種別でない場合。</exception>
        public static AnimatorControllerLayer[] PasteLayers(
            AnimatorCopyClipSet clipSet,
            AnimatorController destAnimatorController,
            IReadOnlyDictionary<AnimationClip, string> animationClipSaveMap = null,
            IReadOnlyDictionary<BlendTree, (AnimatorAssetPersistence.BlendTreeSavePolicy Policy, string AssetPath)> blendTreeSaveMap = null)
        {
            if (!TryPasteLayers(clipSet, destAnimatorController, out AnimatorControllerLayer[] result, animationClipSaveMap, blendTreeSaveMap))
            {
                ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType.Layers, clipSet.Type);
            }
            return result;
        }

        /// <summary>
        /// clipSetの内容(AnimatorStateMachine配下のオブジェクト)を、destLayer直下のAnimatorStateMachineへ貼り付けます。
        /// 実体はTryPasteIntoStateMachine(destLayer.stateMachine)への委譲です。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。</param>
        /// <param name="destLayer">貼り付け先のAnimatorControllerLayer。</param>
        /// <param name="result">成功した場合は貼り付けられたオブジェクトの配列、失敗した場合はnull。</param>
        /// <param name="animationClipSaveMap">クローンされたAnimationClipの保存先パスのマップ。省略可。</param>
        /// <param name="blendTreeSaveMap">クローンされたBlendTreeの保存ポリシーと保存先パスのマップ。省略可。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteIntoLayer(
            AnimatorCopyClipSet clipSet, AnimatorControllerLayer destLayer,
            out UnityEngine.Object[] result,
            IReadOnlyDictionary<AnimationClip, string> animationClipSaveMap = null,
            IReadOnlyDictionary<BlendTree, (AnimatorAssetPersistence.BlendTreeSavePolicy Policy, string AssetPath)> blendTreeSaveMap = null)
            => TryPasteIntoStateMachine(clipSet, destLayer.stateMachine, out result, animationClipSaveMap, blendTreeSaveMap);

        /// <summary>
        /// clipSetの内容(AnimatorStateMachine配下のオブジェクト)を、destLayer直下のAnimatorStateMachineへ貼り付けます。
        /// 実体はPasteIntoStateMachine(destLayer.stateMachine)への委譲です。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。</param>
        /// <param name="destLayer">貼り付け先のAnimatorControllerLayer。</param>
        /// <param name="animationClipSaveMap">クローンされたAnimationClipの保存先パスのマップ。省略可。</param>
        /// <param name="blendTreeSaveMap">クローンされたBlendTreeの保存ポリシーと保存先パスのマップ。省略可。</param>
        /// <returns>貼り付けられたオブジェクトの配列。</returns>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetがAnimatorStateMachine配下のオブジェクトを表す種別でない場合。</exception>
        public static UnityEngine.Object[] PasteIntoLayer(
            AnimatorCopyClipSet clipSet, AnimatorControllerLayer destLayer,
            IReadOnlyDictionary<AnimationClip, string> animationClipSaveMap = null,
            IReadOnlyDictionary<BlendTree, (AnimatorAssetPersistence.BlendTreeSavePolicy Policy, string AssetPath)> blendTreeSaveMap = null)
            => PasteIntoStateMachine(clipSet, destLayer.stateMachine, animationClipSaveMap, blendTreeSaveMap);

        /// <summary>
        /// clipSetの内容(AnimatorStateMachine配下のオブジェクト)を、destStateMachineへ貼り付けます。
        /// 貼り付け先がコピー元の祖先の子孫である場合はコピー元との参照を保持し、そうでない場合は貼り付け先の子孫のみ参照を保持します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。AnimatorStateMachine配下のオブジェクトを表す種別である必要があります。</param>
        /// <param name="destStateMachine">貼り付け先のAnimatorStateMachine。</param>
        /// <param name="result">成功した場合は実際にアセットへ追加されたオブジェクトの配列、失敗した場合はnull。</param>
        /// <param name="animationClipSaveMap">クローンされたAnimationClipの保存先パスのマップ。省略可。</param>
        /// <param name="blendTreeSaveMap">クローンされたBlendTreeの保存ポリシーと保存先パスのマップ。省略可。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteIntoStateMachine(
            AnimatorCopyClipSet clipSet, AnimatorStateMachine destStateMachine,
            out UnityEngine.Object[] result,
            IReadOnlyDictionary<AnimationClip, string> animationClipSaveMap = null,
            IReadOnlyDictionary<BlendTree, (AnimatorAssetPersistence.BlendTreeSavePolicy Policy, string AssetPath)> blendTreeSaveMap = null)
        {
            result = null;
            if (!clipSet.Type.IsInStateMachineCategory())
            {
                return false;
            }

            HashSet<UnityEngine.Object> inScopeObjs = AnimatorGraphTraversal.ListupObjectsInStateMachine(clipSet.AncestorStateMachine);
            inScopeObjs.Add(clipSet.AncestorStateMachine);

            AnimatorCloner cloner = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                cloner.SetRangeClonePolicy(GetCloneScope(clip), AnimatorCloner.ClonePolicy.Clone);
            }

            // 貼り付け先がコピー元の祖先自身、もしくはその子孫であるかを確認
            if (inScopeObjs.Contains(destStateMachine))
            {
                // 同レイヤー間でのコピペのはずなので、コピー元との参照を保持できる
                cloner.SetRangeClonePolicyIfAbsent(inScopeObjs, AnimatorCloner.ClonePolicy.KeepReference);
            }
            else
            {
                // 同レイヤー間のコピペである保証が無いので貼り付け先及びその子孫のみを参照を保持する
                cloner.SetClonePolicyIfAbsent(destStateMachine, AnimatorCloner.ClonePolicy.KeepReference);
                cloner.SetRangeClonePolicyIfAbsent(AnimatorGraphTraversal.ListupObjectsInStateMachine(destStateMachine), AnimatorCloner.ClonePolicy.KeepReference);
            }

            // クリップとそのデータのクローン
            List<AnimatorCopyClip> cloneChildAnimatorStateClips = new();
            List<AnimatorCopyClip> cloneChildAnimatorStateMachineClips = new();
            List<AnimatorCopyClip> cloneAnimatorTransitionClips = new();
            List<AnimatorCopyClip> cloneAnimatorStateTransitionClips = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                AnimatorCopyClip cloneClip = clip.Clone(cloner);
                if (clip.Type == typeof(ChildAnimatorState)) cloneChildAnimatorStateClips.Add(cloneClip);
                else if (clip.Type == typeof(ChildAnimatorStateMachine)) cloneChildAnimatorStateMachineClips.Add(cloneClip);
                else if (clip.Type == typeof(AnimatorTransition)) cloneAnimatorTransitionClips.Add(cloneClip);
                else if (clip.Type == typeof(AnimatorStateTransition)) cloneAnimatorStateTransitionClips.Add(cloneClip);
            }

            // ペースト処理
            List<UnityEngine.Object> objectsToRecursivelyAdd = new();
            List<UnityEngine.Object> objectsToAdd = new();

            List<ChildAnimatorStateMachine> cloneChildAnimatorStateMachines = cloneChildAnimatorStateMachineClips.Select(x => (ChildAnimatorStateMachine)x.Object).ToList();
            destStateMachine.stateMachines = destStateMachine.stateMachines.Union(cloneChildAnimatorStateMachines).ToArray();
            objectsToRecursivelyAdd.AddRange(cloneChildAnimatorStateMachines.Select(x => x.stateMachine));

            List<ChildAnimatorState> cloneChildAnimatorStates = cloneChildAnimatorStateClips.Select(x => (ChildAnimatorState)x.Object).ToList();
            destStateMachine.states = destStateMachine.states.Union(cloneChildAnimatorStates).ToArray();
            objectsToRecursivelyAdd.AddRange(cloneChildAnimatorStates.Select(x => x.state));

            foreach (AnimatorCopyClip cloneClip in cloneAnimatorTransitionClips)
            {
                AnimatorTransition cloneAT = (AnimatorTransition)cloneClip.Object;
                if (cloneAT.destinationState == null && cloneAT.destinationStateMachine == null && !cloneAT.isExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, out object objPropName))
                {
                    AnimatorCopyClip.ContextValue.PropertyName propName = (AnimatorCopyClip.ContextValue.PropertyName)objPropName;

                    if (propName == AnimatorCopyClip.ContextValue.PropertyName.m_StateMachineTransitions &&
                        cloneClip.TryGetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, out object parent) &&
                        destStateMachine.stateMachines.Select(x => x.stateMachine).Contains(parent))
                    {
                        // 元がm_StateMachineTransitionsに登録されていたものなら同様に設定する
                        AnimatorTransition[] smTranss = destStateMachine.GetStateMachineTransitions((AnimatorStateMachine)parent);
                        AnimatorTransition[] newSMTranss = new List<AnimatorTransition>(smTranss) { cloneAT }.ToArray();
                        destStateMachine.SetStateMachineTransitions((AnimatorStateMachine)parent, newSMTranss);

                        objectsToAdd.Add(cloneAT);
                    }
                    else if (propName == AnimatorCopyClip.ContextValue.PropertyName.m_EntryTransitions)
                    {
                        // 元がEntryTransitionなら同様に登録する
                        destStateMachine.entryTransitions = new List<AnimatorTransition>(destStateMachine.entryTransitions) { cloneAT }.ToArray();

                        objectsToAdd.Add(cloneAT);
                        continue;
                    }
                }
            }

            foreach (AnimatorCopyClip cloneClip in cloneAnimatorStateTransitionClips)
            {
                AnimatorStateTransition cloneAST = (AnimatorStateTransition)cloneClip.Object;
                if (cloneAST.destinationState == null && cloneAST.destinationStateMachine == null && !cloneAST.isExit)
                {
                    // Transition先が設定できていないなら
                    continue;
                }

                if (cloneClip.TryGetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, out object parent) && parent != null)
                {
                    if (parent is AnimatorState parentState)
                    {
                        // 親がStateなら通常のTransitionと解釈
                        if (!parentState.transitions.Contains(cloneAST))
                        {
                            parentState.transitions = new List<AnimatorStateTransition>(parentState.transitions) { cloneAST }.ToArray();

                            objectsToAdd.Add(cloneAST);
                        }
                    }
                    else if (parent is AnimatorStateMachine)
                    {
                        // 親がStateMachineならAnyStateTransitionsと解釈
                        if (!destStateMachine.anyStateTransitions.Contains(cloneAST))
                        {
                            destStateMachine.anyStateTransitions = new List<AnimatorStateTransition>(destStateMachine.anyStateTransitions) { cloneAST }.ToArray();

                            objectsToAdd.Add(cloneAST);
                        }
                    }
                }
                else if (cloneClip.TryGetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, out object propName) &&
                    (AnimatorCopyClip.ContextValue.PropertyName)propName == AnimatorCopyClip.ContextValue.PropertyName.m_AnyStateTransitions)
                {
                    // 親が取得できない(親のClonePolicyがDetachの場合)かつ、
                    // 元のプロパティがAnyStateTransitionだった場合
                    destStateMachine.anyStateTransitions = new List<AnimatorStateTransition>(destStateMachine.anyStateTransitions) { cloneAST }.ToArray();

                    objectsToAdd.Add(cloneAST);
                }
            }

            // アセットのセーブ処理
            string destAssetPath = AssetDatabase.GetAssetPath(destStateMachine);
            List<UnityEngine.Object> pastedObjs = new();

            if (!string.IsNullOrEmpty(destAssetPath))
            {
                AnimatorAssetPersistence.SaveAssetsContext context = new(cloner, animationClipSaveMap, blendTreeSaveMap);
                foreach (UnityEngine.Object obj in objectsToRecursivelyAdd)
                {
                    pastedObjs.AddRange(AnimatorAssetPersistence.AddObjectToAssetRecursively(obj, destAssetPath, context));
                }

                foreach (UnityEngine.Object obj in objectsToAdd)
                {
                    if (AnimatorAssetPersistence.CheckAndAddObjectToAsset(obj, destAssetPath)) { pastedObjs.Add(obj); }
                }
            }

            result = pastedObjs.ToArray();
            return true;
        }

        /// <summary>
        /// clipSetの内容(AnimatorStateMachine配下のオブジェクト)を、destStateMachineへ貼り付けます。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。AnimatorStateMachine配下のオブジェクトを表す種別である必要があります。</param>
        /// <param name="destStateMachine">貼り付け先のAnimatorStateMachine。</param>
        /// <param name="animationClipSaveMap">クローンされたAnimationClipの保存先パスのマップ。省略可。</param>
        /// <param name="blendTreeSaveMap">クローンされたBlendTreeの保存ポリシーと保存先パスのマップ。省略可。</param>
        /// <returns>実際にアセットへ追加されたオブジェクトの配列。</returns>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetがAnimatorStateMachine配下のオブジェクトを表す種別でない場合。</exception>
        public static UnityEngine.Object[] PasteIntoStateMachine(
            AnimatorCopyClipSet clipSet, AnimatorStateMachine destStateMachine,
            IReadOnlyDictionary<AnimationClip, string> animationClipSaveMap = null,
            IReadOnlyDictionary<BlendTree, (AnimatorAssetPersistence.BlendTreeSavePolicy Policy, string AssetPath)> blendTreeSaveMap = null)
        {
            if (!TryPasteIntoStateMachine(clipSet, destStateMachine, out UnityEngine.Object[] result, animationClipSaveMap, blendTreeSaveMap))
            {
                ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType.InStateMachineObjects, clipSet.Type);
            }
            return result;
        }

        /// <summary>
        /// clipSetの内容(Behaviours種別)をクローンし、destStateMachineのbehavioursへ追加します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。Behaviours種別である必要があります。</param>
        /// <param name="destStateMachine">貼り付け先のAnimatorStateMachine。</param>
        /// <param name="result">成功した場合は貼り付けられたStateMachineBehaviourの配列、失敗した場合はnull。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteBehaviours(AnimatorCopyClipSet clipSet, AnimatorStateMachine destStateMachine, out StateMachineBehaviour[] result)
        {
            if (!TryCloneBehaviours(clipSet, out result)) return false;
            destStateMachine.behaviours = destStateMachine.behaviours.Concat(result).ToArray();
            return true;
        }

        /// <summary>
        /// clipSetの内容(Behaviours種別)をクローンし、destStateMachineのbehavioursへ追加します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。Behaviours種別である必要があります。</param>
        /// <param name="destStateMachine">貼り付け先のAnimatorStateMachine。</param>
        /// <returns>貼り付けられたStateMachineBehaviourの配列。</returns>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetがBehaviours種別でない場合。</exception>
        public static StateMachineBehaviour[] PasteBehaviours(AnimatorCopyClipSet clipSet, AnimatorStateMachine destStateMachine)
        {
            if (!TryPasteBehaviours(clipSet, destStateMachine, out StateMachineBehaviour[] result))
            {
                ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours, clipSet.Type);
            }
            return result;
        }

        /// <summary>
        /// clipSetの内容(Behaviours種別)をクローンし、destStateのbehavioursへ追加します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。Behaviours種別である必要があります。</param>
        /// <param name="destState">貼り付け先のAnimatorState。</param>
        /// <param name="result">成功した場合は貼り付けられたStateMachineBehaviourの配列、失敗した場合はnull。</param>
        /// <returns>貼り付けに成功した場合はtrue。</returns>
        public static bool TryPasteBehaviours(AnimatorCopyClipSet clipSet, AnimatorState destState, out StateMachineBehaviour[] result)
        {
            if (!TryCloneBehaviours(clipSet, out result)) return false;
            destState.behaviours = destState.behaviours.Concat(result).ToArray();
            return true;
        }

        /// <summary>
        /// clipSetの内容(Behaviours種別)をクローンし、destStateのbehavioursへ追加します。
        /// </summary>
        /// <param name="clipSet">貼り付け対象のAnimatorCopyClipSet。Behaviours種別である必要があります。</param>
        /// <param name="destState">貼り付け先のAnimatorState。</param>
        /// <returns>貼り付けられたStateMachineBehaviourの配列。</returns>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetがBehaviours種別でない場合。</exception>
        public static StateMachineBehaviour[] PasteBehaviours(AnimatorCopyClipSet clipSet, AnimatorState destState)
        {
            if (!TryPasteBehaviours(clipSet, destState, out StateMachineBehaviour[] result))
            {
                ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours, clipSet.Type);
            }
            return result;
        }

        private static bool TryCloneBehaviours(AnimatorCopyClipSet clipSet, out StateMachineBehaviour[] result)
        {
            result = null;
            if (clipSet.Type != AnimatorCopyClipSet.AnimatorCopyClipSetType.Behaviours)
            {
                return false;
            }

            AnimatorCloner cloner = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                cloner.SetRangeClonePolicy(GetCloneScope(clip), AnimatorCloner.ClonePolicy.Clone);
            }

            List<StateMachineBehaviour> cloneBehaviours = new();
            foreach (AnimatorCopyClip clip in clipSet.Clips)
            {
                if (clip.Object != null)
                {
                    StateMachineBehaviour clone = cloner.CloneStateMachineBehaviour((StateMachineBehaviour)clip.Object);
                    cloneBehaviours.Add(clone);
                }
            }

            result = cloneBehaviours.ToArray();
            return true;
        }

        /// <summary>
        /// clipSet(単一のChildAnimatorStateを表すもの)のAnimatorStateとしての設定値を、destStateへ上書きコピーします。
        /// name/behaviours/transitionsはdestState側の値が維持されます。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のChildAnimatorStateを表す種別である必要があります。</param>
        /// <param name="destState">コピー先のAnimatorState。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettings(AnimatorCopyClipSet clipSet, AnimatorState destState)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out ChildAnimatorState srcChildState)) return false;
            PasteSettings(srcChildState, destState);
            return true;
        }

        /// <summary>
        /// clipSet(単一のChildAnimatorStateを表すもの)のAnimatorStateとしての設定値を、destStateへ上書きコピーします。
        /// name/behaviours/transitionsはdestState側の値が維持されます。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のChildAnimatorStateを表す種別である必要があります。</param>
        /// <param name="destState">コピー先のAnimatorState。</param>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetが単一のChildAnimatorStateを表す種別でない場合。</exception>
        public static void PasteSettings(AnimatorCopyClipSet clipSet, AnimatorState destState)
        {
            if (!TryPasteSettings(clipSet, destState))
            {
                ThrowInvalidClipSetTypeException(typeof(ChildAnimatorState), clipSet.Type);
            }
        }

        private static void PasteSettings(ChildAnimatorState srcChildState, AnimatorState destState) => PasteSettings(srcChildState.state, destState);

        private static void PasteSettings(AnimatorState srcState, AnimatorState destState)
        {
            var backupBehaviours = destState.behaviours;
            var backupName = destState.name;
            var backupTransitions = destState.transitions;

            EditorUtility.CopySerialized(srcState, destState);

            destState.behaviours = backupBehaviours;
            destState.name = backupName;
            destState.transitions = backupTransitions;
        }

        /// <summary>
        /// clipSet(単一のAnimatorTransitionを表すもの)のhideFlags/mute/solo設定を、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettings(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out AnimatorTransition srcTransition)) return false;
            PasteSettings(srcTransition, destTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のAnimatorTransitionを表すもの)のhideFlags/mute/solo設定を、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetが単一のAnimatorTransitionを表す種別でない場合。</exception>
        public static void PasteSettings(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition)
        {
            if (!TryPasteSettings(clipSet, destTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(AnimatorTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcTransitionのhideFlags/mute/solo設定を、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcTransition">コピー元のAnimatorTransition。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        public static void PasteSettings(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            destTransition.hideFlags = srcTransition.hideFlags;
            destTransition.mute = srcTransition.mute;
            destTransition.solo = srcTransition.solo;
        }

        /// <summary>
        /// clipSet(単一のAnimatorTransitionを表すもの)のconditionsを、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteConditions(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out AnimatorTransition srcTransition)) return false;
            PasteConditions(srcTransition, destTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のAnimatorTransitionを表すもの)のconditionsを、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetが単一のAnimatorTransitionを表す種別でない場合。</exception>
        public static void PasteConditions(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition)
        {
            if (!TryPasteConditions(clipSet, destTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(AnimatorTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcTransitionのconditionsを、destTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcTransition">コピー元のAnimatorTransition。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        public static void PasteConditions(AnimatorTransition srcTransition, AnimatorTransition destTransition) => destTransition.conditions = srcTransition.conditions.ToArray();

        /// <summary>
        /// clipSet(単一のAnimatorTransitionを表すもの)の設定値とconditionsを、まとめてdestTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettingsAndConditions(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out AnimatorTransition srcTransition)) return false;
            PasteSettingsAndConditions(srcTransition, destTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のAnimatorTransitionを表すもの)の設定値とconditionsを、まとめてdestTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorTransitionを表す種別である必要があります。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetが単一のAnimatorTransitionを表す種別でない場合。</exception>
        public static void PasteSettingsAndConditions(AnimatorCopyClipSet clipSet, AnimatorTransition destTransition)
        {
            if (!TryPasteSettingsAndConditions(clipSet, destTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(AnimatorTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcTransitionの設定値とconditionsを、まとめてdestTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcTransition">コピー元のAnimatorTransition。</param>
        /// <param name="destTransition">コピー先のAnimatorTransition。</param>
        public static void PasteSettingsAndConditions(AnimatorTransition srcTransition, AnimatorTransition destTransition)
        {
            PasteSettings(srcTransition, destTransition);
            PasteConditions(srcTransition, destTransition);
        }

        /// <summary>
        /// clipSet(単一のAnimatorStateTransitionを表すもの)の設定値を、destStateTransitionへ上書きコピーします。
        /// conditions/destinationState/destinationStateMachine/isExit/nameはdestStateTransition側の値が維持されます。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettings(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out AnimatorStateTransition srcStateTransition)) return false;
            PasteSettings(srcStateTransition, destStateTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のAnimatorStateTransitionを表すもの)の設定値を、destStateTransitionへ上書きコピーします。
        /// conditions/destinationState/destinationStateMachine/isExit/nameはdestStateTransition側の値が維持されます。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetが単一のAnimatorStateTransitionを表す種別でない場合。</exception>
        public static void PasteSettings(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            if (!TryPasteSettings(clipSet, destStateTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(AnimatorStateTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcStateTransitionの設定値を、destStateTransitionへ上書きコピーします。
        /// conditions/destinationState/destinationStateMachine/isExit/nameはdestStateTransition側の値が維持されます。
        /// </summary>
        /// <param name="srcStateTransition">コピー元のAnimatorStateTransition。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        public static void PasteSettings(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            var backupConditions = destStateTransition.conditions;
            var backupDestinationState = destStateTransition.destinationState;
            var backupDestinationStateMachine = destStateTransition.destinationStateMachine;
            var backupIsExit = destStateTransition.isExit;
            var backupName = destStateTransition.name;

            EditorUtility.CopySerialized(srcStateTransition, destStateTransition);

            destStateTransition.conditions = backupConditions;
            destStateTransition.destinationState = backupDestinationState;
            destStateTransition.destinationStateMachine = backupDestinationStateMachine;
            destStateTransition.isExit = backupIsExit;
            destStateTransition.name = backupName;
        }

        /// <summary>
        /// clipSet(単一のAnimatorStateTransitionを表すもの)のconditionsを、destStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteConditions(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out AnimatorStateTransition srcStateTransition)) return false;
            PasteConditions(srcStateTransition, destStateTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のAnimatorStateTransitionを表すもの)のconditionsを、destStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetが単一のAnimatorStateTransitionを表す種別でない場合。</exception>
        public static void PasteConditions(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            if (!TryPasteConditions(clipSet, destStateTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(AnimatorStateTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcStateTransitionのconditionsを、destStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcStateTransition">コピー元のAnimatorStateTransition。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        public static void PasteConditions(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition) => destStateTransition.conditions = srcStateTransition.conditions.ToArray();

        /// <summary>
        /// clipSet(単一のAnimatorStateTransitionを表すもの)の設定値とconditionsを、まとめてdestStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        /// <returns>コピーに成功した場合はtrue。</returns>
        public static bool TryPasteSettingsAndConditions(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            if (!TryValidateAndGetSingleClipObjectType(clipSet, out AnimatorStateTransition srcStateTransition)) return false;
            PasteSettingsAndConditions(srcStateTransition, destStateTransition);
            return true;
        }

        /// <summary>
        /// clipSet(単一のAnimatorStateTransitionを表すもの)の設定値とconditionsを、まとめてdestStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="clipSet">コピー元のAnimatorCopyClipSet。単一のAnimatorStateTransitionを表す種別である必要があります。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        /// <exception cref="AnimatorCopyClipSetTypeMismatchException">clipSetが単一のAnimatorStateTransitionを表す種別でない場合。</exception>
        public static void PasteSettingsAndConditions(AnimatorCopyClipSet clipSet, AnimatorStateTransition destStateTransition)
        {
            if (!TryPasteSettingsAndConditions(clipSet, destStateTransition))
            {
                ThrowInvalidClipSetTypeException(typeof(AnimatorStateTransition), clipSet.Type);
            }
        }

        /// <summary>
        /// srcStateTransitionの設定値とconditionsを、まとめてdestStateTransitionへ上書きコピーします。
        /// </summary>
        /// <param name="srcStateTransition">コピー元のAnimatorStateTransition。</param>
        /// <param name="destStateTransition">コピー先のAnimatorStateTransition。</param>
        public static void PasteSettingsAndConditions(AnimatorStateTransition srcStateTransition, AnimatorStateTransition destStateTransition)
        {
            PasteSettings(srcStateTransition, destStateTransition);
            PasteConditions(srcStateTransition, destStateTransition);
        }

        private static bool TryValidateAndGetSingleClipObjectType<T>(AnimatorCopyClipSet clipSet, out T result)
        {
            IAnimatorCopyObjectKind kind = AnimatorCopyObjectKindRegistry.Shared.Resolve(typeof(T));
            if (kind != null && clipSet.Type != kind.SingleClipSetType)
            {
                result = default;
                return false;
            }

            result = (T)clipSet.Clips.First().Object;
            return true;
        }

        private static IEnumerable<UnityEngine.Object> GetCloneScope(AnimatorCopyClip clip) =>
            AnimatorCopyObjectKindRegistry.Shared.Resolve(clip.Type)?.GetCloneScope(clip.Object) ?? Array.Empty<UnityEngine.Object>();

        private static void ThrowInvalidClipSetTypeException(Type requestType, AnimatorCopyClipSet.AnimatorCopyClipSetType clipSetType) => throw new AnimatorCopyClipSetTypeMismatchException($"要求された型({requestType.FullName})に対して、ClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{clipSetType})が一致しません");

        private static void ThrowInvalidClipSetTypeException(AnimatorCopyClipSet.AnimatorCopyClipSetType requestClipSetType, AnimatorCopyClipSet.AnimatorCopyClipSetType clipSetType) => throw new AnimatorCopyClipSetTypeMismatchException($"要求されたClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{requestClipSetType})に対して、ClipSetのデータのタイプ({nameof(AnimatorCopyClipSet.AnimatorCopyClipSetType)}.{clipSetType})が一致しません");
    }
}