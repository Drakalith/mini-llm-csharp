namespace MiniLlmDidatico.Canal;

public sealed class WhatsAppConfig
{
    public string EvolutionApiUrl { get; set; } = "http://localhost:8080";
    public string ApiKey { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "oria";
    public int PortaWebhook { get; set; } = 5000;
}
