#! /usr/bin/python
# -*- encoding: utf-8 -*-

import threading
import BaseHTTPServer
import os, sys, urllib
sys.path.append(os.getcwd())
import ConnectNicolive as CN

httpd = None
commClient = None
anonymous = False

class SendLiveRequestHandler(BaseHTTPServer.BaseHTTPRequestHandler):

    def do_GET(self):
        global commClient
        global anonymous
        self.send_response(200)
        self.send_header("Content-type", "text/plain")
        self.end_headers()

        if self.path.startswith("/connect"):
            if not commClient is None:
                commClient.close()
            lvid = self.path[self.path.index("?")+1:self.path.index("=")]
            cookie = self.path[self.path.index("=")+1:]
            con = CN.ConnectNicolive(cookie, lvid)
            commClient = CN.CommClient(con)
            th = threading.Thread(target=commClient.keepSession)
            th.daemon = True
            th.start()
            print("connect: " + lvid)
        elif self.path.startswith("/send"):
            if commClient == None:
                self.wfile.write("no connect")
                self.wfile.close()
                return
            query = self.path[self.path.index("?")+1:]
            commClient.msg = urllib.unquote(query)
            commClient.anonym = anonymous
            CN.SendMsgThread(commClient).start()
            self.wfile.write("ok")
            self.wfile.close()
        elif self.path.startswith("/set"):
            query = self.path[self.path.index("?")+1:]
            if query == "anonymous":
                anonymous = True
            elif query == "noanonymous":
                anonymous = False
            self.wfile.write("anonymous: " + anonymous)
            self.wfile.close()
        elif self.path.startswith("/recv"):
            if commClient == None:
                self.wfile.write("no connect")
                self.wfile.close()
                return
            commClient.sendReq()
            commClient.read()

    def log_message(self, format, *args):
        return

def SendliveRun(port):
    global httpd
    HandlerClass = SendLiveRequestHandler
    ServerClass  = BaseHTTPServer.HTTPServer
    Protocol     = "HTTP/1.0"

    server_address = ('127.0.0.1', port)

    HandlerClass.protocol_version = Protocol
    httpd = ServerClass(server_address, HandlerClass)

    sa = httpd.socket.getsockname()
    print "Serving HTTP on", sa[0], "port", sa[1], "..."
    #httpd.serve_forever()
    th = threading.Thread(target=httpd.serve_forever)
    th.daemon = True
    th.start()

def SendliveStop():
    global commClient
    global httpd
    if not commClient is None:
        commClient.is_connect = False
        commClient = None
    if not httpd is None:
        httpd.shutdown()
        httpd = None

if False:
#if __name__ == '__main__':
    SendliveRun(8000)
    while True:
        if raw_input() == 'q':
            print "close..."
            break
