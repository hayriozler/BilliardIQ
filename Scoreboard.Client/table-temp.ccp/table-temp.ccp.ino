
#include <OneWire.h>
#include <DallasTemperature.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define ONE_WIRE_PIN 4
#define OLED_MOSI 8    // ekranda SDA
#define OLED_CLK  9    // ekranda SCK
#define OLED_RES  10
#define OLED_DC   11
#define OLED_CS   12

OneWire oneWire(ONE_WIRE_PIN);
DallasTemperature sensors(&oneWire);
Adafruit_SSD1306 display(128, 64, OLED_MOSI, OLED_CLK, OLED_DC, OLED_RES, OLED_CS);

void setup() {
  Serial.begin(115200);
  pinMode(4, INPUT);
  pinMode(5, INPUT);
  // if (!display.begin(SSD1306_SWITCHCAPVCC)) {
  //   Serial.println("OLED bulunamadi!");
  //   while (true) delay(1000);
  // }
  // sensors.begin();
  // sensors.setResolution(12);
  // Serial.printf("Bulunan sensor sayisi: %d\n", sensors.getDeviceCount());
}

void loop() {
  Serial.printf("Pin 4 (veri hatti): %d   |   Pin 5 (test kablosu): %d\n", digitalRead(4), digitalRead(5));
  delay(1000);
  // sensors.requestTemperatures();
  // float t = sensors.getTempCByIndex(0);

  // display.clearDisplay();
  // display.setTextColor(SSD1306_WHITE);
  // display.setTextSize(1);
  // display.setCursor(0, 0);
  // display.println("Masa sicakligi");

  // display.setTextSize(3);
  // display.setCursor(0, 25);
  // if (t == DEVICE_DISCONNECTED_C) {
  //   display.println("HATA");
  // } else {
  //   display.print(t, 1);
  //   display.print((char)247);
  // }
  // display.display();

  // Serial.printf("Sicaklik: %.2f C\n", t);
  // delay(2000);

}
