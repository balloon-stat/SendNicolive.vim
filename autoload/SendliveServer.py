#! /usr/bin/python
# -*- encoding: utf-8 -*-

import threading
import BaseHTTPServer
import urllib
import ConnectNicolive as CN

httpd = None
commClient = None
anonymous = False
is_debug = False

class SendLiveRequestHandler(BaseHTTPServer.BaseHTTPRequestHandler):

    def do_GET(self):
        global commClient
        global anonymous
        self.send_response(200)
        self.send_header("Content-type", "text/plain")
        self.end_headers()

        if self.path.startswith("/connect"):
            if not commClient is None and commClient.is_connect:
                commClient.is_connect = False
                commClient.close()
            lvid = self.path[self.path.index("?")+1:self.path.index("=")]
            cookie = self.path[self.path.index("=")+1:]
            con = CN.ConnectNicolive(cookie, lvid)
            commClient = CN.CommClient(con)
            if not commClient.is_connect:
                print("Can not connect: " + lvid)
                commClient = None
                return
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
            self.wfile.write("anonymous: " + str(anonymous))
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

def run(port):
    global httpd
    HandlerClass = SendLiveRequestHandler
    ServerClass  = BaseHTTPServer.HTTPServer
    Protocol     = "HTTP/1.0"

    server_address = ('127.0.0.1', int(port))

    HandlerClass.protocol_version = Protocol
    httpd = ServerClass(server_address, HandlerClass)

    sa = httpd.socket.getsockname()
    print "Serving HTTP on", sa[0], "port", sa[1], "..."
    #httpd.serve_forever()
    th = threading.Thread(target=httpd.serve_forever)
    th.daemon = True
    th.start()

def stop():
    global commClient
    global httpd
    if not commClient is None:
        commClient.is_connect = False
        commClient.close()
        commClient = None
    if not httpd is None:
        httpd.shutdown()
        httpd = None

if is_debug and __name__ == '__main__':
    run('8000')
    while True:
        if raw_input() == 'q':
            print "Quit sendlive"
            break
