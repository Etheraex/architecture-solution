using Shared.Grpc;

namespace RestApiService.Models;

public record CreateEntityRequest(string Code, string Description, ConfigurationEntityType Type);