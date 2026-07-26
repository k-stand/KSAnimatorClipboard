# KS Animator Clipboard
アニメーター関連のデータをコピペする機能を提供するライブラリ

## 概要
`AnimatorController`を構成するLayer/State/Transition/BlendTree/StateMachineBehaviourなどのオブジェクトを、
参照関係(Motion、DestinationState、StateMachineBehaviourなど)を保ったままコピー&ペースト・クローンするための
Unity Editor拡張ライブラリです。

コピー元オブジェクトの集合を`AnimatorCopyClipSet`として保持し、そこからペースト・クローンを行う2段階の設計により、
「コピーしてから複数箇所に貼り付ける」「コピーしたセットを複数回クローンする」といった使い方が可能です。

クローン時、参照先オブジェクトをどう扱うか(複製する/参照を維持する/切り離してnullにする/未設定として例外を出す)は
`AnimatorCloner.ClonePolicy`(`Clone`/`KeepReference`/`Detach`/`UnSetting`)として、オブジェクトの種別(Kind)ごとに
登録します。標準で用意されていない型への対応や、クローン結果検証への参加は、内部的にはKindレジストリやプラグイン機構
(`IStateMachineBehaviourCloneResultValidator`など)によって実現されていますが、これらは本パッケージ内部限定の
仕組みであり、外部パッケージから拡張することはできません。

NDMFのVirtual Animator API(`nadena.dev.ndmf.animator`)向けの同等機能は
`com.github.k-stand.ksanimatorclipboard.ndmf`パッケージが提供します。

## インストール
### VCC(ALCOM)を利用する方法
1. https://k-stand.github.io/vpm-repos/ の`Add to VCC`を押してVCCにリポジトリを追加します。
2. 導入したいプロジェクトに`Animator Clipboard`をインストールしてください。

### VPAI unitypackageでVCCにインストールする方法
1. 以下から任意のバージョンの`com.github.k-stand.ksanimatorclipboard.X.x.x-installer.unitypackage`をダウンロードして、導入したいプロジェクトにインポートしてください。

0.x.x : [com.github.k-stand.ksanimatorclipboard.0.x.x-installer.unitypackage](https://github.com/k-stand/KSAnimatorClipboard/releases/download/0.2.1/com.github.k-stand.ksanimatorclipboard.0.x.x-installer.unitypackage)

## 使用方法
```csharp
// Layer単位でコピーして、別のAnimatorControllerへペースト
AnimatorCopyClipSet clipSet = AnimatorClipboard.Copy(sourceLayer, sourceController);
AnimatorClipboard.PasteLayers(clipSet, destController);

// State/Transition/BlendTreeなど任意のオブジェクトをコピーして、Layer内にペースト
AnimatorCopyClipSet objClipSet = AnimatorClipboard.Copy(sourceState, sourceLayer);
AnimatorClipboard.PasteIntoLayer(objClipSet, destLayer);

// 同じコピー内容を複数回クローンして、それぞれ別のStateMachineへ独立に貼り付ける
AnimatorCopyClipSet cloned = objClipSet.Clone(out Dictionary<UnityEngine.Object, UnityEngine.Object> clonedMap);
AnimatorClipboard.PasteIntoStateMachine(cloned, destStateMachine);
```

参照先オブジェクトのクローン方針(`ClonePolicy`)は、対応する`IAnimatorCopyObjectKind`実装の登録内容に従います。
未登録の型をクローンしようとした場合、`AnimatorCloner.ValidateRegistrations()`で事前に検出できます。

失敗しても例外を発生させたくない場合は、`Copy`/`PasteLayers`/`PasteIntoLayer`などに対応する`TryCopy`/`TryPasteLayers`/`TryPasteIntoLayer`(戻り値`bool`、結果は`out`引数)を使用してください。

## License
[MIT License](https://github.com/k-stand/KSAnimatorClipboard/blob/main/LICENSE.txt)

## 更新履歴
### [2026-07-26] 0.6.0  
- VRChatAvatars SDK固有の型への対応窓口だった`IParameterReferenceResolver`/`ParameterReferenceResolverRegistry`を削除(破壊的変更)。StateMachineBehaviourが参照するパラメーターは整合性チェックの検出対象外になりました
- README.mdからVRChatAvatars関連の記述を削除(本パッケージはVRChatAvatarsと無関係な汎用ライブラリのため)

### [2026-07-24] 0.5.1  
- README.mdを修正
### [2026-07-24] 0.5.0  
- コピー対象種別ごとの判定ロジックをKindレジストリ方式に再設計(内部実装、破壊的変更を含む)
- AnimatorClipboard.Copy(Behaviour)をCopy(StateMachineBehaviour)に修正(型の誤りを修正、破壊的変更)
- 重複していたAnimatorController探索処理をAnimatorGraphSchema/AnimatorGraphTraversalに統一し、公開範囲をinternalに整理
- Kindごとのクローン範囲判定(GetCloneScope)を実装
- BlendTree/MotionのClonePolicy不具合を修正
- StateMachineBehaviourのクローン結果を検証するプラグイン機構(IStateMachineBehaviourCloneResultValidator)を追加
- パラメーター整合性チェック(AnimatorClipboardParameterConsistency)向けの参照解決プラグイン機構(IParameterReferenceResolver)を追加
- 例外の型を用途ごとに具体化
- 公開型にXMLドキュメントコメントを追加
- EditModeテストを追加

### [2026-06-10] 0.4.2  
- AnimatorCopyClipSet.TypeでNullReferenceExceptionが出る問題の修正の再修正

### [2026-06-10] 0.4.1  
- AnimatorCopyClipSet.TypeでNullReferenceExceptionが出る問題の修正

### [2026-06-09] 0.4.0  
- AnimatorCloner.ValidateRegistrations()メソッドを追加。ClonePolicyが未設定のオブジェクトを検出できます。
- AnimatorClipboardUtility.ValidateCloneResult()メソッドを追加。Animator関連オブジェクトが無効な参照を保有していないか検証できます。
- AnimatorClipboardシステムのアクセシビリティの整理
- クローンシステムをブーリアンによるホワイトリスト形式から、AnimatorCloner.ClonePolicy列挙型形式に変更
- CopyClipBaseクラスのジェネリック型パラメータを削除
- 内部用のコンテキストキーをenum化して汎用コンテキストと分離
- ContextsSettingInternal内の処理を軽量化
- その他複数のリファクタリング、バグ修正

### [2026-05-26] 0.3.0  
- ソースコードのリファクタリング
- 複数の機能の追加
- 複数の非公開だった処理を解放
- 複数の不具合の修正

### [2026-05-19] 0.2.2  
- README.md を修正

### [2026-05-18] 0.2.1  
- GitHubにて公開
