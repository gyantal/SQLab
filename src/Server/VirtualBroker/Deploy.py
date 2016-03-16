import os        # listdir, isfile
import paramiko  # for sftp
import colorama  # for colourful print
from colorama import Fore, Back, Style

# Parameters to change:
rootLocalDir = "g:/work/Archi-data/GitHubRepos/SQLab/src"       #os.walk() gives back in a way that the last character is not slash, so do that way
rootRemoteDir = "/home/ubuntu/SQ/Server/VirtualBroker/src"
acceptedSubTreeRoots = ["Server\\VirtualBroker", "SQLab.Common", "SQLab.RxCommon", "ThirdParty\\Reactive\\System.Reactive.Interfaces", "ThirdParty\\Reactive\\System.Reactive.Core", "ThirdParty\\Reactive\\System.Reactive.Linq", "ThirdParty\\Reactive\\System.Reactive.PlatformServices", "ThirdParty\\IbApiSocketClient"]        # everything under these relPaths is traversed: files or folders too

excludeDirs = set(["bin", "obj", ".vs", "artifacts", "Properties"])
excludeFileExts = set(["sln", "xproj", "log", "ps1", "py", "sh", "user"])

# "mkdir -p" means Create intermediate directories as required. 
# http://stackoverflow.com/questions/14819681/upload-files-using-sftp-in-python-but-create-directories-if-path-doesnt-exist
def mkdir_p(sftp, remote_directory):        
    """Change to this directory, recursively making new folders if needed.     Returns True if any folders were created."""
    if remote_directory == '/':
        # absolute path so change directory to root
        sftp.chdir('/')
        return
    if remote_directory == '':
        # top-level relative directory must exist
        return
    try:
        sftp.chdir(remote_directory) # sub-directory exists
    except IOError:
        dirname, basename = os.path.split(remote_directory.rstrip('/'))
        mkdir_p(sftp, dirname) # make parent directories
        sftp.mkdir(basename) # sub-directory missing, so created it
        sftp.chdir(basename)
        return True


# script START
colorama.init()
print(Fore.MAGENTA + Style.BRIGHT  +  "Start deploying.")

print("SFTP Session is opening...")
transport = paramiko.Transport(("ec2-52-23-207-88.compute-1.amazonaws.com", 22))
transport.connect(username = "ubuntu", pkey = paramiko.RSAKey.from_private_key_file("g:\work\Archi-data\HedgeQuant\src\Server\AmazonAWS\HQaVirtualBrokerDevKeyPairName.pem"))
sftp = paramiko.SFTPClient.from_transport(transport)

for root, dirs, files in os.walk(rootLocalDir, topdown=True):
    curRelPathWin = os.path.relpath(root, rootLocalDir);
    # we have to visit all subdirectories
    dirs[:] = [d for d in dirs if d not in excludeDirs]     #Modifying dirs in-place will prune the (subsequent) files and directories visited by os.walk

    if curRelPathWin != ".":    # root folder is always traversed
        isFilesTraversed = False;
        for aSubTreeRoot in acceptedSubTreeRoots:
            if curRelPathWin.startswith(aSubTreeRoot):
                isFilesTraversed = True   
                break
        if not isFilesTraversed:
            continue        # if none of the acceptedSubTreeRoots matched, skip to the next loop cycle

    goodFiles = [f for f in files if os.path.splitext(f)[1][1:].strip().lower() not in excludeFileExts and not f.endswith(".lock.json")]
    for f in goodFiles:
        if curRelPathWin == ".":
            curRelPathLinux = ""
        else:
            curRelPathLinux = curRelPathWin.replace(os.path.sep, '/') + "/"
        remoteDir = rootRemoteDir + "/" +  curRelPathLinux
        print(Fore.CYAN + Style.BRIGHT  + "Sending: " + remoteDir  + f)
        mkdir_p(sftp, remoteDir) 
        ret = sftp.put(root + "/" + f, remoteDir + f, None, True) # Check FileSize after Put() = True
        #print(Style.RESET_ALL + str(ret))

print(Fore.MAGENTA + Style.BRIGHT  +  "SFTP Session is closing. Deployment is OK.")
sftp.close()
transport.close()
#k = input("Press ENTER...")  
