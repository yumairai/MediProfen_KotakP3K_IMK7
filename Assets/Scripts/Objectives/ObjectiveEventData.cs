namespace MediProfen.Objectives
{
    public readonly struct ObjectiveEventData
    {
        public string TargetId { get; }
        public ObjectiveCompletionType CompletionType { get; }

        public ObjectiveEventData(string targetId, ObjectiveCompletionType completionType)
        {
            TargetId = targetId;
            CompletionType = completionType;
        }
    }
}
