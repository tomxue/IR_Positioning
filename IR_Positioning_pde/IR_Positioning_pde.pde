// pin 19 (Yellow): CLK
// pin 20 (Orange): SI
// pin 11 (Green):  Y-AO
// pin 10 (Brown):  X-AO

int pinXAO = 10;    // analog pin
int pinYAO = 11;    // analog pin
int pinSI = 20;
int pinCLK = 19;

int val_x[128];
int val_y[128];

void setup()
{
  pinMode(BOARD_LED_PIN, OUTPUT); // sets the digital pin 13 as output
  pinMode(pinXAO, INPUT_ANALOG);
  pinMode(pinYAO, INPUT_ANALOG);
  pinMode(pinSI, OUTPUT);
  pinMode(pinCLK, OUTPUT);
}

void loop()
{
  digitalWrite(BOARD_LED_PIN, HIGH); // sets the LED to the button's value

  sampleOneData();

  for(int i=0;i<128;i++)
  {
    SerialUSB.print("i= ");
    SerialUSB.print(i);
    SerialUSB.print(" x value is: ");
    SerialUSB.println(val_x[i]);
  }
  delay(50);
  for(int i=0;i<128;i++)
  {
    SerialUSB.print("i= ");
    SerialUSB.print(i);
    SerialUSB.print(" y value is: ");
    SerialUSB.println(val_y[i]);
  }
  delay(50);
}

void sampleOneData()
{
  for(int j=0;j<2;j++)
  {
    // initialise 1 data sample of both X and Y axises
    digitalWrite(pinSI, LOW);
    digitalWrite(pinCLK, LOW);
    digitalWrite(pinSI, HIGH);

    for(int i=0;i<128;i++)
    {
      digitalWrite(pinCLK, HIGH);
      val_y[i] = analogRead(pinYAO);
      if(i == 0)
        digitalWrite(pinSI, LOW);
      digitalWrite(pinCLK, LOW);
    }

    for(int i=0;i<128;i++)
    {
      digitalWrite(pinCLK, HIGH);
      val_x[i] = analogRead(pinXAO);
      digitalWrite(pinCLK, LOW);
    }

    digitalWrite(pinCLK, HIGH);
    digitalWrite(pinCLK, LOW);
  }
}
