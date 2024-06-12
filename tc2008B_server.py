import json
from http.server import BaseHTTPRequestHandler, HTTPServer
import logging
from office_simulation import OfficeModel, read_office_layout, Robot, Trash

class Server(BaseHTTPRequestHandler):
    model = None

    @staticmethod
    def initialize_model():
        n, m, office_layout = read_office_layout('office_layout.txt')
        Server.model = OfficeModel(m, n, office_layout)

    @staticmethod
    def get_robot_positions():
        positions = []
        for agent in Server.model.schedule.agents:
            if isinstance(agent, Robot):
                positions.append({'id': agent.unique_id, 'x': agent.pos[0], 'y': agent.pos[1], 'collected_trash': agent.collected_trash})
        return positions

    @staticmethod
    def get_trash_positions():
        trash_positions = []
        for agent in Server.model.schedule.agents:
            if isinstance(agent, Trash):
                trash_positions.append({'x': agent.pos[0], 'y': agent.pos[1], 'amount': agent.amount})
        return trash_positions

    @staticmethod
    def get_office_layout():
        layout = []
        for row in Server.model.office_layout:
            layout.append(" ".join(row))
        return layout

    def _set_response(self):
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()

    def do_GET(self):
        self._set_response()
        response = {
            'robots': Server.get_robot_positions(),
            'trash': Server.get_trash_positions(),
            'office_layout': Server.get_office_layout()
        }
        self.wfile.write(json.dumps(response).encode('utf-8'))

    def do_POST(self):
        if Server.model is None:
            Server.initialize_model()

        Server.model.step()
        response = {
            'robots': Server.get_robot_positions(),
            'trash': Server.get_trash_positions(),
            'office_layout': Server.get_office_layout()
        }
        self._set_response()
        self.wfile.write(json.dumps(response).encode('utf-8'))

def run(server_class=HTTPServer, handler_class=Server, port=8585):
    logging.basicConfig(level=logging.INFO)
    server_address = ('', port)
    httpd = server_class(server_address, handler_class)
    logging.info("Starting httpd...\n")
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        pass
    httpd.server_close()
    logging.info("Stopping httpd...\n")

if __name__ == '__main__':
    from sys import argv

    if len(argv) == 2:
        run(port=int(argv[1]))
    else:
        run()
