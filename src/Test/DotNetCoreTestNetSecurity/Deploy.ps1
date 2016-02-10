$projectBaseDir = "g:\work\Archi-data\GitHubRepos\SQLab\src\Test\DotNetCoreTestNetSecurity\";
$remoteBaseDir = "/home/ubuntu/SQ/Server/Test/TestNetSecurity/src/";
$fixFiles = "project.json", "NuGet.Config";


write-host "Start deploying." -foreground "magenta";
Add-Type -Path "g:\install\programming Tools\WinScpDotNetAssembly\5.7.6\WinSCPnet.dll";

try
{
    # Load WinSCP .NET assembly
    #Add-Type -Path "WinSCPnet.dll"
 
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

		$csFiles = get-childitem $projectBaseDir | where {$_.extension -eq ".cs"}
		foreach ($filename in $fixFiles + $csFiles)
        {
			write-host "Sending:"$fileName  -foreground "cyan";
			$transferResult = $session.PutFiles($projectBaseDir + $fileName, $remoteBaseDir, $False, $transferOptions)
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
