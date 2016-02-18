#!/usr/bin/env python3
#chmod +x start.py & dos2unix start.py
#dotnet restore -v Debug
#dotnet -v run

import os
from subprocess import call
print('Python3: Change Directory')
os.chdir("/home/ubuntu/SQ/Server/HealthMonitor/src")
print('dotnet restore')
call(["dotnet", "restore"])
os.chdir("/home/ubuntu/SQ/Server/HealthMonitor/src/Server/HealthMonitor")
print('dotnet run')
call(["dotnet", "run"])
k = input("Press ENTER...")       # raw_input is built but waiting for Enter key 