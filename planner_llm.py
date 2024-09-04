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
            {"role": "system", "content": "You are an AI agent. I refer to you as a agent 1. There exist another AI agent called agent 2. Agent 2 has had a conversation "
                                          "with a human about planning a task for a robot."
                                          "The robot can only use the skills: Release, Grasp, Move To, Push. "
                                          "The Grasp skill can only be used if the robot is near a target first. "
                                          "A target should either be one an object or a position! Avoid using prepositions for targets. "
                                          "Not all skills need to be used. "
                                          "Your goal (agent 1) is to look at a final message between agent 2 and the human that contains a proposed plan. "
                                          "You should process this message and provide a plan as a list of comma separated values (CSV). "
                                          "There should be two columns: action and target. Do not include quotation marks. "
                                          "Your response should contain anything but the CSV message (i.e. no explatanation or descriptions). "
                                       },
        ]
        self.output = ""

        self.model_name = "microsoft/Phi-3-mini-128k-instruct"
        self.model = AutoModelForCausalLM.from_pretrained(self.model_name, device_map="cpu", torch_dtype="auto", trust_remote_code=True)
        self.tokenizer = AutoTokenizer.from_pretrained("microsoft/Phi-3-mini-128k-instruct")
        self.pipe = pipeline("text-generation", model=self.model, tokenizer=self.tokenizer)

    def query(self, message):
        querymsgs = self.messages.copy()

        querymsgs += [{"role": "user", "content": message}, ]

        output = self.pipe(querymsgs, **self.generation_args)
        print(output[0]['generated_text'])

        self.output = output[0]['generated_text']
        return output[0]['generated_text']
    
    def reset(self):
        self.messages = [
            {"role": "system", "content": "You are an AI agent. I refer to you as a agent 1. There exist another AI agent called agent 2. Agent 2 has had a conversation "
                                          "with a human about planning a task for a robot."
                                          "The robot can only use the skills: Release, Grasp, Move To, Push. "
                                          "The Grasp skill can only be used if the robot is near a target first. "
                                          "A target should either be one an object or a position! Avoid using prepositions for targets. "
                                          "Not all skills need to be used. "
                                          "Your goal (agent 1) is to look at a final message between agent 2 and the human that contains a proposed plan. "
                                          "You should process this message and provide a plan as a list of comma separated values (CSV). "
                                          "There should be two columns: action and target. Do not include quotation marks. "
                                          "Your response should contain anything but the CSV message (i.e. no explatanation or descriptions). "
                                       },
        ]