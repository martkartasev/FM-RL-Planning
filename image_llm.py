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

inputs = processor(prompt, image, return_tensors="pt").to("cuda:0")

# autoregressively complete prompt
output = model.generate(**inputs, max_new_tokens=500)

print(processor.decode(output[0], skip_special_tokens=True))
