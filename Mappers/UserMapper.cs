using MuscleRivalsBackend.Models.DTOs.Users;
using MuscleRivalsBackend.Models.Entities;
using Riok.Mapperly.Abstractions;

namespace MuscleRivalsBackend.Mappers;

[Mapper]
public partial class UserMapper
{
    public partial UserDTO UserToUserDTO(UserEntity user);
}