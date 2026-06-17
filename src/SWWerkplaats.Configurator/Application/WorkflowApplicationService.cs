using SWWerkplaats.Configurator.Domain;

namespace SWWerkplaats.Configurator.Application
{
    public sealed class WorkflowData
    {
        public string[] Statuses { get; set; }
        public string[] Roles { get; set; }
        public WorkflowTransitionData[] Transitions { get; set; }
    }

    public sealed class WorkflowTransitionData
    {
        public string FromStatus { get; set; }
        public string ToStatus { get; set; }
        public string Role { get; set; }
    }

    public sealed class WorkflowApplicationService
    {
        public WorkflowData GetWorkflow()
        {
            var transitions = OrderWorkflowPolicy.AllTransitions();
            var data = new WorkflowTransitionData[transitions.Length];
            for (var i = 0; i < transitions.Length; i++)
            {
                data[i] = new WorkflowTransitionData
                {
                    FromStatus = transitions[i].FromStatus,
                    ToStatus = transitions[i].ToStatus,
                    Role = transitions[i].Role.ToString()
                };
            }

            return new WorkflowData
            {
                Statuses = OrderWorkflowStatus.All(),
                Roles = new[] { OrderWorkflowRole.Werkvoorbereider.ToString(), OrderWorkflowRole.Uitvoerder.ToString() },
                Transitions = data
            };
        }
    }
}
