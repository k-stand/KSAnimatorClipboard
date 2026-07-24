using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace com.github.k_stand.ksanimatorclipboard.editor
{
    /// <summary>
    /// クローンされたオブジェクトから複製元オブジェクトを逆引きできるようにし、複製後に新規生成されたAnimationClip/BlendTreeへの
    /// 参照を、名前と複製元のルートオブジェクトを手がかりに一括で付け替えるためのクラスです。
    /// </summary>
    public class ReferenceRemapper
    {
        private readonly Dictionary<UnityEngine.Object, UnityEngine.Object> _cloneToOrigMap = new();

        /// <summary>
        /// AnimatorCloner.GetClonedMap()等で得られる複製元→複製後のマップを、逆方向(複製後→複製元)で登録します。
        /// </summary>
        /// <param name="orig2CloneMap">複製元オブジェクトをキー、複製後オブジェクトを値とするマップ。</param>
        public void AddClonedMap(IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> orig2CloneMap)
        {
            foreach (KeyValuePair<UnityEngine.Object, UnityEngine.Object> pair in orig2CloneMap)
            {
                _cloneToOrigMap[pair.Value] = pair.Key;
            }
        }

        /// <summary>
        /// 登録済みの複製後→複製元マップのコピーを取得します。AnimatorCloner.GetClonedMap()とは方向が逆(複製後→複製元)である点に注意してください。
        /// </summary>
        /// <returns>複製後オブジェクトをキー、複製元オブジェクトを値とするマップ。</returns>
        public Dictionary<UnityEngine.Object, UnityEngine.Object> GetAllClonedMap() => new(_cloneToOrigMap);

        /// <summary>
        /// 登録済みマップを複製元方向へ辿り、指定したオブジェクトの最も根本にある複製元オブジェクトを取得します。
        /// </summary>
        /// <param name="obj">辿り始めるオブジェクト。</param>
        /// <returns>最終的に辿り着いた複製元オブジェクト。マップに登録がない場合はobj自身を返します。</returns>
        public UnityEngine.Object GetOrigRoot(UnityEngine.Object obj)
        {
            HashSet<UnityEngine.Object> visited = new();
            UnityEngine.Object current = obj;
            while (_cloneToOrigMap.TryGetValue(current, out UnityEngine.Object origObj))
            {
                if (!visited.Add(current))
                {
                    // 循環を検出。無限ループを避けるため、循環に入る直前まで辿り着いたオブジェクトを返す
                    return current;
                }
                current = origObj;
            }
            return current;
        }

        /// <summary>
        /// 指定したオブジェクトが持つAnimationClip/BlendTree等の参照を再帰的に辿り、登録済みマップに基づいて新しいオブジェクトへ付け替えます。
        /// </summary>
        /// <param name="obj">参照の付け替えを行う対象オブジェクト。</param>
        public void RemappingRecursively(UnityEngine.Object obj) => RemappingRecursivelyInternal(obj, new RemapperContext());

        /// <summary>
        /// 複数のオブジェクトに対してまとめてRemappingRecursivelyを行います。処理コンテキストを共有するため、
        /// 対象オブジェクト間で同名の付け替え先が重複して生成されることを防ぎます。
        /// </summary>
        /// <param name="objs">参照の付け替えを行う対象オブジェクトの列挙。</param>
        public void RemappingRecursively(IEnumerable<UnityEngine.Object> objs)
        {
            RemapperContext context = new();
            foreach (UnityEngine.Object obj in objs)
            {
                RemappingRecursivelyInternal(obj, context);
            }
        }

        private void RemappingRecursivelyInternal(UnityEngine.Object obj, RemapperContext context)
        {
            if (context.RemappedObjs.Contains(obj)) return;
            context.RemappedObjs.Add(obj);

            SerializedObject so = new(obj);
            so.Update();
            SerializedProperty prop = so.GetIterator();

            while (prop.Next(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                    prop.objectReferenceValue != null)
                {
                    if (prop.objectReferenceValue is AnimationClip or BlendTree)
                    {
                        UnityEngine.Object origRoot = GetOrigRoot(prop.objectReferenceValue);
                        if (context.Remap.TryGetValue((origRoot, prop.objectReferenceValue.name), out UnityEngine.Object remapObj))
                        {
                            prop.objectReferenceValue = remapObj;
                        }
                        else
                        {
                            context.Remap[(origRoot, prop.objectReferenceValue.name)] = prop.objectReferenceValue;
                            if (prop.objectReferenceValue is BlendTree)
                            {
                                RemappingRecursivelyInternal(prop.objectReferenceValue, context);
                            }
                        }
                    }
                    else if (prop.objectReferenceValue is AnimatorController or AnimatorStateMachine or AnimatorState)
                    {
                        RemappingRecursivelyInternal(prop.objectReferenceValue, context);
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        private class RemapperContext
        {
            internal readonly HashSet<UnityEngine.Object> RemappedObjs = new();
            internal readonly Dictionary<(UnityEngine.Object OrigRoot, string Name), UnityEngine.Object> Remap = new();
        }
    }
}
