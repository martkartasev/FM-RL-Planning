import csv

import torch
from transformers import AutoModelForCausalLM, AutoTokenizer, pipeline

torch.random.manual_seed(0)


class PlanModule:
    def __init__(self):
        self.generation_args = {
            "max_new_tokens": 500,
            "return_full_text": False,
            "temperature": 0.0,
            "do_sample": False,
        }

        self.messages = [
            {"role": "system", "content": "You are an AI agent that can move around on wheels and has two arms. You can use the skills Release, Grasp, Move To, Push. "
                                          "The Grasp skill can only be used if you are near the target first. "
                                          "There are the following objects, a red box, a blue box, a yellow box and a white goal square. "
                                          "By default assume you do not have the box and are not close to it."
                                          "A target should either be one an objects or a position! Avoid using prepositions for targets. "
                                          "Not all skills need to be used. Provide a sequential plan of skill target pairs. "
                                          "Give answers as a list of comma separated values (CSV). "
                                          "There should be two columns: action and target. Do not include quotation marks."
                                       },
        ]
        self.output = ""

        self.model_name = "microsoft/Phi-3-mini-128k-instruct"
        self.model = AutoModelForCausalLM.from_pretrained(self.model_name, device_map="cuda", torch_dtype="auto", trust_remote_code=True)
        self.tokenizer = AutoTokenizer.from_pretrained("microsoft/Phi-3-mini-128k-instruct")
        self.pipe = pipeline("text-generation", model=self.model, tokenizer=self.tokenizer)

    def query(self, message):
        querymsgs = self.messages.copy()

        querymsgs += [{"role": "user", "content": message}, ]

        output = self.pipe(querymsgs, **self.generation_args)
        print(output[0]['generated_text'])

        self.output = output[0]['generated_text']
        return output[0]['generated_text']