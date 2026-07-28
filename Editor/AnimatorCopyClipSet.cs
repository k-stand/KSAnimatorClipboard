using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using com.github.k_stand.ksanimatorcopyengine.editor.Copying;

namespace com.github.k_stand.ksanimatorcopyengine.editor
{
    /// <summary>
    /// AnimatorCopyEngine.Copy系メソッドの戻り値として、コピーされたAnimatorController関連オブジェクトの集合を保持します。
    /// </summary>
    public class AnimatorCopyClipSet
    {
        /// <summary>
        /// コピーされた個々のオブジェクトを表すクリップの一覧を取得します。
        /// </summary>
        public ReadOnlyCollection<AnimatorCopyClip> Clips { get; private set; }

        private AnimatorCopyClipSetType type = AnimatorCopyClipSetType.None;

        /// <summary>
        /// Clipsの内容から判定される、このAnimatorCopyClipSetの種別を取得します。
        /// Paste系メソッドはこの値を見て、自身が要求している種別と一致するかどうかを検証します。
        /// </summary>
        public AnimatorCopyClipSetType Type
        {
            get
            {
                if (type == AnimatorCopyClipSetType.None) { type = GetClipSetType(); }
                return type;
            }
        }

        /// <summary>
        /// Layers種別でコピーされた場合の、コピー元レイヤーが属していた親AnimatorControllerを取得します。
        /// レイヤーが指定された親AnimatorControllerに含まれていなかった場合や、Layers種別以外の場合はnullになります。
        /// </summary>
        public AnimatorController ParentController { get; private set; }

        /// <summary>
        /// AnimatorStateMachine配下のオブジェクトとしてコピーされた場合の、コピー元の共通の祖先AnimatorStateMachineを取得します。
        /// コピー対象が指定された祖先の子孫でなかった場合や、対象外の種別の場合はnullになります。
        /// </summary>
        public AnimatorStateMachine AncestorStateMachine { get; private set; }

        /// <summary>
        /// コピー時に指定した親AnimatorController/祖先AnimatorStateMachineと、実際のコピー対象が一致しなかった場合にtrueになります。
        /// </summary>
        public bool IsAncestorMismatched { get; private set; }

        internal AnimatorCopyClipSet(AnimatorControllerLayer layer, AnimatorController parentController) : this(new AnimatorControllerLayer[] { layer }, parentController) { }

        internal AnimatorCopyClipSet(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            ClipSetInit(layers);
            // Type不一致の場合、AncestorSetting/ContextsSettingを行わず未初期化のまま返す。
            // 妥当性はAnimatorCopyEngine.TryCopyが呼び出し後にTypeを見て判定する前提であり、
            // このコンストラクタを直接使う場合はTypeを確認してから使うこと。
            if (Type != AnimatorCopyClipSetType.Layers) return;

            AncestorSetting(layers, parentController);
            ContextsSetting(parentController);
        }

        internal AnimatorCopyClipSet(object obj, AnimatorControllerLayer parentLayer) : this(new object[] { obj }, parentLayer.stateMachine) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs, AnimatorControllerLayer parentLayer) : this(objs, parentLayer.stateMachine) { }

        internal AnimatorCopyClipSet(object obj, AnimatorStateMachine ancestorStateMachine) : this(new object[] { obj }, ancestorStateMachine) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine)
        {
            ClipSetInit(objs);
            // Type不一致の場合、AncestorSetting/ContextsSettingを行わず未初期化のまま返す。
            // 妥当性はAnimatorCopyEngine.TryCopyが呼び出し後にTypeを見て判定する前提であり、
            // このコンストラクタを直接使う場合はTypeを確認してから使うこと。
            if (!Type.IsInStateMachineCategory()) return;

            AncestorSetting(objs, ancestorStateMachine);
            ContextsSetting(ancestorStateMachine);
        }

        internal AnimatorCopyClipSet(StateMachineBehaviour behaviour) : this(new StateMachineBehaviour[] { behaviour }) { }

        internal AnimatorCopyClipSet(IEnumerable<StateMachineBehaviour> behaviours)
        {
            ClipSetInit(behaviours);
            // Type不一致の場合、ContextsSettingを行わず未初期化のまま返す。
            // 妥当性はAnimatorCopyEngine.TryCopyが呼び出し後にTypeを見て判定する前提であり、
            // このコンストラクタを直接使う場合はTypeを確認してから使うこと。
            if (Type != AnimatorCopyClipSetType.Behaviours) return;

            ContextsSetting();
        }

        internal AnimatorCopyClipSet(object obj) : this(new object[] { obj }) { }

        internal AnimatorCopyClipSet(IEnumerable<object> objs)
        {
            ClipSetInit(objs);

            ContextsSetting();
        }

        private AnimatorCopyClipSet(
            IEnumerable<AnimatorCopyClip> clips,
            AnimatorController parentController,
            AnimatorStateMachine ancestorStateMachine)
        {
            Clips = new(clips.ToList());
            ParentController = parentController;
            AncestorStateMachine = ancestorStateMachine;
        }

        /// <summary>
        /// Clipsに含まれる全てのオブジェクトを複製した、新しいAnimatorCopyClipSetを作成します。
        /// </summary>
        /// <returns>複製されたAnimatorCopyClipSet。</returns>
        public AnimatorCopyClipSet Clone() => Clone(out var _);

        /// <summary>
        /// Clipsに含まれる全てのオブジェクトを複製した、新しいAnimatorCopyClipSetを作成します。
        /// </summary>
        /// <param name="clonedMap">複製元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>複製されたAnimatorCopyClipSet。</returns>
        public AnimatorCopyClipSet Clone(out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap)
        {
            AnimatorCloner cloner = new() { DefaultPolicy = AnimatorCloner.ClonePolicy.KeepReference };
            cloner.SetRangeClonePolicy(Clips.SelectMany(GetCloneScope), AnimatorCloner.ClonePolicy.Clone);
            AnimatorCopyClipSet cloneClipSet = Clone(cloner);
            clonedMap = cloner.GetClonedMap();
            return cloneClipSet;
        }

        private static IEnumerable<UnityEngine.Object> GetCloneScope(AnimatorCopyClip clip) =>
            AnimatorCopyObjectKindRegistry.Shared.Resolve(clip.Type)?.GetCloneScope(clip.Object) ?? Array.Empty<UnityEngine.Object>();

        /// <summary>
        /// 指定したAnimatorClonerを使ってClipsに含まれる全てのオブジェクトを複製した、新しいAnimatorCopyClipSetを作成します。
        /// クローン対象の範囲やClonePolicyの設定は、呼び出し側が事前にclonerへ設定しておく必要があります。
        /// </summary>
        /// <param name="cloner">クローンに使用するAnimatorCloner。</param>
        /// <returns>複製されたAnimatorCopyClipSet。</returns>
        public AnimatorCopyClipSet Clone(AnimatorCloner cloner)
        {
            List<AnimatorCopyClip> cloneClips = new();
            foreach (AnimatorCopyClip clip in Clips)
            {
                AnimatorCopyClip cloneClip = clip.Clone(cloner);
                cloneClips.Add(cloneClip);
            }

            AnimatorController assignParentController = cloner.TryCloneObject(ParentController, out object cloneParentController) ? (AnimatorController)cloneParentController : ParentController;
            AnimatorStateMachine assignAncestorStateMachine = cloner.TryCloneObject(AncestorStateMachine, out object cloneAncestorStateMachine) ? (AnimatorStateMachine)cloneAncestorStateMachine : AncestorStateMachine;
            AnimatorCopyClipSet cloneClipSet = new(cloneClips, assignParentController, assignAncestorStateMachine);

            return cloneClipSet;
        }

        private void ClipSetInit(IEnumerable<object> objs)
        {
            Clips = new(objs.Select(o => CreateClipBase(o)).ToList());
        }

        private void AncestorSetting(IEnumerable<AnimatorControllerLayer> layers, AnimatorController parentController)
        {
            if (parentController != null)
            {
                if (layers.All(l => parentController.layers.Any(pcl => l.stateMachine == pcl.stateMachine)))
                {
                    ParentController = parentController;
                }
                else
                {
                    IsAncestorMismatched = true;
                    Debug.LogWarning("指定された親AnimatorControllerに含まれていないAnimatorControllerLayerがコピーされました。\n親AnimatorControllerは未指定状態になります");
                }
            }
        }

        private void AncestorSetting(IEnumerable<object> objs, AnimatorStateMachine ancestorStateMachine)
        {
            if (ancestorStateMachine != null)
            {
                HashSet<object> descendantObjs = new() { ancestorStateMachine };
                descendantObjs.UnionWith(AnimatorGraphTraversal.ListupObjectsInStateMachine(ancestorStateMachine));

                if (
                    objs.All(o => descendantObjs.Contains(o) ||
                        (o is ChildAnimatorState cas && descendantObjs.Contains(cas.state)) ||
                        (o is ChildAnimatorStateMachine casm && descendantObjs.Contains(casm.stateMachine))
                    )
                )
                {
                    AncestorStateMachine = ancestorStateMachine;
                }
                else
                {
                    IsAncestorMismatched = true;
                    Debug.LogWarning("指定されたAnimatorStateMachineの子孫に含まれていないオブジェクトがコピーされました。\n先祖AnimatorStateMachineは未指定状態になります");
                }
            }
        }

        private void ContextsSetting(AnimatorController parentController)
        {
            HashSet<object> relatedObjs = new(parentController.layers) { parentController };
            ContextsSettingInternal(relatedObjs);
        }

        private void ContextsSetting(AnimatorStateMachine ancestorStateMachine)
        {
            HashSet<object> relatedObjs = new() { ancestorStateMachine };
            relatedObjs.UnionWith(AnimatorGraphTraversal.ListupObjectsInStateMachine(ancestorStateMachine));
            ContextsSettingInternal(relatedObjs);
        }

        private void ContextsSetting()
        {
            ContextsSettingInternal(Array.Empty<object>());
        }

        private void ContextsSettingInternal(IEnumerable<object> relatedObjs)
        {
            // Clipsを型ごとに仕分ける
            var groupedClips = Clips.GroupBy(c => c.Type);
            //AnimatorCopyClip[] stateClips = groupedClips.Where(g => g.Key == typeof(ChildAnimatorState)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();
            //AnimatorCopyClip[] stateMachineClips = groupedClips.Where(g => g.Key == typeof(ChildAnimatorStateMachine)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();
            AnimatorCopyClip[] transitionClips = groupedClips.Where(g => g.Key == typeof(AnimatorTransition)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();
            AnimatorCopyClip[] stateTransitionClips = groupedClips.Where(g => g.Key == typeof(AnimatorStateTransition)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();
            AnimatorCopyClip[] layerClips = groupedClips.Where(g => g.Key == typeof(AnimatorControllerLayer)).SelectMany(g => g.Select(cb => (AnimatorCopyClip)cb)).ToArray();

            // Clipsの中身を取り出す
            IEnumerable<object> clipObjs = Clips.Select(static x => x.Object switch
                {
                    ChildAnimatorState cas => cas.state,
                    ChildAnimatorStateMachine csam => csam.stateMachine,
                    _ => x.Object,
                });
            // Clipsの中身を含めた全ての関連性のあるオブジェクト
            HashSet<object> totalRelatedObjHashSet = clipObjs.Union(relatedObjs).ToHashSet();
            var groupedObjs = totalRelatedObjHashSet.GroupBy(c => c.GetType());

            AnimatorState[] stateObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorState)).SelectMany(g => g.Select(cb => (AnimatorState)cb)).ToArray();
            AnimatorStateMachine[] stateMachineObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorStateMachine)).SelectMany(g => g.Select(cb => (AnimatorStateMachine)cb)).ToArray();
            //AnimatorTransition[] transitionObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorTransition)).SelectMany(g => g.Select(cb => (AnimatorTransition)cb)).ToArray();
            //AnimatorStateTransition[] stateTransitionObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorStateTransition)).SelectMany(g => g.Select(cb => (AnimatorStateTransition)cb)).ToArray();
            AnimatorController[] animatorControllerObjs = groupedObjs.Where(g => g.Key == typeof(AnimatorController)).SelectMany(g => g.Select(cb => (AnimatorController)cb)).ToArray();

            // 各Clipsに関連のあるオブジェクトや情報をコンテキストとして登録する
            // AnimatorTransition → (親StateMachine, PropertyName) のインデックス
            var transitionParentIndex = new Dictionary<AnimatorTransition, (AnimatorStateMachine Parent, AnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (AnimatorStateMachine asm in stateMachineObjs)
            {
                foreach (AnimatorTransition at in asm.entryTransitions)
                {
                    transitionParentIndex[at] = (asm, AnimatorCopyClip.ContextValue.PropertyName.m_EntryTransitions);
                }
                foreach (ChildAnimatorStateMachine csm in asm.stateMachines)
                {
                    foreach (AnimatorTransition at in asm.GetStateMachineTransitions(csm.stateMachine))
                    {
                        transitionParentIndex[at] = (asm, AnimatorCopyClip.ContextValue.PropertyName.m_StateMachineTransitions);
                    }
                }
            }

            // AnimatorStateTransition → 親(StateMachine or State) のインデックス
            var stateTransitionParentIndex = new Dictionary<AnimatorStateTransition, (UnityEngine.Object Parent, AnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (AnimatorStateMachine asm in stateMachineObjs)
            {
                foreach (AnimatorStateTransition ast in asm.anyStateTransitions)
                {
                    stateTransitionParentIndex[ast] = (asm, AnimatorCopyClip.ContextValue.PropertyName.m_AnyStateTransitions);
                }
            }
            foreach (AnimatorState state in stateObjs)
            {
                foreach (AnimatorStateTransition ast in state.transitions)
                {
                    stateTransitionParentIndex[ast] = (state, AnimatorCopyClip.ContextValue.PropertyName.m_Transitions);
                }
            }

            // AnimatorControllerLayer → 親AnimatorController のインデックス
            var layerParentIndex = new Dictionary<AnimatorControllerLayer, (AnimatorController Parent, AnimatorCopyClip.ContextValue.PropertyName PropertyName)>();
            foreach (AnimatorController ac in animatorControllerObjs)
            {
                foreach (AnimatorControllerLayer acl in ac.layers)
                {
                    layerParentIndex[acl] = (ac, AnimatorCopyClip.ContextValue.PropertyName.m_AnimatorLayers);
                }
            }


            foreach (AnimatorCopyClip transitionClip in transitionClips)
            {
                if (transitionParentIndex.TryGetValue((AnimatorTransition)transitionClip.Object, out var entry))
                {
                    transitionClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    transitionClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }

            foreach (AnimatorCopyClip stateTransitionClip in stateTransitionClips)
            {
                if (stateTransitionParentIndex.TryGetValue((AnimatorStateTransition)stateTransitionClip.Object, out var entry))
                {
                    stateTransitionClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    stateTransitionClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }

            foreach (AnimatorCopyClip layerClip in layerClips)
            {
                if (layerParentIndex.TryGetValue((AnimatorControllerLayer)layerClip.Object, out var entry))
                {
                    layerClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.Parent, entry.Parent);
                    layerClip.SetAnimatorContext(AnimatorCopyClip.ContextKey.PropertyName, entry.PropertyName);
                }
            }
        }

        private AnimatorCopyClip CreateClipBase(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            object normalized = AnimatorCopyObjectKindRegistry.Shared.Normalize(obj);
            if (AnimatorCopyObjectKindRegistry.Shared.Resolve(normalized.GetType()) == null)
            {
                throw new ArgumentException($"コピー対象として未対応の型です: {normalized.GetType().FullName}", nameof(obj));
            }

            return new AnimatorCopyClip(normalized);
        }

        private AnimatorCopyClipSetType GetClipSetType()
        {
            IAnimatorCopyObjectKind[] kinds = Clips
                .Select(x => AnimatorCopyObjectKindRegistry.Shared.Resolve(x.Type))
                .ToArray();

            if (Array.Exists(kinds, k => k == null))
            {
                return AnimatorCopyClipSetType.Other;
            }

            IAnimatorCopyObjectKind[] distinctKinds = kinds.Distinct().ToArray();

            if (distinctKinds.Length == 1)
            {
                IAnimatorCopyObjectKind kind = distinctKinds[0];
                if (kind.IsInStateMachineObject && Clips.Count >= 2)
                {
                    return AnimatorCopyClipSetType.InStateMachineObjects;
                }

                return kind.SingleClipSetType;
            }

            if (distinctKinds.Length > 0 && Array.TrueForAll(distinctKinds, k => k.IsInStateMachineObject))
            {
                return AnimatorCopyClipSetType.InStateMachineObjects;
            }

            return AnimatorCopyClipSetType.Other;
        }

        /// <summary>
        /// AnimatorCopyClipSetが表しているコピー対象オブジェクトの種別です。
        /// </summary>
        public enum AnimatorCopyClipSetType
        {
            /// <summary>種別が未計算であることを示す内部初期状態。Typeプロパティがこの値を返すことはありません。</summary>
            None,
            /// <summary>AnimatorControllerLayerのコピー。</summary>
            Layers,
            /// <summary>AnimatorTransitionのコピー。</summary>
            Transition,
            /// <summary>AnimatorStateTransitionのコピー。</summary>
            StateTransition,
            /// <summary>ChildAnimatorStateのコピー。</summary>
            ChildState,
            /// <summary>ChildAnimatorStateMachineのコピー。</summary>
            ChildStateMachine,
            /// <summary>AnimatorStateMachine配下のオブジェクトを、複数件または複数種別にまたがってコピーした場合。</summary>
            InStateMachineObjects,
            /// <summary>StateMachineBehaviourのコピー。</summary>
            Behaviours,
            /// <summary>上記いずれにも該当しない、またはコピー対象として未対応の型を含む場合。</summary>
            Other
        }
    }
}