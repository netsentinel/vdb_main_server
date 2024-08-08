using System.ComponentModel.DataAnnotations;

namespace ServicesLayer.Models.Services;

public class SendMailRequest
{
	[Required]
	public string From { get; set; } = null!;

	[Required]
	public string FromName { get; set; } = null!;

	[Required]
	public string To { get; set; } = null!;

	[Required]
	public string Subject { get; set; } = null!;

	[Required]
	public string Body { get; set; } = null!;
}
