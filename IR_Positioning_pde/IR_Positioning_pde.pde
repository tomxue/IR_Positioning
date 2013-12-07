// pin 19 (Yellow): CLK
// pin 20 (Orange): SI
// pin 11 (Green):  Y-AO
// pin 10 (Brown):  X-AO

int pinX_AO = 10;    // analog pin
int pinY_AO = 11;    // analog pin
int pinSI = 20;
int pinCLK = 19;

int val_x = 0;
int val_y = 0;

void setup()
{
  pinMode(BOARD_LED_PIN, OUTPUT); // sets the digital pin 13 as output
  pinMode(pinX_AO, INPUT_ANALOG);
  pinMode(pinY_AO, INPUT_ANALOG);
}

void loop()
{
  digitalWrite(BOARD_LED_PIN, HIGH); // sets the LED to the button's value
  val_x = analogRead(pinX_AO);
  val_y = analogRead(pinY_AO);
  
  SerialUSB.print("x value is: ");
  SerialUSB.println(val_x);
  SerialUSB.print("y value is: ");
  SerialUSB.println(val_y);
  delay(100);
}
