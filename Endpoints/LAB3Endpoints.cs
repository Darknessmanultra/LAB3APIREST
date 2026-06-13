using Microsoft.AspNetCore.Mvc;
using Shortly.Application.Interfaces;
using Shortly.Domain.Entities;

namespace Shortly.Endpoints;

public static class Lab3Endpoints
{
    public static IEndpointRouteBuilder MapLab3Endpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/urls");

        group.MapPost("/", CreateLink);

        group.MapGet("/", GetAllLinks);

        group.MapGet("/{id}", GetLink);

        group.MapDelete("/{id}", DeleteLink);

        group.MapGet("/stats", GetStats);

        return app;
    }

    private static async Task<IResult> CreateLink([FromBody] LinkRequest request,[FromServices] ILinkService service)
    {
        var link = await service.CreateLink(request.Url,request.Id);

        return Results.Created($"/api/links/{link.Id}",link);
    }

    private static async Task<IResult> GetAllLinks(ILinkService service)
    {
        return Results.Ok(await service.GetAllLinks());
    }

    private static async Task<IResult> GetLink([FromBody] LinkRequest request,[FromServices] ILinkService service)
    {
        var link = await service.GetLink(request.Url);

        return link is null
            ? Results.NotFound()
            : Results.Ok(link);
    }

    private static async Task<IResult> DeleteLink([FromBody] DeleteLinkRequest request,[FromServices] ILinkService service)
    {
        var deleted = await service.DeleteLink(request.Url);

        return deleted
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> GetStats(ILinkService service)
    {
        var links = await service.GetAllLinks();

        return Results.Ok(new
    {
        TotalLinks = links.Count,
        TotalClicks = links.Sum(x => x.Clicks),
        MostVisitedLink = links
            .OrderByDescending(x => x.Clicks)
            .Select(x => new
            {
                x.Id,
                x.Url,
                x.Clicks
            })
            .FirstOrDefault(),
        Links = links.Select(x => new
        {
            x.Id,
            x.Clicks,
        })
    });
    }
}