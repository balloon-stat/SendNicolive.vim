SendNicolive.vim
================

vim����j�R�j�R�������փR�����g���邽�߂�Web�T�[�o��vim���̃N���C�A���g�ł��B

���[�J��Web�T�[�o�̎���
-----------------------

SendNicolive.cs��Web�T�[�o�̃\�[�X�R�[�h�ł��B  
SendNicolive.cs���R���p�C�����āA�����ɕK�v�ȏ���t���Ď��s���Ă��������B  
������s���́A  

SendNicolive http://localhost:8000/ --cookie <nico-cookie>

�Ƃ��āA�N�b�L�[��^���邩�A�������́A

SendNicolive http://localhost:8000/ --login <email-address> <password>

�Ƃ��ăj�R�j�R�փ��O�C�����Ă��������B  
���O�C���ł���΃z�[���f�B���N�g���ɃN�b�L�[�̏�񂪕ۑ������̂Ŏ��񂩂�́A

SendNicolive http://localhost:8000/ --continue

�Ƃ���Εۑ����ꂽ�N�b�L�[���g���܂��B

�R�����g�T�[�o�ւ̐ڑ�
----------------------

�ȉ��̃����N�̃u�b�N�}�[�N���b�g�����̂��������߂ł��B

<a href="javascript:(function(){var%20url=location.href.substring(0,location.href.indexOf("?")).replace("live.nicovideo.jp/watch/","localhost:8000/connect?");var%20xhr=new%20XMLHttpRequest();xhr.open('GET',url,true);xhr.send();})()">
    connect
</a>

�����y�[�W���J������Ƀu�b�N�}�[�N���b�g�����s�����  
SendNicolive���j�R�j�R�̃R�����g�T�[�o�ɐڑ����܂��B  

vim��  
:SendToLive {string}<CR>  
�Ƃ����{string}�ƃR�����g���܂��B

vim�̐ݒ��
-----------

nnoremap gl :<C-u>SendToLive <C-^>



