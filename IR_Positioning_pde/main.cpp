// pin 19 (Yellow): CLK
// pin 20 (Orange): SI of x axis
// pin 18 (Blue)  : SI of y axis
// Red            : 5V
// White          : Gnd

#include <wirish/wirish.h>
#include <libmaple/adc.h>

#define COM SerialUSB // use this for Maple

unsigned long start = 0;
unsigned long stop = 0;
unsigned long pixelIndex = 0;
unsigned long sampleCount = 100;
unsigned int pixelVal[129];

int SI_x = 20;
int SI_y = 18;
int CLK = 19;
int AO_x = 3;
int AO_y = 4;

// GPIO
gpio_dev *CLK_dev;
uint8 CLK_pin;
gpio_dev *SI_x_dev;
uint8 SI_x_pin;
gpio_dev *SI_y_dev;
uint8 SI_y_pin;

// ADC
const adc_dev *x_dev;
adc_reg_map *x_regs;
const adc_dev *y_dev;
adc_reg_map *y_regs;

uint32 adc_sequence = 0; //Temporary
uint8 adc_length = 1; //The number of channels to be converted per ADC channel
uint8 ADC1_Sequence[] = { /*The ADC1 channels sequence, left to right*/
  8 }; /* Set the sequence 1-6 for SQR3 (left will be first). Must top up to all 6 channels with zeros */
uint8 ADC2_Sequence[] = { /*The ADC2 channels sequence, left to right*/
  7 };

/*
* calc_adc_sequence(ADCx_Sequence) converts the SQR3 6 channels' (each ADC1 and ADC2) list into
* a valid 6 X 5=30 bits sequence format and returns that 30 bits number.
* For more channels, repeat the same for SQR2, SQR1. (For SQR1 4 channels only!)
*/
uint32 calc_adc_sequence(uint8 adc_sequence_array[6])
{
  adc_sequence = 0;
  for (int i = 0; i<6; i++) // There are 6 available sequences in each SQR3 SQR2, and 4 in SQR1.
  {
    /*This function converts the array into one number by multiplying each 5-bits channel numbers
by multiplications of 2^5
*/
    adc_sequence = adc_sequence + adc_sequence_array[i] * pow(2, (i * 5));
  }
  return adc_sequence;
} //end calc_adc_sequence

void setup()
{
    // the following line is needed for Maple
    pinMode(AO_x, INPUT_ANALOG);
    pinMode(AO_y, INPUT_ANALOG);
    pinMode(CLK, OUTPUT);
    pinMode(SI_x, OUTPUT);
    pinMode(SI_y, OUTPUT);
    // 下面的代码可能不全，会导致串口无输出
    adc_init(ADC1); //rcc_clk_enable(ADC1->clk_id), Must be the first adc command!
    adc_init(ADC2);

    adc_enable(ADC1); //ADC_CR2_ADON_BIT = 1
    adc_enable(ADC2);

    adc_calibrate(ADC1); //Optional
    adc_calibrate(ADC2);

    // 采样率优化的关键寄存器参数
    adc_set_prescaler(ADC_PRE_PCLK2_DIV_2);
    adc_set_sample_rate(ADC1, ADC_SMPR_1_5);
    adc_set_sample_rate(ADC2, ADC_SMPR_1_5);

    adc_set_exttrig(ADC1, 1); //External trigger must be Enabled for both ADC.
    adc_set_exttrig(ADC2, 1); //External trigger must be Enabled for both ADC.
    adc_set_extsel(ADC1, ADC_ADC12_SWSTART); //External trigger Event
    adc_set_extsel(ADC2, ADC_ADC12_SWSTART); //External trigger Event

    adc_set_reg_seqlen(ADC1, adc_length); //The number of channels to be converted.
    adc_set_reg_seqlen(ADC2, adc_length);
    ADC1->regs->SQR3 |= calc_adc_sequence(ADC1_Sequence);
    ADC2->regs->SQR3 |= calc_adc_sequence(ADC2_Sequence);  

    ADC1->regs->CR1 &= ~(16 << 16);     // 0000: independent mode. 
    ADC2->regs->CR1 &= ~(16 << 16);     // 0000: independent mode. 

    // digitalWrite() preparation work
    // CLK
    CLK_dev = PIN_MAP[CLK].gpio_device;
    CLK_pin = PIN_MAP[CLK].gpio_bit;
    // SI x
    SI_x_dev = PIN_MAP[SI_x].gpio_device;
    SI_x_pin = PIN_MAP[SI_x].gpio_bit;
    // SI y
    SI_y_dev = PIN_MAP[SI_y].gpio_device;
    SI_y_pin = PIN_MAP[SI_y].gpio_bit;

    // analogRead() preparation work
    // X analog in
    x_dev = PIN_MAP[AO_x].adc_device;
    x_regs = x_dev->regs;
    adc_set_reg_seqlen(x_dev, 1);
//    x_regs->SQR3 = PIN_MAP[AO_x].adc_channel;
    // Y analog in
    y_dev = PIN_MAP[AO_y].adc_device;
    y_regs = y_dev->regs;
    adc_set_reg_seqlen(y_dev, 1);
//    y_regs->SQR3 = PIN_MAP[AO_y].adc_channel;
}

void loop()
{
    COM.println("\nStarting loops:");
    
    start = micros();
    for(int m=0;m<sampleCount;m++)
    {
        pixelIndex = 0;
        
        // digitalWrite(CLK, LOW);
        CLK_dev->regs->BSRR = (1U << CLK_pin)<<16;    // CLK set low
        SI_x_dev->regs->BSRR = (1U << SI_x_pin)<<16;  // SI x set low
        SI_x_dev->regs->BSRR = (1U << SI_x_pin);      // SI x set high
        CLK_dev->regs->BSRR = (1U << CLK_pin);        // CLK set high
        x_regs->CR2 |= ADC_CR2_SWSTART;
        while (!(x_regs->SR & ADC_SR_EOC))
            ;
        pixelVal[pixelIndex] = (uint16)(x_regs->DR & ADC_DR_DATA);
        SI_x_dev->regs->BSRR = (1U << SI_x_pin)<<16;  // SI x set low
        CLK_dev->regs->BSRR = (1U << CLK_pin)<<16;    // CLK set low
        
        while (pixelIndex < 128)
        {
            pixelIndex++;

            // digitalWrite(CLK, HIGH);
            CLK_dev->regs->BSRR = (1U << CLK_pin);

//          pixelVal[pixelIndex] = analogRead(AO_x);
            x_regs->CR2 |= ADC_CR2_SWSTART;
            while (!(x_regs->SR & ADC_SR_EOC))
                ;
            pixelVal[pixelIndex] = (uint16)(x_regs->DR & ADC_DR_DATA);

            // digitalWrite(CLK, LOW);
            CLK_dev->regs->BSRR = (1U << CLK_pin)<<16;
        }
    }
    stop = micros();

    COM.println("Stop loops:");
    COM.print("Elapsed Time: ");
    COM.print(stop - start);
    COM.print(" us (for ");
    COM.print(sampleCount*128);
    COM.println(" analog reads)");
    COM.print((stop-start)/(double)(sampleCount*128));
    COM.print(" us (for 1 sample) ");
    COM.print((stop-start)/(double)(sampleCount));
    COM.print(" us (for 1 sensor) ");
    COM.println(" pixelVal = ");
    for(int k=0;k<128;k++)
    {
        COM.print(pixelVal[k]);
        COM.print("  ");
        if(k % 20 == 0)
            COM.println("");
    }
    COM.println(" PCLK2 = ");
    COM.println(PCLK2);
}

// Force init to be called *first*, i.e. before static object allocation.
// Otherwise, statically allocated objects that need libmaple may fail.
// 下面的代码缺失的话，下载后maple mini串口设备消失，无法调试
__attribute__((constructor)) void premain() {
    init();
}

int main(void) {
    setup();
    while (true) {
        loop();
    }
    return 0;
}

