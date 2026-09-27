## if you want to status a service use the following command
sudo systemctl status zeymera-scoreboard.service
## if you want to stop a service use the following command
sudo systemctl stop zeymera-scoreboard.service
## if you want to start a service use the following command
sudo systemctl start zeymera-scoreboard.service
## the following command find the all process running chormium and dotnet
ps aux | grep -E 'chromium|dotnet'
## the following command kills the chromium process
pkill -f chromium


## connect to wifi with CLI
First SSH
sudo iwlist wlan0 scan | grep ESSID
sudo nano /etc/wpa_supplicant/wpa_supplicant.conf
country=US
ctrl_interface=DIR=/var/run/wpa_supplicant GROUP=netdev
update_config=1
network={
ssid=”Your_SSID”
psk=”Your_Password”
key_mgmt=WPA-PSK
}

sudo wpa_cli -i wlan0 reconfigure

sudo reboot