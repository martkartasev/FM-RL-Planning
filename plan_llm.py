import csv
import os
import platform
import torch
from PIL import Image
from transformers import AutoModelForCausalLM, AutoTokenizer, pipeline
from transformers import LlavaNextProcessor, LlavaNextForConditionalGeneration
import transformers

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
    def __init__(self):
        self.generation_args = {
            "max_new_tokens": 500,
            "return_full_text": False,
            "temperature": 0.0,
            "do_sample": False,
        }

        self.messages = [
            {
                "role": "system",
                "content": "You are an AI agent that can move around on wheels and has two arms. You can use the skills Release, Grasp, Move To, Push. "
                "The Grasp skill can only be used if you are near the target first. "
                "There are the following objects, a red box, a blue box, a yellow box and a white goal square. "
                "By default assume you do not have the box and are not close to it."
                "A target should either be one an objects or a position! Avoid using prepositions for targets. "
                "Not all skills need to be used. Provide a sequential plan of skill target pairs. "
                "Give answers as a list of comma separated values (CSV). "
                "There should be two columns: action and target. Do not include quotation marks.",
            },
        ]
        self.output = ""

        self.model_name = "microsoft/Phi-3-mini-128k-instruct"
        self.model = AutoModelForCausalLM.from_pretrained(
            self.model_name,
            device_map="cuda",
            torch_dtype="auto",
            trust_remote_code=True,
        )
        self.tokenizer = AutoTokenizer.from_pretrained(
            "microsoft/Phi-3-mini-128k-instruct"
        )
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


class MultiModalPlanModule:
    def __init__(self, image_paths: dict = None):
        self.generation_args = {
            "max_new_tokens": 500,
            "temperature": 0.5,
            "do_sample": True,
        }
        actions = ["Release", "Grasp", "Move To", "Push"]
        targets = ["red box", "blue box", "yellow box", "goal", "position"]
        self.system_prompt = f"""
You are an AI agent that can move around on wheels and has two arms. You can use the skills {actions}.
The Grasp skill can only be used if you are near the target first.
There are the following objects, a red box, a blue box, a yellow box and a white goal square.
By default assume you do not have the box and are not close to it.
A target should either be an object or a position! Avoid using prepositions for targets.
Not all skills need to be used. Provide a sequential plan of skill-target pairs.
Give answers as a list of comma-separated values (CSV).
There should be ONLY two columns: action and target. Do not include quotation marks.
ACTIONS ARE: {actions}
TARGETS ARE: {targets}

YOU OUTPUT EXAMPLE:
Move To, goal
Grasp, red box
Push, yellow box
Release, goal

OUTPUT NOTHING BUT ONLY THE ACTIONS AND TARGETS AS A LIST OF COMMA-SEPARATED VALUES (CSV). 
TWO COLUMN. NO NUMBERED LIST. NO COLONS. NO INDENTATION. NO QUOTATION MARKS. NO INDXES.
        """

        self.image_prompt = """
You are given the following images:
[INST] <image>\nThis is the view from the robot.\n <image>\nThis is the bird-eye view of the environment.[/INST]
        """

        if image_paths is None:
            image_paths = dict(
                eyes=img_paths[system] + "Eyes.png",
                map=img_paths[system] + "MapCamera.png",
            )
        self.image_paths = image_paths

        self.output = ""

        # Load the model and processor in half-precision with device mapping
        self.model_name = "llava-hf/llava-v1.6-mistral-7b-hf"
        self.model = LlavaNextForConditionalGeneration.from_pretrained(
            self.model_name,
            low_cpu_mem_usage=True,
            device_map="cuda",
            torch_dtype="auto",
            trust_remote_code=True,
        )
        self.processor = LlavaNextProcessor.from_pretrained(self.model_name)

        print("MultiModalPlanModule loaded!")

    def query(self, message):
        # Create the conversation with the system prompt, including the images as part of the system's description
        conversation = [
            {
                "role": "assistant",
                "content": [
                    {"type": "text", "text": self.system_prompt},
                ],
            },
            {
                "role": "user",
                "content": [
                    {"type": "image"},  # First image (robot's view)
                    {"type": "image"},  # Second image (bird's-eye view)
                    {"type": "text", "text": f"{message}"},
                ],
            },
        ]

        # Apply the structured conversation template
        prompt = self.processor.apply_chat_template(
            conversation, add_generation_prompt=True
        )
        print("prompt:", prompt)
        # Load images (robot view and bird-eye view)
        images = [
            Image.open(self.image_paths["eyes"]),
            Image.open(self.image_paths["map"]),
        ]

        # Prepare inputs for the model
        inputs = self.processor(
            text=[prompt], images=images, padding=True, return_tensors="pt"
        ).to(self.model.device)

        # Generate the output using the model
        print("query prompt:", prompt)
        output = self.model.generate(**inputs, **self.generation_args)

        # Use decode instead of batch_decode since it's not a batch process
        output = self.processor.decode(
            output[0], skip_special_tokens=True, clean_up_tokenization_spaces=True
        )
        output = clean_output(output, prompt)
        self.output = output
        print("output:", self.output)

        return self.output


def clean_output(output, prompt):

    output = output[len(prompt) :].strip()  # Strip off the prompt part

    # Split the output by the first newline and keep everything after it
    parts = output.split("\n", 1)  # Split into two parts at the first newline
    if len(parts) > 1:
        cleaned_output = parts[1].strip()  # Return everything after the first newline
    else:
        cleaned_output = output.strip()  # Fallback in case there's no newline

    return cleaned_output
