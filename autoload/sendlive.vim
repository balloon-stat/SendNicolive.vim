
let s:save_cpo = &cpo
set cpo&vim

function! sendlive#query(cmd, text)
  let port = g:sendlive_port
  "let command = 'wget -q '
  let command = 'curl -s '
  let body = substitute(a:text, '[^a-zA-Z0-9_.~/-]', '\=s:urlencode_char(submatch(0))', 'g')
  let url = 'http://localhost:'.port.'/'
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

let &cpo = s:save_cpo
unlet s:save_cpo

