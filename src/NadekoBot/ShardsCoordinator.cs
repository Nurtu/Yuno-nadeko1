﻿using NadekoBot.Services;
using NadekoBot.Services.Impl;
using NLog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.ShardCom;

namespace NadekoBot
{
    public class ShardsCoordinator
    {
        private readonly BotCredentials _creds;
        private readonly Process[] ShardProcesses;
        public ShardComMessage[] Statuses { get; }
        public int GuildCount => Statuses.ToArray()
            .Where(x => x != null)
            .Sum(x => x.Guilds);

        private readonly Logger _log;
        private readonly ShardComServer _comServer;
        private readonly int _port;
        private readonly int _curProcessId;

        public ShardsCoordinator(int port)
        {
            LogSetup.SetupLogger();
            _creds = new BotCredentials();
            ShardProcesses = new Process[_creds.TotalShards];
            Statuses = new ShardComMessage[_creds.TotalShards];
            _log = LogManager.GetCurrentClassLogger();
            _port = port;

            _comServer = new ShardComServer(port);
            _comServer.Start();

            _comServer.OnDataReceived += _comServer_OnDataReceived;

            _curProcessId = Process.GetCurrentProcess().Id;
        }

        private Task _comServer_OnDataReceived(ShardComMessage msg)
        {
            Statuses[msg.ShardId] = msg;

            if (msg.ConnectionState == Discord.ConnectionState.Disconnected || msg.ConnectionState == Discord.ConnectionState.Disconnecting)
                _log.Error("!!! SHARD {0} IS IN {1} STATE", msg.ShardId, msg.ConnectionState.ToString());
            return Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            for (int i = 1; i < _creds.TotalShards; i++)
            {
                var p = Process.Start(new ProcessStartInfo()
                {
                    FileName = _creds.ShardRunCommand,
                    Arguments = string.Format(_creds.ShardRunArguments, i, _curProcessId, _port)
                });
                await Task.Delay(5000);
            }
        }

        public async Task RunAndBlockAsync()
        {
            try
            {
                await RunAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            //await Task.Run(() =>
            //{
            //    string input;
            //    while ((input = Console.ReadLine()?.ToLowerInvariant()) != "quit")
            //    {
            //        try
            //        {
            //            switch (input)
            //            {
            //                case "ls":
            //                    var groupStr = string.Join(",", Statuses
            //                        .ToArray()
            //                        .Where(x => x != null)
            //                        .GroupBy(x => x.ConnectionState)
            //                        .Select(x => x.Count() + " " + x.Key));
            //                    _log.Info(string.Join("\n", Statuses
            //                        .ToArray()
            //                        .Where(x => x != null)
            //                        .Select(x => $"Shard {x.ShardId} is in {x.ConnectionState.ToString()} state with {x.Guilds} servers. {(DateTime.UtcNow - x.Time).ToString(@"hh\:mm\:ss")} ago")) + "\n" + groupStr);
            //                    break;
            //                default:
            //                    break;
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            _log.Warn(ex);
            //        }
            //    }
            //});

            await Task.Delay(-1);
            foreach (var p in ShardProcesses)
            {
                try { p.Kill(); } catch { }
                try { p.Dispose(); } catch { }
            }
        }
    }
}
