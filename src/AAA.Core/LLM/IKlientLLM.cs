namespace AAA.Core.LLM;

public interface IKlientLLM
{
    Task<string> WyslijPromptAsync(string prompt, CancellationToken ct = default);
}
