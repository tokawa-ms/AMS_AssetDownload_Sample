# AMS_AssetDownload_Sample
Azure Media Services のアセットの中に入っている動画や字幕などのファイル一式をローカルのディレクトリにダウンロードするためのスクリプトのサンプル。

# How To Use
1. appsettings.tmp.json を appsettings.json にリネーム
1. appsettings.json に、Azure Media Services のアカウントにアクセスするためのサービスプリンシパルの情報を入力
	1. Azure CLI で az ams account sp create --account-name amsaccount --resource-group amsResourceGroup コマンドで作るのが楽。
1. StorageConfig.tmp.json を StorageConfig.json にリネーム
	1. "storageConnectionString" の後ろには AMS で使っているストレージの接続文字列を入力
	1. "downloadBasePath" の後ろにはダウンロード先のローカルパスを入力
1. Visual Studio 2019 でビルドして実行したら、そのまま対象の Media Services の中にあるアセット一式をダウンロードしてきます。