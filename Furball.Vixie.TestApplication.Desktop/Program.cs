using Furball.Vixie.TestApplication;
using NDesk.Options;

int hostPort   = 0;
int clientPort = 0;

OptionSet p = new OptionSet {
    {
        "h|hostport=", () => "The host port to connect to.",
        (int host) => hostPort = host
    }, {
        "c|clientport=",
        () => "The client port to listen on.",
        (int client) => clientPort = client
    }
};

p.Parse(args);

bool harness = hostPort != 0 || clientPort != 0;

new TestGame(harness, hostPort, clientPort).Run();