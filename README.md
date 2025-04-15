# ERIUnityLockstepClient
## 说明
帧同步客户端DEMO，配合 [帧同步服务器](https://github.com/Eerrly/ERIUnityLockstepServer) 使用

### 介绍
+ 登录、房间等业务逻辑，使用TCP通信
+ 战斗、校验等战斗逻辑，使用基于kcp2k的KCP通信[^kcp2k]
+ 通讯数据使用ProtoBuf[^google.protobuf]

### 环境
Unity2019-Unity2021

### 使用
运行 `Main.scene`

### 引用
[^kcp2k]:kcp2k - <https://github.com/MirrorNetworking/kcp2k>\
[^google.protobuf]:google.protobuf - <https://github.com/google/protobuf>