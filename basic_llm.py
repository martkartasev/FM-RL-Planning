import torch
from transformers import AutoModelForCausalLM, AutoTokenizer, pipeline

model_phi3_instruct ="microsoft/Phi-3-mini-4k-instruct"

torch.random.manual_seed(0)
model = AutoModelForCausalLM.from_pretrained(
    model_phi3_instruct,
    device_map="cuda",
    torch_dtype="auto",
    trust_remote_code=True,
)
tokenizer = AutoTokenizer.from_pretrained("microsoft/Phi-3-mini-4k-instruct")

messages = [

    {"role": "user", "content": "There is an AI agent with the skills Move To, Pick Object, Place Object, Rotate Object, Dock with Charger!"},
    {"role": "user", "content": "Produce a sequential plan."},
    {"role": "user", "content": "Give short answers as a python list of key value pairs!"},
    {"role": "user", "content": "Move an Object X from Position A to position B!"},
]

pipe = pipeline(
    "text-generation",
    model=model,
    tokenizer=tokenizer,
)

generation_args = {
    "max_new_tokens": 500,
    "return_full_text": False,
    "temperature": 0.0,
    "do_sample": False,
}

output = pipe(messages, **generation_args)
print(output[0]['generated_text'])

