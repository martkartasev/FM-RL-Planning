import numpy as np
import keyboard
from mlagents_envs.base_env import ActionTuple
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel


def produce_continuous_action(agent_obs):
    relative_target = agent_obs[0:3]
    relative_goal_pos = agent_obs[3:6]

    continuous_actions = np.zeros(18)
    # You can try moving the box into the little square over the bridge
    try:
        if keyboard.is_pressed('w'):
            continuous_actions[0] = 1
        if keyboard.is_pressed('a'):
            continuous_actions[1] = -1
        if keyboard.is_pressed('s'):
            continuous_actions[0] = -1
        if keyboard.is_pressed('d'):
            continuous_actions[1] = 1

        if keyboard.is_pressed('1'):
            continuous_actions[2] = 1
            continuous_actions[5] = 1
        if keyboard.is_pressed('2'):
            continuous_actions[10] = -0.1
            continuous_actions[14] = -0.1
        if keyboard.is_pressed('3'):
            continuous_actions[10] = 0.2
            continuous_actions[14] = 0.2
            continuous_actions[12] = -0.25
            continuous_actions[16] = -0.25
            continuous_actions[13] = -1
            continuous_actions[17] = -1


    except:
        pass

    return continuous_actions


def produce_discrete_action(agent_obs):
    relative_target = agent_obs[0:3]
    relative_goal_pos = agent_obs[3:6]

    agent_module = 0  # Enable a discrete module that will execute a skill
    reset_agent = 0  # 0 keeps going, 1 resets given agent

    try:
        if keyboard.is_pressed('space'):
            reset_agent = 1
    except:
        pass

    return [agent_module, reset_agent]


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
            cont = np.array([produce_continuous_action(observations[i][:]) for i in range(nr_agents)])
            action_tuple.add_discrete(discrete)
            action_tuple.add_continuous(cont)
        else:
            action_tuple.add_continuous(np.zeros((0, 18)))
            action_tuple.add_discrete(np.zeros((0, 2)))

        example_env.set_actions(behavior_name, action_tuple)
        example_env.step()

# You can change viewports by clicking on the game window and using 1 2 3 4
if __name__ == '__main__':
    engine = EngineConfigurationChannel()
    engine.set_configuration_parameters(time_scale=1)  # Can speed up simulation between steps with this
    engine.set_configuration_parameters(quality_level=0)

    env = UnityEnvironment(
        file_name=r"C:\Users\Mart9\Workspace\FM-RL-Planning\builds\win\FM-RL-Unity.exe",
        no_graphics=False,  # Can disable graphics if needed
        base_port=10030,  # for starting multiple envs
        side_channels=[engine])
    env.reset()  # Initializes env
    run_env(env)
    env.reset()  # Resets everything in the executable (all agents)
