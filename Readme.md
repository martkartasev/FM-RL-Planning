# Planning with Foundation Models and Reinforcement Learning

A simple mobile wheeled robot with an articulated body in a Unity scene.

## Installation

### Cloning
This project features a submodule. To ensure you get the submodule initialized when you set up your directory run the initial clone with

```
git clone --recursive git@github.com:martkartasev/FM-RL-Planning.git
```

If you already cloned but forgot the tag use

```
git submodule update --init --recursive
```
*Note: change to http if ssh is blocked. Update git path with `git submodule sync`*


### Simple install
To run the python example with a built version of the engine all you need is to install mlagents-envs and the keyboard module.

This is enough to run the binaries from Unity.

```
pip install -e ./ml-agents/ml-agents-envs
pip install pynput
```

### Development install with full Unity and MLAgents support

For this, I suggest having a dedicated python venv. Personally I am a big fan of Conda.

Suggested python version is 3.10.12.

```
conda create -n mlagents python=3.10.12 -y && conda activate mlagents

pip install -r requirements.txt
```

This install the mlagents version from your local directory. This is a version that is ahead of the current "released" branch, but has a few nice features, like Sentis.

For more details see https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Installation.md

Unpack the Plugins.zip package into the Assets folder in the Unity project to ensure full GRPC support.

### Permission issue
In case the executable cannot be run on linux, it can be the fact that there is a permission restriction. Run the following to the files:
```
chmod +x -R ./*
```

Similarly, for MAC

```
cd <PATH_TO_YOUR_APP>/build.app/Contents/MacOS/
chmod -R 777 FM-RL-Unity
```

### Protocol buffers

```
 python -m grpc_tools.protoc -I./ --python_out=./ --pyi_out=./ --grpc_python_out=./ ./ik.proto
 protoc -I ./ --csharp_out=./ --grpc_csharp_out=./ ./ik.proto
```





# To start experiments
To start the experiments you need to start three different programs: The web server, the plan parser, and the unity environment
### Start web server
```bash
sh init_webserver.sh
```
When prompted select the GPU architecture of your device. When done setting up, navigate to [localhost:7860](localhost:7860).

In the web interface navigate to the model tab and download the LLM of your choosing. We recommend using the Phi-3 model family. Choose the chat setting and not the default chat-instruct setting for the agent to get the most consistent behavior.

### Start plan parser
```bash
python parse_conversation.py
```

### Start unity environment
```bash
python skills_unity_new.py
```

If all things are running, a unity environment should pop up and you should communicate with the planning assistant in the web interface. Once you have confirmed a plan by writing "Execute plan!", the agent should start moving once a plan has been generated.