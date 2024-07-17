from transformers import LlavaNextProcessor, LlavaNextForConditionalGeneration
import torch
from PIL import Image
import requests

processor = LlavaNextProcessor.from_pretrained("llava-hf/llava-v1.6-mistral-7b-hf")

model = LlavaNextForConditionalGeneration.from_pretrained(
    "llava-hf/llava-v1.6-mistral-7b-hf",
    torch_dtype=torch.float16,
    low_cpu_mem_usage=True,
)
model.to("cuda:0")

# prepare image and text prompt, using the appropriate prompt template
image = Image.open("scene-images/unity-global-view.png")
prompt = """
[INST] <image>\n This image is a scene in an environment. There is a robot that aims to achieve a task. 
In an abstract level, describe the scene. Decribe the positions of the objects in the environment. 
The description should be a list. [/INST]

"""

# inputs = processor(prompt, image, return_tensors="pt").to("cuda:0")

# # autoregressively complete prompt
# output = model.generate(**inputs, max_new_tokens=500)

# print(processor.decode(output[0], skip_special_tokens=True))

box_image = Image.open("scene-images/box.png")
bridge_image = Image.open("scene-images/bridge.png")
goal_image = Image.open("scene-images/goal.png")
robot_image = Image.open("scene-images/robot.png")
tree_image = Image.open("scene-images/tree.png")
robot_perspective_image = Image.open("scene-images/robot_perspective.png")
scene_image = Image.open("scene-images/scene.png")

prompt = [
    "[INST] <image>\nThis is a box. <image>\nThis is a bridge. <image>\nThis is the goal. <image>\nThis is the robot. <image>\nThis is a tree. <image>\nThis is the perspective of the robot <image>\nDescribe the scene and how to reach the goal.[/INST]",
]

# prompt = [
#     "[INST] <image>\nDescribe the scene and how to reach the goal.[/INST]",
# ]

inputs = processor(
    text=prompt,
    images=[scene_image],
    images=[
        box_image,
        bridge_image,
        goal_image,
        robot_image,
        tree_image,
        robot_perspective_image,
        scene_image,
    ],
    padding=True,
    return_tensors="pt",
).to(model.device)

# Generate
output = model.generate(**inputs, max_new_tokens=3000)

# # autoregressively complete prompt
output = model.generate(**inputs, max_new_tokens=500)

print(processor.decode(output[0], skip_special_tokens=True))

processor.batch_decode(
    output, skip_special_tokens=True, clean_up_tokenization_spaces=False
)
