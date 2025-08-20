using EdFi.Ods.AdminApi.V1.Helpers;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Extensions;
public static class QueryExtensions
{
    /// <summary>
    /// Apply pagination based on the offset and limit
    /// </summary>
    /// <typeparam name="T">Type of entity</typeparam>
    /// <param name="source">IQueryable entity list to apply the pagination</param>
    /// <param name="offset"></param>
    /// <param name="limit"></param>
    /// <param name="settings">App Setting values</param>
    /// <returns>Paginated list</returns>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> source, int? offset, int? limit, IOptions<AppSettings> settings)
    {
        try
        {
            if (offset == null)
                offset = settings.Value.DefaultPageSizeOffset;

            if (limit == null)
                limit = settings.Value.DefaultPageSizeLimit;

            return source.Skip(offset.Value).Take(limit.Value);
        }
        catch (Exception)
        {
            // If this throws an exception simply don't paginate.
            return source;
        }
    }
}
