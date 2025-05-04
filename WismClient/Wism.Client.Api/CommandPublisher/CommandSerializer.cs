using Newtonsoft.Json;
using Wism.Client.Commands;

public class CommandSerializer
{
    public string Serialize(Command command) =>
        JsonConvert.SerializeObject(command, new JsonSerializerSettings
        {
            ContractResolver = new CommandContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

    public Command Deserialize(string json) =>
        JsonConvert.DeserializeObject<Command>(json);
}