# KS Animator Clipboard
アニメーター関連のデータをコピペする機能を提供するライブラリ

## 概要

## インストール
### VCC(ALCOM)を利用する方法
1. https://k-stand.github.io/vpm-repos/ の`Add to VCC`を押してVCCにリポジトリを追加します。
2. 導入したいプロジェクトに`Animator Clipboard`をインストールしてください。

### VPAI unitypackageでVCCにインストールする方法
1. 以下から任意のバージョンの`com.github.k-stand.ksanimatorclipboard.X.x.x-installer.unitypackage`をダウンロードして、導入したいプロジェクトにインポートしてください。

0.x.x : [com.github.k-stand.ksanimatorclipboard.0.x.x-installer.unitypackage](https://github.com/k-stand/KSAnimatorClipboard/releases/download/0.2.1/com.github.k-stand.ksanimatorclipboard.0.x.x-installer.unitypackage)

## 使用方法

## License
[MIT License](https://github.com/k-stand/KSAnimatorClipboard/blob/main/LICENSE.txt)

## 更新履歴
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
