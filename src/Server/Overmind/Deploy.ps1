$RootLocalDir = "g:\work\Archi-data\GitHubRepos\SQLab\src\";
$RootRemoteDir = "/home/ubuntu/SQ/Server/Overmind/src/";
$RootFixFiles = "global.json", "NuGet.Config";

$CommonLibraryDir = "SQLab.Common";
$CommonLibrarySubDirs = "Utils";
$CommonLibraryFixFiles = "project.json";

$ServerAppDir = "Server";
$AppDir = "Overmind";
$AppFixFiles = "project.json", "NLog.config";

write-host "Start deploying." -foreground "magenta";
Add-Type -Path "g:\install\programming Tools\WinScpDotNetAssembly\5.7.6\WinSCPnet.dll";
try
{
    #Add-Type -Path "WinSCPnet.dll"  # Load WinSCP .NET assembly
    # Setup session options
    $sessionOptions = New-Object WinSCP.SessionOptions -Property @{
        Protocol = [WinSCP.Protocol]::Sftp
        HostName = "ec2-52-23-207-88.compute-1.amazonaws.com"
        UserName = "ubuntu"
        #Password = "mypassword"
		SshPrivateKeyPath = "g:\work\Archi-data\HedgeQuant\src\Server\AmazonAWS\HQaVirtualBrokerDevKeyPairName.ppk"
        SshHostKeyFingerprint = "ssh-rsa 2048 70:34:5d:9c:60:74:f8:f5:59:9c:2f:5e:6e:d4:4c:f1"
    }
    $session = New-Object WinSCP.Session
 
    try
    {
        # Connect
		write-host "SFTP Session is opening..." -foreground "magenta";
        $session.Open($sessionOptions)
		
        # Upload files
        $transferOptions = New-Object WinSCP.TransferOptions
        $transferOptions.TransferMode = [WinSCP.TransferMode]::Binary
 
		# https://winscp.net/eng/docs/library_session_putfiles
        #$transferResult = $session.PutFiles("g:\work\Archi-data\GitHubRepos\SQLab\src\Test\DotNetCoreTestNetSecurity\*", "/home/ubuntu/Test/", $False, $transferOptions)

		# 1. Send Root folder
		foreach ($filename in $RootFixFiles)
        {
			write-host "Sending:"$fileName  -foreground "cyan";
			$transferResult = $session.PutFiles($RootLocalDir + $fileName, $RootRemoteDir, $False, $transferOptions)
			# Throw on any error
			$transferResult.Check()
			## Print results
			foreach ($transfer in $transferResult.Transfers)
			{
				Write-Host ("Upload of {0} succeeded" -f $transfer.FileName)
			}
		}

		# 2. Send CommonLibrary folder
		$localDir = $RootLocalDir + $CommonLibraryDir + "\";
		$remoteDir = $RootRemoteDir + $CommonLibraryDir + "/";
		if (!$session.FileExists($remoteDir))
		{
			$session.CreateDirectory($remoteDir);
		}
		$csFiles = get-childitem $localDir | where {$_.extension -eq ".cs"}
		foreach ($filename in $CommonLibraryFixFiles + $csFiles)
        {
			$localFullPath = $localDir + $fileName;
			write-host "Sending:"$fullLocalPath " To: "$fullRemotePath -foreground "cyan";
			
			$transferResult = $session.PutFiles($localFullPath, $remoteDir , $False, $transferOptions)
			# Throw on any error
			$transferResult.Check()
			## Print results
			foreach ($transfer in $transferResult.Transfers)
			{
				Write-Host ("Upload of {0} succeeded" -f $transfer.FileName)
			}
		}

		# 3. Send CommonLibrary/Utils folder
		$localDir = $RootLocalDir + $CommonLibraryDir + "\" + $CommonLibrarySubDirs + "\";
		$remoteDir = $RootRemoteDir + $CommonLibraryDir + "/" + $CommonLibrarySubDirs + "/";
		if (!$session.FileExists($remoteDir))
		{
			$session.CreateDirectory($remoteDir);
		}
		$csFiles = get-childitem $localDir | where {$_.extension -eq ".cs"}
		foreach ($filename in $csFiles)
        {
			$localFullPath = $localDir + $fileName;
			write-host "Sending:"$fullLocalPath " To: "$fullRemotePath -foreground "cyan";
			
			$transferResult = $session.PutFiles($localFullPath, $remoteDir , $False, $transferOptions)
			# Throw on any error
			$transferResult.Check()
			## Print results
			foreach ($transfer in $transferResult.Transfers)
			{
				Write-Host ("Upload of {0} succeeded" -f $transfer.FileName)
			}
		}

		# 4. Send App folder
		$localDir = $RootLocalDir + $ServerAppDir + "\";
		$remoteDir = $RootRemoteDir + $ServerAppDir + "/";
		if (!$session.FileExists($remoteDir))
		{
			$session.CreateDirectory($remoteDir);
		}
		$localDir = $RootLocalDir + $ServerAppDir + "\" + $AppDir + "\";
		$remoteDir = $RootRemoteDir + $ServerAppDir + "/" + $AppDir + "/";
		if (!$session.FileExists($remoteDir))
		{
			$session.CreateDirectory($remoteDir);
		}
		$csFiles = get-childitem $localDir | where {$_.extension -eq ".cs"}
		foreach ($filename in $AppFixFiles + $csFiles)
        {
			$localFullPath = $localDir + $fileName;
			write-host "Sending:"$fullLocalPath " To: "$fullRemotePath -foreground "cyan";
			
			$transferResult = $session.PutFiles($localFullPath, $remoteDir , $False, $transferOptions)
			# Throw on any error
			$transferResult.Check()
			## Print results
			foreach ($transfer in $transferResult.Transfers)
			{
				Write-Host ("Upload of {0} succeeded" -f $transfer.FileName)
			}
		}
     
		write-host "SFTP Session is closing." -foreground "magenta";  
    }
    finally
    {
        # Disconnect, clean up
        $session.Dispose()
    }
 
    exit 0
}
catch [Exception]
{
    Write-Host $_.Exception.Message
    exit 1
}
