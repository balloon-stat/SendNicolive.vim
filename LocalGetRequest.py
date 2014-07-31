import urllib
import urllib2

class LocalGetRequest:
    @staticmethod
    def query(port, cmd, text):
        body = urllib.quote(text)
        url = "http://localhost:{0}/{1}?{2}".format(port, cmd, body)
        res = urllib2.urlopen(url)
        print res.read()

