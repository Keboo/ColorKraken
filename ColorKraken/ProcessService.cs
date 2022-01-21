using System.Diagnostics;

namespace ColorKraken;

public interface IProcessService
{
    void Start(ProcessStartInfo startInfo);
}

public class ProcessService : IProcessService
{
    public void Start(ProcessStartInfo startInfo)
        => Process.Start(startInfo);
}
