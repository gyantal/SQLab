#!/usr/bin/env python3
#chmod +x start.py & dos2unix start.py
#dotnet restore -v Debug
#dotnet -v run

import os
import time
from subprocess import call
time.sleep(30)  # sleep for 30 sec so IBGateways can start
print('Python3: Change Directory')
os.chdir("/home/ubuntu/SQ/Server/VirtualBroker/src")
print('dotnet restore')
call(["dotnet", "restore"])
os.chdir("/home/ubuntu/SQ/Server/VirtualBroker/src/Server/VirtualBroker")
print('dotnet run --configuration Release')         # the default is the Debug configuration in the project.json file
call(['dotnet', 'run', '--configuration', 'Release'])
k = input("Press ENTER...")       # raw_input is built but waiting for Enter key 