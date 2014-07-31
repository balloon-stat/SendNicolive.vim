
if exists('g:loaded_sendlive')
  finish
endif

let s:save_cpo = &cpo
set cpo&vim

command! -nargs=0 SendliveRun call sendlive#run()
command! -nargs=1 SendliveMessage call sendlive#query("send", <q-args>)
command! -nargs=1 SendliveConnect call sendlive#query("connect", <q-args>)
command! -nargs=0 SendliveIsAnonymous call sendlive#query("set", "isanonymous")
command! -nargs=0 SendliveSetAnonymous call sendlive#query("set", "anonymous")
command! -nargs=0 SendliveSetNoAnonymous call sendlive#query("set", "noanonymous")

let g:sendlive_port = get(g:, 'sendlive_port', '8000')
let g:loaded_sendlive = 1

let &cpo = s:save_cpo
unlet s:save_cpo

