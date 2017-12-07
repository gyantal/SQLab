#!/usr/bin/env python3
#chmod +x start.py & dos2unix start.py
#dotnet restore -v Debug
#dotnet -v run

import os
from subprocess import call
print('Python3: Change Directory')
#os.chdir("/home/ubuntu/SQ/Server/Overmind/src")
os.chdir("/home/ubuntu/SQ/Server/Overmind/src/Server/Overmind")
print('dotnet restore')
call(["dotnet", "restore"])
#print('dotnet run --configuration Release')         # the default is the Debug configuration in the project.json file
#call(['dotnet', 'run', '--configuration', 'Release']) # before dotnet version #2777, `dotnet run` did Console.IsOutputRedirected, while 'dotnet build' not. That made Console.Colors fail.
print('dotnet build --configuration Release')         # the default is the Debug configuration in the project.json file
call(['dotnet', 'build', '--configuration', 'Release'])
#print('dotnet bin/Release/netcoreapp1.0/ubuntu.14.04-x64/Overmind.dll')
#call(['dotnet', 'bin/Release/netcoreapp1.0/ubuntu.14.04-x64/Overmind.dll'])
print('dotnet bin/Release/netcoreapp2.0/Overmind.dll')
call(['dotnet', 'bin/Release/netcoreapp2.0/Overmind.dll'])
k = input("Press ENTER...")       # raw_input is built but waiting for Enter key 