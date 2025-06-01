namespace Fig.Datalayer.BusinessEntities
{
    public class CustomActionExecutionBusinessEntity
    {
        public virtual Guid Id { get; set; }
        
        public virtual string ClientName { get; set; }
        
        public virtual string CustomActionName { get; set; }
        
        public virtual Guid? RunSessionId { get; set; }
        
        public virtual DateTime RequestedAt { get; set; }
        
        public virtual DateTime? ExecutedAt { get; set; }
        
        public virtual string? HandlingInstance { get; set; }

        public virtual string ResultsAsJson { get; set; }
        
        public virtual bool Succeeded { get; set; }
        
        public virtual Guid? ExecutedByRunSessionId { get; set; }
    }
}
