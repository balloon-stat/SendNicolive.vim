#! /usr/bin/python
# -*- encoding: utf-8 -*-

import sys
import urllib
import urllib2
import socket
import threading
import time
import datetime
import cookielib
from xml.etree import ElementTree

class ConnectNicolive:
    def __init__(self, cookie, lvid):
        self.lvid = lvid
        cj = cookielib.CookieJar()
        ck = cookielib.Cookie(version=0, name="user_session", value=cookie, domain="nicovideo.jp", port=None, port_specified=False, domain_specified=False, domain_initial_dot=False, path='/', path_specified=True, secure=False, expires=None, discard=True, comment=None, comment_url=None, rest={'HttpOnly': None}, rfc2109=False)
        cj.set_cookie(ck)
        ckprocesser = urllib2.HTTPCookieProcessor(cj)
        self.opener = urllib2.build_opener(ckprocesser)
        self.token = self.getToken()

    def getPlayerStatus(self):
        url = "http://live.nicovideo.jp/api/getplayerstatus?v=" + self.lvid
        res = self.opener.open(url).read()
        elem = ElementTree.fromstring(res)

        if elem.get("status") != "ok":
            return None
        addr       = elem.findtext(".//addr")
        port       = elem.findtext(".//port")
        thread     = elem.findtext(".//thread")
        base_time  = elem.findtext(".//base_time")
        user_id    = elem.findtext(".//user_id")
        is_premium = elem.findtext(".//is_premium")
        return (addr, port, thread, base_time, user_id, is_premium)

    def getPostkey(self, count, thread):
        block_no = count // 100
        url = "http://live.nicovideo.jp/api/getpostkey?thread=%s&block_no=%s" % (thread, block_no)
        res = self.opener.open(url).read()
        return res[8:]

    def getToken(self):
        url = "http://live.nicovideo.jp/api/getpublishstatus?v="
        res = self.opener.open(url + self.lvid).read()
        elem = ElementTree.fromstring(res)
        return elem.findtext(".//token")

    def sendMsg(self, body, mail=None):
        urlprefix = "http://watch.live.nicovideo.jp/api/broadcast/"
        if mail is None:
            query = [
                ("body",  body),
                ("token", self.token)
            ]
        else:
            query = [
                ("body",  body),
                ("mail",  mail),
                ("token", self.token)
            ]
        url = urlprefix + self.lvid + "?" + urllib.urlencode(query)
        res = self.opener.open(url).read()
        return res

    def is_publish(self):
        return len(self.token) > 0

class SendMsgThread(threading.Thread):
    def __init__(self, cliObj):
        threading.Thread.__init__(self)
        self.comc = cliObj

    def run(self):
        comc = self.comc
        try:
            if not comc.writable():
                print "Sendlive buffer is empty."
                return
            if comc.is_publish():
                print comc.api.sendMsg(comc.msg)
                comc.msg = ""
            else:
                comc.sendReq()
                comc.read()
                comc.msgtobuf()
                comc.write()
        except Exception as e:
            print e.message

class CommClient:
    def __init__(self, apiObj):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.api = apiObj
        info = apiObj.getPlayerStatus()
        if info is None:
            self.is_connect = False
            return
        (addr, port, thread, base_time, user_id, is_premium) = info
        host = socket.gethostbyname(addr)
        self.sock.connect( (host, int(port)) )
        self.thread = thread
        self.base_time = base_time
        self.user_id = user_id
        self.is_premium = is_premium
        self.buf = ""
        self.prev = ""
        self.count = 0
        self.cntBlock = 0
        self.postkey = ""
        self.msg = ""
        self.anonym = False
        self.is_connect = True

    def is_publish(self):
        return self.api.is_publish()

    def sendReq(self):
        try:
            buf = "<thread thread=\"" + self.thread + "\" version=\"20061206\" res_from=\"-1\"/>\0"
            self.sock.sendall(buf)
        except:
            self.is_connect = False

    def msgtobuf(self):
        msg = self.msg
        self.msg = ""
        if self.postkey == "" or self.count - self.cntBlock * 100 > 100:
            self.postkey = self.api.getPostkey(self.count, self.thread)
            self.cntBlock = self.count // 100
        if self.postkey == "":
            raise "Can not get postkey"
        anonymous = ""
        if self.anonym: anonymous = " mail=\"184\""
        srvTimeSpan = int(self.srvtime) - int(self.base_time)
        localTimeSpan = int(time.mktime(datetime.datetime.now().timetuple())) - self.datetimeStart
        vpos = str((srvTimeSpan + localTimeSpan) * 100)
        self.buf = "<chat thread=\"{0}\" ticket=\"{1}\" vpos=\"{2}\" postkey=\"{3}\" user_id=\"{4}\" premium=\"{5}\"{6}>{7}</chat>\0".format(
                    self.thread,
                    self.ticket,
                    vpos,
                    self.postkey,
                    self.user_id,
                    self.is_premium,
                    anonymous, msg)

    def read(self):
        res = ""
        while res == "":
            res = self.sock.recv(1024)
        if self.prev == "" or res.startswith("<chat"):
            xml = res
            self.prev = ""
        else:
            xml = self.prev + res
            self.prev = ""

        for line in xml.split("\0"):
            # print line.decode("utf-8")
            if line.startswith("<thread"):
                elem = ElementTree.fromstring(line)
                # self.count = int(elem.get("last_res"))
                self.ticket = elem.get("ticket")
                self.srvtime = elem.get("server_time")
                self.datetimeStart = int(time.mktime(datetime.datetime.now().timetuple()))
                continue

            if not line.endswith("</chat>"):
                self.prev = line
                continue

            if line.startswith("<chat_result"):
                continue

            if line.startswith("<chat"):
                elem = ElementTree.fromstring(line)
                text = elem.text + "\n"
                proc(elem.get("no"), elem.get("user_id"), text)
                if text == "/disconnect\n":
                    self.close()
                    self.is_connect = False
                    return
                self.count = int(elem.get("no"))
                continue

    def writable(self):
        return (len(self.msg) > 0) and self.is_connect

    def write(self):
        # print "send: " + self.buf.decode("utf-8")
        self.sock.sendall(self.buf)
        self.buf = ""

    def keepSession(self):
        try:
            while self.is_connect:
                time.sleep(600)
                self.sock.sendall("\0")
        except Exception as e:
            print e.message

    def close(self):
        print "close..."
        self.sock.close()
        self.is_connect = False

def proc(no, uid, text):
    print(no + ": " + uid)
    print text
    return

if __name__ == '__main__':
    if len(sys.argv) != 3:
        print "usage: ConnectNicolive.py <cookie> lvxxxxxx"
        exit()
    con = ConnectNicolive(sys.argv[1], sys.argv[2])
    comc = CommClient(con)
    comc.msg = "test"
    thread = SendMsgThread(comc)
    thread.start()
    thread.join()

