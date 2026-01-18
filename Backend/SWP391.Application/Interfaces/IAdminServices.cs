using SWP391.Application.DTOs.Admin;

namespace SWP391.Application.Interfaces;

public interface IGroupService
{
    Task<IEnumerable<GroupDto>> GetAllGroupsAsync();
    Task<GroupDto?> GetGroupByIdAsync(int id);
    Task<GroupDto> CreateGroupAsync(CreateGroupDto dto);
    Task<GroupDto?> UpdateGroupAsync(int id, UpdateGroupDto dto);
    Task<bool> DeleteGroupAsync(int id);
    Task<IEnumerable<UserListDto>> GetGroupMembersAsync(int groupId);
    Task<bool> AddMemberToGroupAsync(int groupId, int userId);
    Task<bool> RemoveMemberFromGroupAsync(int groupId, int userId);
}

public interface IUserManagementService
{
    Task<IEnumerable<UserListDto>> GetAllUsersAsync();
    Task<IEnumerable<UserListDto>> GetUsersByRoleAsync(string role);
    Task<IEnumerable<UserListDto>> GetLecturersAsync();
    Task<UserListDto?> UpdateUserRoleAsync(UpdateUserRoleDto dto);
    Task<bool> AssignUserToGroupAsync(AssignUserToGroupDto dto);
    Task<bool> AssignLecturerToGroupAsync(AssignLecturerDto dto);
}
