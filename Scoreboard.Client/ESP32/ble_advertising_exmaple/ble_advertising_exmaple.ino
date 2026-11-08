#include <BLEDevice.h>
#include <BLEAdvertising.h>

BLEAdvertising* advertising;

void updateAdvertisement(float temperature) {
  BLEAdvertisementData advertisementData;
  advertisementData.setFlags(0x06);
  advertisementData.setName("Masa-Sicaklik");

  String payload;
  payload += (char)0xFF;   // test icin sirket kimligi (0xFFFF)
  payload += (char)0xFF;
  payload += String(temperature, 1);   // ornek: "23.4"
  advertisementData.setManufacturerData(payload);

  advertising->setAdvertisementData(advertisementData);
}

void setup() {
  Serial.begin(115200);
  BLEDevice::init("Masa-Sicaklik");
  advertising = BLEDevice::getAdvertising();
  updateAdvertisement(0.0);
  advertising->start();
  Serial.println("Yayin basladi");
}

void loop() {
  float testTemperature = 20.0 + (millis() / 1000) % 10;  // deneme degeri

  advertising->stop();
  updateAdvertisement(testTemperature);
  advertising->start();

  Serial.println("Yayinlanan: " + String(testTemperature, 1));
  delay(2000);
}