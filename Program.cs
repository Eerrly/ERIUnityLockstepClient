// See https://aka.ms/new-console-template for more information
using System.Diagnostics;

Random random = new Random();
Stopwatch stopwatch= new Stopwatch();

GameManager.Instance.Initialize();
NetworkManager.Instance.Initialize();
NetworkManager.Instance.TcpConnect();

var lastSentFrame = GameManager.Instance.ServerAuthorityFrame;
var command = string.Empty;
var _ = Task.Run(async ()=>{
    while (true){
        if(!string.IsNullOrEmpty(command))
        {
            System.Console.WriteLine($"command: {command}");
            var commandParams = command.Split(" ");
            if (NetworkManager.Instance.TcpConnected)
            {
                switch (commandParams[0])
                {
                    case "l":
                    case "login":
                    {
                        NetworkManager.Instance.SendLogicLoginMessage(commandParams[1], commandParams[2]);   
                        break;
                    }
                    case "cr":
                    case "createroom":
                    {
                        NetworkManager.Instance.SendLogicCreateRoomMessage(uint.Parse(commandParams[1]));
                        break;
                    }
                    case "jr":
                    case "joinroom":
                    {
                        NetworkManager.Instance.SendLogicJoinRoomMessage(uint.Parse(commandParams[1]), uint.Parse(commandParams[2]));
                        break;
                    }
                }
            }
            if (NetworkManager.Instance.KcpConnected)
            {
                switch(commandParams[0])
                {
                    case "c":
                    case "connect":
                    {
                        NetworkManager.Instance.SendBattleConnectMessage(uint.Parse(commandParams[1]), uint.Parse(commandParams[2]));
                        break;
                    }
                    case "r":
                    case "ready":
                    {
                        NetworkManager.Instance.SendBattleReadyMessage(uint.Parse(commandParams[1]), uint.Parse(commandParams[2]));
                        break;
                    }
                    case "s":
                    case "shutdown":
                    {
                        NetworkManager.Instance.KcpShutdown();
                        break;
                    }
                    case "f":
                    case "frame":
                    {
                        var randomValue = random.Next(1, 5);
                        GameManager.Instance.SetFrame((byte)randomValue);
                        break;
                    }
                }
            }
            command = string.Empty;
        }
    }
});

System.Console.WriteLine($"ClientUri: {NetworkManager.Instance.KcpUri}");
while (true) { command = Console.ReadLine(); }