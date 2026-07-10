using Shared.Grpc;

namespace RestApiService.Models;

public record ConfigurationEntityResponse(int Id, string Code, string Description, ConfigurationEntityType Type);