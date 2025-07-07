[Serializable]
public class ExceptionInfoDto
{
    public string Message { get; set; }
    public string Type { get; set; }
    public string StackTrace { get; set; }
    public ExceptionInfoDto InnerException { get; set; }

    public ExceptionInfoDto() { }

    public ExceptionInfoDto(Exception ex)
    {
        if (ex == null) return;
        Message = ex.Message;
        Type = ex.GetType().FullName;
        StackTrace = ex.StackTrace;
        if (ex.InnerException != null)
            InnerException = new ExceptionInfoDto(ex.InnerException);
    }
}