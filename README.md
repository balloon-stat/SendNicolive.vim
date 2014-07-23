SendNicolive.vim
================

vimからニコニコ生放送へコメントするためのWebサーバとvim側のクライアントです。

#### ローカルWebサーバの実行

SendNicolive.csがWebサーバのソースコードです。  
SendNicolive.csをコンパイルして、引数に必要な情報を付けて実行してください。  
初回実行時は、  

```
SendNicolive http://localhost:8000/ --cookie <nico-cookie>
```

として、クッキーを与えるか、もしくは、

```
SendNicolive http://localhost:8000/ --login <email-address> <password>
```

としてニコニコへログインしてください。  
ログインできればホームディレクトリにクッキーの情報が保存されるので次回からは、

```
SendNicolive http://localhost:8000/ --continue
```

とすれば保存されたクッキーを使います。

#### コメントサーバへの接続

以下のリンクのブックマークレットを作るのがおすすめです。

```
javascript:(function(){var%20url=location.href.substring(0,location.href.indexOf("?")).replace("live.nicovideo.jp/watch/","localhost:8000/connect?");var%20xhr=new%20XMLHttpRequest();xhr.open('GET',url,true);xhr.send();})()
```

放送ページを開いた後にブックマークレットを実行すると  
SendNicoliveがニコニコのコメントサーバに接続します。  

vimで  

```
:SendToLive {string}  
```

とすると`{string}`とコメントします。

#### vimの設定例

```
nnoremap gl :<C-u>SendToLive <C-^>
```



