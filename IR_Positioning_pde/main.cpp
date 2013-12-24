// pin 19 (Yellow): CLK
// pin 20 (Orange): SI of x axis
// pin 18 (Blue)  : SI of y axis
// Red            : 5V
// White          : Gnd

// Below assignments are cancelled
// pin 5 (Green)  : miso
// pin 6 (Gray)   : sck
// pin 12 (Purple): spi cs
// pin 13 (Red)   : analog switch
// pin 14 (Gray)  : OE

#include <wirish/wirish.h>
#include <libmaple/adc.h>

#define COMM SerialUSB // use this for Maple
// #define COMM Serial // use this for Arduino
unsigned long start = 0;
unsigned long stop = 0;
unsigned long counter = 0;
unsigned long limit = 100000;
unsigned int val[129];

int pinSI_x = 20;
int pinSI_y = 18;
int pinCLK = 19;
int X_analogIn = 3;
int Y_analogIn = 4;

void setup()
{
    // the following line is needed for Maple
    pinMode(X_analogIn, INPUT_ANALOG);
//    pinMode(Y_analogIn, INPUT_ANALOG);
    pinMode(pinCLK, OUTPUT);
    pinMode(pinSI_x, OUTPUT);
    pinMode(pinSI_y, OUTPUT);
    // 下面的代码可能不全，会导致串口无输出
//    adc_init(ADC1); //rcc_clk_enable(ADC1->clk_id), Must be the first adc command!
//    adc_init(ADC2);
//
//    adc_enable(ADC1); //ADC_CR2_ADON_BIT = 1
//    adc_enable(ADC2);
//
//    adc_calibrate(ADC1); //Optional
//    adc_calibrate(ADC2);
    // 采样率优化的关键寄存器参数
    adc_set_prescaler(ADC_PRE_PCLK2_DIV_2);
    adc_set_sample_rate(ADC1, ADC_SMPR_1_5);
    // the following line is needed for Arduino
    // COMM.begin(57600);
}

void loop()
{
    COMM.println("\nStarting loops:");

    // digitalWrite() preparation work
    // CLK
    gpio_dev *pinClk_dev = PIN_MAP[pinCLK].gpio_device;
    uint8 pinCLK_pin = PIN_MAP[pinCLK].gpio_bit;
    // SI x
    gpio_dev *pinSI_x_dev = PIN_MAP[pinSI_x].gpio_device;
    uint8 pinSI_x_pin = PIN_MAP[pinSI_x].gpio_bit;
    // SI y
    gpio_dev *pinSI_y_dev = PIN_MAP[pinSI_y].gpio_device;
    uint8 pinSI_y_pin = PIN_MAP[pinSI_y].gpio_bit;

    // analogRead() preparation work
    // X analog in
    const adc_dev *xdev = PIN_MAP[X_analogIn].adc_device;
    adc_reg_map *xregs = xdev->regs;
    adc_set_reg_seqlen(xdev, 1);
    xregs->SQR3 = PIN_MAP[X_analogIn].adc_channel;
    // Y analog in
//    const adc_dev *ydev = PIN_MAP[Y_analogIn].adc_device;
//    adc_reg_map *yregs = ydev->regs;
//    adc_set_reg_seqlen(ydev, 1);
//    yregs->SQR3 = PIN_MAP[Y_analogIn].adc_channel;
    
    start = micros();
    for(int m=0;m<100;m++)
    {
        counter = 0;
        
        // digitalWrite(pinCLK, LOW);
        pinClk_dev->regs->BSRR = (1U << pinCLK_pin)<<16;    // CLK set low
        pinSI_x_dev->regs->BSRR = (1U << pinSI_x_pin)<<16;  // SI x set low
        pinSI_x_dev->regs->BSRR = (1U << pinSI_x_pin);      // SI x set high
        pinClk_dev->regs->BSRR = (1U << pinCLK_pin);        // CLK set high
        xregs->CR2 |= ADC_CR2_SWSTART;
        while (!(xregs->SR & ADC_SR_EOC))
            ;
        val[counter] = (uint16)(xregs->DR & ADC_DR_DATA);
        pinSI_x_dev->regs->BSRR = (1U << pinSI_x_pin)<<16;  // SI x set low
        pinClk_dev->regs->BSRR = (1U << pinCLK_pin)<<16;    // CLK set low
        
        while (counter < 128)
        {
            counter++;

            // digitalWrite(pinCLK, HIGH);
            pinClk_dev->regs->BSRR = (1U << pinCLK_pin);

//            val[counter] = analogRead(X_analogIn);
            xregs->CR2 |= ADC_CR2_SWSTART;
            while (!(xregs->SR & ADC_SR_EOC))
                ;
            val[counter] = (uint16)(xregs->DR & ADC_DR_DATA);

            // digitalWrite(pinCLK, LOW);
            pinClk_dev->regs->BSRR = (1U << pinCLK_pin)<<16;
        }
    }
    stop = micros();

    COMM.println("Stop loops:");
    COMM.print("Elapsed Time: ");
    COMM.print(stop - start);
    COMM.print(" us (for ");
    COMM.print(limit);
    COMM.println(" analog reads)");
    COMM.println(" val = ");
    for(int k=0;k<128;k++)
        COMM.println(val[k]);
    COMM.println(val[1]);
    COMM.println(" PCLK2 = ");
    COMM.println(PCLK2);
    COMM.println(" pinCLK_pin = ");
    COMM.println(pinCLK_pin);
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
