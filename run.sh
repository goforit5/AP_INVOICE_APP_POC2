#!/bin/bash

# Start backend in a new terminal tab
echo "Starting the backend in a new terminal..."
osascript <<EOF
tell application "Terminal"
    do script "cd $(pwd)/Server && dotnet run"
end tell
EOF

# Start frontend in another new terminal tab
echo "Starting the frontend in a new terminal..."
osascript <<EOF
tell application "Terminal"
    do script "cd $(pwd)/Client && npm start"
end tell
EOF

echo "Backend and frontend are now running in separate Terminal tabs."