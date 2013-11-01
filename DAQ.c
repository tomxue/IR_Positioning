#include <sys/types.h>
#include <stdio.h>
#include <sys/ioctl.h>
#include <sys/mman.h>
#include <fcntl.h>
#include <memory.h>

// for SPI
#include <stdint.h>
#include <unistd.h>
#include <getopt.h>
#include <linux/types.h>
#include <linux/spi/spidev.h>

// for socket
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <netinet/in.h>

#define bool int
#define false 0
#define true 1

#define SEND_DATA_COUNT 128*2*2 // 128 pixel/axis, 2-axis, ADC: 2 bytes/pixel

//===============================================================================
// SPI functions
#define ARRAY_SIZE(a) (sizeof(a) / sizeof((a)[0]))

static void pabort(const char *s)
{
    perror(s);
    abort();  // abort()是使异常程序终止，同时发送SIGABRT信号给调用进程
}

static const char *device = "/dev/spidev4.0";
static uint8_t mode;
static uint8_t bits = 8;
static uint32_t speed = 48000000;
static uint16_t delay;
uint8_t rxXY[SEND_DATA_COUNT] = {0, };

bool spiSampleOnePixel(int fd)
{
    int ret;
    static int pixelCount;
    bool XYDataReady;
    uint8_t tx[2] = {0x31, 0x32, };
    uint8_t rx[2] = {0, };	//the comma here doesn't matter, tested by Tom Xue
//    uint8_t rx0Raw, rx1Raw;
    
    struct spi_ioc_transfer tr = {
        .tx_buf = (unsigned long)tx,
        .rx_buf = (unsigned long)rx,
        .len = ARRAY_SIZE(tx),
        .delay_usecs = delay,
        .speed_hz = speed,
        .bits_per_word = bits,	//important, bits = 8 means byte transfer is possible
    };

    ret = ioctl(fd, SPI_IOC_MESSAGE(1), &tr);
    if (ret < 1)
        pabort("can't send spi message\n");

    // to fill in the X-Y array
//    if(pixelCount == 0)
//        printf("The SPI raw data is: %.2X %.2X ------ ", rx[0], rx[1]);
//    rx0Raw = rx[0];
//    rx1Raw = rx[1];
    rx[0] = rx[0] & 0x3f;   // ADC: 2 leading zeros
    rx[1] = rx[1] & 0xfc;   // ADC: 2 trailing zeros
    rxXY[pixelCount] = rx[0];
    rxXY[pixelCount+1] = rx[1];
//    if(pixelCount == 0)
//    {
//        printf("The SPI data is: %.2X %.2X --- ", rxXY[pixelCount], rxXY[pixelCount+1]);
//        if(rx0Raw == rx[0] && rx1Raw == rx[1])
//            printf("matched \n");
//        else
//            printf("dismatched!!!!!!!!!!!! \n");
//    }

    pixelCount = pixelCount+2;
    if(pixelCount == SEND_DATA_COUNT)
    {
        pixelCount = 0;
        XYDataReady = true;
    }
    else
        XYDataReady = false;

    return XYDataReady;
}

int spiPrepare()
{
    int ret = 0;
    int fd;

    fd = open(device, O_RDWR);
    if (fd < 0)
        pabort("can't open device\n");

    /*
     * spi mode
     */
    mode = 2;   // SPI_MODE_2
    ret = ioctl(fd, SPI_IOC_WR_MODE, &mode);
    if (ret == -1)
        pabort("can't set spi mode\n");

    ret = ioctl(fd, SPI_IOC_RD_MODE, &mode);
    if (ret == -1)
        pabort("can't get spi mode\n");

    /*
     * bits per word
     */
    ret = ioctl(fd, SPI_IOC_WR_BITS_PER_WORD, &bits);
    if (ret == -1)
        pabort("can't set bits per word\n");

    ret = ioctl(fd, SPI_IOC_RD_BITS_PER_WORD, &bits);
    if (ret == -1)
        pabort("can't get bits per word\n");

    /*
     * max speed hz
     */
    ret = ioctl(fd, SPI_IOC_WR_MAX_SPEED_HZ, &speed);
    if (ret == -1)
        pabort("can't set max speed hz\n");

    ret = ioctl(fd, SPI_IOC_RD_MAX_SPEED_HZ, &speed);
    if (ret == -1)
        pabort("can't get max speed hz\n");

    printf("open device: %s\n", device);
    printf("set spi mode: %d\n", mode);
    printf("set bits per word: %d\n", bits);
    printf("set max speed: %d Hz (%d MHz)\n", speed, speed/1000000);

    return fd;
}
//===============================================================================

// run on BB-XM-00 RevC
// GPIO_144oe: IR_positioning OE pin
// GPIO_145clk/GPT10_PWMEVT: IR_positioning CLK_1V8 pin
// GPIO_146si/GPT11_PWMEVT: IR_positioning SI_1V8 pin
// GPIO_139sw: IR_positioning Analog Switch IN pin
// McSPI4 applied

// By setting PADCONFS/OE/DATAOUT registers, it is finally done. Great!
// Beagle Board uses a transistor to drive the LED, which is controlled by GPIO; GPIO -> transistor -> LED
// that means GPIO's output really drives the LED

#define GPIO_BASE 			0x48002000
//GPIO_144oe register address, the resigter is 32-bit
#define GPIO_139sw_OFFSET 		0x168   // P2437, high 16 bits
#define GPIO_143cs_OFFSET 		0x170   // P2437, high 16 bits
#define GPIO_144oe_OFFSET 		0x174   // P2437, low 16 bits
#define GPIO_145clk_OFFSET 		0x174   // P2437, high 16 bits
#define GPIO_146si_OFFSET 		0x178   // P2437, low 16 bits

//P3461,  General-Purpose Interface Integration Figure, GPIO5: GPIO_[159:128]
#define GPIO139sw   0x00000800   		// Bit 21, GPIO149; Bit 16, GPIO144oe; Bit 10, GPIO138
#define GPIO143cs   0x00008000   		// Bit 21, GPIO149; Bit 16, GPIO144oe; Bit 10, GPIO138
#define GPIO144oe   0x00010000   		// Bit 21, GPIO149; Bit 16, GPIO144oe; Bit 10, GPIO138
#define GPIO145clk  0x00020000   	    // Bit 21, GPIO149; Bit 16, GPIO144oe; Bit 10, GPIO138
#define GPIO146si   0x00040000   		// Bit 21, GPIO149; Bit 16, GPIO144oe; Bit 10, GPIO138

#define GPIO5_BASE 		        0x49056000	//P3478
#define GPIO5_OE_OFFSET 		0x034		//P3489, Output Data Enable Register
#define GPIO5_DATAOUT_OFFSET	0x03C		//P3490, Data Out register

#define INT *(volatile unsigned int*)

/*
 * IEN  - Input Enable
 * IDIS - Input Disable
 * PTD  - Pull type Down
 * PTU  - Pull type Up
 * DIS  - Pull type selection is inactive
 * EN   - Pull type selection is active
 * M0   - Mode 0
 */

#define IEN     (1 << 8)
#define IDIS    (0 << 8)
#define PTU     (1 << 4)
#define PTD     (0 << 4)
#define EN      (1 << 3)
#define DIS     (0 << 3)

#define M0      0
#define M1      1
#define M2      2
#define M3      3
#define M4      4
#define M5      5
#define M6      6
#define M7      7

//void below means the pointer points to byte data, if e.g. unsigned int *map_base
//then should be: INT(map_base+GPIO_144oe_OFFSET/4) = padconf;
void *map_base;
int n, fd, spifd, k;
unsigned int padconf;

int wifiPrepare(char *argv)
{
    int sockfd;
    char buf[256];
    struct sockaddr_in their_addr;
    int i = 0;
    
    ////将基本名字和地址转换
    ////he = gethostbyname(argv[1]);
    
    ////建立一个TCP套接口
    if((sockfd = socket(AF_INET,SOCK_STREAM,0))==-1)
    {
        perror("socket");
        printf("create socket error.建立一个TCP套接口失败");
        exit(1);
    }
    
    ////初始化结构体，连接到服务器的2323端口
    their_addr.sin_family = AF_INET;
    their_addr.sin_port = htons(2323);
    //// their_addr.sin_addr = *((struct in_addr *)he->h_addr);
    /* inet_aton: Convert Internet host address from numbers-and-dots notation in CP
   into binary data and store the result in the structure INP.  */
    if(inet_pton(AF_INET, argv, &their_addr.sin_addr) <= 0)
    {
        printf("[%s] is not a valid IPaddress\n", argv);
        exit(1);
    }
    //inet_aton( "192.168.114.171", &their_addr.sin_addr );
    bzero(&(their_addr.sin_zero),8);
    
    ////和服务器建立连接
    if(connect(sockfd,(struct sockaddr *)&their_addr,sizeof(struct sockaddr))==-1)
    {
        perror("connect");
        exit(1);
    }

    return sockfd;
}
    
int wifiSendData(int sockfd)
{
    int sendCount, totalSendCount = 0;
    ////向服务器发送数据
    while(totalSendCount < SEND_DATA_COUNT)
    {
        if((sendCount = send(sockfd,rxXY,SEND_DATA_COUNT,0))==-1)
        {
            perror("send");
            exit(1);
        }
        totalSendCount = totalSendCount + sendCount;
    }
    totalSendCount = 0;
   
    return 0;
}

int DAQStart(char *argv)
{
    int CLKCount = 0, sockfd, XYLoop = 0, delayCount = 0;
    bool XYDataReady;

    if((fd=open("/dev/mem",O_RDWR | O_SYNC))==-1)
    {
        perror("open error!\n");
        return(-1);
    }

    printf("fd=%d\n",fd);

    //GPIO5: Set the pinmux to select the GPIO signal
    map_base = mmap(0,0x200,PROT_READ | PROT_WRITE,MAP_SHARED,fd,GPIO_BASE);
    printf("GPIO_BASE map_base=%p\n",map_base);
    //GPIO139sw
    padconf = INT(map_base+GPIO_139sw_OFFSET);
    padconf &= 0x0000FFFF; //[31:16]=GPIO_139sw  - Clear register bits [15:0]
    padconf |= 0x00040000; //[31:16]=GPIO_139sw  - Select mux mode 4 for gpio
    INT(map_base+GPIO_139sw_OFFSET) = padconf;
    printf("GPIO_139sw_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);
    //GPIO143cs
    padconf = INT(map_base+GPIO_143cs_OFFSET);
    padconf &= 0x0000FFFF; //[31:16]=GPIO_143cs  - Clear register bits [15:0]
    padconf |= 0x00040000; //[31:16]=GPIO_143cs  - Select mux mode 4 for gpio
    INT(map_base+GPIO_143cs_OFFSET) = padconf;
    printf("GPIO_143cs_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);
    //GPIO144oe
    padconf = INT(map_base+GPIO_144oe_OFFSET);
    padconf &= 0xFFFF0000; //[15:0]=GPIO_144oe  - Clear register bits [15:0]
    padconf |= 0x00000004; //[15:0]=GPIO_144oe  - Select mux mode 4 for gpio
    INT(map_base+GPIO_144oe_OFFSET) = padconf;
    printf("GPIO_144oe_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);
    //GPIO145clk
    padconf = INT(map_base+GPIO_145clk_OFFSET);
    padconf &= 0x0000FFFF; //[31:16]=GPIO_145clk  - Clear register bits [15:0]
    padconf |= 0x00040000; //[31:16]=GPIO_145clk  - Select mux mode 4 for gpio
    INT(map_base+GPIO_145clk_OFFSET) = padconf;
    printf("GPIO_145clk_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);
    // GPIO146si
    padconf = INT(map_base+GPIO_146si_OFFSET);
    padconf &= 0xFFFF0000; //[15:0]=GPIO_146si  - Clear register bits [15:0]
    padconf |= 0x00000004; //[15:0]=GPIO_146si  - Select mux mode 4 for gpio
    INT(map_base+GPIO_146si_OFFSET) = padconf;
    printf("GPIO_146si_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);

    munmap(map_base,0x200);

    //GPIO5: Set the OE and DATAOUT registers
    map_base = mmap(0,0x40,PROT_READ | PROT_WRITE,MAP_SHARED,fd,GPIO5_BASE);
    printf("GPIO5_BASE map_base=%p\n",map_base);
    //OE
    padconf = INT(map_base+GPIO5_OE_OFFSET);
    padconf &= ~(GPIO139sw+GPIO143cs+GPIO144oe+GPIO145clk+GPIO146si);  // Set GPIO_139sw, GPIO143cs, GPIO144oe, GPIO_145clk and GPIO_146si to output
    INT(map_base+GPIO5_OE_OFFSET) = padconf;
    printf("GPIO5_OE_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);

    //DATAOUT
    padconf = INT(map_base+GPIO5_DATAOUT_OFFSET);
    //Set GPIO_143cs high
    padconf |=  GPIO143cs;
    INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;
    printf("GPIO5_DATAOUT_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);
    //Set GPIO_144oe high
    padconf |=  GPIO144oe;
    INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;
    printf("GPIO5_DATAOUT_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);

    spifd = spiPrepare();
    sockfd = wifiPrepare(argv);

    padconf &= ~GPIO139sw;    // set GPIO139sw low
    INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;

    while(1)
    {
        // sw, clk, si
        padconf &= ~(GPIO145clk+GPIO146si);    // set GPIO145clk and GPIO146si low
        INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;

        // ------------------after the falling edge of clock, considering si and sw------------------
        CLKCount++;  // according to TSL202's spec, page 5, needs 129 clock cycles
        if(CLKCount == 257)
            CLKCount = 1;

        if(CLKCount == 1)
            XYLoop++;
        if(XYLoop == 257)
            XYLoop = 1;

        // si
        if(CLKCount == 1)
        {
            padconf |=  GPIO146si;    // Set GPIO_146si high
            INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;
        }

        // sw: analog switch: to switch the output of the 2 light sensors
//        if(CLKCount >= 1 && CLKCount <=128)
        if(CLKCount == 1)
        {
            padconf &= ~GPIO139sw;    // Set GPIO139sw low, S1 on, U1(Y) output applied
            INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;
        }
        else if(CLKCount == 129)
        {
            padconf |=  GPIO139sw;    // Set GPIO139sw high, S2 on, U2(X) output applied
            INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;
        }
        // ------------------after the falling edge of clock, considering si and sw------------------

        // clk
        padconf |=  GPIO145clk;    // Set GPIO_145clk high
        INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;

        // ===================after rising edge of clock, considering the sample handler===================
        if(CLKCount == XYLoop)
        {
            // add some delay (integration time) for sample
            for(delayCount;delayCount<4;delayCount++)
                INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;
            // cs
            padconf &=  ~GPIO143cs;    // Set GPIO_143cs low, the sample point
            INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;
        }

        if(CLKCount == 256)
        {
            XYDataReady = spiSampleOnePixel(spifd);

            // cs
            padconf |=  GPIO143cs;    // Set GPIO_143cs high
            INT(map_base+GPIO5_DATAOUT_OFFSET) = padconf;
            
            if(XYDataReady == true)
                wifiSendData(sockfd);
        }
        // ===================after rising edge of clock, considering the sample handler===================
    }
    printf("GPIO5_DATAOUT_OFFSET - The register value is set to: 0x%x = 0d%u\n", padconf,padconf);

    close(fd);
    close(spifd);
    close(sockfd);
    munmap(map_base,0x40);
}

int main(int argc,char *argv[])
{
    DAQStart(argv[1]);
}
