
from PIL import Image 
import requests 
from transformers import AutoModelForCausalLM 
from transformers import AutoProcessor 

#import pdb;pdb.set_trace()

mistral_model = "llava-hf/llava-v1.6-mistral-7b-hf"

def converse(context, prompt):
    messages = context
    messages.append({"role": "user", "content": prompt})

    path = "./hugg/images/"
    image0 = Image.open(path+"image_0.png") 
    image1 = Image.open(path+"image_1.png")
    image_area = Image.open(path+"image_area.png") 
    image_box = Image.open(path+"image_box.png") 
    image_bridge = Image.open(path+"image_bridge.png") 
    image_robot = Image.open(path+"image_robot.png") 
    image_tree = Image.open(path+"image_tree.png") 

    prompt = processor.tokenizer.apply_chat_template(messages, tokenize=False, add_generation_prompt=True)
    inputs = processor(prompt, [image_area, image_box, image_bridge, image_robot, image_tree, image0, image1], return_tensors="pt").to("cuda:0")

    generation_args = { 
        "max_new_tokens": 500, 
        "temperature": 0.0, 
        "do_sample": False,
    } 

    generate_ids = model.generate(**inputs, eos_token_id=processor.tokenizer.eos_token_id, **generation_args) 

    # remove input tokens 
    generate_ids = generate_ids[:, inputs['input_ids'].shape[1]:]
    response = processor.batch_decode(generate_ids, skip_special_tokens=True, clean_up_tokenization_spaces=False)[0]

    messages.append({"role": "assistant", "content": response})
    print(response) 
    return messages, response

model_id = "microsoft/Phi-3-vision-128k-instruct" 

model = AutoModelForCausalLM.from_pretrained(model_id, device_map="cuda", trust_remote_code=True, torch_dtype="auto", _attn_implementation='flash_attention_2', token="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") # use _attn_implementation='eager' to disable flash attention flash_attention_2

processor = AutoProcessor.from_pretrained(model_id, trust_remote_code=True) 

messages = [ 
    {"role": "user", "content": "I will give you a set of images describing some objects in an environment."},
    {"role": "user", "content": "<|image_1|>\nThis is the goal area."},
    {"role": "user", "content": "<|image_2|>\nThis is a box that can be moved."},
    {"role": "user", "content": "<|image_3|>\nThis is a bridge that goes over a moat that the robot can traverse."},
    {"role": "user", "content": "<|image_4|>\nThis is the robot that can be controlled."},
    {"role": "user", "content": "<|image_5|>\nThis is a tree."},
    {"role": "user", "content": "Now I will give you some images describing the robot in the environment from the robots perspective and a global perspective."},
    {"role": "user", "content": "<|image_6|>\nThis is the view for the robot."},
    {"role": "user", "content": "<|image_7|>\nThis is the view from a global perspective."},
    {"role": "user", "content": "Now I will give you a set of skills that the robot can do. Move To, Pick Object, Place Object, Rotate Object, Dock with Charger."},
    {"role": "user", "content": "Give me a plan given the skills that the robot the should execute to move the box to the goal area."},
    #{"role": "user", "content": "<|image_1|>\nWhat is shown in this image?"},
    #{"role": "user", "content": "<|image_2|>\nWhat is shown in this image?"},
    #{"role": "user", "content": "What can you tell from both these images?"} 
] 

#url = "https://assets-c4akfrf5b4d3f4b7.z01.azurefd.net/assets/2024/04/BMDataViz_661fb89f3845e.png" 
path = "./hugg/images/"
image0 = Image.open(path+"image_0.png") 
image1 = Image.open(path+"image_1.png")
image_area = Image.open(path+"image_area.png") 
image_box = Image.open(path+"image_box.png") 
image_bridge = Image.open(path+"image_bridge.png") 
image_robot = Image.open(path+"image_robot.png") 
image_tree = Image.open(path+"image_tree.png") 

prompt = processor.tokenizer.apply_chat_template(messages, tokenize=False, add_generation_prompt=True)

inputs = processor(prompt, [image_area, image_box, image_bridge, image_robot, image_tree, image0, image1], return_tensors="pt").to("cuda:0")

generation_args = { 
    "max_new_tokens": 500, 
    "temperature": 0.0, 
    "do_sample": False,
} 

generate_ids = model.generate(**inputs, eos_token_id=processor.tokenizer.eos_token_id, **generation_args) 

# remove input tokens 
generate_ids = generate_ids[:, inputs['input_ids'].shape[1]:]
response = processor.batch_decode(generate_ids, skip_special_tokens=True, clean_up_tokenization_spaces=False)[0]

messages.append({"role": "assistant", "content": response})

print(response) 

import pdb;pdb.set_trace()
messages, response = converse(messages, prompt)




