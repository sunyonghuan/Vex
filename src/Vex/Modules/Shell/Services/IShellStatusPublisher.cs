namespace Vex.Modules.Shell.Services;

public interface IShellStatusPublisher
{
    void Publish(string message);
}
