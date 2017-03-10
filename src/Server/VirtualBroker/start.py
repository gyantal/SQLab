#!/usr/bin/env python3
#chmod +x start.py & dos2unix start.py
#dotnet restore -v Debug
#dotnet -v run

import os
import time
from subprocess import call
print('Sleeping for 50 sec, so the 3 IBGateways can start...')
time.sleep(50)  # sleep for 50 sec, so IBGateways can start.2016-09-19: 30sec waiting time was not enough for 2 IBGateways, and now we have 3 IBGateways
print('Python3: Change Directory')
#os.chdir("/home/ubuntu/SQ/Server/VirtualBroker/src")
os.chdir("/home/ubuntu/SQ/Server/VirtualBroker/src/Server/VirtualBroker")
print('dotnet restore')
call(["dotnet", "restore"])
#print('dotnet run --configuration Release')         # the default is the Debug configuration in the project.json file
#call(['dotnet', 'run', '--configuration', 'Release']) # before dotnet version #2777, `dotnet run` did Console.IsOutputRedirected, while 'dotnet build' not. That made Console.Colors fail.
print('dotnet build --configuration Release')         # the default is the Debug configuration in the project.json file
call(['dotnet', 'build', '--configuration', 'Release'])
#print('dotnet bin/Release/netcoreapp1.0/ubuntu.14.04-x64/VirtualBroker.dll')
#call(['dotnet', 'bin/Release/netcoreapp1.0/ubuntu.14.04-x64/VirtualBroker.dll'])
print('dotnet bin/Release/netcoreapp1.1/VirtualBroker.dll')
call(['dotnet', 'bin/Release/netcoreapp1.1/VirtualBroker.dll'])
k = input("Press ENTER...")       # raw_input is built but waiting for Enter key 