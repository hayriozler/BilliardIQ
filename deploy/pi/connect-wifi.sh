```bash
#!/bin/bash
set -e

SSID="Hayri's Galaxy S21 FE 5G"
PASSWORD="XXXX"
CONNECTION_NAME="Hayri's Galaxy S21 FE 5G"

echo "Setting up Wi-Fi connection: $SSID"

# Remove an old profile with the same name, if it exists
sudo nmcli connection delete "$CONNECTION_NAME" 2>/dev/null || true

sudo nmcli device wifi rescan
sleep 3
nmcli device wifi list

# Create the Wi-Fi connection
sudo nmcli connection add \
    type wifi \
    ifname wlan0 \
    con-name "$CONNECTION_NAME" \
    ssid "$SSID"

# Configure WPA-PSK security
sudo nmcli connection modify "$CONNECTION_NAME" \
    wifi-sec.key-mgmt wpa-psk \
    wifi-sec.psk "$PASSWORD"

# Connect
sudo nmcli connection up "$CONNECTION_NAME"

echo
echo "Wi-Fi connection configured successfully."
echo
nmcli device status
