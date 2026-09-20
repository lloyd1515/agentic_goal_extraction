
from http.server import HTTPServer, SimpleHTTPRequestHandler
import os

class CORSRequestHandler(SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Access-Control-Allow-Origin', '*')
        self.send_header('Access-Control-Allow-Methods', 'GET, OPTIONS')
        self.send_header('Access-Control-Allow-Headers', '*')
        super().end_headers()
    def do_OPTIONS(self):
        self.send_response(200)
        self.end_headers()

os.chdir('/home/vlads4r4/dev/side-projects/Agentic__Transcribe_Goal_Todolist/implementation')
httpd = HTTPServer(('127.0.0.1', 8089), CORSRequestHandler)
print('Serving on 8089 with CORS')
httpd.serve_forever()
