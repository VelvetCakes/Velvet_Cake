namespace VelvetCakes.Api.DTOs;

public class SendNotificationDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class SendByEmailDto
{
    public string Email { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}