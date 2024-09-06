import csv
import os
import platform
from PIL import Image

import torch
import transformers
from transformers import AutoModelForCausalLM, AutoTokenizer, pipeline, AutoProcessor

# Get the version of the transformers library
version = transformers.__version__
required_version = "4.44.2"

assert (
    version >= required_version
), f"The huggingface version ({version}) is below {required_version}. Please upgrade."

torch.random.manual_seed(0)

# Get the current working directory
current_path = os.getcwd()
# Detect the operating system
system = platform.system()

img_paths = {
    "Darwin": os.path.join(current_path, "builds/mac/build.app/Contents/Screenshots/"),
    "Windows": os.path.join(current_path, r"builds\win\FM-RL-Unity_Data\Screenshots\\"),
    "Linux": os.path.join(current_path, "builds/linux/FM-RL-Unity_Data/Screenshots/"),
}


class PlanModule:
    def __init__(self, model_name="microsoft/Phi-3-mini-128k-instruct", device="cuda"):
        self.generation_args = {
            "max_new_tokens": 500,
            "return_full_text": False,
            "temperature": 0.0,
            "do_sample": False,
        }

        self.messages = [
            {
                "role": "system",
                "content": "You are an AI agent. I refer to you as a agent 1. There exist another AI agent called agent 2. Agent 2 has had a conversation "
                "with a human about planning a task for a robot."
                "The robot can only use the skills: Release, Grasp, Move To, Push. "
                "The Grasp skill can only be used if the robot is near a target first. "
                "A target should either be one an object or a position! Avoid using prepositions for targets. "
                "Not all skills need to be used. "
                "Your goal (agent 1) is to look at a final message between agent 2 and the human that contains a proposed plan. "
                "You should process this message and provide a plan as a list of comma separated values (CSV). "
                "There should be two columns: action and target. Do not include quotation marks. "
                "Your response should contain anything but the CSV message (i.e. no explatanation or descriptions). ",
            },
        ]
        self.output = ""

        self.model_name = model_name
        self.model = AutoModelForCausalLM.from_pretrained(
            self.model_name,
            device_map=device,
            torch_dtype="auto",
            trust_remote_code=True,
        )
        self.tokenizer = AutoTokenizer.from_pretrained(model_name)
        self.pipe = pipeline(
            "text-generation", model=self.model, tokenizer=self.tokenizer
        )

    def query(self, message):
        querymsgs = self.messages.copy()

        querymsgs += [
            {"role": "user", "content": message},
        ]

        output = self.pipe(querymsgs, **self.generation_args)
        print(output[0]["generated_text"])
        self.output = output[0]["generated_text"]
        return output[0]["generated_text"]

    def reset(self):
        self.messages = [
            {
                "role": "system",
                "content": "You are an AI agent. I refer to you as a agent 1. There exist another AI agent called agent 2. Agent 2 has had a conversation "
                "with a human about planning a task for a robot."
                "The robot can only use the skills: Release, Grasp, Move To, Push. "
                "The Grasp skill can only be used if the robot is near a target first. "
                "A target should either be one an object or a position! Avoid using prepositions for targets. "
                "Not all skills need to be used. "
                "Your goal (agent 1) is to look at a final message between agent 2 and the human that contains a proposed plan. "
                "You should process this message and provide a plan as a list of comma separated values (CSV). "
                "There should be two columns: action and target. Do not include quotation marks. "
                "Your response should contain anything but the CSV message (i.e. no explatanation or descriptions). ",
            },
        ]


class MultiModalPlanModule(PlanModule):
    def __init__(
        self,
        model_name: str = "microsoft/Phi-3.5-vision-instruct",
        image_paths: dict = None,
    ):
        super().__init__(model_name=model_name)

        self.generation_args.pop("return_full_text")
        self.pipe = None

        self.image_prompt = """
Also the following images are given to understand the scene:
"<|image_1|>\nThis is the view from the robot.\n "<|image_2|>\nThis is the bird-eye view of the environment.\n
The images provide a visual context of the environment. Describe the scene and how to reach the goal.
Reason about the positions of the objects in the environment. You may need to go to some objects before you can interact with them.
"""

        if image_paths is None:
            image_paths = dict(
                eyes=img_paths[system] + "Eyes.png",
                map=img_paths[system] + "MapCamera.png",
            )
        self.image_paths = image_paths
        self.processor = AutoProcessor.from_pretrained(
            self.model_name,
            trust_remote_code=True,
            num_crops=4,
            _attn_implementation="flash_attention_2",
        )

    def fetch_latest_images(self):

        images = [
            Image.open(self.image_paths["eyes"]),
            Image.open(self.image_paths["map"]),
        ]
        return images

    def query(self, message):

        querymsgs = self.messages.copy()
        print("querying message:", message)
        querymsgs[0]["content"] += self.image_prompt
        querymsgs += [
            {
                "role": "user",
                "content": message,
            },
        ]

        print("querymsgs:", querymsgs)
        prompt = self.processor.tokenizer.apply_chat_template(
            querymsgs, tokenize=False, add_generation_prompt=True
        )
        print("prompt:", prompt)

        inputs = self.processor(
            prompt, self.fetch_latest_images(), return_tensors="pt"
        ).to("cuda:0")

        generate_ids = self.model.generate(
            **inputs,
            eos_token_id=self.processor.tokenizer.eos_token_id,
            **self.generation_args,
        )

        # remove input tokens
        generate_ids = generate_ids[:, inputs["input_ids"].shape[1] :]
        output = self.processor.batch_decode(
            generate_ids, skip_special_tokens=True, clean_up_tokenization_spaces=False
        )[0]
        print("\noutput:", output, "\n")
        self.output = output
        return self.output
