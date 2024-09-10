using Microsoft.Extensions.Options;
public class EnvironmentConfigurationProvider : IConfigureOptions<KafkaConfiguration>
{
    private const string EnvVariableBindingName = "BINDING_NAME";
    private const string EnvVariableTopicName = "TOPIC_NAME";

    public void Configure(KafkaConfiguration options)
    {
        options.BindingName = Environment.GetEnvironmentVariable(EnvVariableBindingName) ?? string.Empty;
        options.TopicName = Environment.GetEnvironmentVariable(EnvVariableTopicName) ?? string.Empty;
    }
}