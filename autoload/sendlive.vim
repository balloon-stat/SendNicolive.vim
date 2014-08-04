
let s:save_cpo = &cpo
set cpo&vim

let s:sendlive_is_run = 0

let s:path = expand("<sfile>:p:h")
python <<EOM
import sys, vim
if not vim.eval('s:path') in sys.path:
  sys.path.append(vim.eval('s:path'))
EOM

function! sendlive#run()
  if !has("python")
    echoerr "This pulgin needs python"
    return
  endif
  python import SendliveServer
  python import LocalGetRequest
  execute "python SendliveServer.run('".g:sendlive_port."')"
  let s:sendlive_is_run = 1
endfunction

function! sendlive#stop()
  if has("python") && s:sendlive_is_run
    python SendliveServer.stop()
    let s:sendlive_is_run = 0
  endif
endfunction

function! sendlive#query(cmd, text)
  let port = g:sendlive_port
  if has("python")
    if s:sendlive_is_run
      let body = iconv(a:text, &encoding, "utf-8")
      execute "python LocalGetRequest.query('".port."','".a:cmd."','".body."')"
    else
      echo "sendlive is not run"
    endif
  else
    call s:get_request(port, a:cmd, a:text)
  endif
endfunction

function! s:get_request(port, cmd, text)
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

