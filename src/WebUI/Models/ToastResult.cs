namespace WebUI.Models;

public class ToastResult
{
    public JsonStatus Status { get; set; }
    public string? Msg { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Management { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string Callback { get; set; } = string.Empty;

    public static ToastResult SuccessResult(string msg, string management, string link = "", string callback = "") =>
        new() { Status = JsonStatus.Success, Msg = msg, Management = management, Color = "success", Link = link, Callback = callback };

    public static ToastResult ErrorResult(string msg, string management, string link = "") =>
        new() { Status = JsonStatus.Error, Msg = msg, Management = management, Color = "error", Link = link };

    public static ToastResult ExistResult(string msg, string management, string link = "", string callback = "") =>
        new() { Status = JsonStatus.Exist, Msg = msg, Management = management, Color = "error", Link = link, Callback = callback };
}

public enum JsonStatus
{
    Exist = -1,
    Error = 0,
    Success = 1,
    Warning = 2
}
