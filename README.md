SendNicolive.vim
================

vimからニコニコ生放送へコメントするためのHTTPサーバとvim側のクライアントです。

#### ローカルHTTPサーバの実行

サーバはPython版とC#版があります。  
Python版は2.7でのみ動作確認をしています。  
if_pythonが使えるならば、  

```
:SendliveRun
```

でサーバが立ち上がります。

C#版はコンパイルして実行する必要があります。  
SendNicolive.csがサーバのソースコードです。  

#### コメントサーバへの接続

以下の内容のブックマークレットを作るのがおすすめです。

```
javascript:(function(){if(location.href.indexOf("live.nicovideo.jp")==-1){alert("Run%20with%20live.nicovideo.jp");return;}var%20ck=document.cookie;var%20idx=ck.indexOf("user_session");var%20ckVal=ck.substring(idx,ck.indexOf(";",idx)==-1?ck.length:ck.indexOf(";",idx)).replace("user_session=","");var%20url=location.href.substring(0,location.href.indexOf("?")==-1?location.href.length:location.href.indexOf("?")).replace("live.nicovideo.jp/watch/","localhost:8000/connect?")+"="+ckVal;var%20xhr=new%20XMLHttpRequest();xhr.open("GET",url,true);xhr.send();})()
```

放送ページを開いた後にブックマークレットを実行すると  
SendNicoliveがニコニコのコメントサーバに接続します。  

無事、接続できればvimで  

```
:SendliveMessage {string}  
```

とすると`{string}`とコメントできます。

```
:SendliveSetNoAnonymaous
```

で184の解除、

```
:SendliveSetAnonymaous
```

で184の再設定、

```
:SendliveIsAnonymaous
```

で確認ができます。

#### vimの設定例

例えば、以下のような設定をvimrcに書いておくと便利です。

```
nnoremap gl :<C-u>SendliveMessage <C-^>
```

