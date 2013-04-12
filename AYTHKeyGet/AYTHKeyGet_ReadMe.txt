==============================
AYTHKeyGet alternative
==============================

【説明】

2013年4月のRadikoアップデート以降、Radikaがフックのみでしか聴取できないという報告がたくさんあります。
IE10にアップデートすれば正常に動作するという報告もありますが、XPなどIE10がインストールできないOSでRadikoを聴くためにこれを作りました。

【セットアップ】

1. swfextract.exeをダウンロードする
SWFTools for Windows 9.1 以降に含まれるswfextract.exeが必要なのでダウンロードしてください。
現在は http://www.swftools.org/swftools-2013-04-09-1007.exe でダウンロードできますが、見つからない場合は次のページから探して下さい。

http://www.swftools.org/download.html
http://wiki.swftools.org/wiki/Main_Page

swfextract.exeはSWFToolsをインストールしたフォルダにあります。

2. RadikaがインストールされているフォルダにあるAYTHKeyGet.exeのファイル名を変更して下さい
AYTHKeyGet.exeのファイル名を、例えば、AYTHKeyGet.original.exeなどに変更して下さい。

3. 必要なファイルをRadikaがインストールされているをフォルダへコピーする
このプロジェクトまたはzipに含まれているAYTHKeyGet.exeと、1.でダウンロード・インストールしたswfextract.exeをRadikaがインストールされているフォルダにコピーします。
radika.exeと同じフォルダにこの２つのファイルがある必要があります。

4. Radikaを使ってみる
あとは普通にRadikaからチューナーにラジコを指定して再生・録音をすることができます。

【アンインストール（元に戻す）】

1. インストールしたAYTHKeyGet.exeを削除する
2. ファイル名を変更したAYTHKeyGet.exeを元に戻す
3. swfextract.exeを削除する

【注意事項】

ラジコ受信しかテストしていません。このツールを使うと、NHKの受信ができないなど他の問題が起きるかもしれません。
エラーなど問題が起きた時は、コマンドプロンプトから直接AYTHKeyGet.exeを起動して表示される内容をコピーしてどこかに貼ってくれると、対処できるかもしれません。

【既知の問題】

1. 次のエラーにより聴けない

radika.AMFReaderException: {level=error,code=NetConnection.Connect.Rejected,description=Connection failed: Application rejected connection.,clientid=XXXXXXXXXX,}
場所 radika.AMFAudioSharedClient.AMFAudioSharedReader.ReadPacket()
場所 radika.BaseTunerDevice.MyAMFReader.ReadPacket()
場所 radika.RadikaTask.<>c__DisplayClassb.<Initialize>b__a()

PACファイル等でradiko.jp/*.radiko.jpのみプロキシ サーバーを通している時にこの問題が起きます。インターネット オプションの接続設定から、インターネット接続すべてをプロキシ サーバー経由でするように設定してみて下さい。

【参考にしたページ】

https://gist.github.com/3956266
http://ux.getuploader.com/loveradiko/
http://backslash.ddo.jp/wordpress/archives/1021