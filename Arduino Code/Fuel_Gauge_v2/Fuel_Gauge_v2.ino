/*

   8-10-16, Matt Cashatt

   WHAT IS IT:
   Simple FSX Fuel Gauge using Simconnect and a 10-segment LED Display from Microtivity.

   RELATED HARDWARE:
   Amazon link: https://www.amazon.com/gp/product/B0085MOT2U/ref=oh_aui_detailpage_o02_s00?ie=UTF8&psc=1

   NOTE: Since you need to have an available pin for each "bar" of the readout, I plan to flash an Atmel 328P using it's own internal 8Mhz clock.  I will use one of it's pins to read the value from the master controller, then run the code below for the display.

   POSSIBLE IMPROVEMENTS: Right now I send a whole number rounded value to the arduino.  So, if FSX tells me I have 34.5% of fuel left in the tank, we send a value of 30 to Arduino.  If 35.55%, we send 40, etc. In the future, it may be better to send a decimal
   over to Arduino and then either fade or blink a bar segment depending on its value.  For example, a value of 34.5% would show 3 solid bars and one blinking bar whereas 35.55% would show four solid bars.

   LICENSE: You are free to use this code as you see fit.  No warranties are made.  Use at your own risk.

   THANKS:
   http://playground.arduino.cc/Csharp/SerialCommsCSharp

*/
//Setup Output

int p1 = 3;
int p2 = 4;
int p3 = 5;
int p4 = 6;
int p5 = 7;
int p6 = 8;
int p7 = 9;
int p8 = 10;
int p9 = 11;
int p10 = 12;

//Setup
void setup() {

  pinMode(p1, OUTPUT);
  pinMode(p2, OUTPUT);
  pinMode(p3, OUTPUT);
  pinMode(p4, OUTPUT);
  pinMode(p5, OUTPUT);
  pinMode(p6, OUTPUT);
  pinMode(p7, OUTPUT);
  pinMode(p8, OUTPUT);
  pinMode(p9, OUTPUT);
  pinMode(p10, OUTPUT);

  Serial.begin(115200);

  BlinkAll(1);

}

//Main Loop
void loop() {
  if (Serial.available() > 0)
  {
    String msg = Serial.readString();
    msg = msg.substring(0, msg.length() - 1);

    //Handshake
    if (msg == "handshake") {
      Serial.print("HELLO FROM ROADRUNNER\n");
      BlinkAll(3);
    } else {
      int fuelLevel =  msg.toInt();

      ProcessFuel(p10, fuelLevel, 90);
      ProcessFuel(p9, fuelLevel, 80);
      ProcessFuel(p8, fuelLevel, 70);
      ProcessFuel(p7, fuelLevel, 60);
      ProcessFuel(p6, fuelLevel, 50);
      ProcessFuel(p5, fuelLevel, 40);
      ProcessFuel(p4, fuelLevel, 30);
      ProcessFuel(p3, fuelLevel, 20);
      ProcessFuel(p2, fuelLevel, 10);
      ProcessFuel(p1, fuelLevel, 0);

      Serial.print("MESSAGE RECEIVED\n");
    }
  }
}

void ProcessFuel(int led, int fuelLevel, int benchmark) {
  if (fuelLevel > benchmark) {
    if (fuelLevel > (benchmark + 10)) {
      digitalWrite(led, HIGH);
    } else {
      int modulo = fuelLevel % benchmark;
      if (modulo >= 5) {
        digitalWrite(led, HIGH);
      } else {
        FadeLed(led);
      }
    }
  } else {
    digitalWrite(led, LOW);
  }
}

void BlinkAll(int num) {

  for (int count = 0; count <= num; count++) {
    digitalWrite(p1, HIGH);
    digitalWrite(p2, HIGH);
    digitalWrite(p3, HIGH);
    digitalWrite(p4, HIGH);
    digitalWrite(p5, HIGH);
    digitalWrite(p6, HIGH);
    digitalWrite(p7, HIGH);
    digitalWrite(p8, HIGH);
    digitalWrite(p9, HIGH);
    digitalWrite(p10, HIGH);
    delay(250);
    digitalWrite(p1, LOW);
    digitalWrite(p2, LOW);
    digitalWrite(p3, LOW);
    digitalWrite(p4, LOW);
    digitalWrite(p5, LOW);
    digitalWrite(p6, LOW);
    digitalWrite(p7, LOW);
    digitalWrite(p8, LOW);
    digitalWrite(p9, LOW);
    digitalWrite(p10, LOW);
    delay(250);
  }

}

void FadeLed(int ledPin) {

  for (int fadeValue = 0 ; fadeValue <= 1000; fadeValue += 1) {
    digitalWrite(ledPin, HIGH);
    delayMicroseconds(fadeValue);
    digitalWrite(ledPin, LOW);
    delayMicroseconds(1000 - fadeValue);
  }

  // fade out from max to min in increments of 5 points:
  for (int fadeValue = 1000 ; fadeValue >= 0; fadeValue -= 1) {
    digitalWrite(ledPin, HIGH);
    delayMicroseconds(fadeValue);
    digitalWrite(ledPin, LOW);
    delayMicroseconds(1000 - fadeValue);
  }
}
