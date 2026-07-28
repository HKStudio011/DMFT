namespace DMFT.Services;

public interface IYtDlpConfigProvider
{
    string ExecutablePath { get; }
    string ExtraArguments { get; }
    string OutputTemplate { get; }
    string FormatString { get; }
}
