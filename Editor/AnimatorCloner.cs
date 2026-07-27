using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.editor
{
    /// <summary>
    /// AnimatorController関連オブジェクトのクローンを行うエンジンです。
    /// オブジェクトごとにClonePolicyを設定することで、クローンする/元の参照を保持する/切り離す(null化)を制御できます。
    /// </summary>
    public class AnimatorCloner
    {
        /// <summary>
        /// 個別にClonePolicyが設定されていないオブジェクトに適用される既定のポリシーを取得または設定します。初期値はDetachです。
        /// </summary>
        public ClonePolicy DefaultPolicy { get; set; } = ClonePolicy.Detach;

        private readonly Dictionary<UnityEngine.Object, ClonePolicy> _policyMap = new();

        private readonly Dictionary<UnityEngine.Object, UnityEngine.Object> _parentMap = new();

        private readonly Dictionary<UnityEngine.Object, UnityEngine.Object> _cloneMap = new();

        /// <summary>
        /// このクラスがクローン対象として認識しているAnimatorController関連の型の一覧です。
        /// </summary>
        public static readonly IReadOnlyCollection<Type> CloneableTypes = new HashSet<Type>
        {
            typeof(AnimatorController),
            typeof(AnimatorControllerParameter),
            typeof(AnimatorControllerLayer),
            typeof(ChildAnimatorStateMachine),
            typeof(AnimatorStateMachine),
            typeof(ChildAnimatorState),
            typeof(AnimatorState),
            typeof(AnimatorTransition),
            typeof(AnimatorStateTransition),
            typeof(AnimatorCondition),
            typeof(StateMachineBehaviour),
            typeof(AnimationClip),
            typeof(BlendTree),
        };

        //public Func<string, string> NameTransformer { get; set; } = static origName => string.IsNullOrEmpty(origName) ? "" : origName + " (Clone)";
        /// <summary>
        /// クローン後のオブジェクトの名前を、元の名前から生成する関数を取得または設定します。既定では元の名前をそのまま使用します。
        /// </summary>
        public Func<string, string> NameTransformer { get; set; } = static origName => string.IsNullOrEmpty(origName) ? "" : origName;

        /// <summary>
        /// 指定したオブジェクトのClonePolicyを設定します。あわせてそのオブジェクトの子要素を、ポリシー継承のために内部登録します。
        /// </summary>
        /// <param name="obj">ポリシーを設定するオブジェクト。nullの場合は何もしません。</param>
        /// <param name="policy">設定するClonePolicy。</param>
        public void SetClonePolicy(UnityEngine.Object obj, ClonePolicy policy)
        {
            if (obj == null) return;
            _policyMap[obj] = policy;
            RegisterChildrenRecursively(obj);
        }

        /// <summary>
        /// 複数のオブジェクトに対して、まとめてSetClonePolicyを行います。
        /// </summary>
        /// <param name="objs">ポリシーを設定するオブジェクトの列挙。</param>
        /// <param name="policy">設定するClonePolicy。</param>
        public void SetRangeClonePolicy(IEnumerable<UnityEngine.Object> objs, ClonePolicy policy)
        {
            foreach (UnityEngine.Object obj in objs) SetClonePolicy(obj, policy);
        }

        /// <summary>
        /// 指定したオブジェクトに、まだ設定されていないか、より優先度の低いClonePolicyしか設定されていない場合にのみ、ClonePolicyを設定します。
        /// 既に同等以上の優先度のポリシーが設定済みの場合は何もしません。
        /// </summary>
        /// <param name="obj">ポリシーを設定するオブジェクト。nullの場合は何もしません。</param>
        /// <param name="policy">設定するClonePolicy。</param>
        public void SetClonePolicyIfAbsent(UnityEngine.Object obj, ClonePolicy policy)
        {
            if (obj != null && (!_policyMap.TryGetValue(obj, out ClonePolicy current) || current < policy))
            {
                _policyMap[obj] = policy;
                RegisterChildrenRecursively(obj);
            }
        }

        /// <summary>
        /// 複数のオブジェクトに対して、まとめてSetClonePolicyIfAbsentを行います。
        /// </summary>
        /// <param name="objs">ポリシーを設定するオブジェクトの列挙。</param>
        /// <param name="policy">設定するClonePolicy。</param>
        public void SetRangeClonePolicyIfAbsent(IEnumerable<UnityEngine.Object> objs, ClonePolicy policy)
        {
            foreach (UnityEngine.Object obj in objs) SetClonePolicyIfAbsent(obj, policy);
        }

        /// <summary>
        /// 指定したオブジェクトに個別設定されているClonePolicyを削除します。以降はDefaultPolicyまたは親からの継承が適用されます。
        /// </summary>
        /// <param name="obj">設定を削除するオブジェクト。</param>
        public void RemoveClonePolicy(UnityEngine.Object obj) => _policyMap.Remove(obj);

        /// <summary>
        /// 指定したオブジェクトに個別設定されているClonePolicyの取得を試みます。親からの継承やDefaultPolicyは考慮しません。
        /// </summary>
        /// <param name="obj">ClonePolicyを取得するオブジェクト。</param>
        /// <param name="policy">個別設定されている場合はそのClonePolicy、されていない場合は既定値。</param>
        /// <returns>個別設定が存在する場合はtrue。</returns>
        public bool TryGetClonePolicy(UnityEngine.Object obj, out ClonePolicy policy) => _policyMap.TryGetValue(obj, out policy);

        private ClonePolicy GetClonePolicy(UnityEngine.Object obj)
        {
            // 手動設定があればそれを使う
            if (_policyMap.TryGetValue(obj, out ClonePolicy policy)) return policy;

            // 親を辿って継承
            if (_parentMap.TryGetValue(obj, out UnityEngine.Object parent))
                return GetClonePolicy(parent); // 再帰

            // どこにも設定がなければDefaultPolicy
            return DefaultPolicy;
        }

        /// <summary>
        /// 個別設定されている全てのClonePolicyのコピーを取得します。
        /// </summary>
        /// <returns>オブジェクトをキー、設定されているClonePolicyを値とするマップ。</returns>
        public Dictionary<UnityEngine.Object, ClonePolicy> GetAllClonePolicy() => new(_policyMap);

        private void RegisterChildrenRecursively(UnityEngine.Object obj)
        {
            switch (obj)
            {
                case AnimatorController ac:
                    foreach (AnimatorControllerLayer layer in ac.layers)
                    {
                        if (layer.stateMachine != null && !(_parentMap.TryGetValue(layer.stateMachine, out UnityEngine.Object registeredParent) && registeredParent == ac))
                        {
                            _parentMap[layer.stateMachine] = ac;
                            RegisterChildrenRecursively(layer.stateMachine);
                        }
                    }
                    break;
                case AnimatorStateMachine asm:
                    foreach (AnimatorStateTransition ast in asm.anyStateTransitions)
                    {
                        if (ast != null && !(_parentMap.TryGetValue(ast, out UnityEngine.Object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[ast] = asm;
                            RegisterChildrenRecursively(ast);
                        }
                    }
                    foreach (AnimatorTransition at in asm.entryTransitions)
                    {
                        if (at != null && !(_parentMap.TryGetValue(at, out UnityEngine.Object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[at] = asm;
                            RegisterChildrenRecursively(at);
                        }
                    }
                    foreach (ChildAnimatorState cas in asm.states)
                    {
                        if (cas.state != null && !(_parentMap.TryGetValue(cas.state, out UnityEngine.Object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[cas.state] = asm;
                            RegisterChildrenRecursively(cas.state);
                        }
                    }
                    foreach (ChildAnimatorStateMachine casm in asm.stateMachines)
                    {
                        if (casm.stateMachine != null && !(_parentMap.TryGetValue(casm.stateMachine, out UnityEngine.Object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[casm.stateMachine] = asm;
                            RegisterChildrenRecursively(casm.stateMachine);
                        }
                    }
                    foreach (StateMachineBehaviour behaviour in asm.behaviours)
                    {
                        if (behaviour != null && !(_parentMap.TryGetValue(behaviour, out UnityEngine.Object registeredParent) && registeredParent == asm))
                        {
                            _parentMap[behaviour] = asm;
                        }
                    }
                    break;
                case AnimatorState state:
                    foreach (AnimatorStateTransition transition in state.transitions)
                    {
                        if (transition != null && !(_parentMap.TryGetValue(transition, out UnityEngine.Object registeredParent) && registeredParent == state))
                        {
                            _parentMap[transition] = state;
                        }
                    }
                    foreach (StateMachineBehaviour behaviour in state.behaviours)
                    {
                        if (behaviour != null && !(_parentMap.TryGetValue(behaviour, out UnityEngine.Object registeredParent) && registeredParent == state))
                        {
                            _parentMap[behaviour] = state;
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// これまでにクローンされたオブジェクトの、元オブジェクトから複製後オブジェクトへのマップを取得します。
        /// ClonePolicy.KeepReference等により元と同一のまま返されたものは含まれません。
        /// </summary>
        /// <returns>元オブジェクトをキー、複製後オブジェクトを値とするマップ。</returns>
        public Dictionary<UnityEngine.Object, UnityEngine.Object> GetClonedMap() => _cloneMap.Where(kvp => kvp.Key != kvp.Value).ToDictionary(x => x.Key, x => x.Value);

        /// <summary>
        /// これまでにクローンされたAnimationClipのみを抽出した、元クリップから複製後クリップへのマップを取得します。
        /// </summary>
        /// <returns>元のAnimationClipをキー、複製後のAnimationClipを値とするマップ。</returns>
        public Dictionary<AnimationClip, AnimationClip> GetClonedAnimationClip() => _cloneMap
            .Where(kvp => kvp.Key is AnimationClip origClip &&
                          kvp.Value is AnimationClip cloneClip &&
                          origClip != cloneClip)
            .ToDictionary(kvp => (AnimationClip)kvp.Key, kvp => (AnimationClip)kvp.Value);

        /// <summary>
        /// これまでにクローンされたBlendTreeのみを抽出した、元ツリーから複製後ツリーへのマップを取得します。
        /// </summary>
        /// <returns>元のBlendTreeをキー、複製後のBlendTreeを値とするマップ。</returns>
        public Dictionary<BlendTree, BlendTree> GetClonedBlendTrees() => _cloneMap
            .Where(kvp => kvp.Key is BlendTree origTree &&
                          kvp.Value is BlendTree cloneTree &&
                          origTree != cloneTree)
            .ToDictionary(kvp => (BlendTree)kvp.Key, kvp => (BlendTree)kvp.Value);

        /// <summary>
        /// GetClonedMap()の内容のうち、指定した型に一致する元オブジェクトと複製後オブジェクトの組み合わせごとにactionを呼び出します。
        /// </summary>
        /// <param name="action">元オブジェクトと複製後オブジェクトを引数に呼び出されるコールバック。</param>
        public void ForEachCloned<T>(Action<T, T> action) where T : UnityEngine.Object
            => ForEachCloned(GetClonedMap(), action);

        /// <summary>
        /// 指定した元オブジェクトから複製後オブジェクトへのマップのうち、指定した型に一致する組み合わせごとにactionを呼び出します。
        /// </summary>
        /// <param name="clonedMap">元オブジェクトをキー、複製後オブジェクトを値とするマップ。</param>
        /// <param name="action">元オブジェクトと複製後オブジェクトを引数に呼び出されるコールバック。</param>
        public static void ForEachCloned<T>(IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> clonedMap, Action<T, T> action) where T : UnityEngine.Object
        {
            foreach (KeyValuePair<UnityEngine.Object, UnityEngine.Object> kvp in clonedMap)
            {
                if (kvp.Key is T orig && kvp.Value is T clone && orig != clone)
                {
                    action(orig, clone);
                }
            }
        }

        private TResult CloneWithMap<TArg, TResult>(TArg orig, Func<TArg, TResult> cloneInternal, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap)
        {
            TResult clone = cloneInternal(orig);
            clonedMap = GetClonedMap();
            return clone;
        }

        /// <summary>
        /// オブジェクトの実際の型を判定してクローンします。CloneableTypesに含まれない型の場合はnullを返します。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public object CloneObject(object orig) => CloneObject(orig, out _);

        /// <summary>
        /// オブジェクトの実際の型を判定してクローンします。CloneableTypesに含まれない型の場合はnullを返します。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public object CloneObject(object orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneObjectInternal, out clonedMap);

        /// <summary>
        /// AnimatorControllerをクローンします。parameters/layersを含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorController。</param>
        /// <returns>クローンされたAnimatorController。</returns>
        public AnimatorController CloneAnimatorController(AnimatorController orig) => CloneAnimatorController(orig, out _);

        /// <summary>
        /// AnimatorControllerをクローンします。parameters/layersを含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorController。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたAnimatorController。</returns>
        public AnimatorController CloneAnimatorController(AnimatorController orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneAnimatorControllerInternal, out clonedMap);

        /// <summary>
        /// 複数のAnimatorControllerLayerをまとめてクローンします。
        /// レイヤー自体は常に新規クローンされます(ClonePolicyの対象外)。内部のstateMachine以下はClonePolicyに従います。
        /// </summary>
        /// <param name="origs">クローン元のレイヤーの列挙。</param>
        /// <returns>クローンされたレイヤーの配列。</returns>
        public AnimatorControllerLayer[] CloneAnimatorControllerLayers(IEnumerable<AnimatorControllerLayer> origs) => CloneAnimatorControllerLayers(origs, out _);

        /// <summary>
        /// 複数のAnimatorControllerLayerをまとめてクローンします。
        /// レイヤー自体は常に新規クローンされます(ClonePolicyの対象外)。内部のstateMachine以下はClonePolicyに従います。
        /// </summary>
        /// <param name="origs">クローン元のレイヤーの列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたレイヤーの配列。</returns>
        public AnimatorControllerLayer[] CloneAnimatorControllerLayers(IEnumerable<AnimatorControllerLayer> origs, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(origs, CloneAnimatorControllerLayersInternal, out clonedMap);

        /// <summary>
        /// AnimatorControllerLayerをクローンします。stateMachineを含め再帰的に複製されます。
        /// レイヤー自体は常に新規クローンされます(ClonePolicyの対象外)。内部のstateMachine以下はClonePolicyに従います。
        /// </summary>
        /// <param name="orig">クローン元のレイヤー。</param>
        /// <returns>クローンされたレイヤー。</returns>
        public AnimatorControllerLayer CloneAnimatorControllerLayer(AnimatorControllerLayer orig) => CloneAnimatorControllerLayer(orig, out _);

        /// <summary>
        /// AnimatorControllerLayerをクローンします。stateMachineを含め再帰的に複製されます。
        /// レイヤー自体は常に新規クローンされます(ClonePolicyの対象外)。内部のstateMachine以下はClonePolicyに従います。
        /// </summary>
        /// <param name="orig">クローン元のレイヤー。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたレイヤー。</returns>
        public AnimatorControllerLayer CloneAnimatorControllerLayer(AnimatorControllerLayer orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneAnimatorControllerLayerInternal, out clonedMap);

        /// <summary>
        /// 複数のChildAnimatorStateMachineをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public ChildAnimatorStateMachine[] CloneChildAnimatorStateMachines(IEnumerable<ChildAnimatorStateMachine> origs) => CloneChildAnimatorStateMachines(origs, out _);

        /// <summary>
        /// 複数のChildAnimatorStateMachineをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた配列。</returns>
        public ChildAnimatorStateMachine[] CloneChildAnimatorStateMachines(IEnumerable<ChildAnimatorStateMachine> origs, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(origs, CloneChildAnimatorStateMachinesInternal, out clonedMap);

        /// <summary>
        /// ChildAnimatorStateMachineをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public ChildAnimatorStateMachine CloneChildAnimatorStateMachine(ChildAnimatorStateMachine orig) => CloneChildAnimatorStateMachine(orig, out _);

        /// <summary>
        /// ChildAnimatorStateMachineをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public ChildAnimatorStateMachine CloneChildAnimatorStateMachine(ChildAnimatorStateMachine orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneChildAnimatorStateMachineInternal, out clonedMap);

        /// <summary>
        /// AnimatorStateMachineをクローンします。states/stateMachines/transitions/behaviours等を含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorStateMachine。</param>
        /// <returns>クローンされたAnimatorStateMachine。</returns>
        public AnimatorStateMachine CloneAnimatorStateMachine(AnimatorStateMachine orig) => CloneAnimatorStateMachine(orig, out _);

        /// <summary>
        /// AnimatorStateMachineをクローンします。states/stateMachines/transitions/behaviours等を含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorStateMachine。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたAnimatorStateMachine。</returns>
        public AnimatorStateMachine CloneAnimatorStateMachine(AnimatorStateMachine orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneAnimatorStateMachineInternal, out clonedMap);

        /// <summary>
        /// 複数のChildAnimatorStateをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public ChildAnimatorState[] CloneChildAnimatorStates(IEnumerable<ChildAnimatorState> origs) => CloneChildAnimatorStates(origs, out _);

        /// <summary>
        /// 複数のChildAnimatorStateをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた配列。</returns>
        public ChildAnimatorState[] CloneChildAnimatorStates(IEnumerable<ChildAnimatorState> origs, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(origs, CloneChildAnimatorStatesInternal, out clonedMap);

        /// <summary>
        /// ChildAnimatorStateをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public ChildAnimatorState CloneChildAnimatorState(ChildAnimatorState orig) => CloneChildAnimatorState(orig, out _);

        /// <summary>
        /// ChildAnimatorStateをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたオブジェクト。</returns>
        public ChildAnimatorState CloneChildAnimatorState(ChildAnimatorState orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneChildAnimatorStateInternal, out clonedMap);

        /// <summary>
        /// AnimatorStateをクローンします。transitions/behaviours/motion(AnimationClip/BlendTree)を含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorState。</param>
        /// <returns>クローンされたAnimatorState。</returns>
        public AnimatorState CloneAnimatorState(AnimatorState orig) => CloneAnimatorState(orig, out _);

        /// <summary>
        /// AnimatorStateをクローンします。transitions/behaviours/motion(AnimationClip/BlendTree)を含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorState。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたAnimatorState。</returns>
        public AnimatorState CloneAnimatorState(AnimatorState orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneAnimatorStateInternal, out clonedMap);

        /// <summary>
        /// 複数のAnimatorTransitionをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public AnimatorTransition[] CloneAnimatorTransitions(IEnumerable<AnimatorTransition> origs) => CloneAnimatorTransitions(origs, out _);

        /// <summary>
        /// 複数のAnimatorTransitionをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた配列。</returns>
        public AnimatorTransition[] CloneAnimatorTransitions(IEnumerable<AnimatorTransition> origs, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(origs, CloneAnimatorTransitionsInternal, out clonedMap);

        /// <summary>
        /// AnimatorTransitionをクローンします。destinationState/destinationStateMachine/conditionsを含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorTransition。</param>
        /// <returns>クローンされたAnimatorTransition。</returns>
        public AnimatorTransition CloneAnimatorTransition(AnimatorTransition orig) => CloneAnimatorTransition(orig, out _);

        /// <summary>
        /// AnimatorTransitionをクローンします。destinationState/destinationStateMachine/conditionsを含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorTransition。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたAnimatorTransition。</returns>
        public AnimatorTransition CloneAnimatorTransition(AnimatorTransition orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneAnimatorTransitionInternal, out clonedMap);

        /// <summary>
        /// 複数のAnimatorStateTransitionをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public AnimatorStateTransition[] CloneAnimatorStateTransitions(IEnumerable<AnimatorStateTransition> origs) => CloneAnimatorStateTransitions(origs, out _);

        /// <summary>
        /// 複数のAnimatorStateTransitionをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた配列。</returns>
        public AnimatorStateTransition[] CloneAnimatorStateTransitions(IEnumerable<AnimatorStateTransition> origs, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(origs, CloneAnimatorStateTransitionsInternal, out clonedMap);

        /// <summary>
        /// AnimatorStateTransitionをクローンします。destinationState/destinationStateMachine/conditionsを含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorStateTransition。</param>
        /// <returns>クローンされたAnimatorStateTransition。</returns>
        public AnimatorStateTransition CloneAnimatorStateTransition(AnimatorStateTransition orig) => CloneAnimatorStateTransition(orig, out _);

        /// <summary>
        /// AnimatorStateTransitionをクローンします。destinationState/destinationStateMachine/conditionsを含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorStateTransition。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたAnimatorStateTransition。</returns>
        public AnimatorStateTransition CloneAnimatorStateTransition(AnimatorStateTransition orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneAnimatorStateTransitionInternal, out clonedMap);

        /// <summary>
        /// 複数のStateMachineBehaviourをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public StateMachineBehaviour[] CloneStateMachineBehaviours(IEnumerable<StateMachineBehaviour> origs) => CloneStateMachineBehaviours(origs, out _);

        /// <summary>
        /// 複数のStateMachineBehaviourをまとめてクローンします。ClonePolicyがKeepReference未満のものは結果から除外されます。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた配列。</returns>
        public StateMachineBehaviour[] CloneStateMachineBehaviours(IEnumerable<StateMachineBehaviour> origs, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(origs, CloneStateMachineBehavioursInternal, out clonedMap);

        /// <summary>
        /// StateMachineBehaviourをクローンします。実際の具象型のインスタンスが生成され、シリアライズ内容がコピーされます。
        /// </summary>
        /// <param name="orig">クローン元のStateMachineBehaviour。</param>
        /// <returns>クローンされたStateMachineBehaviour。</returns>
        public StateMachineBehaviour CloneStateMachineBehaviour(StateMachineBehaviour orig) => CloneStateMachineBehaviour(orig, out _);

        /// <summary>
        /// StateMachineBehaviourをクローンします。実際の具象型のインスタンスが生成され、シリアライズ内容がコピーされます。
        /// </summary>
        /// <param name="orig">クローン元のStateMachineBehaviour。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたStateMachineBehaviour。</returns>
        public StateMachineBehaviour CloneStateMachineBehaviour(StateMachineBehaviour orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneStateMachineBehaviourInternal, out clonedMap);

        /// <summary>
        /// AnimationClipをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のAnimationClip。</param>
        /// <returns>クローンされたAnimationClip。</returns>
        public AnimationClip CloneAnimationClip(AnimationClip orig) => CloneAnimationClip(orig, out _);

        /// <summary>
        /// AnimationClipをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のAnimationClip。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたAnimationClip。</returns>
        public AnimationClip CloneAnimationClip(AnimationClip orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneAnimationClipInternal, out clonedMap);

        /// <summary>
        /// BlendTreeをクローンします。childrenを含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のBlendTree。</param>
        /// <returns>クローンされたBlendTree。</returns>
        public BlendTree CloneBlendTree(BlendTree orig) => CloneBlendTree(orig, out _);

        /// <summary>
        /// BlendTreeをクローンします。childrenを含め再帰的に複製されます。
        /// </summary>
        /// <param name="orig">クローン元のBlendTree。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたBlendTree。</returns>
        public BlendTree CloneBlendTree(BlendTree orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneBlendTreeInternal, out clonedMap);

        /// <summary>
        /// 複数のChildMotionをまとめてクローンします。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public ChildMotion[] CloneChildMotions(IEnumerable<ChildMotion> origs) => CloneChildMotions(origs, out _);

        /// <summary>
        /// 複数のChildMotionをまとめてクローンします。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされた配列。</returns>
        public ChildMotion[] CloneChildMotions(IEnumerable<ChildMotion> origs, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(origs, CloneChildMotionsInternal, out clonedMap);

        /// <summary>
        /// ChildMotionをクローンします。motion(AnimationClip/BlendTree)を含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のChildMotion。</param>
        /// <returns>クローンされたChildMotion。</returns>
        public ChildMotion CloneChildMotion(ChildMotion orig) => CloneChildMotion(orig, out _);

        /// <summary>
        /// ChildMotionをクローンします。motion(AnimationClip/BlendTree)を含め複製されます。
        /// </summary>
        /// <param name="orig">クローン元のChildMotion。</param>
        /// <param name="clonedMap">このクローン完了時点での、元オブジェクトから複製後オブジェクトへのマップ。</param>
        /// <returns>クローンされたChildMotion。</returns>
        public ChildMotion CloneChildMotion(ChildMotion orig, out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap) => CloneWithMap(orig, CloneChildMotionInternal, out clonedMap);

        private object CloneObjectInternal(object orig) => orig switch
        {
            AnimatorController castedOrig => CloneAnimatorControllerInternal(castedOrig),
            AnimatorControllerParameter castedOrig => CloneAnimatorControllerParameter(castedOrig),
            AnimatorControllerLayer castedOrig => CloneAnimatorControllerLayerInternal(castedOrig),
            ChildAnimatorStateMachine castedOrig => CloneChildAnimatorStateMachineInternal(castedOrig),
            AnimatorStateMachine castedOrig => CloneAnimatorStateMachineInternal(castedOrig),
            ChildAnimatorState castedOrig => CloneChildAnimatorStateInternal(castedOrig),
            AnimatorState castedOrig => CloneAnimatorStateInternal(castedOrig),
            AnimatorTransition castedOrig => CloneAnimatorTransitionInternal(castedOrig),
            AnimatorStateTransition castedOrig => CloneAnimatorStateTransitionInternal(castedOrig),
            AnimatorCondition castedOrig => CloneAnimatorCondition(castedOrig),
            StateMachineBehaviour castedOrig => CloneStateMachineBehaviourInternal(castedOrig),
            AnimationClip castedOrig => CloneAnimationClipInternal(castedOrig),
            BlendTree castedOrig => CloneBlendTreeInternal(castedOrig),
            _ => null,
        };

        /// <summary>
        /// オブジェクトの実際の型を判定してクローンを試みます。
        /// </summary>
        /// <param name="orig">クローン元のオブジェクト。</param>
        /// <param name="clone">成功した場合はクローンされたオブジェクト、失敗した場合はnull。</param>
        /// <returns>origがnullでなく、かつCloneableTypesに含まれる型でクローンに成功した場合はtrue。</returns>
        public bool TryCloneObject(object orig, out object clone)
        {
            object tempClone;
            if (orig == null || (tempClone = CloneObjectInternal(orig)) == null)
            {
                clone = null;
                return false;
            }

            clone = tempClone;
            return true;
        }


        private AnimatorController CloneAnimatorControllerInternal(AnimatorController orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, out AnimatorController clone);
            if (!isCreated) return clone;

            clone.hideFlags = orig.hideFlags;

            clone.parameters = CloneAnimatorControllerParameters(orig.parameters);
            ThrowIfKeepReferenceChildren(orig.layers.Select(x => x.stateMachine));
            clone.layers = CloneAnimatorControllerLayersInternal(orig.layers);

            return clone;
        }

        /// <summary>
        /// 複数のAnimatorControllerParameterをまとめてクローンします。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public AnimatorControllerParameter[] CloneAnimatorControllerParameters(IEnumerable<AnimatorControllerParameter> origs)
        {
            return origs.Select(orig => CloneAnimatorControllerParameter(orig)).ToArray();
        }

        /// <summary>
        /// AnimatorControllerParameterをクローンします。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorControllerParameter。</param>
        /// <returns>クローンされたAnimatorControllerParameter。</returns>
        public AnimatorControllerParameter CloneAnimatorControllerParameter(AnimatorControllerParameter orig)
        {
            return new()
            {
                defaultBool = orig.defaultBool,
                defaultFloat = orig.defaultFloat,
                defaultInt = orig.defaultInt,
                name = GetCloneObjName(orig.name),
                type = orig.type
            };
        }

        private AnimatorControllerLayer[] CloneAnimatorControllerLayersInternal(IEnumerable<AnimatorControllerLayer> origs)
        {
            return origs.Select(orig => CloneAnimatorControllerLayerInternal(orig)).ToArray();
        }

        private AnimatorControllerLayer CloneAnimatorControllerLayerInternal(AnimatorControllerLayer orig)
        {
            AnimatorControllerLayer clone = new()
            {
                avatarMask = orig.avatarMask,
                blendingMode = orig.blendingMode,
                defaultWeight = orig.defaultWeight,
                iKPass = orig.iKPass,
                name = GetCloneObjName(orig.name),
                syncedLayerAffectsTiming = orig.syncedLayerAffectsTiming,
                syncedLayerIndex = orig.syncedLayerIndex,
                stateMachine = CloneAnimatorStateMachineInternal(orig.stateMachine)
            };


            StateMotionPair[] overrideStateMotionPairs = AnimatorInternalsAdapterProvider.Current.GetAllOverrideStateMotionPairs(orig);
            if (overrideStateMotionPairs != null)
            {
                AnimatorInternalsAdapterProvider.Current.InitOverrideStateMotionPairs(clone);
                foreach (StateMotionPair pair in overrideStateMotionPairs)
                {
                    AnimatorState cloneAS = CloneAnimatorStateInternal(pair.State);
                    clone.SetOverrideMotion(cloneAS, pair.Motion);
                }
            }

            StateBehavioursPair[] overrideBehavioursPairs = AnimatorInternalsAdapterProvider.Current.GetAllOverrideBehavioursPairs(orig);
            if (overrideBehavioursPairs != null)
            {
                AnimatorInternalsAdapterProvider.Current.InitOverrideStateBehavioursPairs(clone);
                foreach (StateBehavioursPair pair in overrideBehavioursPairs)
                {
                    AnimatorState cloneAS = CloneAnimatorStateInternal(pair.State);
                    StateMachineBehaviour[] cloneSMBs = CloneStateMachineBehavioursInternal(pair.Behaviours);
                    clone.SetOverrideBehaviours(cloneAS, cloneSMBs);
                }
            }

            return clone;
        }

        private ChildAnimatorStateMachine[] CloneChildAnimatorStateMachinesInternal(IEnumerable<ChildAnimatorStateMachine> origs)
        {
            return origs.Where(orig => GetClonePolicy(orig.stateMachine) >= ClonePolicy.KeepReference).Select(orig => CloneChildAnimatorStateMachineInternal(orig)).ToArray();
        }

        private ChildAnimatorStateMachine CloneChildAnimatorStateMachineInternal(ChildAnimatorStateMachine orig)
        {
            ChildAnimatorStateMachine clone = new()
            {
                position = orig.position,
                stateMachine = CloneAnimatorStateMachineInternal(orig.stateMachine)
            };

            return clone;
        }

        private AnimatorStateMachine CloneAnimatorStateMachineInternal(AnimatorStateMachine orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, out AnimatorStateMachine clone);
            if (!isCreated) return clone;

            clone.hideFlags = orig.hideFlags;

            clone.anyStatePosition = orig.anyStatePosition;
            clone.entryPosition = orig.entryPosition;
            clone.exitPosition = orig.exitPosition;
            clone.name = GetCloneObjName(orig.name);
            clone.parentStateMachinePosition = orig.parentStateMachinePosition;

            ThrowIfKeepReferenceChildren(orig.states.Select(x => x.state));
            clone.states = CloneChildAnimatorStatesInternal(orig.states);
            ThrowIfKeepReferenceChildren(orig.stateMachines.Select(x => x.stateMachine));
            clone.stateMachines = CloneChildAnimatorStateMachinesInternal(orig.stateMachines);
            clone.defaultState = CloneAnimatorStateInternal(orig.defaultState);

            ThrowIfKeepReferenceChildren(orig.anyStateTransitions);
            clone.anyStateTransitions = CloneAnimatorStateTransitionsInternal(orig.anyStateTransitions);
            ThrowIfKeepReferenceChildren(orig.entryTransitions);
            clone.entryTransitions = CloneAnimatorTransitionsInternal(orig.entryTransitions);
            foreach (ChildAnimatorStateMachine curCASM in orig.stateMachines)
            {
                AnimatorStateMachine cloneStateMachine = CloneAnimatorStateMachineInternal(curCASM.stateMachine);

                AnimatorTransition[] transitions = orig.GetStateMachineTransitions(curCASM.stateMachine);
                ThrowIfKeepReferenceChildren(transitions);
                AnimatorTransition[] cloneTransitions = CloneAnimatorTransitionsInternal(transitions);

                clone.SetStateMachineTransitions(cloneStateMachine, cloneTransitions);
            }

            ThrowIfKeepReferenceChildren(orig.behaviours);
            clone.behaviours = CloneStateMachineBehavioursInternal(orig.behaviours);

            return clone;
        }

        private ChildAnimatorState[] CloneChildAnimatorStatesInternal(IEnumerable<ChildAnimatorState> origs)
        {
            return origs.Where(orig => GetClonePolicy(orig.state) >= ClonePolicy.KeepReference).Select(orig => CloneChildAnimatorStateInternal(orig)).ToArray();
        }

        private ChildAnimatorState CloneChildAnimatorStateInternal(ChildAnimatorState orig)
        {
            ChildAnimatorState clone = new()
            {
                position = orig.position,
                state = CloneAnimatorStateInternal(orig.state)
            };

            return clone;
        }

        private AnimatorState CloneAnimatorStateInternal(AnimatorState orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, out AnimatorState clone);
            if (!isCreated) return clone;

            clone.hideFlags = orig.hideFlags;

            clone.cycleOffset = orig.cycleOffset;
            clone.cycleOffsetParameter = orig.cycleOffsetParameter;
            clone.cycleOffsetParameterActive = orig.cycleOffsetParameterActive;
            clone.iKOnFeet = orig.iKOnFeet;
            clone.mirror = orig.mirror;
            clone.mirrorParameter = orig.mirrorParameter;
            clone.mirrorParameterActive = orig.mirrorParameterActive;
            clone.motion = orig.motion switch
            {
                AnimationClip origAnimationClip => CloneAnimationClipInternal(origAnimationClip),
                BlendTree origBlendTree => CloneBlendTreeInternal(origBlendTree),
                _ => orig.motion
            };
            clone.name = GetCloneObjName(orig.name);
            clone.speed = orig.speed;
            clone.speedParameter = orig.speedParameter;
            clone.speedParameterActive = orig.speedParameterActive;
            clone.tag = orig.tag;
            clone.timeParameter = orig.timeParameter;
            clone.timeParameterActive = orig.timeParameterActive;
            clone.writeDefaultValues = orig.writeDefaultValues;

            ThrowIfKeepReferenceChildren(orig.transitions);
            clone.transitions = CloneAnimatorStateTransitionsInternal(orig.transitions);

            ThrowIfKeepReferenceChildren(orig.behaviours);
            clone.behaviours = CloneStateMachineBehavioursInternal(orig.behaviours);

            return clone;
        }

        private AnimatorTransition[] CloneAnimatorTransitionsInternal(IEnumerable<AnimatorTransition> origs)
        {
            return origs.Where(orig => GetClonePolicy(orig) >= ClonePolicy.KeepReference).Select(orig => CloneAnimatorTransitionInternal(orig)).ToArray();
        }

        private AnimatorTransition CloneAnimatorTransitionInternal(AnimatorTransition orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, out AnimatorTransition clone);
            if (!isCreated) return clone;

            clone.hideFlags = orig.hideFlags;

            clone.isExit = orig.isExit;
            clone.mute = orig.mute;
            clone.name = GetCloneObjName(orig.name);
            clone.solo = orig.solo;
            clone.destinationState = CloneAnimatorStateInternal(orig.destinationState);
            clone.destinationStateMachine = CloneAnimatorStateMachineInternal(orig.destinationStateMachine);
            clone.conditions = CloneAnimatorConditions(orig.conditions);

            return clone;
        }

        private AnimatorStateTransition[] CloneAnimatorStateTransitionsInternal(IEnumerable<AnimatorStateTransition> origs)
        {
            return origs.Where(orig => GetClonePolicy(orig) >= ClonePolicy.KeepReference).Select(orig => CloneAnimatorStateTransitionInternal(orig)).ToArray();
        }

        private AnimatorStateTransition CloneAnimatorStateTransitionInternal(AnimatorStateTransition orig)
        {
            bool isCreated = TryGetOrCreateCloneInstance(orig, out AnimatorStateTransition clone);
            if (!isCreated) return clone;

            clone.hideFlags = orig.hideFlags;

            clone.canTransitionToSelf = orig.canTransitionToSelf;
            clone.duration = orig.duration;
            clone.exitTime = orig.exitTime;
            clone.hasExitTime = orig.hasExitTime;
            clone.hasFixedDuration = orig.hasFixedDuration;
            clone.interruptionSource = orig.interruptionSource;
            clone.isExit = orig.isExit;
            clone.mute = orig.mute;
            clone.name = GetCloneObjName(orig.name);
            clone.offset = orig.offset;
            clone.orderedInterruption = orig.orderedInterruption;
            clone.solo = orig.solo;
            clone.destinationState = CloneAnimatorStateInternal(orig.destinationState);
            clone.destinationStateMachine = CloneAnimatorStateMachineInternal(orig.destinationStateMachine);
            clone.conditions = CloneAnimatorConditions(orig.conditions);

            return clone;
        }

        /// <summary>
        /// 複数のAnimatorConditionをまとめてクローンします。
        /// </summary>
        /// <param name="origs">クローン元の列挙。</param>
        /// <returns>クローンされた配列。</returns>
        public AnimatorCondition[] CloneAnimatorConditions(IEnumerable<AnimatorCondition> origs)
        {
            return origs.Select(orig => CloneAnimatorCondition(orig)).ToArray();
        }

        /// <summary>
        /// AnimatorConditionをクローンします。AnimatorConditionは値型のため、実質的には元の値をそのまま返します。
        /// </summary>
        /// <param name="orig">クローン元のAnimatorCondition。</param>
        /// <returns>クローンされたAnimatorCondition。</returns>
        public AnimatorCondition CloneAnimatorCondition(AnimatorCondition orig)
        {
            return orig;
        }

        private StateMachineBehaviour[] CloneStateMachineBehavioursInternal(IEnumerable<StateMachineBehaviour> origs)
        {
            return origs.Where(orig => GetClonePolicy(orig) >= ClonePolicy.KeepReference).Select(orig => CloneStateMachineBehaviourInternal(orig)).ToArray();
        }

        private StateMachineBehaviour CloneStateMachineBehaviourInternal(StateMachineBehaviour orig)
        {
            StateMachineBehaviour clone = (StateMachineBehaviour)ScriptableObject.CreateInstance(orig.GetType());
            EditorUtility.CopySerialized(orig, clone);
            clone.hideFlags = orig.hideFlags;

            return clone;
        }

        private AnimationClip CloneAnimationClipInternal(AnimationClip orig)
        {
            bool isCreated = TryGetOrCreateMotionCloneInstance(orig, out Motion motionClone);
            AnimationClip clone = (AnimationClip)motionClone;
            if (!isCreated) return clone;

            EditorUtility.CopySerialized(orig, clone);

            clone.hideFlags = orig.hideFlags;

            clone.name = GetCloneObjName(orig.name);

            return clone;
        }

        private BlendTree CloneBlendTreeInternal(BlendTree orig)
        {
            bool isCreated = TryGetOrCreateMotionCloneInstance(orig, out Motion motionClone);
            BlendTree clone = (BlendTree)motionClone;
            if (!isCreated) return clone;

            clone.hideFlags = orig.hideFlags;

            clone.blendParameter = orig.blendParameter;
            clone.blendParameterY = orig.blendParameterY;
            clone.blendType = orig.blendType;
            clone.children = CloneChildMotionsInternal(orig.children);
            clone.maxThreshold = orig.maxThreshold;
            clone.minThreshold = orig.minThreshold;
            clone.name = GetCloneObjName(orig.name);
            clone.useAutomaticThresholds = orig.useAutomaticThresholds;

            return clone;
        }

        private ChildMotion[] CloneChildMotionsInternal(IEnumerable<ChildMotion> origs)
        {
            return origs.Select(orig => CloneChildMotionInternal(orig)).ToArray();
        }

        private ChildMotion CloneChildMotionInternal(ChildMotion orig)
        {
            ChildMotion clone = orig;
            clone.motion = orig.motion switch
            {
                AnimationClip origAnimationClip => CloneAnimationClipInternal(origAnimationClip),
                BlendTree origBlendTree => CloneBlendTreeInternal(origBlendTree),
                _ => orig.motion
            };
            return clone;
        }

        private void ThrowIfKeepReferenceChildren(IEnumerable<UnityEngine.Object> objs)
        {
            foreach (UnityEngine.Object obj in objs)
            {
                if (GetClonePolicy(obj) == ClonePolicy.KeepReference)
                {
                    throw new InvalidOperationException(
                        $"親がCloneのオブジェクトの子に、KeepReferenceが設定されています。" +
                        $"対象: {obj.name} ({obj.GetType().Name})");
                }
            }
        }

        private bool TryGetOrCreateCloneInstance<T>(T orig, out T clone) where T : UnityEngine.Object, new()
        {
            if (orig == null)
            {
                clone = default;
                return false;
            }
            if (_cloneMap.TryGetValue(orig, out UnityEngine.Object cached) && cached is T tCached)
            {
                clone = tCached;
                return false;
            }

            ClonePolicy policy = GetClonePolicy(orig);

            return TryGetOrCreateCloneInstanceInternal(orig, out clone, policy);
        }

        private bool TryGetOrCreateMotionCloneInstance(Motion orig, out Motion clone)
        {
            if (orig == null)
            {
                clone = default;
                return false;
            }
            if (_cloneMap.TryGetValue(orig, out UnityEngine.Object cached) && cached is Motion tCached)
            {
                clone = tCached;
                return false;
            }

            // 他の子オブジェクト(transition/behaviour等)と異なり、motionはRegisterChildrenRecursivelyで
            // _parentMapに登録されないため、親のClonePolicy(Cloneなど)を継承しない。
            // ここで直接設定されていなければ、常にDefaultPolicy基準で解決される。
            _policyMap.TryGetValue(orig, out ClonePolicy policy);
            ClonePolicy resolvedPolicy = policy switch
            {
                // 手動でポリシーが設定されていればそれを使用
                ClonePolicy.Clone or ClonePolicy.KeepReference or ClonePolicy.Detach => policy,
                ClonePolicy.UnSetting or _ => DefaultPolicy switch
                {
                    // デフォルトポリシーが KeepReference 未満なら KeepReferenceに昇格
                    ClonePolicy.Clone or ClonePolicy.KeepReference => DefaultPolicy,
                    ClonePolicy.Detach or ClonePolicy.UnSetting or _ => ClonePolicy.KeepReference
                }
            };

            // Motionは抽象クラス(new()制約を満たせない)のため、具象型ごとにTryGetOrCreateCloneInstanceInternal<T>へ振り分ける。
            // AnimationClip/BlendTreeはUnity APIにおけるMotionの唯一の具象型。
            switch (orig)
            {
                case AnimationClip clipOrig:
                    bool clipCreated = TryGetOrCreateCloneInstanceInternal(clipOrig, out AnimationClip clipClone, resolvedPolicy);
                    clone = clipClone;
                    return clipCreated;
                case BlendTree treeOrig:
                    bool treeCreated = TryGetOrCreateCloneInstanceInternal(treeOrig, out BlendTree treeClone, resolvedPolicy);
                    clone = treeClone;
                    return treeCreated;
                default:
                    throw new InvalidOperationException($"未対応のMotion派生型です: {orig.GetType().FullName}");
            }
        }

        private bool TryGetOrCreateCloneInstanceInternal<T>(T orig, out T clone, ClonePolicy policy) where T : UnityEngine.Object, new()
        {
            switch (policy)
            {
                case ClonePolicy.Clone:
                    clone = new T();
                    _cloneMap[orig] = _cloneMap[clone] = clone;
                    return true;

                case ClonePolicy.KeepReference:
                    _cloneMap[orig] = clone = orig;
                    return false;

                case ClonePolicy.Detach:
                default:
                    _cloneMap[orig] = clone = default;
                    return false;

                case ClonePolicy.UnSetting:
                    throw new InvalidOperationException("ClonePolicyが未設定のオブジェクトをクローンしようとしました");
            }
        }

        private string GetCloneObjName(string origName) => NameTransformer(origName);

        /// <summary>
        /// ClonePolicyの登録漏れを検出する
        /// </summary>
        /// <param name="target">検証対象のオブジェクト。</param>
        /// <returns>検出された、ClonePolicy未設定または不正な登録の一覧。</returns>
        public IReadOnlyCollection<InvalidEntry> ValidateRegistration(UnityEngine.Object target) => ValidateRegistrationInternal(target);


        /// <summary>
        /// ClonePolicyの登録漏れを検出する
        /// </summary>
        /// <param name="targets">検証対象のオブジェクトの列挙。</param>
        /// <returns>検出された、ClonePolicy未設定または不正な登録の一覧。</returns>
        public IReadOnlyCollection<InvalidEntry> ValidateRegistrations(IEnumerable<UnityEngine.Object> targets) => targets.SelectMany(t => ValidateRegistration(t)).ToHashSet();


        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationInternal(object target)
        {
            if (target == null)
            {
                return new List<InvalidEntry>();
            }

            HashSet<UnityEngine.Object> visitedObjSet = new();

            return ValidateRegistrationDispatch(target, null, "", ref visitedObjSet);
        }

        // AnimatorGraphSchema.GetChildrenが列挙した子要素を、実際の型に応じて対応するValidateRegistrationXxxへ振り分ける。
        // トップレベルのValidateRegistrationInternalと、各ノードの子要素再帰の両方から使う共通の入口。
        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationDispatch(object target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet) => target switch
        {
            null => Array.Empty<InvalidEntry>(),
            AnimatorController castedObj => ValidateRegistrationAnimatorController(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorControllerLayer castedObj => ValidateRegistrationAnimatorControllerLayer(castedObj, parent, memberName, ref visitedObjSet),
            ChildAnimatorStateMachine castedObj => ValidateRegistrationChildAnimatorStateMachine(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorStateMachine castedObj => ValidateRegistrationAnimatorStateMachine(castedObj, parent, memberName, ref visitedObjSet),
            ChildAnimatorState castedObj => ValidateRegistrationChildAnimatorState(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorState castedObj => ValidateRegistrationAnimatorState(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorTransition castedObj => ValidateRegistrationAnimatorTransition(castedObj, parent, memberName, ref visitedObjSet),
            AnimatorStateTransition castedObj => ValidateRegistrationAnimatorStateTransition(castedObj, parent, memberName, ref visitedObjSet),
            StateMachineBehaviour castedObj => ValidateRegistrationStateMachineBehaviour(castedObj, parent, memberName, ref visitedObjSet),
            AnimationClip castedObj => ValidateRegistrationAnimationClip(castedObj, parent, memberName, ref visitedObjSet),
            BlendTree castedObj => ValidateRegistrationBlendTree(castedObj, parent, memberName, ref visitedObjSet),
            _ => Array.Empty<InvalidEntry>(),
        };

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorController(AnimatorController target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return entries;
        }

        // 複数形の一括検証版。ValidateRegistrationAnimatorController自体はAnimatorGraphSchema経由の
        // 再帰で完結するため内部からは呼ばれないが、既存の利用者(テスト等)向けにinternalとして残す。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorControllerLayers(IEnumerable<AnimatorControllerLayer> target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (AnimatorControllerLayer acl in target)
            {
                entries.UnionWith(ValidateRegistrationAnimatorControllerLayer(acl, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorControllerLayer(AnimatorControllerLayer target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            // AnimatorControllerLayerはUnityEngine.Objectではないため、次の階層のparentにはなれない。
            // 受け取ったparent(このレイヤー自身の親)をそのまま子要素へ引き継ぐ。
            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, parent, $"{memberName}.{childMemberName}", ref visitedObjSet));
            }
            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationAnimatorControllerLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationChildAnimatorStateMachines(IEnumerable<ChildAnimatorStateMachine> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (ChildAnimatorStateMachine target in targets)
            {
                entries.UnionWith(ValidateRegistrationChildAnimatorStateMachine(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationChildAnimatorStateMachine(ChildAnimatorStateMachine target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            return ValidateRegistrationAnimatorStateMachine(target.stateMachine, parent, $"{memberName}.{nameof(target.stateMachine)}", ref visitedObjSet);
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorStateMachine(AnimatorStateMachine target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();

            // Policy未登録の検証
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            // 不正なPolicy登録(親Clone、子KeepReference)の検証
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.states.Select(x => x.state), target, nameof(target.states)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.stateMachines.Select(x => x.stateMachine), target, nameof(target.stateMachines)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.entryTransitions, target, nameof(target.entryTransitions)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.anyStateTransitions, target, nameof(target.anyStateTransitions)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.behaviours, target, nameof(target.behaviours)));
            foreach (ChildAnimatorStateMachine curCASM in target.stateMachines)
            {
                AnimatorTransition[] transitions = target.GetStateMachineTransitions(curCASM.stateMachine);
                entries.UnionWith(ValidateKeepReferenceChildRegistrations(transitions, target, $"StateMachineTransitions()[{curCASM.stateMachine.name}]"));
            }

            return entries;
        }

        /// <summary>
        /// 不正なPolicy登録(親Clone、子KeepReference)の検証
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="parent"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        private IReadOnlyCollection<InvalidEntry> ValidateKeepReferenceChildRegistrations(IEnumerable<UnityEngine.Object> targets, UnityEngine.Object parent, string memberName)
        {
            // 不正なPolicy登録(親Clone、子KeepReference)の検証
            HashSet<InvalidEntry> entries = new();
            int i = 0;
            foreach (UnityEngine.Object curObj in targets)
            {
                // 手動設定で子がKeepReferenceになっていないか確認
                if (GetClonePolicy(curObj) == ClonePolicy.KeepReference)
                {
                    entries.Add(new(InvalidType.KeepReferenceChild, curObj, parent, $"{memberName}[{i}]"));
                }
                i++;
            }
            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationAnimatorControllerLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationChildAnimatorStates(IEnumerable<ChildAnimatorState> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (ChildAnimatorState target in targets)
            {
                entries.UnionWith(ValidateRegistrationChildAnimatorState(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationChildAnimatorState(ChildAnimatorState target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            return ValidateRegistrationAnimatorState(target.state, parent, $"{memberName}.{nameof(target.state)}", ref visitedObjSet);
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorState(AnimatorState target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }

            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.transitions, target, nameof(target.transitions)));
            entries.UnionWith(ValidateKeepReferenceChildRegistrations(target.behaviours, target, nameof(target.behaviours)));

            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationAnimatorControllerLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorTransitions(IEnumerable<AnimatorTransition> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (AnimatorTransition target in targets)
            {
                entries.UnionWith(ValidateRegistrationAnimatorTransition(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorTransition(AnimatorTransition target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationAnimatorControllerLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorStateTransitions(IEnumerable<AnimatorStateTransition> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (AnimatorStateTransition target in targets)
            {
                entries.UnionWith(ValidateRegistrationAnimatorStateTransition(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimatorStateTransition(AnimatorStateTransition target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return Array.Empty<InvalidEntry>();
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return Array.Empty<InvalidEntry>();

            HashSet<InvalidEntry> entries = new();
            foreach ((string childMemberName, object child) in AnimatorGraphSchema.GetChildren(target))
            {
                entries.UnionWith(ValidateRegistrationDispatch(child, target, childMemberName, ref visitedObjSet));
            }
            return entries;
        }

        // 複数形の一括検証版。用途はValidateRegistrationAnimatorControllerLayersと同様(内部の再帰からは呼ばれない)。
        internal IReadOnlyCollection<InvalidEntry> ValidateRegistrationStateMachineBehaviours(IEnumerable<StateMachineBehaviour> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (StateMachineBehaviour target in targets)
            {
                entries.UnionWith(ValidateRegistrationStateMachineBehaviour(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationStateMachineBehaviour(StateMachineBehaviour target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return new InvalidEntry[0];
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            return new InvalidEntry[0];
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationAnimationClip(AnimationClip target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return new InvalidEntry[0];
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            return new InvalidEntry[0];
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationBlendTree(BlendTree target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            if (visitedObjSet.Contains(target)) return new InvalidEntry[0];
            visitedObjSet.Add(target);
            bool validPolicy = ValidateAndCreateUnregisteredEntry(target, parent, memberName, out InvalidEntry entry, out ClonePolicy policy);
            if (!validPolicy) return new InvalidEntry[] { entry };
            if (policy != ClonePolicy.Clone) return new InvalidEntry[0];

            HashSet<InvalidEntry> entries = new();

            entries.UnionWith(ValidateRegistrationChildMotions(target.children, target, nameof(target.children), ref visitedObjSet));

            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationChildMotions(IEnumerable<ChildMotion> targets, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet)
        {
            int i = 0;
            HashSet<InvalidEntry> entries = new();
            foreach (ChildMotion target in targets)
            {
                entries.UnionWith(ValidateRegistrationChildMotion(target, parent, $"{memberName}[{i}]", ref visitedObjSet));
                i++;
            }
            return entries;
        }

        private IReadOnlyCollection<InvalidEntry> ValidateRegistrationChildMotion(ChildMotion target, UnityEngine.Object parent, string memberName, ref HashSet<UnityEngine.Object> visitedObjSet) => target.motion switch
        {
            AnimationClip clip => ValidateRegistrationAnimationClip(clip, parent, $"{memberName}.{nameof(target.motion)}", ref visitedObjSet),
            BlendTree tree => ValidateRegistrationBlendTree(tree, parent, $"{memberName}.{nameof(target.motion)}", ref visitedObjSet),
            _ => new InvalidEntry[0],
        };

        private bool ValidateAndCreateUnregisteredEntry(UnityEngine.Object target, UnityEngine.Object parent, string memberName, out InvalidEntry entry, out ClonePolicy policy)
        {
            entry = null;
            policy = default;
            // DefaultPolicyを加味したPolicy設定を確認
            if (target == null || (policy = GetClonePolicy(target)) != ClonePolicy.UnSetting) return true;
            entry = new(InvalidType.UnregisteredEntry, target, parent, memberName);
            return false;
        }

        /// <summary>
        /// 値が大きいほど優先度が高い。
        /// SetPolicyIfAbsentは現在の設定より低い優先度のポリシーを無視する。
        /// 新しいポリシーを追加する際は優先度順に並べること。
        /// </summary>
        public enum ClonePolicy
        {
            /// <summary>
            /// 未設定(このポリシーのオブジェクトをクローンしようとした場合、例外を吐く)。
            /// ただしMotion(AnimationClip/BlendTree)は例外で、UnSettingのままでも例外にはならず、
            /// DefaultPolicyに基づき自動的にKeepReference以上へ昇格する(詳細はTryGetOrCreateMotionCloneInstance参照)。
            /// </summary>
            UnSetting,
            /// <summary>nullとして扱う(切り離す)</summary>
            Detach,
            /// <summary>元のオブジェクトへの参照を保持する</summary>
            KeepReference,
            /// <summary>クローンを生成する</summary>
            Clone,
        }

        /// <summary>
        /// ValidateRegistration/ValidateRegistrationsで検出された、ClonePolicyの登録に関する問題1件を表します。
        /// </summary>
        public record InvalidEntry
        {
            /// <summary>問題の種別を取得します。</summary>
            public InvalidType InvalidType { get; }
            /// <summary>問題のあるオブジェクトを取得します。</summary>
            public UnityEngine.Object InvalidEntryObject { get; }
            /// <summary>InvalidEntryObjectを参照していた親オブジェクトを取得します。</summary>
            public UnityEngine.Object ReferencedFrom { get; }
            /// <summary>InvalidEntryObjectが参照されていたメンバー名を取得します。</summary>
            public string MemberName { get; }

            /// <summary>
            /// InvalidEntryの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="invalidType">問題の種別。</param>
            /// <param name="invalidEntryObject">問題のあるオブジェクト。</param>
            /// <param name="referencedFrom">invalidEntryObjectを参照していた親オブジェクト。</param>
            /// <param name="memberName">invalidEntryObjectが参照されていたメンバー名。</param>
            public InvalidEntry(InvalidType invalidType, UnityEngine.Object invalidEntryObject, UnityEngine.Object referencedFrom, string memberName)
            {
                InvalidType = invalidType;
                InvalidEntryObject = invalidEntryObject;
                ReferencedFrom = referencedFrom;
                MemberName = memberName;
            }
        }

        /// <summary>
        /// ValidateRegistrationで検出される、ClonePolicy登録に関する問題の種別です。
        /// </summary>
        public enum InvalidType
        {
            /// <summary>ClonePolicyが一切登録されていない(UnSettingのままの)オブジェクトが見つかった場合。</summary>
            UnregisteredEntry,
            /// <summary>親にCloneが設定されているにもかかわらず、子にKeepReferenceが設定されている場合。</summary>
            KeepReferenceChild
        }
    }
}
