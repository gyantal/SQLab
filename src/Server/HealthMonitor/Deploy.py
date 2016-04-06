import os        # listdir, isfile
import paramiko  # for sftp
import colorama  # for colourful print
from stat import S_ISDIR
from colorama import Fore, Back, Style

# Parameters to change:
rootLocalDir = "g:/work/Archi-data/GitHubRepos/SQLab/src"       #os.walk() gives back in a way that the last character is not slash, so do that way
rootRemoteDir = "/home/ubuntu/SQ/Server/HealthMonitor/src"
acceptedSubTreeRoots = ["Server\\HealthMonitor", "SQLab.Common"]        # everything under these relPaths is traversed: files or folders too

serverHost = "ec2-52-23-207-88.compute-1.amazonaws.com"
serverPort = 22
serverUser = "ubuntu"
serverRsaKeyFile = "g:\work\Archi-data\HedgeQuant\src\Server\AmazonAWS\HQaVirtualBrokerDevKeyPairName.pem"

excludeDirs = set(["bin", "obj", ".vs", "artifacts", "Properties"])
excludeFileExts = set(["sln", "xproj", "log", "sqlog", "ps1", "py", "sh", "user"])

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

#remove directory recursively
# http://stackoverflow.com/questions/20507055/recursive-remove-directory-using-sftp
# http://stackoverflow.com/questions/3406734/how-to-delete-all-files-in-directory-on-remote-server-in-python
def isdir(path):
    try:
        return S_ISDIR(sftp.stat(path).st_mode)
    except IOError:
        return False

# remove the root folder too
def rm(sftp, path): 
    files = sftp.listdir(path=path)
    for f in files:
        #filepath = os.path.join(path, f)       # it adds '\\', but my server is Linux
        filepath = path + "/" + f 
        if isdir(filepath):
            print("Removing: " + filepath)
            rm(sftp, filepath)
        else:
            sftp.remove(filepath)
    sftp.rmdir(path)

# do not remove the root folder, only the subfolders recursively
def rm_onlySubdirectories(sftp, path):
    files = sftp.listdir(path=path)
    for f in files:
        #filepath = os.path.join(path, f)       # it adds '\\', but my server is Linux
        filepath = path + "/" + f 
        if isdir(filepath):
            print("Removing: " + filepath)
            rm_onlySubdirectories(sftp, filepath)
            sftp.rmdir(filepath)
        else:
            sftp.remove(filepath)    

# script START
colorama.init()
print(Fore.MAGENTA + Style.BRIGHT  +  "Start deploying '" + acceptedSubTreeRoots[0] + "' ...")

#quicker to do one remote command then removing files/folders recursively one by one
#in the future. We can 7-zip locally, upload it by Sftp, unzip it with SSHClient commands. It is about 2 days development, so, later.
#command = "ls " + rootRemoteDir
command = "rm -rf " + rootRemoteDir
print("SSHClient. Executing remote command: " + command)
sshClient = paramiko.SSHClient()
sshClient.set_missing_host_key_policy(paramiko.AutoAddPolicy())
sshClient.connect(serverHost, serverPort, username = serverUser, pkey = paramiko.RSAKey.from_private_key_file(serverRsaKeyFile))
(stdin, stdout, stderr) = sshClient.exec_command(command)
for line in stdout.readlines():
    print(line)
sshClient.close()

print("SFTPClient is connecting...")
transport = paramiko.Transport((serverHost, serverPort))
transport.connect(username = serverUser, pkey = paramiko.RSAKey.from_private_key_file(serverRsaKeyFile))
sftp = paramiko.SFTPClient.from_transport(transport)
#rm_onlySubdirectories(sftp, rootRemoteDir)

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

print(Fore.MAGENTA + Style.BRIGHT  +  "SFTPClient is closing. Deployment '" + acceptedSubTreeRoots[0] + "' is OK.")
sftp.close()
transport.close()
#k = input("Press ENTER...")  
