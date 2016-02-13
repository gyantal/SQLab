#!/bin/bash
#chmod 777 start.sh & dos2unix start.sh
#dotnet restore -v Debug
#dotnet -v run

cd /home/ubuntu/SQ/Server/HealthMonitor/src
echo "dotnet restore at global.json will restore all Class Libraries too"
dotnet restore
cd /home/ubuntu/SQ/Server/HealthMonitor/src/Server/HealthMonitor
echo "dotnet run in a specific App folder"
dotnet run
read


