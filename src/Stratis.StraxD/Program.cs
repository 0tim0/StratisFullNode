using System;
using System.Threading.Tasks;
using NBitcoin.Protocol;
using Stratis.Bitcoin;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Features.Api;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Features.ColdStaking;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.MemoryPool;
using Stratis.Bitcoin.Features.Miner;
using Stratis.Bitcoin.Features.RPC;
using Stratis.Bitcoin.Features.SignalR;
using Stratis.Bitcoin.Features.SignalR.Broadcasters;
using Stratis.Bitcoin.Features.SignalR.Events;
using Stratis.Bitcoin.Networks;
using Stratis.Bitcoin.Utilities;
using Stratis.Features.Diagnostic;
using Stratis.Features.SQLiteWalletRepository;

namespace Stratis.StraxD
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // set the console window title to identify this as a Strax full node (for clarity when running Strax and Cirrus on the same machine)
                Console.Title = "Strax Full Node";
                var nodeSettings = new NodeSettings(networksSelector: Networks.Strax, protocolVersion: ProtocolVersion.PROVEN_HEADER_VERSION, args: args)
                {
                    MinProtocolVersion = ProtocolVersion.PROVEN_HEADER_VERSION
                };

                IFullNodeBuilder nodeBuilder = new FullNodeBuilder()
                    .UseNodeSettings(nodeSettings)
                    .UseBlockStore()
                    .UsePosConsensus()
                    .UseMempool()
                    .UseColdStakingWallet()
                    .AddSQLiteWalletRepository()
                    .AddPowPosMining(true)
                    .UseApi()
                    .AddRPC()
                    .UseDiagnosticFeature();

                if (nodeSettings.EnableSignalR)
                {
                    nodeBuilder.AddSignalR(options =>
                    {
                        options.EventsToHandle = new[]
                        {
                            (IClientEvent) new BlockConnectedClientEvent(),
                            new TransactionReceivedClientEvent()
                        };

                        options.ClientEventBroadcasters = new[]
                        {
                            (Broadcaster: typeof(StakingBroadcaster), ClientEventBroadcasterSettings: new ClientEventBroadcasterSettings
                                {
                                    BroadcastFrequencySeconds = 5
                                }),
                            (Broadcaster: typeof(WalletInfoBroadcaster), ClientEventBroadcasterSettings: new ClientEventBroadcasterSettings
                                {
                                    BroadcastFrequencySeconds = 5
                                })
                        };
                    });
                }

                IFullNode node = nodeBuilder.Build();

                if (node != null)
                    await node.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing the node. Details: '{0}'", ex);
            }
        }
    }
}
