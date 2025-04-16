# ERIUnityLockstepClient
## 说明
帧同步客户端DEMO，配合 [帧同步服务器](https://github.com/Eerrly/ERIUnityLockstepServer) 使用

### 介绍
+ 登录、房间等业务逻辑，使用TCP通信
+ 战斗、校验等战斗逻辑，使用基于kcp2k的KCP通信[^kcp2k]
+ 通讯数据使用Google的ProtoBuf[^google.protobuf]

### 环境
[Unity官网](https://unity.com/) Unity2019.4.37f1 - Unity2021.3.42f1

### 使用
1. 先启动服务器
2. 运行 `Main.scene` 在界面中输入账号密码
3. **[Player1]**:`Attach` -> `Login` **[Player2]**:`Attach` -> `Login`
4. **[Player1]**:`CreateRoom` -> `JoinRoom` **[Player2]**:`JoinRoom`
5. **[Player1]**:`Connect` -> `Ready` **[Player2]**:`Connect` -> `Ready`
6. `W`、`A`、`S`、`D`操控方向移动，`J`、`K`、`L`为按键，目前只支持`J`键，为攻击键

### 引用
[^kcp2k]:kcp2k - <https://github.com/MirrorNetworking/kcp2k>
[^google.protobuf]:google.protobuf - <https://github.com/google/protobuf>