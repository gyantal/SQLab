#!/usr/bin/env python3
#chmod +x start.py & dos2unix start.py
#dotnet restore -v Debug
#dotnet -v run

import os
from subprocess import call
print('Python3: Change Directory')
os.chdir("/home/ubuntu/SQ/Server/Overmind/src")
print('dotnet restore')
call(["dotnet", "restore"])
os.chdir("/home/ubuntu/SQ/Server/Overmind/src/Server/Overmind")
print('dotnet run --configuration Release')         # the default is the Debug configuration in the project.json file
call(['dotnet', 'run', '--configuration', 'Release'])
k = input("Press ENTER...")       # raw_input is built but waiting for Enter key 