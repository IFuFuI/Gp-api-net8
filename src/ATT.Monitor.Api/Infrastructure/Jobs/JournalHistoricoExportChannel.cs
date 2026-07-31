using System.Threading.Channels;

namespace ATT.Monitor.Api.Infrastructure.Jobs;

/// <summary>Cola ligera entre controlador (<see cref="ChannelWriter{T}"/>) y worker (<see cref="ChannelReader{T}"/>).</summary>
public sealed class JournalHistoricoExportChannel
{
    private JournalHistoricoExportChannel(Channel<string> backing)
    {
        Writer = backing.Writer;
        Reader = backing.Reader;
    }

    public ChannelWriter<string> Writer { get; }
    public ChannelReader<string> Reader { get; }

    public static JournalHistoricoExportChannel Create()
    {
        var backing = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = false
        });
        return new JournalHistoricoExportChannel(backing);
    }
}
