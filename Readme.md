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
*Note: change to http if ssh is blocked*

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
conda create -n mlagents python=3.10.12 && conda activate mlagents

pip3 install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121

pip install -e ./ml-agents/ml-agents-envs
pip install -e ./ml-agents/ml-agents
pip install transformers
pip install accelerate
pip install bitsandbytes
```

This install the mlagents version from your local directory. This is a version that is ahead of the current "released" branch, but has a few nice features, like Sentis.

For more details see https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Installation.md

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
