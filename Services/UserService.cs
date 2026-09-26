
using Microsoft.EntityFrameworkCore;
using MuscleRivalsBackend.Data;
using MuscleRivalsBackend.Mappers;
using MuscleRivalsBackend.Models.DTOs.Users;
using MuscleRivalsBackend.Models.Entities;

namespace MuscleRivalsBackend.Services;

public class UserService(MuscleRivalsDBContext dbContext, UserMapper userMapper)
{

    private readonly MuscleRivalsDBContext _dbContext = dbContext;
    private readonly UserMapper _userMapper = userMapper;

    public async Task<List<UserEntity>> GetUsers(List<int> userIds)
    {
        return await _dbContext.Users.Where(x => userIds.Contains(x.Id)).ToListAsync();
    }


    public async Task<List<UserDTO>> GetUsersDTOs(List<int> userIds)
    {
        List<UserEntity> users = await GetUsers(userIds);

        return [.. users.Select(_userMapper.UserToUserDTO)];
    }


}