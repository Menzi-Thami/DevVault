using DevVault.Application.Snippets.Commands.CreateSnippet;
using DevVault.Application.Snippets.Dtos;
using DevVault.Application.Snippets.Queries.GetSnippetById;
using DevVault.Application.Snippets.Queries.ListSnippets;
using Microsoft.AspNetCore.Mvc;

namespace DevVault.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SnippetsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SnippetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SnippetDto>> Create(
        [FromBody] CreateSnippetCommand command,
        [FromServices] CreateSnippetHandler handler,
        CancellationToken cancellationToken)
    {
        var dto = await handler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SnippetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SnippetDto>>> List(
        [FromServices] ListSnippetsHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SnippetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SnippetDto>> GetById(
        Guid id,
        [FromServices] GetSnippetByIdHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));
}
