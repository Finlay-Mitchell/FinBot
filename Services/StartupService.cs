using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FinBot.Services
{
    public class StartupService
    {
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public StartupService(IServiceProvider services)
        {
            _services = services;
            _discord = _services.GetRequiredService<DiscordShardedClient>();
            _commands = _services.GetRequiredService<CommandService>();
        }

        public async Task StartAsync()
        {
            if (string.IsNullOrWhiteSpace(Global.Token))
            {
                throw new Exception("Token missing from config.json! Please enter your token there (root directory)");
            }

            await _discord.LoginAsync(TokenType.Bot, Global.Token);
            await _discord.StartAsync();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            OpenPyMod();
        }

        public void OpenPyMod()
        {
            bool hasRanSuccessfully = false;

            try
            {
                while (!hasRanSuccessfully)
                {
                    int PrevPID = Global.GetPreviousProcessTaskPID();
                    Process currentProcess = Process.GetCurrentProcess();

                    if (Process.GetProcesses().Any(x => x.Id == PrevPID) /* && Process.GetProcessById(PrevPID).ProcessName == currentProcess.ProcessName*/)
                    {
                        Process.GetProcessById(PrevPID).Close();
                    }

                    else
                    {
                        ProcessStartInfo start = new ProcessStartInfo();
                        ProcessStartInfo processStartInfo = new ProcessStartInfo(Global.Pythoninterpreter);
                        processStartInfo.UseShellExecute = false;
                        processStartInfo.RedirectStandardOutput = true;
                        processStartInfo.Arguments = $"{Directory.GetCurrentDirectory()}../../../../Modules/Python/main.py";
                        Process process1 = new Process();
                        process1.StartInfo = processStartInfo;
                        process1.Start();
                        Global.processes.ProcessID = process1.Id;
                        Global.UpdatePIDValue(process1.Id);
                        hasRanSuccessfully = true;
                    }
                }
            }

            catch (Exception ex)
            {
                throw new Exception($"There was an issue starting the python module. Could your python interpreter be missing or incorrect in config.json? Reason: {ex.Message}\n{ex.TargetSite}");
            }
        }
    }
}
