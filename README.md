# LightmapSettingsPrefab
## 概要
Lightmap情報をプレハブに保存して、ランタイムで反映します。 
![スクリーンショット](screenshot.jpg)

## 事前準備
1. Lighting ウィンドウにある全タブのAuto GenerateをOFFにします。 

## アセット作成方法
1. 任意のシーンを開きます。
2. ヒエラルキーにあるプレハブのルートオブジェクトにLightmapSettingsPrefabをAddします。
3. Lighting ウィンドウで任意の設定を行います。
4. 2のLightmapSettingsPrefabにあるBakeボタンを押します。

## アセット使用方法
1. アセット作成方法で作ったプレハブをロードします。

## ポイント
- ライトマップの変更にシーンチェンジを必要としません。 
- オブジェクトごとにライトマップを設定するので1シーンに複数のライトマップを扱うことができます。 
