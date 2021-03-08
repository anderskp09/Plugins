
namespace Oxide.Plugins
{
    [Info("Command", "Mikey", "0.0.1")]
    class Command : RustPlugin
    {
        [ChatCommand("test")]
        void test(BasePlayer player, string command, string[] args)
        {
            SendReply(player, args[0]);
        }
        [ConsoleCommand("Contest")]
        void contest(ConsoleSystem.Arg args)
        {
            Puts("you suck in console");
        }
    }
}