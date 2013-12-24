// pin 19 (Yellow): CLK
// pin 20 (Orange): SI of Y axis
// pin 18 (Blue)  : SI of X axis
// Red            : 5V
// White          : Gnd

// Below assignments are cancelled
// pin 5 (Green)  : miso
// pin 6 (Gray)   : sck
// pin 12 (Purple): spi cs
// pin 13 (Red)   : analog switch
// pin 14 (Gray)  : OE

// Use SPI port number 1
HardwareSPI spi(1);

int pinSI_y = 20;
int pinSI_x = 18;
int pinCLK = 19;
int pinTiming = 12;
int pinCS = 12;
int pinSW = 13; // 0: S1(Y) on; 1: S2(X) on
int pinOE = 14;

byte valx8[256];    // for one axis, it contains 128 pixels, and one pixel's ADC data occupies 2 bytes
byte valy8[256];
unsigned int rx32;
unsigned int valx32[128];
unsigned int valy32[128];
byte tx[2] = {
  0x31, 0x32, };
byte rx[2] = {
  0, };
int sumX = 0, sumY = 0;
double thresholdX = 0, thresholdY = 0;
unsigned int x32_1, x32_2, x32_3, x32_4;
unsigned int y32_1, y32_2, y32_3, y32_4;
int incomingByte = 0;   // for incoming serial data

void setup()
{
  // Turn on the SPI port
  spi.begin(SPI_18MHZ, MSBFIRST, 0);

  pinMode(BOARD_LED_PIN, OUTPUT); // sets the digital pin 13 as output
  pinMode(pinSI_y, OUTPUT);
  pinMode(pinCLK, OUTPUT);
  pinMode(pinTiming, OUTPUT);
  pinMode(pinCS, OUTPUT);
  pinMode(pinSW, OUTPUT);
  pinMode(pinOE, OUTPUT);
}

void loop()
{
  digitalWrite(BOARD_LED_PIN, HIGH); // sets the LED to the button's value

  // The TXB0104 has an OE input that is used to disable the device by setting OE = low
  digitalWrite(pinOE, HIGH);

  // measure the timing of sampling data
  digitalWrite(pinTiming, HIGH);
  sumX = 0;
  sumY = 0;
  sampleData();
  calcThreshold();
  digitize();
  digitalWrite(pinTiming, LOW);

  for(int i=0;i<128;i++)
  {
    SerialUSB.print("i= ");
    SerialUSB.print(i);
    SerialUSB.print(" x value is: ");
    SerialUSB.println(valx32[i]);
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
    SerialUSB.println(valy32[i]);
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

void sampleData()
{
  // sample X sensor data
  digitalWrite(pinSW, HIGH);
  for(int k=0;k<2;k++)
  {
    // initialise 1 data sample of both X and Y axises
    digitalWrite(pinSI_x, LOW);
    digitalWrite(pinCLK, LOW);
    digitalWrite(pinSI_x, HIGH);

    for(int i=0;i<128;i++)
    {
      digitalWrite(pinCLK, HIGH);
      digitalWrite(pinCS, LOW);
      valx8[2*i] = spi.transfer(0x0);
      valx8[2*i+1] = spi.transfer(0x0);
      digitalWrite(pinCS, HIGH);
      if(i == 0)
        digitalWrite(pinSI_x, LOW);
      digitalWrite(pinCLK, LOW);
    }

    // the 129th dummy data
    digitalWrite(pinCLK, HIGH);
    digitalWrite(pinCLK, LOW);
  }

  // sample Y sensor data
  digitalWrite(pinSW, LOW);
  for(int k=0;k<2;k++)
  {
    // initialise 1 data sample of both X and Y axises
    digitalWrite(pinSI_y, LOW);
    digitalWrite(pinCLK, LOW);
    digitalWrite(pinSI_y, HIGH);

    for(int i=0;i<128;i++)
    {
      digitalWrite(pinCLK, HIGH);
      digitalWrite(pinCS, LOW);
      valy8[2*i] = spi.transfer(0x0);
      valy8[2*i+1] = spi.transfer(0x0);
      digitalWrite(pinCS, HIGH);
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
    valx8[2*i] = valx8[2*i] & 0x3f;   // ADC: 2 leading zeros
    valx8[2*i+1] = valx8[2*i+1] & 0xfc;   // ADC: 2 trailing zeros
    valx32[i] = (valx8[2*i] << 8 | valx8[2*i+1]) >> 2;

    valy8[2*i] = valy8[2*i] & 0x3f;   // ADC: 2 leading zeros
    valy8[2*i+1] = valy8[2*i+1] & 0xfc;   // ADC: 2 trailing zeros
    valy32[i] = (valy8[2*i] << 8 | valy8[2*i+1]) >> 2;

    sumX += valx32[i];
    sumY += valy32[i];
  }

  thresholdX = sumX / 128.0;
  thresholdY = sumY / 128.0;
}

void digitize()
{
  for(int i=0;i<32;i++)
  {
    if(valx32[i] >= thresholdX)
      x32_1 |= 1 << (31-i);
    else
      x32_1 &= ~(1 << (31-i));

    if(valx32[i+32] >= thresholdX)
      x32_2 |= 1 << (31-i);
    else
      x32_2 &= ~(1 << (31-i));

    if(valx32[i+64] >= thresholdX)
      x32_3 |= 1 << (31-i);
    else
      x32_3 &= ~(1 << (31-i));

    if(valx32[i+96] >= thresholdX)
      x32_4 |= 1 << (31-i);
    else
      x32_4 &= ~(1 << (31-i));

    if(valy32[i] >= thresholdY)
      y32_1 |= 1 << (31-i);
    else
      y32_1 &= ~(1 << (31-i));

    if(valy32[i+32] >= thresholdY)
      y32_2 |= 1 << (31-i);
    else
      y32_2 &= ~(1 << (31-i));

    if(valy32[i+64] >= thresholdY)
      y32_3 |= 1 << (31-i);
    else
      y32_3 &= ~(1 << (31-i));

    if(valy32[i+96] >= thresholdY)
      y32_4 |= 1 << (31-i);
    else
      y32_4 &= ~(1 << (31-i));
  }
}

