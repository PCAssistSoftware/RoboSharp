using RoboSharp;
using RoboSharp.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// This example console app uses Top-Level Statements introduced in .Net6, hence no need for a 'Program' class with a 'Main' routine.
// See https://aka.ms/new-console-template for more information

try
{
    // test construction of a basic command
    using (var testCmd = new RoboCommand()){ }

    // Change this to the desired factory - a custom factory will be required for use with linux systems
    IRoboCommandFactory commandFactory = RoboCommandFactory.Default;
    await RunCommand(commandFactory, args);

}
catch(Exception e)
{
    Console.WriteLine(e.Message);
    Console.ReadLine();
    System.Environment.Exit(1);
}
System.Environment.Exit(0);


static async Task RunCommand(IRoboCommandFactory commandFactory, params string[] args)
{
    // Get the command and execute it
    IRoboCommand cmd;
    if (args != null && args.Where(s => !string.IsNullOrWhiteSpace(s)).Any())
    {
        cmd = RoboSharp.RoboCommandParser.Parse(new StringBuilder().AppendJoin(' ', args).ToString(), commandFactory);
    }
    else
    {
        cmd = AskForCommandParameters(commandFactory);
    }
    Console.WriteLine("Command Parsed Successfully -- Starting command.\n");
    cmd.OnFileProcessed += Cmd_OnFileProcessed;
    cmd.OnError += Cmd_OnError;

    await cmd.Start(); // If using the default factory, this will throw PlatformNotSupported in a non-windows environment!
    if (cmd is IDisposable dp) dp.Dispose();
}

static IRoboCommand AskForCommandParameters(IRoboCommandFactory factory)
{
    while (true)
    {
        try
        {
            Console.WriteLine("Enter your Robocopy command to be parsed: ");
            string? input = Console.ReadLine();
            IRoboCommand command = RoboSharp.RoboCommandParser.Parse(input, factory);
            return command;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine("\n\n-------------------------------------------");
        }
    }
}

static void Cmd_OnFileProcessed(IRoboCommand sender, FileProcessedEventArgs e)
{
    Console.WriteLine(e.ProcessedFile.ToString());
}

static void Cmd_OnError(IRoboCommand sender, RoboSharp.ErrorEventArgs e)
{
    Console.WriteLine(e.Error);
    Console.WriteLine(e.ErrorDescription);
}