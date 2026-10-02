## if you want to status a service use the following command
sudo systemctl status billiardiq-scoreboard.service
## if you want to stop a service use the following command
sudo systemctl stop billiardiq-scoreboard.service
## if you want to start a service use the following command
sudo systemctl start billiardiq-scoreboard.service
## the following command find the all process running chormium and dotnet
ps aux | grep -E 'chromium|dotnet'
## the following command kills the chromium process
pkill -f chromium

## Copy files from windows to linux vice versa
scp -r "SourceDirectory" "${PiUser}@${PiHost}:${RemoteAppDir}/"