namespace QA.Pages.Message
{
    public class ProcedurePointUsedMessage : IProcedureMessage
    {
        public string procedureName { get; set; }
        public int PointIdx { get; set; }
        public bool IsUsed { get; set; }
    }
}
