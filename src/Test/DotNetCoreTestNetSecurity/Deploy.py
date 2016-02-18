import os        # listdir, isfile
import paramiko  # for sftp
import colorama  # for colourful print
from colorama import Fore, Back, Style

# Parameters to change:
localeProjectBaseDir = "g:/work/Archi-data/GitHubRepos/SQLab/src/Test/DotNetCoreTestNetSecurity/"
remoteBaseDir = "/home/ubuntu/SQ/Server/Test/TestNetSecurity/src/"
fixFiles = ["project.json", "../../NuGet.Config"]

# script run
colorama.init()
print(Fore.MAGENTA + Style.BRIGHT  +  "Start deploying.")

print("SFTP Session is opening...")
transport = paramiko.Transport(("ec2-52-23-207-88.compute-1.amazonaws.com", 22))
transport.connect(username = "ubuntu", pkey = paramiko.RSAKey.from_private_key_file("g:\work\Archi-data\HedgeQuant\src\Server\AmazonAWS\HQaVirtualBrokerDevKeyPairName.pem"))
sftp = paramiko.SFTPClient.from_transport(transport)

csFiles = [f for f in os.listdir(localeProjectBaseDir) if f.endswith('.cs')]

for f in csFiles + fixFiles:
    print(Fore.CYAN + Style.BRIGHT  + "Sending:" + f)
    ret = sftp.put(localeProjectBaseDir + f, remoteBaseDir + f, None, True) # Check FileSize after Put() = True
    print(Style.RESET_ALL + str(ret))
    
print(Fore.MAGENTA + Style.BRIGHT  +  "SFTP Session is closing.")
sftp.close()
transport.close()
#k = input("Press ENTER...")  
