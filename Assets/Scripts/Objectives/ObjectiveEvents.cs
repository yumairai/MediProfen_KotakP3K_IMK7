using System;

namespace MediProfen.Objectives
{
    public static class ObjectiveEvents
    {
        public static event Action<ObjectiveEventData> TargetCompleted;

        public static void RaiseTargetCompleted(string targetId, ObjectiveCompletionType completionType)
        {
            TargetCompleted?.Invoke(new ObjectiveEventData(targetId, completionType));
        }
    }
}
