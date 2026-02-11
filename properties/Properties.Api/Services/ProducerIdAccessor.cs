using System.Security.Claims;

namespace Properties.Api.Services;

public interface IProducerIdAccessor
{
    Guid? GetProducerId();
}

public class ProducerIdAccessor : IProducerIdAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProducerIdAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetProducerId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        var claim = context.User.FindFirst("producer_id") ?? context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && Guid.TryParse(claim.Value, out var id))
            return id;

        var header = context.Request.Headers["X-Producer-Id"].FirstOrDefault();
        if (header != null && Guid.TryParse(header, out var headerId))
            return headerId;

        return null;
    }
}
