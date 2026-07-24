using System;
using System.Collections.Generic;

namespace com.github.k_stand.ksanimatorclipboard.editor.CrossController
{
    /// <summary>
    /// IParameterReferenceResolverの登録・解決を行うレジストリです。
    /// </summary>
    public sealed class ParameterReferenceResolverRegistry
    {
        /// <summary>
        /// プロセス全体で共有されるデフォルトインスタンスを取得します。外部パッケージはこのインスタンスにResolverを登録します。
        /// </summary>
        public static ParameterReferenceResolverRegistry Shared { get; } = CreateDefault();

        private readonly Dictionary<Type, IParameterReferenceResolver> _resolvers = new();

        /// <summary>
        /// IParameterReferenceResolverを登録します。同じBehaviourTypeが既に登録済みの場合は上書きされます。
        /// </summary>
        /// <param name="resolver">登録するresolver。</param>
        /// <exception cref="ArgumentNullException">resolverがnullの場合。</exception>
        public void Register(IParameterReferenceResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _resolvers[resolver.BehaviourType] = resolver;
        }

        /// <summary>
        /// 指定した型に対応するIParameterReferenceResolverの登録を解除します。
        /// </summary>
        /// <param name="behaviourType">登録を解除するBehaviourType。</param>
        public void Unregister(Type behaviourType) => _resolvers.Remove(behaviourType);

        internal IParameterReferenceResolver Resolve(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (_resolvers.TryGetValue(current, out IParameterReferenceResolver resolver))
                {
                    return resolver;
                }
            }

            return null;
        }

        private static ParameterReferenceResolverRegistry CreateDefault() => new();
    }
}
