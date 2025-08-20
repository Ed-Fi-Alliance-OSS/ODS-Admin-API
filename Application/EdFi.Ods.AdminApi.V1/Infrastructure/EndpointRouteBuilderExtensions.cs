using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Infrastructure;

public static class EndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder WithResponseCode(this RouteHandlerBuilder builder, int code, string? description = null)
    {
        builder.Produces(code);
        builder.WithMetadata(new SwaggerResponseAttribute(code, description));
        return builder;
    }

    public static RouteHandlerBuilder WithResponse<T>(this RouteHandlerBuilder builder, int code, string? description = null)
    {
        builder.Produces(code, responseType: typeof(T));
        builder.WithMetadata(new SwaggerResponseAttribute(code, description, typeof(T)));
        return builder;
    }
}
