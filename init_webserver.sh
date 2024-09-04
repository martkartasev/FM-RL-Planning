#!/bin/bash


# Define variables
MAIN_REPO_URL="https://github.com/martkartasev/FM-RL-Planning.git"
MAIN_REPO_DIR="FM-RL-Planning"
SUBMODULE_PATH="text-generation-webui" # Path to the submodule directory within the main repo
FILE_TO_COPY="Planning-assistant.yaml" # The file to copy from the main repo
IMAGE_TO_COPY="Planning-assistant.png" # The image to copy from the main repo
DESTINATION_FOLDER="characters" # The folder inside the submodule where the file will be copied

detect_os() {
    case "$(uname -s)" in
        Linux*)     echo "Linux" ;;
        Darwin*)    echo "Mac" ;;
        CYGWIN*|MINGW*|MSYS_NT*) echo "Windows" ;;
        *)          echo "Unknown" ;;
    esac
}

# Function to execute actions based on the detected OS
execute_for_os() {
    local OS="$1"

    case $OS in
        Linux)
            echo "Running script for Linux..."
            bash "$SUBMODULE_PATH/start_linux.sh"
            chmod +x -R ./*
            ;;
        Mac)
            echo "Running script for Mac..."
            bash "$SUBMODULE_PATH/start_macos.sh"
	    chmod -R 755 ./builds/mac/build.app/Contents/MacOS/FM-RL-Unity
            ;;
        Windows)
            echo "Running script for Windows..."
            cmd.exe /c "$SUBMODULE_PATH/start_windows.bat"
            ;;
        *)
            echo "Unknown operating system. Please choose:"
            echo "0 - Windows"
            echo "1 - Linux"
            echo "2 - Mac"
            read -p "Enter your choice (0, 1, or 2): " choice
            case $choice in
                0)
                    echo "Running script for Windows..."
                    cmd.exe /c "$SUBMODULE_PATH/start_windows.bat"
                    ;;
                1)
                    echo "Running script for Linux..."
                    bash "$SUBMODULE_PATH/start_linux.sh"
                    chmod +x -R ./*
                    ;;
                2)
                    echo "Running script for Mac..."
                    bash "$SUBMODULE_PATH/start_macos.sh"
		            chmod -R 755 ./builds/mac/build.app/Contents/MacOS/FM-RL-Unity
                    ;;
                *)
                    echo "Invalid choice. Exiting."
                    exit 1
                    ;;
            esac
            ;;
    esac
}

# Copy the file from the main repo to the destination folder inside the submodule
cp "$FILE_TO_COPY" "$SUBMODULE_PATH/$DESTINATION_FOLDER/"
cp "$IMAGE_TO_COPY" "$SUBMODULE_PATH/$DESTINATION_FOLDER/"

echo "File copied to submodule successfully!"

# Detect the operating system
OS=$(detect_os)

# Execute commands for the detected or chosen OS
execute_for_os "$OS"


