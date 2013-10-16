#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <netdb.h>
#include <sys/types.h>
#include <sys/socket.h>

int main(int argc,char *argv[])
{
    int sockfd,new_fd;
    struct sockaddr_in my_addr;
    struct sockaddr_in their_addr;
    int sin_size,i;

    //建立TCP套接口
    //AF_INET: Internet IP Protocol
    //SOCK_STREAM: Sequenced, reliable, connection-based byte streams
    //0: IPPROTO_IP = 0, Dummy protocol for TCP
    if((sockfd = socket(AF_INET,SOCK_STREAM,0))==-1)
    {
        printf("create socket error");
        perror("socket");
        exit(1);
    }
    
    ////初始化sockaddr_in结构体（地址和通道），并绑定2323端口
    my_addr.sin_family = AF_INET;
    //host byte order to net
    my_addr.sin_port = htons(2323);
    //INADDR_ANY: Address to accept any incoming messages
    my_addr.sin_addr.s_addr = INADDR_ANY;
    //#define sin_zero __pad
    bzero(&(my_addr.sin_zero),8);
    
    ////绑定套接口
    if(bind(sockfd,(struct sockaddr *)&my_addr,sizeof(struct sockaddr))==-1)
    {
        perror("bind socket error");
        exit(1);
    }
    
    ////创建监听套接口
    //N connection requests will be queued before further requests are refused.
    if(listen(sockfd,10)==-1)
    {
        perror("listen");
        exit(1);
    }
    
    ////等待连接
    while(1)
    {
        sin_size = sizeof(struct sockaddr); //either sockaddr or sockaddr_in can work normally
        
        ////如果建立连接，将产生一个全新的套接字
        if((new_fd = accept(sockfd,(struct sockaddr *)&their_addr,&sin_size))==-1)
        {
            perror("accept");
            exit(1);
        }
        
        ////生成一个子进程来完成和客户端的会话，父进程继续监听
        //fork: Return -1 for errors, 0 to the new process
        if(!fork())
        {
            ////读取客户端发来的信息
            unsigned int recvCount, totalrecvCount = 0, rxXY;
            uint8_t buff[512];

            while(1)
            {
//begin:
                memset(buff,0,512);
                rxXY = 0;
                
Rerecv:
                if((recvCount = recv(new_fd,buff,512,0))==-1)
//                if((recvCount = recv(new_fd,buff,sizeof(buff),0))==-1)
                {
                    perror("recv");
                    exit(1);
                }
//                if(recvCount != 512)
//                    goto begin;
                totalrecvCount = totalrecvCount + recvCount;
                if(totalrecvCount < 512)
                    goto Rerecv;

                printf("recvCount=%d totalrecvCount=%d \n", recvCount, totalrecvCount);
                totalrecvCount = 0;
                for(i=0;i<recvCount;i=i+2)
                {
                    rxXY = buff[i];
                    rxXY = rxXY<<8 | buff[i+1];
                    printf("(i=%d, %d) ",i,rxXY);
                }
                puts("");
            }
           
            close(new_fd);
            exit(0);
        }
        close(new_fd);
    }
    
    close(sockfd);
}
