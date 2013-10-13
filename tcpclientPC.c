#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <netdb.h>
#include <sys/types.h>
#include <sys/socket.h>

int main(int argc,char *argv[])
{
    int sockfd,numbytes;
    char buf[256];
    struct sockaddr_in their_addr;
    int i = 0;
    
    ////���������ֺ͵�ַת��
    ////he = gethostbyname(argv[1]);
    
    ////����һ��TCP�׽ӿ�
    if((sockfd = socket(AF_INET,SOCK_STREAM,0))==-1)
    {
        perror("socket");
        printf("create socket error.����һ��TCP�׽ӿ�ʧ��");
        exit(1);
    }
    
    ////��ʼ���ṹ�壬���ӵ���������2323�˿�
    their_addr.sin_family = AF_INET;
    their_addr.sin_port = htons(2323);
    //// their_addr.sin_addr = *((struct in_addr *)he->h_addr);
    /* inet_aton: Convert Internet host address from numbers-and-dots notation in CP
   into binary data and store the result in the structure INP.  */
    if(inet_pton(AF_INET, argv[1], &their_addr.sin_addr) <= 0)
    {
        printf("[%s] is not a valid IPaddress\n", argv[1]);
        exit(1);
    }
    //inet_aton( "192.168.114.171", &their_addr.sin_addr );
    bzero(&(their_addr.sin_zero),8);
    
    ////�ͷ�������������
    if(connect(sockfd,(struct sockaddr *)&their_addr,sizeof(struct sockaddr))==-1)
    {
        perror("connect");
        exit(1);
    }
    
    ////���������������, 6���ֽ���ζ��ֻ��hello!������
    if(send(sockfd,argv[2],strlen(argv[2]),0)==-1)
    {
        perror("send");
        exit(1);
    }
    
    ////���ܴӷ��������ص���Ϣ
    if((numbytes = recv(sockfd,buf,256,0))==-1)
    {
        perror("recv");
        exit(1);
    }
    buf[numbytes] = '\0'; //�ַ�����β
    printf("Recive from server:%s\n",buf);
    
    ////�ر�socket
    close(sockfd);
    return 0;
}
