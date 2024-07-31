import csv
import os
import platform
from enum import Enum
from threading import Thread

import numpy as np
from PIL import Image

import ik_server
from plan_llm import PlanModule
from mlagents_envs.base_env import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel

# Get the current working directory
current_path = os.getcwd()
# Detect the operating system
system = platform.system()

# Dictionary mapping operating systems to their respective executable paths
exe_paths = {
    "Darwin": os.path.join(current_path, "builds/mac/build.app/Contents/MacOS/FM-RL-Unity"),
    "Windows": os.path.join(current_path, r"builds\win\FM-RL-Unity.exe"),
    "Linux": os.path.join(current_path, "builds/linux/env.x86_64")
}

img_paths = {
    "Darwin": os.path.join(current_path, "builds/mac/build.app/Contents/Screenshots/"),
    "Windows": os.path.join(current_path, r"builds\win\FM-RL-Unity_Data\Screenshots\\"),
    "Linux": os.path.join(current_path, "builds/linux/FM-RL-Unity_Data/Screenshots/")
}


class Position(Enum):
    ForceStop = 50
    RampTopR = 10
    RampBottomR = 9
    RampTopL = 8
    RampBottomL = 7
    BridgeFar = 6
    BridgeNear = 5
    RedBox = 4
    BlueBox = 3
    YellowBox = 2
    Goal = 1
    NoTarget = 0


class Module(Enum):
    LowLevelControl = 0
    SimplifiedControl = 1
    SkillBasedControl = 2


class SkillBasedEnv:

    def __init__(self):
        self.ikserver = Thread(target=ik_server.serve)
        self.ikserver.start()

        self.plans = PlanModule()
        # self.plans.query("There are some boxes that are away from the agent. I want the yellow box to be moved to the goal!")
        self.plans.query(
            "There are some boxes that are away from the agent. I want the yellow box to be moved to the goal! After that is done, I also want the red box to be moved to the goal! Finally, move the blue box to the goal!")

        try:
            exe_file = exe_paths[system]
        except KeyError:
            raise ValueError(f"Unknown operating system: {system}")

        print(f"Executable path: {exe_file}")

        engine = EngineConfigurationChannel()
        engine.set_configuration_parameters(time_scale=1, width=800,
                                            height=600)  # Can speed up simulation between steps with this
        engine.set_configuration_parameters(quality_level=0)

        print("Trying to connect to Unity Environment!")
        self.env = UnityEnvironment(
            file_name=exe_file,
            no_graphics=False,  # Can disable graphics if needed
            base_port=10030,  # for starting multiple envs
            side_channels=[engine])
        print("Unity Environment connected!")
        self.env.reset()

        self.actions = []
        self.current_action = ""
        self.current_target = Position.NoTarget

        self.move_target = Position.NoTarget
        self.pick_target = Position.NoTarget

    def run_env(self, ):
        behavior_name = "Lifter?team=0"

        for i in range(1000000):
            (decision_steps, terminal_steps) = self.env.get_steps(behavior_name)
            nr_agents = len(decision_steps.agent_id)
            action_tuple = ActionTuple()
            if nr_agents > 0:
                observations = decision_steps.obs[0]
                discrete = np.array([self.produce_discrete_action(observations[i][:]) for i in range(nr_agents)])
                action_tuple.add_discrete(discrete)
                action_tuple.add_continuous(np.zeros((nr_agents, 24)))
            else:
                action_tuple.add_continuous(np.zeros((0, 24)))
                action_tuple.add_discrete(np.zeros((0, 2)))

            self.env.set_actions(behavior_name, action_tuple)
            self.env.step()

    def produce_discrete_action(self, agent_obs):
        # All obs relative to agent
        goal_pos = agent_obs[0:3]
        hand_l_pos = agent_obs[3:6]
        hand_r_pos = agent_obs[6:9]
        box_red_pos = agent_obs[9:12]
        box_yellow_pos = agent_obs[12:15]
        box_blue_pos = agent_obs[15:18]
        ramp_l_bottom_pos = agent_obs[18:21]
        ramp_l_top_pos = agent_obs[21:24]
        ramp_r_bottom_pos = agent_obs[24:27]
        ramp_r_top_pos = agent_obs[27:30]
        bridge_far_pos = agent_obs[30:33]
        bridge_near_pos = agent_obs[33:36]
        move_to_done = agent_obs[36]
        pick_done = agent_obs[37]

        if os.path.isfile(img_paths[system] + "eyes.png"):
            eyes_image = Image.open(img_paths[system] + "eyes.png")

        self.actions = self.parse_plan()

        current_action_done = move_to_done if self.current_action == "Move To" else (
            pick_done if self.current_action == "Grasp" else 1)

        if current_action_done:
            if self.current_action == "Move To":
                self.move_target = Position.NoTarget
            (self.current_action, self.current_target) = self.parse_action(self.actions)

        if self.current_action == "Move To":
            self.move_target = self.current_target

        if self.current_action == "Grasp":
            self.pick_target = self.current_target

        if self.current_action == "Release":
            self.pick_target = Position.ForceStop

        agent_module = Module.SkillBasedControl.value
        camera = 1  # 0 - no change, 1 - Isometric, 2 - Third person behind, 3 - Third Person front
        reset_agent = 0
        screenshot = 3  # 0 - no screenshot saved, 1 - eyes only, 2 - map only, 3 - both
        return [agent_module, self.move_target.value, self.pick_target.value, camera, reset_agent, screenshot]

    def parse_action(self, actions):
        if len(actions) > 0:
            (skill, target) = actions.pop(0)
            if "blue box" in target:
                target = Position.BlueBox
            elif "yellow box" in target:
                target = Position.YellowBox
            elif "red box" in target:
                target = Position.RedBox
            elif "goal" in target:
                target = Position.Goal
            return skill, target
        return "", Position.ForceStop

    def parse_plan(self):
        if self.plans.output != "":
            lines = self.plans.output.splitlines()
            reader = csv.reader(lines)
            actions = list(reader)

            if actions[0][0].strip() == "skill" or actions[0][0].strip() == "action":
                actions.pop(0)
            print(actions)
            self.plans.output = ""
            return actions
        return self.actions


if __name__ == '__main__':
    env = SkillBasedEnv()
    env.run_env()
