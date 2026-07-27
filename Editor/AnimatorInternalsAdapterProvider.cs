using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.github.k_stand.ksanimatorcopyengine.editor
{
    internal static class AnimatorInternalsAdapterProvider
    {
        // 既知のアダプターを優先順に並べる(将来Unity6アダプターが増えたらここに追加する)。
        // フォールバック(下記KnownAdapters[^1])は「最後の要素=最新版」を前提にしているため、
        // 新しいアダプターは必ず末尾に追加すること(先頭挿入するとフォールバック先が変わってしまう)。
        private static readonly IReadOnlyList<(IReadOnlyCollection<int> SupportedMajorVersions, Func<IAnimatorInternalsAdapter> Factory)> KnownAdapters = new[]
        {
            (Unity2022AnimatorInternalsAdapter.SupportedMajorVersions, (Func<IAnimatorInternalsAdapter>)(() => new Unity2022AnimatorInternalsAdapter())),
        };

        private static readonly Lazy<IAnimatorInternalsAdapter> _current = new(() => Resolve(Application.unityVersion));

        internal static IAnimatorInternalsAdapter Current => _current.Value;

        internal static IAnimatorInternalsAdapter Resolve(string unityVersionString)
        {
            int majorVersion = ParseMajorVersion(unityVersionString);

            (IReadOnlyCollection<int> SupportedMajorVersions, Func<IAnimatorInternalsAdapter> Factory) match =
                KnownAdapters.FirstOrDefault(x => x.SupportedMajorVersions.Contains(majorVersion));

            IAnimatorInternalsAdapter adapter;
            if (match.Factory != null)
            {
                adapter = match.Factory();
            }
            else
            {
                // 一致するアダプターがなくても、直近の既知アダプターへベストエフォートでフォールバックする
                adapter = KnownAdapters[^1].Factory();
                Debug.LogWarning($"KSAnimatorCopyEngine: Unity {unityVersionString} は動作検証されていません。{adapter.GetType().Name} でのフォールバック動作を試みます。");
            }

            ValidateOrThrow(adapter, unityVersionString);

            return adapter;
        }

        // Resolveから分離し、テストからフェイクアダプターを直接渡して検証できるようにする
        internal static void ValidateOrThrow(IAnimatorInternalsAdapter adapter, string unityVersionString)
        {
            try
            {
                adapter.Validate();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"KSAnimatorCopyEngine: Unity {unityVersionString} では内部API({adapter.GetType().Name})の解決に失敗しました。" +
                    "Unityのバージョンアップにより内部実装が変更された可能性があります。", e);
            }
        }

        private static int ParseMajorVersion(string unityVersionString) => int.Parse(unityVersionString.Split('.')[0]);
    }
}
