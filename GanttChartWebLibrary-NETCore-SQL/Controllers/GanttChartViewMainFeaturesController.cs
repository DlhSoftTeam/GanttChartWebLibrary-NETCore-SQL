using Microsoft.AspNetCore.Mvc;
using GanttChartWebLibrary_NETCore_SQL.Models.DB;
using DlhSoft.Web.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GanttChartWebLibrary_NETCore_SQL.Controllers
{
    public class GanttChartViewMainFeaturesController : Controller
    {
        public IActionResult Index()
        {
            return View(model: LoadData());
        }
        public IActionResult UpdateGanttChartItem(GanttChartItem item)
        {
            SaveData(item);
            return Ok();
        }
        private List<GanttChartItem> LoadData()
        {
            // Create a new database context.
            using (var context = new DatabaseEntities())
            {
                // Prepare data items mapping them to task ID values, and pre-compute project start and finish date and times to be able to set the timeline of the view..
                var items = new List<GanttChartItem>();
                Dictionary<int, GanttChartItem> taskItemMap = new Dictionary<int, GanttChartItem>();
                foreach (Models.DB.Task task in context.Tasks.OrderBy(t => t.Index))
                {
                    var item = new GanttChartItem
                    {
                        Content = task.Name,
                        Indentation = task.Indentation,
                        ItemIndex = task.Index,
                        Start = task.Start,
                        Finish = task.Finish,
                        CompletedFinish = task.Completion,
                        IsMilestone = task.IsMilestone,
                        AssignmentsContent = task.Assignments,
                        Key = task.Id
                    };
                    items.Add(item);
                    taskItemMap.Add(task.Id, item);
                }
                // Prepare predecessor data items, using the pre-established map between task ID values and the view items created in the previous step.
                foreach (Predecessor predecessor in context.Predecessors)
                {
                    GanttChartItem dependentTaskItem = taskItemMap[predecessor.DependentTaskId];
                    GanttChartItem predecessorTaskItem = taskItemMap[predecessor.PredecessorTaskId];
                    var predecessorItem = new PredecessorItem
                    {
                        Item = predecessorTaskItem,
                        DependencyType = (DependencyType)predecessor.DependencyType,
                        Key = predecessor.PredecessorTaskId
                    };
                    dependentTaskItem.Predecessors.Add(predecessorItem);
                }
                // Set the items to the view.
                return items;
            }
        }
        private void SaveData(GanttChartItem item)
        {
            // Create a new database context.
            using (var context = new DatabaseEntities())
            {
                int taskID = item.Key;
                Models.DB.Task? task = context.Tasks.Where(t => t.Id == taskID).Include(t => t.Predecessors).SingleOrDefault();
                if (task == null)
                    return;
                // Update the appropriate task property values.
                task.Name = item.Content?.ToString() ?? string.Empty;
                task.Indentation = item.Indentation;
                task.Index = item.ItemIndex;
                task.Start = item.Start.ToLocalTime();
                task.Finish = item.Finish.ToLocalTime();
                task.Completion = item.CompletedFinish.ToLocalTime();
                task.IsMilestone = item.IsMilestone;
                task.Assignments = item.AssignmentsContent?.ToString() ?? string.Empty;
                // Remove predecessors that are no longer in the view.
                var predecessorsToDelete = new List<Predecessor>();
                foreach (Predecessor predecessor in task.Predecessors)
                {
                    if (!item.Predecessors.Any(p => p.Key == predecessor.PredecessorTaskId))
                        predecessorsToDelete.Add(predecessor);
                }
                foreach (Predecessor predecessor in predecessorsToDelete)
                    task.Predecessors.Remove(predecessor);
                // Add new and update existing predecessors based on the view.
                foreach (PredecessorItem predecessorItem in item.Predecessors)
                {
                    var predecessorTask = context.Tasks.SingleOrDefault(t => t.Index == predecessorItem.ItemIndex);
                    if (predecessorTask == null)
                        continue;
                    var predecessorTaskID = predecessorTask.Id;
                    var predecessor = task.Predecessors.Where(p => p.PredecessorTaskId == predecessorTaskID).SingleOrDefault();
                    if (predecessor == null) // create new predecessor if needed
                    {
                        predecessor = new Predecessor { PredecessorTaskId = (int)predecessorTaskID };
                        task.Predecessors.Add(predecessor);
                    }
                    predecessor.DependencyType = (int)predecessorItem.DependencyType;
                }
                // Actually save changes to the database.
                context.SaveChanges();
            }
        }

        [HttpPost]
        public IActionResult CreateNewGanttChartItem([FromBody] GanttChartItem item)
        {
            // Add a new task to the database.
            using (var context = new DatabaseEntities())
            {
                int index = item.ItemIndex < 0 ? (context.Tasks.Any() ? context.Tasks.Max(t => t.Index) : 0) + 1 : item.ItemIndex;
                if (item.ItemIndex >= 0)
                {
                    var tasks = context.Tasks.Where(t => t.Index >= item.ItemIndex).ToArray();
                    foreach (var t in tasks)
                        t.Index++;
                }
                var task = new Models.DB.Task
                {
                    Name = item.Content,
                    Indentation = item.Indentation,
                    Start = item.Start.ToLocalTime(),
                    Finish = item.Finish.ToLocalTime(),
                    Completion = item.CompletedFinish.ToLocalTime(),
                    IsMilestone = item.IsMilestone,
                    Assignments = string.Empty,
                    Index = index
                };
                context.Tasks.Add(task);
                context.SaveChanges();
                return Ok(task.Id);
            }
        }
        public IActionResult DeleteGanttChartItem(int id)
        {
            // Remove selected task from the database.
            using (var context = new DatabaseEntities())
            {
                var task = context.Tasks.Include(t => t.Predecessors).Include(t => t.Successors).SingleOrDefault(t => t.Id == id);
                if (task == null)
                    return NotFound();
                var tasks = context.Tasks.Where(t => t.Index > task.Index).ToArray();
                foreach (var t in tasks)
                    t.Index--;
                var predecessors = task.Predecessors;
                var successors = task.Successors;
                foreach (var predecessor in predecessors)
                    context.Predecessors.Remove(predecessor);
                foreach (var successor in successors)
                    context.Predecessors.Remove(successor);
                context.Tasks.Remove(task);
                context.SaveChanges();
            }
            return Ok();
        }
    }
}
