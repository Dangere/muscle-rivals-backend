using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MuscleRivalsBackend.Enums;

namespace MuscleRivalsBackend.Models.DTOs.Game.Modes;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "typeIndex")]
[JsonDerivedType(typeof(MaxRepsGameMode), typeDiscriminator: ((int)GameModeType.MaxReps))]
[JsonDerivedType(typeof(TimeLimitedGameMode), typeDiscriminator: ((int)GameModeType.TimeLimited))]
public abstract class BaseGameMode
{
    public int MaxLifeTimeMinutes { get; } = 1;
}

public class TimeLimitedGameMode : BaseGameMode
{
    public int TimeLimitSeconds { get; } = 60;

}

public class MaxRepsGameMode(ExerciseType exercise) : BaseGameMode
{
    public int MaxReps { get; } = exercise switch
    {
        ExerciseType.Pullups => 20,
        ExerciseType.Pushups => 20,
        ExerciseType.Squats => 40,
        _ => 30
    };
}