import os
import re
from datetime import datetime
import time
import json
from planner_llm import PlanModule
import csv
import io
import os

def clean_csv_string(csv_string):
    # Remove unwanted characters such as backticks
    cleaned_string = re.sub(r'[`\r]', '', csv_string)  # Remove backticks and carriage returns
    cleaned_lines = [line for line in cleaned_string.split('\n') if line.strip()]
    
    # Rejoin the cleaned lines into a single string
    cleaned_string = '\n'.join(cleaned_lines)
    return cleaned_string

def save_string_as_csv(csv_string, file_path):
    # Step 1: Clean the CSV string
    cleaned_string = clean_csv_string(csv_string)

    # Step 2: Check if the cleaned string is a valid CSV by trying to parse it
    try:
        csv.reader(io.StringIO(cleaned_string))
    except Exception as e:
        raise ValueError(f"Invalid CSV data after cleaning: {e}")

    # Step 3: Save the cleaned string as a CSV file, overwriting if necessary
    with open(file_path, 'w', newline='') as csvfile:
        csvfile.write(cleaned_string)
    print(f"CSV file successfully saved at {file_path}")

def find_latest_file(path):
    # Regular expression to match the file pattern "yyyymmdd-hh-mm-ss.json"
    pattern = re.compile(r'(\d{8}-\d{2}-\d{2}-\d{2})\.json$')

    # List to store valid filenames and their corresponding datetime objects
    files_with_dates = []

    # Iterate over all files in the specified directory
    for file_name in os.listdir(path):
        # Match the filename against the pattern
        match = pattern.match(file_name)
        if match:
            # Extract the datetime part from the filename
            datetime_str = match.group(1)  # yyyymmdd-hh-mm-ss
            try:
                # Convert the datetime string to a datetime object
                file_datetime = datetime.strptime(datetime_str, '%Y%m%d-%H-%M-%S')
                # Append the filename and its datetime object to the list
                files_with_dates.append((file_name, file_datetime))
            except ValueError:
                # If the conversion fails, skip this file
                continue

    # If no valid files are found, return None
    if not files_with_dates:
        return None

    # Sort the files by their datetime values in descending order
    files_with_dates.sort(key=lambda x: x[1], reverse=True)

    # Return the name of the latest file
    return files_with_dates[0][0]

def extract_message_between_backticks(ai_response):
    try:
        # Use regular expression to find the content between ```
        pattern = r"```(.*?)```"
        match = re.search(pattern, ai_response, re.DOTALL)  # re.DOTALL makes '.' match newlines
        if match:
            return match.group(1).strip()
        else:
            print("No message found between backticks.")
            return None
    except Exception as e:
        print(f"Error while extracting message between backticks: {e}")
        return None

def find_execute_plan_message(data):
    # Iterate through the visible section to find the entry
    for index, entry in enumerate(reversed(data["visible"])):
        user_message, ai_response = entry
        
        # Check if the user message contains "execute plan!" (case-insensitive)
        if user_message and "execute plan!" in user_message.lower():
            return index, ai_response
    
    # If not found, return None
    return None, None








#Should the confirmed plans be indexed, and how should we know if a new plan is discovered. Can it be done by using a check to see if the active file has changed and if the messege index is higher for when the "execute plan!" message was sent? I think this is the right way to do it

log_path = './text-generation-webui/logs/chat/Planning-assistant'
new_message = False
current_index = -1

planner = PlanModule()

if __name__ == "__main__":
    latest_file = find_latest_file(log_path)
    full_path = os.path.join(log_path,latest_file)
    if latest_file == None:
        print("No file found, please start a conversation with the Planning agent!")
    else:
        print(f"Conversation found at: {full_path}. Checking for confirmed plan...")
    #Get current file:
    while True:
        if latest_file != None:
            try:
                with open(full_path, 'r') as file:
                    data = json.load(file)

                # Find the entry with "execute plan!" in the user message
                index, ai_response = find_execute_plan_message(data)
                extracted_message = extract_message_between_backticks(ai_response)

                # Display the result
                if index is not None:
                    candidate_index = index
                else:
                    candidate_index = current_index
                    print("No 'execute plan!' message found from the user.")

            except FileNotFoundError:
                current_index = -1
                print(f"The file {full_path} was not found. The conversation might have been deleted. Start a new conversation to solve the problem!")
            except json.JSONDecodeError:
                print(f"Failed to decode JSON from the file {full_path}. Please check if it's a valid JSON.")

            #If plan is confirmed get the latest message from the agent that contains the plan (Confirmed means that Execute plan is visible and a plan is available in the immedately following message)
            #If it is not confirmed sleep for 5 seconds again and check again

            # Pass the proposed plan to the parsing agent and let it come up with something that the unity environment can understand
        
        if current_index != candidate_index or new_message and extracted_message != None:
            print("\nPlan detected, parsing plan!")
            current_index = candidate_index
            new_message = False

            print("Converting plan from message to csv...")
            csv_string = planner.query(ai_response) #Feeding plan to LLM and waiting for response
            csv_path = "./plans/plan.csv"
            save_string_as_csv(csv_string = csv_string, file_path=csv_path)
            #Save plan to file

        time.sleep(5)
        print("\n"+"="*80)
        print("Polling for new conversation... ", end="")
        proposed_new_file = find_latest_file(log_path)

        if proposed_new_file == None:
            print("No file found, please start a conversation with the Planning agent!")
        elif latest_file == proposed_new_file:
            print(f"No new file found, using latest conversation at: {full_path}.")
        else:
            latest_file = proposed_new_file
            full_path = os.path.join(log_path,latest_file)
            new_message = True
            print(f"New conversation found at: {full_path}. Changing context.")
        #Check if new file exists, if it does find a new one print to terminal that new file is discovered and that it will wait till a plan is confirmed