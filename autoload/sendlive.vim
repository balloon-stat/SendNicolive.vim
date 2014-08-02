
let s:save_cpo = &cpo
set cpo&vim

let s:sendlive_is_run = 0

function! sendlive#run()
  if !has("python")
    echo "if_python is disabled"
    return
  endif
  let path = ""
  for ph in split(&rtp, ',')
    if filereadable(ph."/plugin/sendlive.vim")
      let path = ph
      break
    endif
  endfor
  if path == ""
    echo "Can not founc sendlive directory"
    return
  endif
  let save_cwd = getcwd()
  execute "cd ".path
  pyfile SendliveServer.py
  pyfile LocalGetRequest.py
  execute "python SendliveRun(".g:sendlive_port.")"
  execute "cd ".save_cwd
  let s:sendlive_is_run = 1
endfunction

function! sendlive#stop()
  if has("python") && s:sendlive_is_run
    python SendliveStop()
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
    call s:get(port, a:cmd, a:text)
  endif
endfunction

function! s:get(port, cmd, text)
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

