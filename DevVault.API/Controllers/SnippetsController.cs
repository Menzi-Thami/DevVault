using DevVault.API.Contracts;
using DevVault.Application.Common.Paging;
using DevVault.Application.Snippets.Commands.CreateSnippet;
using DevVault.Application.Snippets.Dtos;
using DevVault.Application.Snippets.Queries.GetSnippetById;
using DevVault.Application.Snippets.Queries.ListSnippets;
using Microsoft.AspNetCore.Mvc;

namespace DevVault.API.Controllers;

// Authenticated by the fallback policy; every action is scoped to the caller's own snippets.
[ApiController]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class SnippetsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SnippetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SnippetDto>> Create(
        [FromBody] CreateSnippetRequest request,
        [FromServices] CreateSnippetHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateSnippetCommand(request.Title, request.Content, request.Language);
        var dto = await handler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet]
    [ProducesResponseType(typeof(CursorPage<SnippetSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CursorPage<SnippetSummaryDto>>> List(
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        [FromServices] ListSnippetsHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(new ListSnippetsQuery(pageSize, cursor), cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SnippetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SnippetDto>> GetById(
        Guid id,
        [FromServices] GetSnippetByIdHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));
}
