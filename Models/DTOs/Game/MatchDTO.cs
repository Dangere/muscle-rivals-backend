using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Models.DTOs.Game.Modes;
using MuscleRivalsBackend.Models.DTOs.Users;

namespace MuscleRivalsBackend.Models.DTOs.Game;

public record MatchDTO
(
    int RoomId,
    List<UserDTO> Players,
    DateTime CreationDate,
    ExerciseType ExerciseType,
    BaseGameMode GameMode

);