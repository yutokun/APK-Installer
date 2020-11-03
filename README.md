# APK Installer for Windows

ドロップすると接続している全てのデバイスに APK をインストールします。複数まとめてドロップすることも出来ます。ADB を内包しているので、事前のインストール等も不要です。

![Succeeded](./doc/Succeeded.gif)

## どうして作ったのか

`adb install -r path/to/apk` を叩くのが面倒になってしまって…

## ダウンロード

[ここをクリック](https://github.com/yutokun/APK-Installer/releases/latest/download/APKInstaller.exe)してダウンロードできます。

## できること

- ウインドウにドロップしてインストール
- exe ファイルにドロップしてインストール
- 複数 APK の一括インストール
- 接続している全てのデバイスへ一括インストール
- APK をダブルクリックしてインストール

## APK の関連付け

**関連付け**メニューから APK ファイルの関連付けを行うと、APK をダブルクリックしてインストールできるようになります。

![Association](./doc/Association.png)

![DoubleClick](./doc/DoubleClick.gif)

## その他

エラーと対策を少し優しく教えてくれます。

![Succeeded](./doc/Failed.gif)

## License

[Apache License 2.0](LICENSE)