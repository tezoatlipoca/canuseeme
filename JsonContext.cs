using System.Text.Json.Serialization;

[JsonSerializable(typeof(RemoteSiteDto))]
[JsonSerializable(typeof(ExceptionInfoDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}