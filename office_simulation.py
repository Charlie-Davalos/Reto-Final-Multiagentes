import numpy as np
from mesa import Agent, Model
from mesa.space import MultiGrid
from mesa.time import RandomActivation
import re

class Trash(Agent):
    def __init__(self, unique_id, model, amount):
        super().__init__(unique_id, model)
        self.amount = amount

    def step(self):
        pass

class Robot(Agent):
    def __init__(self, unique_id, model):
        super().__init__(unique_id, model)
        self.collected_trash = 0
        self.returning = False  
        self.origin_pos = None  # Almacena la posición original del robot antes de ir al trashcan

    def move_to(self, destination):
        current_x, current_y = self.pos
        dest_x, dest_y = destination
        possible_steps = []

        if current_x < dest_x:
            possible_steps.append((current_x + 1, current_y))
        elif current_x > dest_x:
            possible_steps.append((current_x - 1, current_y))

        if current_y < dest_y:
            possible_steps.append((current_x, current_y + 1))
        elif current_y > dest_y:
            possible_steps.append((current_x, current_y - 1))

        possible_steps = [pos for pos in possible_steps if not self.model.grid.out_of_bounds(pos)]
        possible_steps = [pos for pos in possible_steps if not any(isinstance(agent, Robot) for agent in self.model.grid.get_cell_list_contents(pos))]
        possible_steps = [pos for pos in possible_steps if self.model.office_layout[pos[1]][pos[0]] != 'X']

        if possible_steps:
            new_position = self.random.choice(possible_steps)
            self.model.grid.move_agent(self, new_position)

    def step(self):
        if self.collected_trash < 5 and not self.returning:
            possible_steps = self.model.grid.get_neighborhood(self.pos, moore=True, include_center=False)
            possible_steps = [pos for pos in possible_steps if not self.model.grid.out_of_bounds(pos)]
            possible_steps = [pos for pos in possible_steps if not any(isinstance(agent, Robot) for agent in self.model.grid.get_cell_list_contents(pos))]
            possible_steps = [pos for pos in possible_steps if self.model.office_layout[pos[1]][pos[0]] != 'X']

            if possible_steps:
                new_position = self.random.choice(possible_steps)
                self.model.grid.move_agent(self, new_position)

                contents = self.model.grid.get_cell_list_contents([self.pos])
                for content in contents:
                    if isinstance(content, Trash):
                        amount = min(5 - self.collected_trash, content.amount)
                        self.collected_trash += amount
                        content.amount -= amount
                        if content.amount == 0:
                            self.model.grid.remove_agent(content)
                            self.model.schedule.remove(content)

                if self.collected_trash == 5:
                    self.returning = True
                    self.origin_pos = self.pos
        else:
            if self.returning:
                if self.pos == self.model.trashcan_pos:
                    self.model.trashcan.amount += self.collected_trash
                    self.collected_trash = 0
                    self.returning = False
                else:
                    self.move_to(self.model.trashcan_pos)
            else:
                if self.pos == self.origin_pos:
                    self.returning = False
                else:
                    self.move_to(self.origin_pos)

class TrashCan(Agent):
    def __init__(self, unique_id, model):
        super().__init__(unique_id, model)
        self.amount = 0

    def step(self):
        pass

class OfficeModel(Model):
    def __init__(self, width, height, office_layout):
        super().__init__()
        self.grid = MultiGrid(width, height, False)
        self.schedule = RandomActivation(self)

        self.office_layout = office_layout
        self.trashcan_pos = None
        self.trashcan = None

        for y in range(height):
            for x in range(width):
                if office_layout[y][x] == 'P':
                    self.trashcan_pos = (x, y)
                    self.trashcan = TrashCan(1, self)
                    self.grid.place_agent(self.trashcan, self.trashcan_pos)
                    self.schedule.add(self.trashcan)
                elif office_layout[y][x] != 'X' and office_layout[y][x] != '0':
                    amount = int(office_layout[y][x])
                    trash = Trash((x, y), self, amount)
                    self.grid.place_agent(trash, (x, y))
                    self.schedule.add(trash)

        robot_count = 0
        while robot_count < 5:
            x = self.random.randrange(width)
            y = self.random.randrange(height)
            if office_layout[y][x] == '0':
                if not any(isinstance(agent, Robot) for agent in self.grid.get_cell_list_contents((x, y))):
                    robot = Robot(robot_count, self)
                    self.grid.place_agent(robot, (x, y))
                    self.schedule.add(robot)
                    robot_count += 1

    def step(self):
        self.schedule.step()

def read_office_layout(file_path):
    with open(file_path, 'r') as file:
        lines = file.readlines()
    n, m = map(int, re.split(r'[,\s]', lines[0].strip()))
    office_layout = [re.split(r'[,\s]', line.strip()) for line in lines[1:]]
    return n, m, office_layout
