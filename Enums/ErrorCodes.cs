namespace MuscleRivalsBackend.Enums;


public enum ErrorCodes
{
    // Access / permission errors
    ACCESS_DENIED,

    // User errors
    USER_NOT_FOUND,
    USER_ALREADY_VERIFIED,
    USER_NOT_ASSIGNED_TO_TASK,
    INVALID_URL,

    // Auth errors
    INVALID_CREDENTIALS,
    EMAIL_ALREADY_IN_USE,
    USERNAME_ALREADY_IN_USE,
    CREDENTIALS_ALREADY_IN_USE,
    INVALID_TOKEN,        // covers invalid/expired refresh, verification, and password reset tokens
    INVALID_GOOGLE_TOKEN, // Google JWT validation failure

    // Member management errors
    USER_ALREADY_GRANTED,
    USER_ALREADY_REVOKED,
    NO_USERNAMES_PROVIDED,

    // Server errors
    INTERNAL_ERROR,
    EMAIL_SEND_FAILED,

    // Game errors
    USER_NOT_IN_HUB,
    USER_ALREADY_IN_ROOM,
}
