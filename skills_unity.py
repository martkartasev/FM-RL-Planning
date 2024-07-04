import os
import platform
import time
from enum import Enum
from threading import Thread

import numpy as np

import ik_server
from mlagents_envs.base_env import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel


class Position(Enum):
    ForceStop = 9
    RampTopR = 8
    RampBottomR = 7
    RampTopL = 6
    RampBottomL = 5
    BridgeFar = 4
    BridgeNear = 3
    Goal = 2
    Box = 1
    NoTarget = 0


class Module(Enum):
    LowLevelControl = 0
    SimplifiedControl = 1
    SkillBasedControl = 2


def produce_discrete_action(agent_obs):
    # All obs relative to agent
    box_pos = agent_obs[0:3]
    hand_l_pos = agent_obs[3:6]
    hand_r_pos = agent_obs[6:9]
    goal_pos = agent_obs[9:12]
    ramp_l_bottom_pos = agent_obs[12:15]
    ramp_l_top_pos = agent_obs[15:18]
    ramp_r_bottom_pos = agent_obs[18:21]
    ramp_r_top_pos = agent_obs[21:24]
    bridge_far_pos = agent_obs[24:27]
    bridge_near_pos = agent_obs[27:30]

    agent_module = Module.SkillBasedControl.value
    pick_target = Position.NoTarget.value
    move_target = Position.Box.value

    if np.linalg.norm(box_pos) < 1.1:
        move_target = Position.NoTarget.value
        pick_target = Position.Box.value

    camera = 2
    reset_agent = 0
    return [agent_module, move_target, pick_target, camera, reset_agent]


def run_env(example_env):
    behavior_name = "Lifter?team=0"
    action_spec = example_env.behavior_specs.get(behavior_name).action_spec
    # print(action_spec.discrete_branches)
    observation_spec = example_env.behavior_specs.get(behavior_name).observation_specs[0]
    # print(observation_spec.shape)

    for i in range(10000):
        (decision_steps, terminal_steps) = example_env.get_steps(behavior_name)
        # Decision steps -> agent info that needs an action this step
        # Terminal steps -> agent info whose episode has ended
        nr_agents = len(decision_steps.agent_id)
        # print(nr_agents)
        # print(decision_steps.agent_id)
        action_tuple = ActionTuple()
        if nr_agents > 0:
            observations = decision_steps.obs[0]  # Strange structure, but this is how you get the observations array
            discrete = np.array([produce_discrete_action(observations[i][:]) for i in range(nr_agents)])
            action_tuple.add_discrete(discrete)
            action_tuple.add_continuous(np.zeros((nr_agents, 24)))
        else:
            action_tuple.add_continuous(np.zeros((0, 24)))
            action_tuple.add_discrete(np.zeros((0, 2)))

        example_env.set_actions(behavior_name, action_tuple)
        example_env.step()


# You can change viewports by clicking on the game window and using 1 2 3 4
if __name__ == '__main__':
    engine = EngineConfigurationChannel()
    engine.set_configuration_parameters(time_scale=1, width=800,
                                        height=600)  # Can speed up simulation between steps with this
    engine.set_configuration_parameters(quality_level=0)

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

    # Get the executable path based on the operating system
    try:
        exe_file = exe_paths[system]
    except KeyError:
        raise ValueError(f"Unknown operating system: {system}")

    print(f"Executable path: {exe_file}")

    ikserver = Thread(target=ik_server.serve).start()
    print("Trying to connect to Unity Environment!")
    env = UnityEnvironment(
        file_name=exe_file,
        no_graphics=False,  # Can disable graphics if needed
        base_port=10030,  # for starting multiple envs
        side_channels=[engine])
    print("Unity Environment connected!")
    env.reset()
    run_env(env)
    env.reset()
