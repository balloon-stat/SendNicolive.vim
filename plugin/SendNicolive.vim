command! -nargs=1 SendToLive call QueryForLive("send", "<args>")
command! -nargs=1 ConnectLive call QueryForLive("connext", "<args>")

function! QueryForLive(query, text)
  let command = 'wget -q '
  let body = substitute(a:text, '[^a-zA-Z0-9_.~/-]', '\=s:urlencode_char(submatch(0))', 'g')
  let url = 'http://localhost:8000/'
  let res = system(command.url.a:query."?".body)
  echo res
endfunction

function! s:urlencode_char(c)
  let utf = iconv(a:c, &encoding, "utf-8")
  if utf == ""
    let utf = a:c
  endif
  let s = ""
  for i in range(strlen(utf))
    let s .= printf("%%%02X", char2nr(utf[i]))
  endfor
  return s
endfunction

