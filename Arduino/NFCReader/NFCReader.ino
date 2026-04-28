// LudumDare58 Bumper — NFC reader sketch for Arduino UNO R3 + MFRC522.
// Protocol (matches Assets/Scripts/NFCReader.cs):
//   - 9600 baud
//   - On every debounced swipe, sends one line:  UID:xx:xx:xx:xx[:xx:xx:xx]
//     Unity is responsible for slot assignment (Blue/Red), profile lookup, etc.
//   - Lines starting with '#' are diagnostic — Unity ignores them.
//
// Supports UIDs up to 10 bytes (covers 4-byte MIFARE Classic and 7-byte
// DESFire / Ultralight / NTAG — USC student cards fall in the 7-byte group).

#include <SPI.h>
#include <MFRC522.h>

#define SS_PIN  10
#define RST_PIN 9
#define MAX_UID 10

MFRC522 rfid(SS_PIN, RST_PIN);

const unsigned long DEBOUNCE_MS = 1000;
byte lastUid[MAX_UID];
byte lastUidLen = 0;
unsigned long lastSwipeMs = 0;

bool sameAsLast(const byte* uid, byte len) {
  if (lastUidLen != len) return false;
  for (byte i = 0; i < len; i++) if (lastUid[i] != uid[i]) return false;
  return true;
}

void rememberUid(const byte* uid, byte len) {
  lastUidLen = (len > MAX_UID) ? MAX_UID : len;
  for (byte i = 0; i < lastUidLen; i++) lastUid[i] = uid[i];
}

void setup() {
  Serial.begin(9600);
  delay(200);
  Serial.println("# boot");
  SPI.begin();
  Serial.println("# spi ok");
  rfid.PCD_Init();
  Serial.println("# rfid init");
  Serial.println("# NFCReader ready");
}

unsigned long lastHeartbeatMs = 0;

void loop() {
  // Heartbeat every 3 s so you know Arduino is alive even if RC522 is broken.
  if (millis() - lastHeartbeatMs > 3000) {
    lastHeartbeatMs = millis();
    Serial.println("# alive");
  }

  if (!rfid.PICC_IsNewCardPresent()) return;
  if (!rfid.PICC_ReadCardSerial())   return;

  const byte* uid = rfid.uid.uidByte;
  byte len = rfid.uid.size;
  unsigned long now = millis();

  // Debounce: suppress repeat reads of the same held card within 1 s
  if (sameAsLast(uid, len) && now - lastSwipeMs < DEBOUNCE_MS) {
    rfid.PICC_HaltA();
    rfid.PCD_StopCrypto1();
    return;
  }
  rememberUid(uid, len);
  lastSwipeMs = now;

  Serial.print("UID:");
  for (byte i = 0; i < len; i++) {
    if (uid[i] < 0x10) Serial.print('0');
    Serial.print(uid[i], HEX);
    if (i < len - 1) Serial.print(':');
  }
  Serial.println();

  rfid.PICC_HaltA();
  rfid.PCD_StopCrypto1();
}
