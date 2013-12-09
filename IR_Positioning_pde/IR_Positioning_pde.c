// pin 19 (Yellow): CLK
// pin 20 (Orange): SI
// pin 11 (Green):  Y-AO
// pin 10 (Brown):  X-AO

int pinXAO = 10;    // analog pin
int pinYAO = 11;    // analog pin
int pinSI_y = 20;
int pinSI_x = 18;
int pinCLK = 19;

int val_x[128];
int val_y[128];
int sumX = 0, sumY = 0;
double thresholdX = 0, thresholdY = 0;
unsigned int x32_1, x32_2, x32_3, x32_4;
unsigned int y32_1, y32_2, y32_3, y32_4;
int incomingByte = 0;   // for incoming serial data

void setup()
{
  pinMode(BOARD_LED_PIN, OUTPUT); // sets the digital pin 13 as output
  pinMode(pinXAO, INPUT_ANALOG);
  pinMode(pinYAO, INPUT_ANALOG);
  pinMode(pinSI_y, OUTPUT);
  pinMode(pinCLK, OUTPUT);
}

void loop()
{
  digitalWrite(BOARD_LED_PIN, HIGH); // sets the LED to the button's value

  sumX = 0;
  sumY = 0;
  sampleXOneData();
  sampleYOneData();
  calcThreshold();
  digitize();

  for(int i=0;i<128;i++)
  {
    SerialUSB.print("i= ");
    SerialUSB.print(i);
    SerialUSB.print(" x value is: ");
    SerialUSB.println(val_x[i]);
  }
  SerialUSB.print("x threshold is: ");
  SerialUSB.println(thresholdX);
  SerialUSB.print("x32_1 is: ");
  SerialUSB.println(x32_1, HEX);
  SerialUSB.print("x32_2 is: ");
  SerialUSB.println(x32_2, HEX);
  SerialUSB.print("x32_3 is: ");
  SerialUSB.println(x32_3, HEX);
  SerialUSB.print("x32_4 is: ");
  SerialUSB.println(x32_4, HEX);
  SerialUSB.println("---------------------------------------------------");

  for(int i=0;i<128;i++)
  {
    SerialUSB.print("i= ");
    SerialUSB.print(i);
    SerialUSB.print(" y value is: ");
    SerialUSB.println(val_y[i]);
  }
  SerialUSB.print("y threshold is: ");
  SerialUSB.println(thresholdY);
  SerialUSB.print("y32_1 is: ");
  SerialUSB.println(y32_1, HEX);
  SerialUSB.print("y32_2 is: ");
  SerialUSB.println(y32_2, HEX);
  SerialUSB.print("y32_3 is: ");
  SerialUSB.println(y32_3, HEX);
  SerialUSB.print("y32_4 is: ");
  SerialUSB.println(y32_4, HEX);
  SerialUSB.println("---------------------------------------------------");

  //  incomingByte = SerialUSB.read();
  //  if(incomingByte != 0)
  //  {
  //    digitalWrite(BOARD_LED_PIN, LOW);
  //    delay(10);
  //  }
}

void sampleXOneData()
{
  for(int k=0;k<129;k++)
  {
    // initialise 1 data sample of both X and Y axises
    digitalWrite(pinSI_x, LOW);
    digitalWrite(pinCLK, LOW);
    digitalWrite(pinSI_x, HIGH);

    for(int i=0;i<128;i++)
    {
      digitalWrite(pinCLK, HIGH);
      if(k == i)
        val_x[k] = analogRead(pinXAO);
      if(k == 128 && i == 0)    // read the 1st data once more
        val_x[0] = analogRead(pinXAO);
      if(i == 0)
        digitalWrite(pinSI_x, LOW);
      digitalWrite(pinCLK, LOW);
    }

    // the 129th dummy data
    digitalWrite(pinCLK, HIGH);
    digitalWrite(pinCLK, LOW);
  }
}

void sampleYOneData()
{
  for(int k=0;k<129;k++)
  {
    // initialise 1 data sample of both X and Y axises
    digitalWrite(pinSI_y, LOW);
    digitalWrite(pinCLK, LOW);
    digitalWrite(pinSI_y, HIGH);

    for(int i=0;i<128;i++)
    {
      digitalWrite(pinCLK, HIGH);
      if(k == i)
        val_y[k] = analogRead(pinYAO);
      if(k == 128 && i == 0)    // read the 1st data once more
        val_y[0] = analogRead(pinYAO);
      if(i == 0)
        digitalWrite(pinSI_y, LOW);
      digitalWrite(pinCLK, LOW);
    }

    // the 129th dummy data
    digitalWrite(pinCLK, HIGH);
    digitalWrite(pinCLK, LOW);
  }
}

void calcThreshold()
{
  for(int i=0;i<128;i++)
  {
    sumX += val_x[i];
    sumY += val_y[i];
  }

  thresholdX = sumX / 128.0;
  thresholdY = sumY / 128.0;
}

void digitize()
{
  for(int i=0;i<32;i++)
  {
    if(val_x[i] >= thresholdX)
      x32_1 |= 1 << (31-i);
    else
      x32_1 &= ~(1 << (31-i));

    if(val_x[i+32] >= thresholdX)
      x32_2 |= 1 << (31-i);
    else
      x32_2 &= ~(1 << (31-i));

    if(val_x[i+64] >= thresholdX)
      x32_3 |= 1 << (31-i);
    else
      x32_3 &= ~(1 << (31-i));

    if(val_x[i+96] >= thresholdX)
      x32_4 |= 1 << (31-i);
    else
      x32_4 &= ~(1 << (31-i));

    if(val_y[i] >= thresholdY)
      y32_1 |= 1 << (31-i);
    else
      y32_1 &= ~(1 << (31-i));

    if(val_y[i+32] >= thresholdY)
      y32_2 |= 1 << (31-i);
    else
      y32_2 &= ~(1 << (31-i));

    if(val_y[i+64] >= thresholdY)
      y32_3 |= 1 << (31-i);
    else
      y32_3 &= ~(1 << (31-i));

    if(val_y[i+96] >= thresholdY)
      y32_4 |= 1 << (31-i);
    else
      y32_4 &= ~(1 << (31-i));
  }
}
