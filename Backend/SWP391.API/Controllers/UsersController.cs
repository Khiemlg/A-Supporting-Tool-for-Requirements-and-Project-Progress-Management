using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Application.DTOs.Admin;
using SWP391.Application.Interfaces;

namespace SWP391.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userService;

    public UsersController(IUserManagementService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Lấy danh sách tất cả users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserListDto>>> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Lấy users theo role
    /// </summary>
    [HttpGet("by-role/{role}")]
    public async Task<ActionResult<IEnumerable<UserListDto>>> GetByRole(string role)
    {
        var users = await _userService.GetUsersByRoleAsync(role);
        return Ok(users);
    }

    /// <summary>
    /// Lấy danh sách lecturers
    /// </summary>
    [HttpGet("lecturers")]
    public async Task<ActionResult<IEnumerable<UserListDto>>> GetLecturers()
    {
        var lecturers = await _userService.GetLecturersAsync();
        return Ok(lecturers);
    }

    /// <summary>
    /// Cập nhật role của user
    /// </summary>
    [HttpPut("role")]
    public async Task<ActionResult<UserListDto>> UpdateRole([FromBody] UpdateUserRoleDto dto)
    {
        var user = await _userService.UpdateUserRoleAsync(dto);
        if (user == null) return BadRequest("Failed to update role");
        return Ok(user);
    }

    /// <summary>
    /// Assign user vào group
    /// </summary>
    [HttpPost("assign-group")]
    public async Task<ActionResult> AssignToGroup([FromBody] AssignUserToGroupDto dto)
    {
        var result = await _userService.AssignUserToGroupAsync(dto);
        if (!result) return BadRequest("Failed to assign user to group");
        return Ok();
    }

    /// <summary>
    /// Assign lecturer cho group
    /// </summary>
    [HttpPost("assign-lecturer")]
    public async Task<ActionResult> AssignLecturer([FromBody] AssignLecturerDto dto)
    {
        var result = await _userService.AssignLecturerToGroupAsync(dto);
        if (!result) return BadRequest("Failed to assign lecturer. Make sure user is a Lecturer.");
        return Ok();
    }
}
