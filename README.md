# Verdant Valor Server Project (2025.10.18 ~ 진행 중)

서버 프로젝트 입니다.   
웹 서버, 게임 서버, 채팅 서버를 구축하는 것을 목표로 만들고 있습니다.   

<details>
  <summary>이 프로젝트와 관련된 레파지토리</summary>

  - [클라이언트](https://github.com/binna/VerdantValor_Client)
  - [서버-클라이언트 공통 모듈(Shared)](https://github.com/binna/VerdantValor_Shared)
  - [개발 스케줄 보드](https://github.com/users/binna/projects/1)

</details>
  

<br><br>

## 기술스택

1. 웹 서버
 
    | 구분 | 기술 |
    |------|------|
    | **Framework** | ASP.NET Core(.NET 9.0), EF Core |
    | **DB** | MySQL, Redis |    

2. 채팅 서버
    | 구분 | 기술 |
    |------|------|
    | **Network** | TCP Socket(TcpClient/TcpListener 기반 예정) |    

<br><br>

## 부록
### A. 에코 서버 테스트
1. 클라이언트 10개를 동시에 서버에 연결한 뒤,  
   서버가 수신한 메시지를 모든 클라이언트에게 브로드캐스트하여    
   정상적으로 전달되는지 확인하는 테스트입니다.
       
   [동영상보기](https://youtu.be/xZfiTMKN-EU)
       
   <details>
     <summary>이미지보기</summary>
     <img width="800" height="1400" alt="image" src="https://github.com/user-attachments/assets/46423cc8-b74b-4971-b620-87aac8450785" />
   </details>

2. 클라이언트 100개를 동시에 서버에 연결한 뒤,  
   서버가 수신한 메시지를 모든 클라이언트에게 브로드캐스트하여    
   정상적으로 전달되는지 확인하는 테스트입니다.
       
   [동영상보기](https://youtu.be/KhASnoavt8o)     
