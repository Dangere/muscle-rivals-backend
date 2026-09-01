using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MuscleRivalsBackend.Enums;

namespace MuscleRivalsBackend.Models.Entities;


[Table("reports", Schema = "public"), Index(nameof(UserId))]
public class ReportEntity
{

    public int Id { get; set; }
    public required ReportType Type { get; set; }

    public required string Message { get; set; }
    public required string AppVersion { get; set; }
    public required string Platform { get; set; }
    public required string OsVersion { get; set; }
    public required string DeviceModel { get; set; }
    public required string Locale { get; set; }

    [Column("breadcrumbs_list_json")]
    public required Dictionary<string, object>[] Breadcrumbs { get; set; }

    [Column("user_session_json")]
    public required Dictionary<string, object> UserSession { get; set; }

    [Column("app_state_json")]
    public required Dictionary<string, object> AppState { get; set; }
    public required DateTime CreationDate { get; set; }

    public int? UserId { get; set; }
    public UserEntity? User { get; set; } = null;

}