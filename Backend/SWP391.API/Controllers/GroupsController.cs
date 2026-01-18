using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Application.DTOs.Admin;
using SWP391.Application.Interfaces;

namespace SWP391.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    /// <summary>
    /// Lấy danh sách tất cả groups
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetAll()
    {
        var groups = await _groupService.GetAllGroupsAsync();
        return Ok(groups);
    }

    /// <summary>
    /// Lấy thông tin group theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupDto>> GetById(int id)
    {
        var group = await _groupService.GetGroupByIdAsync(id);
        if (group == null) return NotFound();
        return Ok(group);
    }

    /// <summary>
    /// Tạo group mới
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<GroupDto>> Create([FromBody] CreateGroupDto dto)
    {
        var group = await _groupService.CreateGroupAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    /// <summary>
    /// Cập nhật group
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<GroupDto>> Update(int id, [FromBody] UpdateGroupDto dto)
    {
        var group = await _groupService.UpdateGroupAsync(id, dto);
        if (group == null) return NotFound();
        return Ok(group);
    }

    /// <summary>
    /// Xóa group (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _groupService.DeleteGroupAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Lấy danh sách members của group
    /// </summary>
    [HttpGet("{id}/members")]
    public async Task<ActionResult<IEnumerable<UserListDto>>> GetMembers(int id)
    {
        var members = await _groupService.GetGroupMembersAsync(id);
        return Ok(members);
    }

    /// <summary>
    /// Thêm member vào group
    /// </summary>
    [HttpPost("{id}/members/{userId}")]
    public async Task<ActionResult> AddMember(int id, int userId)
    {
        var result = await _groupService.AddMemberToGroupAsync(id, userId);
        if (!result) return BadRequest("Failed to add member");
        return Ok();
    }

    /// <summary>
    /// Xóa member khỏi group
    /// </summary>
    [HttpDelete("{id}/members/{userId}")]
    public async Task<ActionResult> RemoveMember(int id, int userId)
    {
        var result = await _groupService.RemoveMemberFromGroupAsync(id, userId);
        if (!result) return NotFound();
        return NoContent();
    }
}
