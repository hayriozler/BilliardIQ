
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


String readPinState(int pin) {
  pinMode(pin, INPUT_PULLDOWN); delay(5);
  int pulledDown = digitalRead(pin);
  pinMode(pin, INPUT_PULLUP);   delay(5);
  int pulledUp = digitalRead(pin);
  pinMode(pin, INPUT);
  if (pulledDown && pulledUp)   return "3.3V";
  if (!pulledDown && !pulledUp) return "GND";
  return "BAGLANTI YOK";
}

void setup() {
  Serial.begin(115200);
}

// void setup() {
//   Serial.begin(115200);
//   delay(1000);   // Serial Monitor'ün hazır olması için kısa bekleme
//   if (!display.begin(SSD1306_SWITCHCAPVCC)) {
//     Serial.println("OLED bulunamadi!");
//     while (true) delay(1000);
//   }
//   sensors.begin();
//   sensors.setResolution(12);
//   Serial.printf("Bulunan sensor sayisi: %d\n", sensors.getDeviceCount());
// }

// void loop() {
//   sensors.requestTemperatures();
//   float t = sensors.getTempCByIndex(0);

//   display.clearDisplay();
//   display.setTextColor(SSD1306_WHITE);
//   display.setTextSize(1);
//   display.setCursor(0, 0);
//   display.println("Masa sicakligi");

//   display.setTextSize(3);
//   display.setCursor(0, 25);
//   if (t == DEVICE_DISCONNECTED_C) {
//     display.println("HATA");
//   } else {
//     display.print(t, 1);
//     display.print((char)247);
//   }
//   display.display();

//   Serial.printf("Sicaklik: %.2f C\n", t);
//   delay(2000);

// }
void loop() {
  Serial.print("Pin 4: ");
  Serial.print(readPinState(4));
  Serial.print("   |   Test kablosu (pin 5): ");
  Serial.println(readPinState(5));
  delay(700);
}