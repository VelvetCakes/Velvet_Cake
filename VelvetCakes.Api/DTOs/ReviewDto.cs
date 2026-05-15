using System.ComponentModel.DataAnnotations;

namespace VelvetCakes.Api.DTOs;

public class CreateReviewDto
{
    [Required(ErrorMessage = "Имя обязательно")]
    public string AuthorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Текст отзыва обязателен")]
    public string Text { get; set; } = string.Empty;
}