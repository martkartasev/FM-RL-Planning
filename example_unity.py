import os
import platform
import numpy as np
from pynput import keyboard
from mlagents_envs.base_env import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel

current_keys = set()

pressing_key = None
def on_press(key):
    # Add the key to the set of currently pressed keys
    current_keys.add(key)
    
    global pressing_key
    # Convert the set to a frozenset for easy comparison
    pressing_key = frozenset(current_keys)

def on_release(key):
    # Remove the key from the set of currently pressed keys
    if key in current_keys:
        current_keys.remove(key)


def produce_continuous_action(agent_obs):
    relative_target = agent_obs[0:3]
    relative_goal_pos = agent_obs[3:6]

    continuous_actions = np.zeros(18)
    # You can try moving the box into the little square over the bridge
    if current_keys is None:
        return continuous_actions
    
    try:
        if keyboard.KeyCode(char='w') in current_keys:
            continuous_actions[0] = 1
        if keyboard.KeyCode(char='a') in current_keys:
            continuous_actions[1] = -1
        if keyboard.KeyCode(char='s') in current_keys:
            continuous_actions[0] = -1
        if keyboard.KeyCode(char='d') in current_keys:
            continuous_actions[1] = 1

        if keyboard.KeyCode(char='1') in current_keys:
            continuous_actions[2] = 1
            continuous_actions[5] = 1
        if keyboard.KeyCode(char='2') in current_keys:
            continuous_actions[10] = -0.1
            continuous_actions[14] = -0.1
        if keyboard.KeyCode(char='3') in current_keys:
            continuous_actions[10] = 0.2
            continuous_actions[14] = 0.2
            continuous_actions[12] = -0.25
            continuous_actions[16] = -0.25
            continuous_actions[13] = -1
            continuous_actions[17] = -1


    except Exception as e:
        print("Error pressing the keyboard:", e)
        pass

    return continuous_actions


def produce_discrete_action(agent_obs):
    relative_target = agent_obs[0:3]
    relative_goal_pos = agent_obs[3:6]

    agent_module = 0  # Enable a discrete module that will execute a skill
    reset_agent = 0  # 0 keeps going, 1 resets given agent

    try:
        if pressing_key == frozenset([keyboard.Key.space]):
            reset_agent = 1
    except Exception as e:
        print("Error pressing space:", e)
        pass

    return [agent_module, reset_agent]


def run_env(example_env):
    behavior_name = "Lifter?team=0"
    action_spec = example_env.behavior_specs.get(behavior_name).action_spec
    # print(action_spec.discrete_branches)
    observation_spec = example_env.behavior_specs.get(behavior_name).observation_specs[0]
    # print(observation_spec.shape)

    with keyboard.Listener(
            on_press=on_press,
            on_release=on_release) as listener:

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
                cont = np.array([produce_continuous_action(observations[i][:]) for i in range(nr_agents)])
                action_tuple.add_discrete(discrete)
                action_tuple.add_continuous(cont)
            else:
                action_tuple.add_continuous(np.zeros((0, 18)))
                action_tuple.add_discrete(np.zeros((0, 2)))

            example_env.set_actions(behavior_name, action_tuple)
            example_env.step()
        listener.join()

# You can change viewports by clicking on the game window and using 1 2 3 4
if __name__ == '__main__':
    engine = EngineConfigurationChannel()
    engine.set_configuration_parameters(time_scale=1)  # Can speed up simulation between steps with this
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
    
    env = UnityEnvironment(
        file_name=exe_file,
        no_graphics=False,  # Can disable graphics if needed
        base_port=10030,  # for starting multiple envs
        side_channels=[engine])
    env.reset()  # Initializes env
    run_env(env)
    env.reset()  # Resets everything in the executable (all agents)
