using System;
using System.Collections.Generic;

namespace GanttChartWebLibrary_NETCore_SQL.Models.DB
{
    public partial class Task
    {
        public Task()
        {
            Predecessors = new HashSet<Predecessor>();
            Successors = new HashSet<Predecessor>();
        }

        public int Id { get; set; }
        public int Index { get; set; }
        public string Name { get; set; } = null!;
        public int Indentation { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public DateTime Completion { get; set; }
        public bool IsMilestone { get; set; }
        public string Assignments { get; set; } = null!;

        public virtual ICollection<Predecessor> Predecessors { get; set; }
        public virtual ICollection<Predecessor> Successors { get; set; }
    }
}
